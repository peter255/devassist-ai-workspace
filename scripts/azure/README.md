# Azure provisioning scripts

Automated setup for DevAssist Azure dependencies. After a successful run, a `.env` file is written to the repo root with connection strings and keys — ready for `dotnet run`.

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | `az version` ≥ 2.50 |
| Active subscription | `az login` |
| **Azure OpenAI access** | Subscription must be approved for OpenAI (portal request). Use `-SkipOpenAi` if not yet approved. |
| PowerShell 5.1+ (Windows) or Bash | Windows: built-in `powershell`; optional [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) (`pwsh`) |

Register resource providers (first time only):

```bash
az provider register --namespace Microsoft.CognitiveServices --wait
az provider register --namespace Microsoft.Search --wait
az provider register --namespace Microsoft.Storage --wait
az provider register --namespace Microsoft.ServiceBus --wait
az provider register --namespace Microsoft.KeyVault --wait
az provider register --namespace Microsoft.Insights --wait
```

## Quick start

From the repository root:

**Windows (PowerShell — works with built-in Windows PowerShell 5.1):**

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\azure\provision.ps1
```

Optional if you installed PowerShell 7: `pwsh ./scripts/azure/provision.ps1`

**Linux / macOS / Git Bash:**

```bash
chmod +x scripts/azure/provision.sh
./scripts/azure/provision.sh
```

This creates resource group **`rg-devassist-ai`** in **`swedencentral`** with a unique name suffix, provisions all services, and writes **`.env`**.

### Common options

| Flag | PowerShell | Bash | Description |
|------|------------|------|-------------|
| Resource group | `-ResourceGroup "my-rg"` | `--resource-group my-rg` | Default: `rg-devassist-ai` |
| Region | `-Location "eastus"` | `--location eastus` | Default: `swedencentral` |
| Search tier | `-SearchSku "standard"` | `--search-sku standard` | `basic` (default) or `standard` (semantic ranker) |
| Skip OpenAI | `-SkipOpenAi` | `--skip-openai` | Storage, Search, Service Bus, etc. only |
| Skip optional | `-SkipOptional` | `--skip-optional` | Skip Key Vault, App Insights, Document Intelligence |
| Dry run | `-WhatIf` | `--what-if` | Print planned actions only |
| Overwrite `.env` | `-ForceEnv` | `--force-env` | Replace existing `.env` without prompt |

**Example — full stack with semantic search:**

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\azure\provision.ps1 -SearchSku standard
```

**Example — no OpenAI (local AI fallbacks, Azure storage + search only):**

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\azure\provision.ps1 -SkipOpenAi
```

## What gets created

| Azure resource | DevAssist usage | Script name pattern |
|----------------|-----------------|---------------------|
| Resource group | Container for all resources | `rg-devassist-ai` |
| Azure OpenAI | Copilot, embeddings, analyzers | `devassist-openai-{suffix}` |
| Azure AI Search | Hybrid document retrieval | `devassist-search-{suffix}` |
| Storage account + `documents` container | Uploaded files | `devassistst{suffix}` |
| Document Intelligence | OCR for scanned PDFs | `devassist-docint-{suffix}` |
| Service Bus + `devassist-indexing` queue | Background indexing | `devassist-sb-{suffix}` |
| Key Vault | Optional secrets store | `devassist-kv-{suffix}` |
| Application Insights | Telemetry | `devassist-ai-{suffix}` |

The **search index** (`devassist-documents`) is created automatically by the app on first document upload — no manual index step required.

> **SQL Server** stays local via Docker (`docker compose up -d sqlserver`). Azure SQL is not provisioned by these scripts (Wave 1 roadmap item).

## After provisioning

```bash
# 1. Start local SQL
docker compose up -d sqlserver

# 2. Run the app (.env is loaded automatically)
dotnet run --project src/DevAssist.Api --launch-profile http
```

Open http://localhost:5147 and sign in with `admin` / `Admin@123!`.

## Regenerate `.env` from existing resources

If resources already exist in a resource group:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\azure\write-env.ps1 -ResourceGroup rg-devassist-ai
```

```bash
./scripts/azure/write-env.sh --resource-group rg-devassist-ai
```

## Tear down

Deletes the entire resource group and all contained resources:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\azure\teardown.ps1 -ResourceGroup rg-devassist-ai
```

```bash
./scripts/azure/teardown.sh --resource-group rg-devassist-ai
```

You will be prompted to type the resource group name to confirm.

## Troubleshooting

| Issue | Action |
|-------|--------|
| OpenAI region not available | Try `-Location eastus` or `-Location westeurope` |
| OpenAI access denied | Request access in Azure portal; use `-SkipOpenAi` meanwhile |
| Name already taken | Script generates a random suffix; re-run or pass `-Suffix abc12` |
| Deployment model version failed | Edit model version vars at top of script or deploy models manually in portal |
| `.env` already exists | Use `-ForceEnv` or delete/rename manually |

See also [docs/azure-setup.md](../../docs/azure-setup.md) for manual portal steps and fallback behavior.
