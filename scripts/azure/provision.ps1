<#
.SYNOPSIS
  Provisions Azure resources for DevAssist and writes .env to the repo root.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File ./scripts/azure/provision.ps1
  powershell -ExecutionPolicy Bypass -File ./scripts/azure/provision.ps1 -SearchSku standard
  powershell -ExecutionPolicy Bypass -File ./scripts/azure/provision.ps1 -SkipOpenAi -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $ResourceGroup = "rg-devassist-ai",
    [string] $Location = "swedencentral",
    [ValidateSet("basic", "standard")]
    [string] $SearchSku = "basic",
    [string] $Suffix = "",
    [switch] $SkipOpenAi,
    [switch] $SkipOptional,
    [switch] $ForceEnv,
    [string] $ChatDeploymentName = "gpt-4o",
    [string] $ChatModelName = "gpt-4o",
    [string] $ChatModelVersion = "2024-08-06",
    [string] $EmbeddingDeploymentName = "text-embedding-ada-002",
    [string] $EmbeddingModelName = "text-embedding-ada-002",
    [string] $EmbeddingModelVersion = "2"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$envPath = Join-Path $repoRoot ".env"

function Write-Step([string] $Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Az([string[]] $Args) {
    if ($WhatIfPreference) {
        Write-Host "[WhatIf] az $($Args -join ' ')" -ForegroundColor DarkGray
        return $null
    }
    $output = & az @Args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az failed: az $($Args -join ' ') -> $output"
    }
    return $output
}

function Get-RandomSuffix([int] $Length = 5) {
    -join ((48..57) + (97..122) | Get-Random -Count $Length | ForEach-Object { [char]$_ })
}

function Test-AzResourceGroup([string] $Name) {
    if ($WhatIfPreference) { return $false }
    $exists = Invoke-Az resource group exists --name $Name | ConvertFrom-Json
    return [bool]$exists
}

function Write-Utf8NoBomFile([string] $Path, [string] $Content) {
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) not found. Install: https://learn.microsoft.com/cli/azure/install-azure-cli"
}

$account = Invoke-Az account show
if ($WhatIfPreference) {
    Write-Host "Subscription: (WhatIf - skipped az account show)"
} else {
    $account = $account | ConvertFrom-Json
    if (-not $account) { throw "Not logged in. Run: az login" }
    Write-Host "Subscription: $($account.name) ($($account.id))"
}

if ([string]::IsNullOrWhiteSpace($Suffix)) {
    $Suffix = Get-RandomSuffix
}
$Suffix = $Suffix.ToLowerInvariant()

# Global naming constraints (lowercase, no hyphens where noted)
$openAiName    = "devassist-openai-$Suffix"
$searchName    = "devassist-search-$Suffix"
$storageName   = "devassistst$Suffix"          # max 24, alphanumeric
$docIntName    = "devassist-docint-$Suffix"
$serviceBusName = "devassist-sb-$Suffix"       # max 50
$keyVaultName  = "devassist-kv-$Suffix"        # max 24
$appInsightsName = "devassist-ai-$Suffix"

Write-Step "Plan"
Write-Host @"
  Resource group : $ResourceGroup
  Location       : $Location
  Suffix         : $Suffix
  Search SKU     : $SearchSku
  Skip OpenAI    : $SkipOpenAi
  Skip optional  : $SkipOptional
  Output .env    : $envPath
"@

if (-not $WhatIfPreference) {
    if ((Test-Path $envPath) -and -not $ForceEnv) {
        throw ".env already exists at $envPath. Use -ForceEnv to overwrite or delete it first."
    }
}

Write-Step "Resource group"
if (-not (Test-AzResourceGroup $ResourceGroup)) {
    if ($PSCmdlet.ShouldProcess($ResourceGroup, "Create resource group")) {
        Invoke-Az group create --name $ResourceGroup --location $Location | Out-Null
    }
} else {
    Write-Host "Resource group already exists."
}

$secrets = [ordered]@{
    OpenAiEndpoint = ""
    OpenAiKey = ""
    SearchEndpoint = ""
    SearchAdminKey = ""
    StorageConnectionString = ""
    DocIntEndpoint = ""
    DocIntKey = ""
    ServiceBusConnectionString = ""
    KeyVaultUri = ""
    AppInsightsConnectionString = ""
}

Write-Step "Storage account + container"
if ($PSCmdlet.ShouldProcess($storageName, "Create storage account")) {
    Invoke-Az storage account create `
        --name $storageName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku Standard_LRS `
        --kind StorageV2 `
        --min-tls-version TLS1_2 | Out-Null

    Invoke-Az storage container create `
        --name documents `
        --account-name $storageName `
        --auth-mode login | Out-Null

    $secrets.StorageConnectionString = (Invoke-Az storage account show-connection-string `
        --name $storageName `
        --resource-group $ResourceGroup `
        --query connectionString -o tsv)
}

Write-Step "Azure AI Search ($SearchSku)"
if ($PSCmdlet.ShouldProcess($searchName, "Create search service")) {
    Invoke-Az search service create `
        --name $searchName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku $SearchSku | Out-Null

    $secrets.SearchEndpoint = "https://$searchName.search.windows.net"
    $secrets.SearchAdminKey = Invoke-Az search admin-key show `
        --service-name $searchName `
        --resource-group $ResourceGroup `
        --query primaryKey -o tsv
}

Write-Step "Service Bus namespace + queue"
if ($PSCmdlet.ShouldProcess($serviceBusName, "Create Service Bus")) {
    Invoke-Az servicebus namespace create `
        --name $serviceBusName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku Standard | Out-Null

    Invoke-Az servicebus queue create `
        --name devassist-indexing `
        --namespace-name $serviceBusName `
        --resource-group $ResourceGroup `
        --max-delivery-count 10 | Out-Null

    $secrets.ServiceBusConnectionString = Invoke-Az servicebus namespace authorization-rule keys list `
        --resource-group $ResourceGroup `
        --namespace-name $serviceBusName `
        --name RootManageSharedAccessKey `
        --query primaryConnectionString -o tsv
}

if (-not $SkipOpenAi) {
    Write-Step "Azure OpenAI + model deployments"
    if ($PSCmdlet.ShouldProcess($openAiName, "Create Azure OpenAI")) {
        try {
            Invoke-Az cognitiveservices account create `
                --name $openAiName `
                --resource-group $ResourceGroup `
                --kind OpenAI `
                --sku S0 `
                --location $Location `
                --yes | Out-Null

            $secrets.OpenAiEndpoint = (Invoke-Az cognitiveservices account show `
                --name $openAiName `
                --resource-group $ResourceGroup `
                --query properties.endpoint -o tsv).TrimEnd("/") + "/"

            $keys = Invoke-Az cognitiveservices account keys list `
                --name $openAiName `
                --resource-group $ResourceGroup | ConvertFrom-Json
            $secrets.OpenAiKey = $keys.key1

            Write-Host "Deploying chat model: $ChatDeploymentName"
            Invoke-Az cognitiveservices account deployment create `
                --name $openAiName `
                --resource-group $ResourceGroup `
                --deployment-name $ChatDeploymentName `
                --model-name $ChatModelName `
                --model-version $ChatModelVersion `
                --model-format OpenAI `
                --sku-capacity 10 `
                --sku-name Standard | Out-Null

            Write-Host "Deploying embedding model: $EmbeddingDeploymentName"
            Invoke-Az cognitiveservices account deployment create `
                --name $openAiName `
                --resource-group $ResourceGroup `
                --deployment-name $EmbeddingDeploymentName `
                --model-name $EmbeddingModelName `
                --model-version $EmbeddingModelVersion `
                --model-format OpenAI `
                --sku-capacity 10 `
                --sku-name Standard | Out-Null
        } catch {
            Write-Warning "Azure OpenAI provisioning failed: $_"
            Write-Warning "Re-run with -SkipOpenAi or deploy models manually in the portal."
            $secrets.OpenAiEndpoint = ""
            $secrets.OpenAiKey = ""
        }
    }
} else {
    Write-Host "Skipping Azure OpenAI (-SkipOpenAi)."
}

if (-not $SkipOptional) {
    Write-Step "Document Intelligence (OCR)"
    if ($PSCmdlet.ShouldProcess($docIntName, "Create Document Intelligence")) {
        try {
            Invoke-Az cognitiveservices account create `
                --name $docIntName `
                --resource-group $ResourceGroup `
                --kind FormRecognizer `
                --sku S0 `
                --location $Location `
                --yes | Out-Null

            $secrets.DocIntEndpoint = (Invoke-Az cognitiveservices account show `
                --name $docIntName `
                --resource-group $ResourceGroup `
                --query properties.endpoint -o tsv).TrimEnd("/") + "/"

            $docKeys = Invoke-Az cognitiveservices account keys list `
                --name $docIntName `
                --resource-group $ResourceGroup | ConvertFrom-Json
            $secrets.DocIntKey = $docKeys.key1
        } catch {
            Write-Warning "Document Intelligence provisioning failed: $_"
        }
    }

    Write-Step "Key Vault"
    if ($PSCmdlet.ShouldProcess($keyVaultName, "Create Key Vault")) {
        try {
            Invoke-Az keyvault create `
                --name $keyVaultName `
                --resource-group $ResourceGroup `
                --location $Location `
                --enable-rbac-authorization true | Out-Null
            $secrets.KeyVaultUri = "https://$keyVaultName.vault.azure.net/"
        } catch {
            Write-Warning "Key Vault provisioning failed: $_"
        }
    }

    Write-Step "Application Insights"
    if ($PSCmdlet.ShouldProcess($appInsightsName, "Create Application Insights")) {
        try {
            Invoke-Az monitor app-insights component create `
                --app $appInsightsName `
                --location $Location `
                --resource-group $ResourceGroup `
                --application-type web | Out-Null

            $secrets.AppInsightsConnectionString = Invoke-Az monitor app-insights component show `
                --app $appInsightsName `
                --resource-group $ResourceGroup `
                --query connectionString -o tsv
        } catch {
            Write-Warning "Application Insights provisioning failed: $_"
        }
    }
} else {
    Write-Host "Skipping optional services (-SkipOptional)."
}

$semanticConfig = if ($SearchSku -eq "standard") { "devassist-semantic" } else { "" }

$envContent = @"
# DevAssist AI Workspace — generated by scripts/azure/provision.ps1
# Resource group: $ResourceGroup | Location: $Location | Suffix: $Suffix
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

ASPNETCORE_ENVIRONMENT=Development

# SQL Server (local Docker — see docker-compose.yml port 14333)
ConnectionStrings__DevAssistDb=Server=localhost,14333;Database=DevAssistDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True

# Azure OpenAI ($openAiName)
AzureOpenAi__Endpoint=$($secrets.OpenAiEndpoint)
AzureOpenAi__ApiKey=$($secrets.OpenAiKey)
AzureOpenAi__DeploymentName=$ChatDeploymentName
AzureOpenAi__EmbeddingDeploymentName=$EmbeddingDeploymentName

# Azure AI Search ($searchName)
AzureSearch__Endpoint=$($secrets.SearchEndpoint)
AzureSearch__ApiKey=$($secrets.SearchAdminKey)
AzureSearch__IndexName=devassist-documents
AzureSearch__SemanticConfigurationName=$semanticConfig
AzureSearch__VectorDimensions=1536

# Azure Blob Storage ($storageName)
BlobStorage__ConnectionString=$($secrets.StorageConnectionString)
BlobStorage__ContainerName=documents

LocalFileStorage__RootPath=./data/documents

# Azure Document Intelligence ($docIntName)
DocumentIntelligence__Endpoint=$($secrets.DocIntEndpoint)
DocumentIntelligence__ApiKey=$($secrets.DocIntKey)

# Azure Service Bus ($serviceBusName)
ServiceBus__ConnectionString=$($secrets.ServiceBusConnectionString)
ServiceBus__QueueName=devassist-indexing

# Azure Key Vault ($keyVaultName)
KeyVault__Uri=$($secrets.KeyVaultUri)

# Application Insights ($appInsightsName)
ApplicationInsights__ConnectionString=$($secrets.AppInsightsConnectionString)

# JWT (local auth)
Jwt__Secret=devassist-local-jwt-secret-change-in-production-min-32-chars!!
Jwt__Issuer=devassist
Jwt__Audience=devassist
Jwt__DefaultAdminPassword=Admin@123!

AzureAd__TenantId=
AzureAd__ClientId=
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__Audience=

RateLimiting__GeneralPermits=100
RateLimiting__AiPermits=20
RateLimiting__WindowSeconds=60

Cors__AllowedOrigins__0=http://localhost:5173
Cors__AllowedOrigins__1=https://localhost:5173

VITE_API_BASE_URL=
VITE_AAD_CLIENT_ID=
VITE_AAD_TENANT_ID=
VITE_AAD_SCOPE=
"@

Write-Step "Write .env"
if ($WhatIfPreference) {
    Write-Host "[WhatIf] Would write $envPath"
} else {
    Write-Utf8NoBomFile -Path $envPath -Content $envContent
    Write-Host "Wrote $envPath" -ForegroundColor Green
}

Write-Step "Done"
Write-Host @"

Next steps:
  1. docker compose up -d sqlserver
  2. dotnet run --project src/DevAssist.Api --launch-profile http
  3. Open http://localhost:5147 - admin / Admin@123!

To tear down: powershell -ExecutionPolicy Bypass -File ./scripts/azure/teardown.ps1 -ResourceGroup $ResourceGroup
"@
