# DevAssist AI Workspace

An internal **Azure AI-powered engineering workspace** for software delivery teams. DevAssist is a practical, multi-workflow assistant that helps engineers retrieve knowledge from documentation, triage incidents consistently, and decompose feature requests into implementation-ready plans — all from a single web application.

It is structured as a **modular monolith** with clear Azure integration boundaries, designed to be **demo-ready today** and **extensible toward production-grade Azure AI patterns** without pretending the MVP is fully hardened.

---

## Executive summary

DevAssist is a demo-ready internal MVP that packages three common delivery workflows — **engineering knowledge retrieval**, **ticket triage**, and **requirement decomposition** — into one cohesive workspace. The solution is intentionally built as a **modular monolith** (ASP.NET Core + React) so teams can evolve each workflow independently while sharing persistence and infrastructure. It demonstrates **Azure AI application patterns** — grounded Q&A, structured model outputs, retrieval boundaries, and storage abstractions — with **local fallbacks** so reviewers can run and present the project without Azure credentials.

---

## Why DevAssist exists

Software delivery teams routinely face three friction points:

| Problem | Impact |
|---------|--------|
| **Engineering knowledge is hard to search** | Architecture notes, runbooks, and ADRs live in scattered folders. Engineers re-ask the same questions or guess. |
| **Ticket triage is inconsistent and slow** | Bug reports arrive in free text. Severity, ownership, and next steps vary by who reads them. |
| **Requirements need manual decomposition** | Feature requests land as paragraphs. Teams spend meetings turning them into backend tasks, frontend work, and acceptance criteria. |

DevAssist addresses these with three focused AI-assisted modules backed by a clean layered architecture — suitable for internal review, iterative hardening, and extension into production Azure integrations.

---

## Core engineering workflows

### Knowledge Copilot
- Upload engineering documents (`.txt`, `.md`)
- Index content into searchable chunks
- Ask grounded questions with **citations** from indexed material
- RAG flow: retrieve chunks → build prompt → Azure OpenAI answer (with local fallbacks for dev)

### Ticket & Incident Analyzer
- Paste a bug report, incident note, or support ticket
- Receive structured triage: summary, severity, category, impacted module, suggested action
- Persisted analysis history

### Requirement Breakdown Agent
- Paste a feature request or requirement
- Receive implementation-oriented output: functional summary, backend/frontend tasks, testing checklist, risks, assumptions, acceptance criteria
- Persisted breakdown history with reload by ID

### Dashboard
- Workspace overview with document and analysis counts
- Quick navigation to all modules
- API health indicator

---

## Azure AI coverage in this project

DevAssist was intentionally designed to exercise practical **Azure AI Apps / Agents** engineering patterns — not as a thin wrapper around a chat box, but as a workspace where retrieval, prompts, storage, and task-specific AI flows are separated and swappable.

| Azure capability | Where it appears in DevAssist |
| ---------------- | ----------------------------- |
| **Azure OpenAI integration** | Copilot answers, ticket analysis, requirement breakdown (structured JSON) |
| **Prompt orchestration** | Grounded copilot prompts; structured analyzer prompts with fixed output schemas |
| **Retrieval-Augmented Generation (RAG)** | Document chunking, chunk retrieval, citation-backed copilot answers |
| **Azure AI Search integration boundary** | Search indexer/retriever abstractions; **scaffolded** — local SQL keyword search used when not configured |
| **Blob / file storage abstraction** | Engineering document upload and storage (`Azure Blob` or local filesystem) |
| **AI workflow orchestration** | Multiple task-focused AI modules (copilot, tickets, requirements) in one workspace |
| **Local fallback strategy** | Heuristic analyzers, SQL retrieval, filesystem storage — demo-friendly without Azure credentials |

**Honest scope note:** Vector embeddings and full Azure AI Search upsert/query are **integration boundaries with placeholders**, not production-complete implementations. See [Known MVP limitations](#known-mvp-limitations).

---

## Tech stack snapshot

| Layer | Technologies |
| ----- | ------------ |
| **Backend** | ASP.NET Core 8, MediatR, FluentValidation, EF Core, SQL Server, Serilog |
| **Frontend** | React, TypeScript, Vite, TanStack Query, React Router |
| **AI integrations** | Azure OpenAI, Azure AI Search (boundary), Azure Blob Storage (boundary) |
| **Local fallbacks** | SQL keyword retrieval, filesystem storage, heuristic analyzers |

---

## Architecture overview

```
┌─────────────────────────────────────────────────────────────┐
│                  React + TypeScript (Vite)                  │
│         Dashboard · Copilot · Tickets · Requirements        │
└──────────────────────────┬──────────────────────────────────┘
                           │ REST /api/*
┌──────────────────────────▼──────────────────────────────────┐
│              DevAssist.Api (ASP.NET Core 8)                 │
│   Controllers → MediatR handlers → Application services     │
└──────────────────────────┬──────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌───────────────────┐
│  SQL Server   │  │ Azure OpenAI  │  │ Azure AI Search   │
│  (EF Core)    │  │ chat + JSON   │  │ (optional)        │
└───────────────┘  └───────────────┘  └───────────────────┘
                           │
                   ┌───────▼────────┐
                   │ Azure Blob     │
                   │ (optional)     │
                   └────────────────┘
```

**Backend:** ASP.NET Core 8 modular monolith — Domain, Application, Infrastructure, Contracts, API.

**Frontend:** React 19, TypeScript, Vite, TanStack Query, React Router.

**Data:** SQL Server for documents, chunks, chat sessions, ticket analyses, requirement analyses.

**Azure (optional in local dev):**
- **Azure OpenAI** — copilot answers, ticket analysis, requirement breakdown
- **Azure AI Search** — retrieval boundary (scaffolded; SQL keyword fallback locally)
- **Azure Blob Storage** — document files (falls back to local `./data/documents`)

When Azure credentials are empty, the app runs with **local fallbacks** (heuristic analyzers, SQL search, filesystem storage) so demos work without cloud setup.

See [docs/architecture.md](docs/architecture.md) for request flows and module boundaries.

---

## Repository structure

```
devassist-ai-workspace/
├── src/
│   ├── DevAssist.Api/           # HTTP API, controllers, Program.cs, Swagger
│   ├── DevAssist.Application/   # MediatR commands/queries, validators, interfaces
│   ├── DevAssist.Domain/        # Entities and enums
│   ├── DevAssist.Infrastructure/# EF Core, Azure adapters, repositories, AI services
│   └── DevAssist.Contracts/     # Request/response DTOs shared with API consumers
├── frontend/devassist-ui/       # React SPA
├── docs/                        # Architecture, API spec, demo script, prompts
├── samples/                     # Demo-ready ticket, requirement, and engineering docs
├── docker-compose.yml           # SQL Server for local development
├── nuget.config                 # NuGet.org feed for clean CI/GitHub clones
├── .env.example                 # Environment variable template
└── DevAssist.sln
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.x |
| [Node.js](https://nodejs.org/) | 20+ recommended |
| [Docker](https://www.docker.com/) | For SQL Server container |
| Azure resources | Optional — local fallbacks work without them |

---

## Quick start

### 1. Start SQL Server

```bash
docker compose up -d
```

Default SA password matches `appsettings.json` and `.env.example`: `Your_strong_password123`.

### 2. Configure environment (optional)

Copy `.env.example` to `.env` and fill Azure values when ready. For a local demo, defaults work without Azure.

### 3. Run the app (API + UI — Visual Studio SPA style)

One command starts the API and launches Vite automatically via **SpaProxy** (same as pressing F5 in Visual Studio):

```bash
dotnet restore DevAssist.sln
dotnet run --project src/DevAssist.Api --launch-profile http
```

Or from the repo root: `npm run dev` / `scripts\dev.cmd` (Windows).

> **Note:** `nuget.config` at the repo root avoids unreachable corporate NuGet feeds. See [docs/troubleshooting.md](docs/troubleshooting.md) if restore fails.

- **App (open this):** `http://localhost:5147`
- Swagger (Development): `http://localhost:5147/swagger`
- Health: `http://localhost:5147/health`
- Migrations apply automatically in Development
- Node.js is required; `npm install` runs in `frontend/devassist-ui` on first build if `node_modules` is missing

### 4. Run frontend only (optional)

If you prefer two terminals:

```bash
cd frontend/devassist-ui
npm install
npm run dev
```

- UI: `http://localhost:5173` (Vite proxies `/api` and `/health` to the API)

---

## Open the demo workspace

After both services are running, verify the project end-to-end:

1. Open **http://localhost:5147** — confirm the Dashboard shows API health.
2. Go to **Knowledge Copilot** — upload `samples/sample-docs/authentication-architecture.md`.
3. Click **Index** on the uploaded document and wait for status **Indexed**.
4. Start a chat session and ask: *"How does user logout work?"* — review the answer and citations.
5. Open **Ticket Analyzer** — load the sample ticket and run analysis.
6. Open **Requirement Breakdown** — load the sample requirement and review the structured output.

For a guided presenter script, see [docs/demo-scenarios.md](docs/demo-scenarios.md).

---

## Configuration

Settings live in `src/DevAssist.Api/appsettings.json` or environment variables (double-underscore nesting).

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings__DevAssistDb` | SQL Server connection string |
| `AzureOpenAi__Endpoint` | Azure OpenAI endpoint URL |
| `AzureOpenAi__ApiKey` | Azure OpenAI API key |
| `AzureOpenAi__DeploymentName` | Chat model deployment name |
| `AzureSearch__Endpoint` | Azure AI Search service URL |
| `AzureSearch__ApiKey` | Search admin/query key |
| `AzureSearch__IndexName` | Index name (default: `devassist-documents`) |
| `BlobStorage__ConnectionString` | Azure Storage connection string |
| `BlobStorage__ContainerName` | Blob container (default: `documents`) |
| `LocalFileStorage__RootPath` | Local file path when blob is empty (default: `./data/documents`) |
| `VITE_API_BASE_URL` | Frontend API base (empty = use Vite proxy) |

**Local fallback behavior:** Empty `AzureOpenAi` → heuristic ticket/requirement analyzers + grounded local copilot. Empty `AzureSearch` → SQL keyword chunk retrieval. Empty `BlobStorage` → local filesystem.

---

## Demo flow (~5–7 minutes)

A full presenter script is in [docs/demo-scenarios.md](docs/demo-scenarios.md).

1. **Dashboard** — show module overview and API health.
2. **Knowledge Copilot** — upload `samples/sample-docs/authentication-architecture.md`, index it, ask: *"How does session logout work?"*
3. **Ticket Analyzer** — paste `samples/sample-ticket.txt`, review structured triage.
4. **Requirement Breakdown** — paste `samples/sample-requirement.txt`, review tasks and acceptance criteria.
5. **Close** — explain value: faster knowledge access, consistent triage, implementation-ready breakdowns.

---

## API documentation

See [docs/api-spec.md](docs/api-spec.md) for endpoint details, request/response shapes, and behavior notes.

---

## Prompting strategy

See [docs/prompts.md](docs/prompts.md) for copilot grounding rules, structured JSON outputs for analyzers, and tuning notes.

---

## Known MVP limitations

This repository is intentionally a **demo-ready internal MVP**. Some Azure integrations are **scaffolded or deferred** by design; the items below are **known boundaries**, not accidental gaps.

| Area | Status |
|------|--------|
| Azure AI Search upsert/query | Scaffolded; local SQL search used when not configured |
| Azure OpenAI embeddings | Placeholder; chunking uses text splits without vectors |
| PDF / DOCX extraction | Not supported — explicit error returned |
| Copilot message history | Session persisted server-side; UI reload does not restore thread |
| Authentication / RBAC | Not implemented — internal MVP assumption |
| Proposal Assistant | Not in scope for this release |

---

## Suggested next engineering steps

- **Azure AI Search + embeddings** — wire indexer, retriever, and vector pipeline end-to-end
- **Azure OpenAI hardening** — production prompts, monitoring, and error handling
- **Authentication** — Entra ID / internal SSO and tenant-scoped data access
- **Proposal Assistant** — draft technical proposals from requirements + architecture context
- **Azure DevOps integration** — create work items from ticket/requirement outputs
- **OCR** — extract text from screenshots and scanned PDFs
- **Microsoft Teams bot** — ask copilot and submit tickets from chat
- **Production hardening** — rate limiting, observability, secret management

---

## Troubleshooting

See [docs/troubleshooting.md](docs/troubleshooting.md) for NuGet feeds, SQL Server, API connectivity, and Azure fallback behavior.

---

## Screenshots

Add UI screenshots to `docs/screenshots/` for GitHub README embedding. See [docs/screenshots/README.md](docs/screenshots/README.md).

---

## Project status

DevAssist is currently a **demo-ready internal MVP**: the core workflows are implemented end-to-end, the architecture is intentionally layered for extension, and local fallbacks make it presentable without Azure credentials.

The project is **not** production-complete. The natural next steps are wiring Azure AI Search indexing and embeddings, strengthening Azure OpenAI integration quality, adding authentication, and adding observability — building on the abstractions already in place rather than restructuring the solution.

---

## License

Internal / demonstration project. Adjust licensing before external distribution.
