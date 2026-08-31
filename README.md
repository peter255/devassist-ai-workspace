# DevAssist AI Workspace

An internal **Azure AI-powered engineering workspace** for software delivery teams. DevAssist is a practical, multi-workflow assistant that helps engineers retrieve knowledge from documentation, triage incidents consistently, and decompose feature requests into implementation-ready plans — all from a single web application.

It is structured as a **modular monolith** with clear Azure integration boundaries, designed to be **demo-ready today** and **extensible toward production-grade Azure AI patterns**.

---

## Screenshots

Full visual walkthrough: **[docs/user-guide.md](docs/user-guide.md)**

### Sign in & Dashboard
![Sign in](docs/screenshots/01-login.png)
![Dashboard](docs/screenshots/02-dashboard.png)

### Knowledge Copilot
![Copilot — chat workspace](docs/screenshots/03-knowledge-copilot-empty.png)
![Document library](docs/screenshots/04-knowledge-copilot-document-library.png)

**English — grounded answer with citations**

![Copilot English](docs/screenshots/05-knowledge-copilot-english-answer.png)

### Ticket & Incident Analyzer
![Ticket Analyzer — result](docs/screenshots/07-ticket-analyzer-result.png)

### Requirement Breakdown
![Requirement Breakdown — result](docs/screenshots/09-requirement-breakdown-result.png)

### Admin — User Management
![User list](docs/screenshots/10-admin-users-list.png)

---

## Executive summary

DevAssist packages three delivery workflows — **engineering knowledge retrieval**, **ticket triage**, and **requirement decomposition** — into one cohesive workspace with **multi-user authentication**, **per-user chat history**, and optional **Azure AI** integrations.

The solution is a **modular monolith** (ASP.NET Core 8 + React 19) with **local fallbacks** so teams can demo without Azure credentials, and **config-driven Azure services** when a `.env` file or Key Vault is wired up.

---

## Why DevAssist exists

| Problem | Impact |
|---------|--------|
| **Engineering knowledge is hard to search** | Architecture notes, runbooks, and ADRs live in scattered folders. Engineers re-ask the same questions or guess. |
| **Ticket triage is inconsistent and slow** | Bug reports arrive in free text. Severity, ownership, and next steps vary by who reads them. |
| **Requirements need manual decomposition** | Feature requests land as paragraphs. Teams spend meetings turning them into backend tasks, frontend work, and acceptance criteria. |

---

## Core engineering workflows

### Knowledge Copilot
- **Document library** (full-screen drawer): upload, inspect, and index documents
- Supported formats: `.txt`, `.md`, `.pdf`, `.docx` (OCR for scanned PDFs via Azure Document Intelligence)
- **Multi-file upload** in one batch
- **Background indexing** after upload (Service Bus or in-memory queue)
- **Per-user chat sessions** — list past sessions, resume, and continue with SSE streaming
- Grounded Q&A with **citations**; searches all indexed documents (no per-file selection required)
- RAG: retrieve chunks → build prompt → Azure OpenAI answer (local fallbacks when Azure is empty)

### Ticket & Incident Analyzer
- Paste a bug report, incident note, or support ticket
- Structured triage: summary, severity, category, impacted module, suggested action
- Persisted analysis history on the dashboard

### Requirement Breakdown Agent
- Paste a feature request or requirement
- Implementation-oriented output: functional summary, backend/frontend tasks, testing checklist, risks, assumptions, acceptance criteria
- Persisted breakdown history with reload by ID

### Dashboard
- Workspace overview with document and analysis counts
- API health indicator
- Quick navigation to all modules

### Authentication & admin
- **Local JWT login** (username/password) when `Jwt:Secret` is configured
- Roles: **Admin** and **User**
- **Admin panel** (`/admin/users`): create users, assign roles, reset passwords
- Chat sessions are **scoped per user** — no cross-user session access
- **Microsoft Entra ID** supported as an optional alternative (see Configuration)
- Default dev admin (seeded on first run): `admin` / value of `Jwt:DefaultAdminPassword` in appsettings

---

## Azure AI coverage

| Azure capability | Where it appears in DevAssist |
| ---------------- | ----------------------------- |
| **Azure OpenAI** | Copilot answers, ticket/requirement analyzers, embeddings |
| **Azure OpenAI Embeddings** | `AzureOpenAiEmbeddingService` — vectors for hybrid search |
| **Azure AI Search** | BM25 + KNN (HNSW) + optional semantic re-ranking; SQL keyword fallback |
| **Azure Blob Storage** | Document files; local `./data/documents` fallback |
| **Azure Document Intelligence** | OCR for scanned/image PDFs |
| **Azure Service Bus** | Durable document indexing queue; in-memory Channel fallback |
| **Azure Key Vault** | Optional secrets provider (early in config pipeline) |
| **Application Insights** | Optional telemetry + Serilog sink |
| **Prompt orchestration** | `CopilotPromptBuilder`, `TicketAnalyzerPromptBuilder`, `RequirementBreakdownPromptBuilder` |
| **Local fallback strategy** | Every Azure integration has a local equivalent for dev/demo |

A reference Azure resource group for this project: **`rg-devassist-ai`** (Sweden Central). Copy `.env.example` to `.env` and fill values after provisioning.

See [docs/azure-setup.md](docs/azure-setup.md) for provisioning steps.

---

## Tech stack

| Layer | Technologies |
| ----- | ------------ |
| **Backend** | ASP.NET Core 8, MediatR, FluentValidation, EF Core, SQL Server, Serilog, JWT Bearer |
| **Frontend** | React 19, TypeScript, Vite, TanStack Query, React Router |
| **AI** | Azure OpenAI, Azure AI Search, Azure Blob, Document Intelligence (all optional) |
| **CI/CD** | GitHub Actions (`ci.yml`, `cd.yml`) |
| **Local dev** | Docker SQL Server, filesystem storage, SQL keyword retrieval, heuristic AI fallbacks |

---

## Architecture overview

```
┌─────────────────────────────────────────────────────────────┐
│                  React + TypeScript (Vite)                  │
│    Login · Dashboard · Copilot · Tickets · Requirements     │
└──────────────────────────┬──────────────────────────────────┘
                           │ REST /api/*  (+ SSE ask-stream)
┌──────────────────────────▼──────────────────────────────────┐
│              DevAssist.Api (ASP.NET Core 8)                   │
│   JWT auth · Controllers · MediatR · Rate limiting            │
└──────────────────────────┬──────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌───────────────────┐
│  SQL Server   │  │ Azure OpenAI  │  │ Azure AI Search   │
│  users, docs, │  │ chat + embed  │  │ hybrid retrieval  │
│  chat sessions│  └───────────────┘  └───────────────────┘
└───────────────┘            │
                     ┌───────▼────────┐
                     │ Azure Blob     │
                     │ Service Bus    │
                     └────────────────┘
```

See [docs/architecture.md](docs/architecture.md) for request flows and module boundaries.

---

## Repository structure

```
devassist-ai-workspace/
├── src/
│   ├── DevAssist.Api/            # HTTP API, auth, controllers, Program.cs
│   ├── DevAssist.Application/    # MediatR commands/queries, validators
│   ├── DevAssist.Domain/         # Entities (AppUser, ChatSession, …)
│   ├── DevAssist.Infrastructure/ # EF Core, Azure adapters, AI services
│   └── DevAssist.Contracts/      # Request/response DTOs
├── frontend/devassist-ui/        # React SPA
├── docs/                         # Architecture, API spec, user guide, demo script
├── samples/                      # Demo ticket, requirement, and docs
├── .github/workflows/            # CI + CD (build, publish artifacts)
├── docker-compose.yml            # SQL Server (local dev)
├── .env.example                  # Environment template (never commit .env)
└── DevAssist.sln
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.x |
| [Node.js](https://nodejs.org/) | 20+ |
| [Docker Desktop](https://www.docker.com/) | For local SQL Server |
| Azure resources | Optional — local fallbacks work without them |

---

## Quick start

### 1. Start SQL Server

```bash
docker compose up -d sqlserver
```

Default port: **`1433`** (mapped from container `1433`). SA password: `Your_strong_password123` (see `docker-compose.yml` and `appsettings.json`).

### 2. Configure environment (optional)

```bash
cp .env.example .env
```

Fill Azure values when ready. For a fully local demo, defaults in `appsettings.json` work without Azure.

The API loads `.env` from the repo root automatically (see `Program.cs`).

### 3. Run the app

**Option A — API + UI together (SpaProxy):**

```bash
dotnet restore DevAssist.sln
dotnet run --project src/DevAssist.Api --launch-profile http
```

Open **http://localhost:5147**

**Option B — separate frontend dev server:**

```bash
# Terminal 1
dotnet run --project src/DevAssist.Api --launch-profile http

# Terminal 2
cd frontend/devassist-ui
npm install
npm run dev
```

Open **http://localhost:5173** (Vite proxies `/api` to the API).

### 4. Sign in

On first run with JWT enabled, a default admin is seeded:

- **Username:** `admin`
- **Password:** `Admin@123!` (or `Jwt:DefaultAdminPassword` in appsettings)

Change the password via **User Management** after first login.

### Endpoints

| URL | Purpose |
|-----|---------|
| `http://localhost:5147` | App (UI + API via SpaProxy) |
| `http://localhost:5147/swagger` | OpenAPI (Development) |
| `http://localhost:5147/health` | Health check |

> **Note:** `nuget.config` at the repo root avoids unreachable corporate NuGet feeds. See [docs/troubleshooting.md](docs/troubleshooting.md).

---

## Demo flow (~5–7 minutes)

1. **Login** as admin (or a regular user).
2. **Dashboard** — API health, document and analysis counts.
3. **Knowledge Copilot** — **Manage documents** → upload `samples/sample-docs/authentication-architecture.md` (indexing runs in background) → start or resume a chat session → ask: *"How does session logout work?"* → review streaming answer and citations.
4. **Ticket Analyzer** — paste `samples/sample-ticket.txt`, review triage output.
5. **Requirement Breakdown** — paste `samples/sample-requirement.txt`, review structured plan.

Full script: [docs/demo-scenarios.md](docs/demo-scenarios.md)

---

## Configuration

Settings live in `appsettings.json`, environment variables, or `.env` (double-underscore nesting).

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings__DevAssistDb` | SQL Server connection string |
| `Jwt__Secret` | Enables local JWT auth when set (min 32 chars) |
| `Jwt__Issuer` / `Jwt__Audience` | Token validation |
| `Jwt__DefaultAdminPassword` | First-run admin seed password |
| `AzureOpenAi__Endpoint` / `ApiKey` | Azure OpenAI |
| `AzureOpenAi__DeploymentName` | Chat model (e.g. `gpt-4o`) |
| `AzureOpenAi__EmbeddingDeploymentName` | Embeddings (e.g. `text-embedding-ada-002`) |
| `AzureSearch__Endpoint` / `ApiKey` | Azure AI Search |
| `AzureSearch__SemanticConfigurationName` | Semantic ranker (Standard tier+) |
| `BlobStorage__ConnectionString` | Azure Blob; empty → `./data/documents` |
| `DocumentIntelligence__Endpoint` / `ApiKey` | OCR for scanned PDFs |
| `ServiceBus__ConnectionString` | Durable indexing queue; empty → in-memory |
| `KeyVault__Uri` | Azure Key Vault secrets provider |
| `ApplicationInsights__ConnectionString` | Telemetry |
| `AzureAd__TenantId` / `ClientId` | Microsoft Entra ID (optional; use instead of or alongside JWT setup) |
| `Cors__AllowedOrigins__*` | Allowed frontend origins |
| `RateLimiting__*` | Per-IP rate limits (general + AI endpoints) |
| `VITE_API_BASE_URL` | Frontend API base (empty = Vite proxy) |
| `VITE_AAD_*` | Entra ID for frontend MSAL (optional) |

**Fallback behavior:** Empty Azure sections → local filesystem, SQL keyword search, and heuristic analyzers. The app remains runnable for demos.

---

## Phase completion status

### Phase 2 — Azure AI Integration ✅

| Capability | Status |
|------------|--------|
| IAiAgent abstraction (`AzureFoundryAgent` + `LocalFallbackAgent`) | Done |
| Azure OpenAI embeddings + hybrid search (BM25 + KNN) | Done |
| Semantic re-ranking (config-driven, Standard tier) | Done |
| Azure Blob Storage + local fallback | Done |
| Background indexing (Channel / Service Bus) | Done |
| Auto-queue on upload | Done |
| PDF (PdfPig) + DOCX (OpenXml) extraction | Done |
| OCR via Document Intelligence | Done |

### Phase 3 — Enterprise Hardening ✅

| Capability | Status |
|------------|--------|
| Local JWT auth + Admin/User roles + user management API/UI | Done |
| Per-user copilot sessions + session list + history reload | Done |
| SSE streaming copilot responses | Done |
| Rate limiting (general + AI endpoints) | Done |
| Application Insights + Key Vault providers | Done |
| Service Bus indexing queue | Done |
| GitHub Actions CI/CD | Done |
| HTTPS redirect (production) + CORS | Done |
| Microsoft Entra ID (optional, config-driven) | Done |
| Multi-file document upload | Done |

### Phase 4 — Ecosystem 🟡

| Capability | Status |
|------------|--------|
| Streaming SSE (Copilot) | Done |
| Proposal Assistant | Planned |
| Azure DevOps work-item integration | Planned |
| Microsoft Teams bot | Planned |
| Multi-tenant isolation | Planned |

---

## Known limitations

| Area | Notes |
|------|-------|
| **Production deploy** | CD publishes artifacts only; no Azure App Service deploy yet |
| **Secrets** | Key Vault wired but secrets typically still in `.env` locally |
| **Azure SQL** | Local Docker SQL only; no managed DB in repo yet |
| **Azure AI Search Basic** | Vector/semantic features limited; upgrade to Standard for full hybrid quality |
| **Automated tests** | No test projects in solution yet; CI `dotnet test` has nothing to run |
| **Document delete** | Not implemented |
| **Session rename/delete** | Not implemented in UI |
| **Entra ID** | Scaffolded; local JWT is the default dev path |

---

## Roadmap (suggested next steps)

### Wave 1 — Production ready
- Azure SQL + App Service deploy + CD pipeline to environment
- Migrate secrets to Key Vault + Managed Identity
- Upgrade Azure AI Search to Standard + semantic configuration
- Add integration/unit tests (auth, copilot, documents)

### Wave 2 — UX polish
- Document status polling after upload; **Index all** for pending files
- Delete documents (API + UI)
- Rename/delete copilot sessions; change-password profile page

### Wave 3 — Ecosystem (Phase 4)
- Azure DevOps: create work items from ticket/requirement outputs
- Proposal Assistant module
- Microsoft Teams bot
- Optional: full Entra ID as primary corporate auth

---

## API & prompts

- **User guide (screenshots):** [docs/user-guide.md](docs/user-guide.md)
- **API reference:** [docs/api-spec.md](docs/api-spec.md)
- **Prompting strategy:** [docs/prompts.md](docs/prompts.md)
- **Troubleshooting:** [docs/troubleshooting.md](docs/troubleshooting.md)

---

## Project status

DevAssist is a **demo-ready internal MVP** with **multi-user auth**, **Azure-ready integrations**, and **local fallbacks**. Core workflows are implemented end-to-end through Phase 3. Phase 4 ecosystem modules and production hardening (deploy, tests, Key Vault-only secrets) are the main remaining work.

---

## License

Internal / demonstration project. Adjust licensing before external distribution.
