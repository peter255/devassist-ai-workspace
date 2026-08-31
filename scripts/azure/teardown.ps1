<#
.SYNOPSIS
  Deletes the DevAssist Azure resource group (all resources inside).
#>
param(
    [string] $ResourceGroup = "rg-devassist-ai",
    [switch] $Yes
)

$ErrorActionPreference = "Stop"

if (-not $Yes) {
    Write-Host "This will DELETE resource group '$ResourceGroup' and ALL resources inside it." -ForegroundColor Yellow
    $confirm = Read-Host "Type the resource group name to confirm"
    if ($confirm -ne $ResourceGroup) {
        Write-Host "Aborted."
        exit 1
    }
}

Write-Host "Deleting resource group $ResourceGroup (this may take several minutes)..."
az group delete --name $ResourceGroup --yes --no-wait
Write-Host "Delete initiated. Check status: az group show --name $ResourceGroup" -ForegroundColor Green
