#!/usr/bin/env bash
# Provisions Azure resources for DevAssist and writes .env to the repo root.
set -euo pipefail

RESOURCE_GROUP="rg-devassist-ai"
LOCATION="swedencentral"
SEARCH_SKU="basic"
SUFFIX=""
SKIP_OPENAI=false
SKIP_OPTIONAL=false
FORCE_ENV=false
WHAT_IF=false
CHAT_DEPLOYMENT="gpt-4o"
CHAT_MODEL="gpt-4o"
CHAT_MODEL_VERSION="2024-08-06"
EMBED_DEPLOYMENT="text-embedding-ada-002"
EMBED_MODEL="text-embedding-ada-002"
EMBED_MODEL_VERSION="2"

usage() {
  cat <<'EOF'
Usage: ./scripts/azure/provision.sh [options]

Options:
  --resource-group NAME   Resource group (default: rg-devassist-ai)
  --location REGION       Azure region (default: swedencentral)
  --search-sku SKU        basic or standard (default: basic)
  --suffix SUFFIX         Name suffix (default: random 5 chars)
  --skip-openai           Skip Azure OpenAI
  --skip-optional         Skip Key Vault, App Insights, Document Intelligence
  --force-env             Overwrite existing .env
  --what-if               Print planned actions only
  -h, --help              Show help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group) RESOURCE_GROUP="$2"; shift 2 ;;
    --location) LOCATION="$2"; shift 2 ;;
    --search-sku) SEARCH_SKU="$2"; shift 2 ;;
    --suffix) SUFFIX="$2"; shift 2 ;;
    --skip-openai) SKIP_OPENAI=true; shift ;;
    --skip-optional) SKIP_OPTIONAL=true; shift ;;
    --force-env) FORCE_ENV=true; shift ;;
    --what-if) WHAT_IF=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1"; usage; exit 1 ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ENV_PATH="$REPO_ROOT/.env"

step() { echo -e "\n==> $1"; }

az_cmd() {
  if [[ "$WHAT_IF" == true ]]; then
    echo "[WhatIf] az $*"
    return 0
  fi
  az "$@"
}

random_suffix() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -hex 3
  else
    tr -dc 'a-z0-9' </dev/urandom | head -c 5
  fi
}

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI (az) not found." >&2
  exit 1
fi

ACCOUNT="$(az account show -o json 2>/dev/null || true)"
if [[ -z "$ACCOUNT" ]]; then
  echo "Not logged in. Run: az login" >&2
  exit 1
fi

SUB_NAME="$(echo "$ACCOUNT" | jq -r .name)"
SUB_ID="$(echo "$ACCOUNT" | jq -r .id)"
echo "Subscription: $SUB_NAME ($SUB_ID)"

if [[ -z "$SUFFIX" ]]; then
  SUFFIX="$(random_suffix | tr '[:upper:]' '[:lower:]')"
fi
SUFFIX="${SUFFIX,,}"

OPENAI_NAME="devassist-openai-$SUFFIX"
SEARCH_NAME="devassist-search-$SUFFIX"
STORAGE_NAME="devassistst$SUFFIX"
DOCINT_NAME="devassist-docint-$SUFFIX"
SB_NAME="devassist-sb-$SUFFIX"
KV_NAME="devassist-kv-$SUFFIX"
AI_NAME="devassist-ai-$SUFFIX"

step "Plan"
cat <<EOF
  Resource group : $RESOURCE_GROUP
  Location       : $LOCATION
  Suffix         : $SUFFIX
  Search SKU     : $SEARCH_SKU
  Skip OpenAI    : $SKIP_OPENAI
  Skip optional  : $SKIP_OPTIONAL
  Output .env    : $ENV_PATH
EOF

if [[ "$WHAT_IF" != true ]]; then
  if [[ -f "$ENV_PATH" && "$FORCE_ENV" != true ]]; then
    echo ".env already exists at $ENV_PATH. Use --force-env to overwrite." >&2
    exit 1
  fi
fi

OPENAI_ENDPOINT=""
OPENAI_KEY=""
SEARCH_ENDPOINT=""
SEARCH_KEY=""
STORAGE_CS=""
DOCINT_ENDPOINT=""
DOCINT_KEY=""
SB_CS=""
KV_URI=""
APPINSIGHTS_CS=""

step "Resource group"
if [[ "$(az_cmd group exists --name "$RESOURCE_GROUP")" != "true" ]]; then
  az_cmd group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null
else
  echo "Resource group already exists."
fi

step "Storage account + container"
az_cmd storage account create \
  --name "$STORAGE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 >/dev/null

az_cmd storage container create \
  --name documents \
  --account-name "$STORAGE_NAME" \
  --auth-mode login >/dev/null

if [[ "$WHAT_IF" != true ]]; then
  STORAGE_CS="$(az storage account show-connection-string \
    --name "$STORAGE_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query connectionString -o tsv)"
fi

step "Azure AI Search ($SEARCH_SKU)"
az_cmd search service create \
  --name "$SEARCH_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "$SEARCH_SKU" >/dev/null

SEARCH_ENDPOINT="https://${SEARCH_NAME}.search.windows.net"
if [[ "$WHAT_IF" != true ]]; then
  SEARCH_KEY="$(az search admin-key show \
    --service-name "$SEARCH_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query primaryKey -o tsv)"
fi

step "Service Bus namespace + queue"
az_cmd servicebus namespace create \
  --name "$SB_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard >/dev/null

az_cmd servicebus queue create \
  --name devassist-indexing \
  --namespace-name "$SB_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --max-delivery-count 10 >/dev/null

if [[ "$WHAT_IF" != true ]]; then
  SB_CS="$(az servicebus namespace authorization-rule keys list \
    --resource-group "$RESOURCE_GROUP" \
    --namespace-name "$SB_NAME" \
    --name RootManageSharedAccessKey \
    --query primaryConnectionString -o tsv)"
fi

if [[ "$SKIP_OPENAI" != true ]]; then
  step "Azure OpenAI + model deployments"
  if az_cmd cognitiveservices account create \
    --name "$OPENAI_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --kind OpenAI \
    --sku S0 \
    --location "$LOCATION" \
    --yes >/dev/null 2>&1; then

    if [[ "$WHAT_IF" != true ]]; then
      OPENAI_ENDPOINT="$(az cognitiveservices account show \
        --name "$OPENAI_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.endpoint -o tsv)"
      OPENAI_ENDPOINT="${OPENAI_ENDPOINT%/}/"
      OPENAI_KEY="$(az cognitiveservices account keys list \
        --name "$OPENAI_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query key1 -o tsv)"

      echo "Deploying chat model: $CHAT_DEPLOYMENT"
      az cognitiveservices account deployment create \
        --name "$OPENAI_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --deployment-name "$CHAT_DEPLOYMENT" \
        --model-name "$CHAT_MODEL" \
        --model-version "$CHAT_MODEL_VERSION" \
        --model-format OpenAI \
        --sku-capacity 10 \
        --sku-name Standard >/dev/null || echo "Warning: chat deployment failed — deploy manually in portal."

      echo "Deploying embedding model: $EMBED_DEPLOYMENT"
      az cognitiveservices account deployment create \
        --name "$OPENAI_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --deployment-name "$EMBED_DEPLOYMENT" \
        --model-name "$EMBED_MODEL" \
        --model-version "$EMBED_MODEL_VERSION" \
        --model-format OpenAI \
        --sku-capacity 10 \
        --sku-name Standard >/dev/null || echo "Warning: embedding deployment failed — deploy manually in portal."
    fi
  else
    echo "Warning: Azure OpenAI provisioning failed. Use --skip-openai or request access in portal."
  fi
else
  echo "Skipping Azure OpenAI (--skip-openai)."
fi

if [[ "$SKIP_OPTIONAL" != true ]]; then
  step "Document Intelligence (OCR)"
  if az_cmd cognitiveservices account create \
    --name "$DOCINT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --kind FormRecognizer \
    --sku S0 \
    --location "$LOCATION" \
    --yes >/dev/null 2>&1; then
    if [[ "$WHAT_IF" != true ]]; then
      DOCINT_ENDPOINT="$(az cognitiveservices account show \
        --name "$DOCINT_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.endpoint -o tsv)"
      DOCINT_ENDPOINT="${DOCINT_ENDPOINT%/}/"
      DOCINT_KEY="$(az cognitiveservices account keys list \
        --name "$DOCINT_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query key1 -o tsv)"
    fi
  else
    echo "Warning: Document Intelligence provisioning failed."
  fi

  step "Key Vault"
  if az_cmd keyvault create \
    --name "$KV_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --enable-rbac-authorization true >/dev/null 2>&1; then
    KV_URI="https://${KV_NAME}.vault.azure.net/"
  else
    echo "Warning: Key Vault provisioning failed."
  fi

  step "Application Insights"
  if az_cmd monitor app-insights component create \
    --app "$AI_NAME" \
    --location "$LOCATION" \
    --resource-group "$RESOURCE_GROUP" \
    --application-type web >/dev/null 2>&1; then
    if [[ "$WHAT_IF" != true ]]; then
      APPINSIGHTS_CS="$(az monitor app-insights component show \
        --app "$AI_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query connectionString -o tsv)"
    fi
  else
    echo "Warning: Application Insights provisioning failed."
  fi
else
  echo "Skipping optional services (--skip-optional)."
fi

SEMANTIC=""
if [[ "$SEARCH_SKU" == "standard" ]]; then
  SEMANTIC="devassist-semantic"
fi

step "Write .env"
if [[ "$WHAT_IF" == true ]]; then
  echo "[WhatIf] Would write $ENV_PATH"
else
  cat > "$ENV_PATH" <<EOF
# DevAssist AI Workspace — generated by scripts/azure/provision.sh
# Resource group: $RESOURCE_GROUP | Location: $LOCATION | Suffix: $SUFFIX
# Generated: $(date '+%Y-%m-%d %H:%M:%S')

ASPNETCORE_ENVIRONMENT=Development

ConnectionStrings__DevAssistDb=Server=localhost,14333;Database=DevAssistDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True

AzureOpenAi__Endpoint=$OPENAI_ENDPOINT
AzureOpenAi__ApiKey=$OPENAI_KEY
AzureOpenAi__DeploymentName=$CHAT_DEPLOYMENT
AzureOpenAi__EmbeddingDeploymentName=$EMBED_DEPLOYMENT

AzureSearch__Endpoint=$SEARCH_ENDPOINT
AzureSearch__ApiKey=$SEARCH_KEY
AzureSearch__IndexName=devassist-documents
AzureSearch__SemanticConfigurationName=$SEMANTIC
AzureSearch__VectorDimensions=1536

BlobStorage__ConnectionString=$STORAGE_CS
BlobStorage__ContainerName=documents

LocalFileStorage__RootPath=./data/documents

DocumentIntelligence__Endpoint=$DOCINT_ENDPOINT
DocumentIntelligence__ApiKey=$DOCINT_KEY

ServiceBus__ConnectionString=$SB_CS
ServiceBus__QueueName=devassist-indexing

KeyVault__Uri=$KV_URI

ApplicationInsights__ConnectionString=$APPINSIGHTS_CS

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
EOF
  echo "Wrote $ENV_PATH"
fi

step "Done"
cat <<EOF

Next steps:
  1. docker compose up -d sqlserver
  2. dotnet run --project src/DevAssist.Api --launch-profile http
  3. Open http://localhost:5147 — admin / Admin@123!

To tear down: ./scripts/azure/teardown.sh --resource-group $RESOURCE_GROUP
EOF
