# Azure Setup Guide — DevAssist AI Workspace (Phase 2)

This guide covers provisioning and configuring the Azure resources required for Phase 2 (Azure AI Foundry Integration). All Azure services are **optional** — the application falls back gracefully to local heuristics and SQL keyword search when credentials are absent.

---

## Required Azure Resources

| Resource | Purpose | Tier |
|----------|---------|------|
| Azure OpenAI / AI Foundry | Chat completions + embeddings | Standard S0 |
| Azure AI Search | Vector + hybrid + semantic search | Basic or Standard |
| Azure Blob Storage | Document file storage | LRS Standard |
| Azure SQL Server | Metadata + chat history (already in MVP) | — |

---

## 1. Azure OpenAI / Azure AI Foundry

### Option A — Standard Azure OpenAI

1. Create an **Azure OpenAI** resource in the Azure portal.
2. Deploy a **chat model** (recommended: `gpt-4o` or `gpt-35-turbo`).
3. Deploy an **embedding model** (recommended: `text-embedding-3-small` or `text-embedding-ada-002`).
4. Copy endpoint and key from **Keys and Endpoint**.

```env
AzureOpenAi__Endpoint=https://<resource>.openai.azure.com
AzureOpenAi__ApiKey=<your-key>
AzureOpenAi__DeploymentName=gpt-4o
AzureOpenAi__EmbeddingDeploymentName=text-embedding-3-small
```

### Option B — Azure AI Foundry Serverless Endpoint

1. Go to [Azure AI Foundry](https://ai.azure.com) and create a project.
2. Deploy a model via **Serverless API** (e.g. `gpt-4o`, `Phi-3.5-mini`).
3. Copy the endpoint and API key from the deployment card.

> Foundry serverless endpoints end in `/v1` — the application automatically detects this and uses the OpenAI-compatible REST path (no `api-version` parameter).

```env
AzureOpenAi__Endpoint=https://<resource>.services.ai.azure.com/models/v1
AzureOpenAi__ApiKey=<your-key>
AzureOpenAi__DeploymentName=gpt-4o
AzureOpenAi__EmbeddingDeploymentName=text-embedding-3-small
```

---

## 2. Azure AI Search

### Provisioning

1. Create an **Azure AI Search** resource (Basic tier supports vectors; Standard tier adds semantic ranker).
2. Note the **endpoint URL** and **Admin key** from **Keys**.

```env
AzureSearch__Endpoint=https://<resource>.search.windows.net
AzureSearch__ApiKey=<admin-key>
AzureSearch__IndexName=devassist-documents
AzureSearch__VectorDimensions=1536
```

### Index

The index is created automatically on first document upload with these fields:

| Field | Type | Purpose |
|-------|------|---------|
| `id` | String (key) | Chunk unique key |
| `documentId` | String | Parent document ID |
| `documentName` | Searchable | BM25 keyword match |
| `documentType` | String | Filterable |
| `chunkOrder` | Int32 | Ordering |
| `content` | Searchable | Full-text BM25 search |
| `contentVector` | Vector (1536) | HNSW cosine similarity |

### Semantic Search (Optional)

Requires **Standard tier** with the semantic ranker add-on enabled:

1. In the Azure portal, enable **Semantic Search** on your Search resource.
2. Set the configuration name in your environment:

```env
AzureSearch__SemanticConfigurationName=devassist-semantic
```

When set, the retriever uses `QueryType.Semantic` with extractive captions and answers on top of hybrid results.

---

## 3. Azure Blob Storage

1. Create a **Storage Account** (LRS Standard is sufficient).
2. Create a **container** named `documents` (or use your own name).
3. Get the connection string from **Access keys**.

```env
BlobStorage__ConnectionString=DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
BlobStorage__ContainerName=documents
```

---

## 4. Local `.env` Setup

Copy `.env.example` to `.env` in the workspace root and fill in only the services you want:

```bash
cp .env.example .env
# Edit .env with your values
```

The `.env` loader runs before configuration is built. Real environment variables always take precedence over `.env` values.

---

## 5. Fallback Behavior Matrix

| Azure Service | Configured | Fallback |
|---------------|-----------|---------|
| Azure OpenAI | ❌ | Rule-based heuristics for tickets/requirements; chunk summary for copilot |
| Azure AI Search | ❌ | SQL BM25 keyword search (always available) |
| Azure Blob Storage | ❌ | Local filesystem (`./data/documents`) |
| Embeddings | ❌ | Placeholder vectors (no vector search) |
| Semantic ranker | ❌ | Hybrid BM25 + vector search (no re-ranking) |

---

## 6. Deployment Steps (Summarized)

```bash
# 1. Start SQL Server
docker-compose up -d

# 2. Set your .env with Azure credentials (or leave empty for local mode)
cp .env.example .env
# edit .env

# 3. Run the API (migrations auto-run in Development)
dotnet run --project src/DevAssist.Api

# 4. In a separate terminal, start the frontend
cd frontend/devassist-ui
npm install
npm run dev
```

The app is now available at `http://localhost:5173`.

---

## 7. Verification Checklist

### Local-only (no Azure credentials)
- [ ] Upload a `.txt` or `.md` document
- [ ] Status changes to `Indexed` (visible in document list)
- [ ] Ask a question in Knowledge Copilot — receives a grounded summary from document chunks
- [ ] Analyze a ticket — receives rule-based triage
- [ ] Break down a requirement — receives template-based task list

### With Azure OpenAI configured
- [ ] Same flows above but responses come from the LLM
- [ ] Copilot answer is richer and cites document chunks

### With Azure AI Search configured
- [ ] Upload → chunks appear in Azure Search index (verify in portal)
- [ ] Copilot retrieves from Azure AI Search (hybrid mode)
- [ ] Fallback to SQL still works if index is empty

### With Azure Blob Storage configured
- [ ] Uploaded file appears in the configured container
- [ ] Metadata and chunks still stored in SQL
