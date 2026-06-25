# Architecture

DevAssist AI Workspace is a **modular monolith**: one deployable API with clear module boundaries, shared persistence, and swappable Azure integrations.

---

## Layered backend structure

```
DevAssist.Api
    └── Controllers (HTTP)
            └── MediatR (Commands / Queries)
                    └── Application handlers
                            └── Domain entities
                            └── Infrastructure services (via interfaces)
```

| Project | Responsibility |
|---------|----------------|
| `DevAssist.Domain` | Entities (`Document`, `DocumentChunk`, `ChatSession`, `ChatMessage`, `TicketAnalysis`, `RequirementAnalysis`) and enums |
| `DevAssist.Application` | Use cases: commands, queries, validators, mappers, service interfaces |
| `DevAssist.Infrastructure` | EF Core `DevAssistDbContext`, repositories, Azure adapters, prompt builders, AI service implementations |
| `DevAssist.Contracts` | API DTOs (`ApiResponse<T>`, module request/response records) |
| `DevAssist.Api` | Composition root, CORS, Serilog, Swagger, health checks, auto-migrate (Development) |

**Cross-cutting:** FluentValidation on commands, Serilog request logging, `ApiResponse<T>` envelope for module endpoints, global `ApiExceptionHandler` for consistent error JSON.

---

## Module boundaries

### Documents
- Upload files to blob or local storage
- Track metadata and status (`Uploaded` → `Processing` → `Indexed` / `Failed`)
- Extract text (`.txt`, `.md`), chunk, persist chunks, optional search index upsert

**Key types:** `IDocumentRepository`, `IDocumentStorageService`, `IDocumentIndexingOrchestrator`, `IDocumentSearchIndexer`

### Knowledge Copilot
- Chat sessions and messages in SQL
- Retrieve relevant chunks (Azure Search or SQL keyword fallback)
- Build grounded prompt → Azure OpenAI (or local fallback)
- Return answer with citation DTOs

**Key types:** `IChatRepository`, `IDocumentSearchRetriever`, `ICopilotPromptBuilder`, `IKnowledgeCopilotService`, `IAzureOpenAiChatService`

### Ticket Analyzer
- Analyze free-text ticket → structured JSON via Azure OpenAI
- Map severity string to `TicketSeverity` enum
- Persist `TicketAnalysis`

**Key types:** `ITicketAnalyzerService`, `ITicketAnalysisRepository`, `ITicketAnalyzerPromptBuilder`

### Requirement Breakdown
- Analyze requirement text → structured implementation plan JSON
- Persist `RequirementAnalysis` (task lists as JSON columns)
- List and get-by-id for history reload

**Key types:** `IRequirementBreakdownService`, `IRequirementAnalysisRepository`, `IRequirementBreakdownPromptBuilder`

---

## Request flows

### Document upload and indexing

```mermaid
sequenceDiagram
    participant UI
    participant API
    participant App as Application
    participant Store as Blob/Local Storage
    participant DB as SQL Server
    participant Search as AI Search (optional)

    UI->>API: POST /api/documents/upload (multipart)
    API->>App: UploadDocumentCommand
    App->>Store: UploadAsync(stream)
    App->>DB: Insert Document (Uploaded)
    API-->>UI: document id, status

    UI->>API: POST /api/documents/{id}/index
    API->>App: IndexDocumentCommand
    App->>Store: OpenReadAsync
    App->>App: Extract text, chunk
    App->>DB: ReplaceChunks, status=Indexed
    App->>Search: Upsert chunks (if configured)
    API-->>UI: chunk count, status
```

### Copilot question answering

```mermaid
sequenceDiagram
    participant UI
    participant API
    participant Copilot as KnowledgeCopilotService
    participant DB as SQL Server
    participant Retriever as Search/SQL Retriever
    participant OAI as Azure OpenAI

    UI->>API: POST /api/copilot/sessions
    API-->>UI: sessionId

    UI->>API: POST /api/copilot/ask
    API->>Copilot: AskAsync(sessionId, question)
    Copilot->>DB: Save user message
    Copilot->>Retriever: Retrieve chunks for question
    Copilot->>Copilot: Build prompt (history + chunks)
    Copilot->>OAI: CompleteAsync
    Copilot->>DB: Save assistant message + citations JSON
    API-->>UI: answer, citations
```

### Ticket analysis

```mermaid
sequenceDiagram
    participant UI
    participant API
    participant Handler as AnalyzeTicketCommandHandler
    participant AI as TicketAnalyzerService
    participant DB as SQL Server

    UI->>API: POST /api/tickets/analyze { text }
    API->>Handler: command
    Handler->>AI: AnalyzeAsync(text)
    AI-->>Handler: TicketAnalysisOutput (JSON parsed)
    Handler->>DB: Insert TicketAnalysis
    API-->>UI: structured response + id
```

### Requirement breakdown

```mermaid
sequenceDiagram
    participant UI
    participant API
    participant Handler as BreakdownRequirementCommandHandler
    participant AI as RequirementBreakdownService
    participant DB as SQL Server

    UI->>API: POST /api/requirements/breakdown { text }
    API->>Handler: command
    Handler->>AI: AnalyzeAsync(text)
    AI-->>Handler: RequirementBreakdownOutput
    Handler->>DB: Insert RequirementAnalysis
    API-->>UI: tasks, risks, criteria, id
```

---

## Infrastructure responsibilities

| Component | Implementation | When not configured |
|-----------|----------------|---------------------|
| Document storage | `AzureBlobDocumentStorageService` | `LocalFileDocumentStorageService` |
| Search indexing | `AzureSearchDocumentIndexer` | `NoOpDocumentSearchIndexer` |
| Chunk retrieval | `AzureSearchDocumentRetriever` | `SqlDocumentSearchRetriever` |
| Embeddings | `AzureOpenAiEmbeddingService` | `PlaceholderEmbeddingService` |
| Chat completion | `AzureOpenAiChatService` | `LocalGroundedChatService` |
| Ticket analysis | `AzureOpenAiTicketAnalyzerService` | `LocalTicketAnalyzerService` |
| Requirement breakdown | `AzureOpenAiRequirementBreakdownService` | `LocalRequirementBreakdownService` |

Registration is centralized in `InfrastructureServiceCollectionExtensions.cs` — each Azure service is selected based on configuration presence.

---

## Azure service responsibilities

### Azure OpenAI
- **Chat deployment:** copilot answers, ticket JSON, requirement JSON
- **Embeddings (planned):** semantic chunk search when wired

### Azure AI Search
- Store document chunk index for hybrid/vector retrieval
- Currently scaffolded; local SQL `LIKE` search is the dev fallback

### Azure Blob Storage
- Durable document file storage for uploads
- Local `./data/documents` mirrors this in development

### SQL Server
- System of record: documents, chunks, chat, analyses
- Also serves as retrieval fallback for copilot

---

## Frontend architecture

```
frontend/devassist-ui/src/
├── api/           # Typed fetch clients + parseApiResponse
├── app/           # routes, queryKeys, global styles
├── components/    # documents/, copilot/, ui/
├── layout/        # AppLayout shell
├── pages/         # Dashboard, Copilot, Tickets, Requirements
└── types/         # TypeScript DTO mirrors
```

- **TanStack Query** for server state, caching, and invalidation after mutations
- **Vite dev proxy** forwards `/api` and `/health` to `localhost:5147`
- Module pages use consistent panels, loading/error states, and history sidebars

---

## Key extension points

| Extension | How |
|-----------|-----|
| New AI module | Add interface in Application, prompt builder + service in Infrastructure, MediatR handler, controller, frontend page |
| New document type / extractor | Implement `IDocumentTextExtractor`, register in DI |
| Swap retrieval strategy | Implement `IDocumentSearchRetriever`, register based on config |
| Auth | Add middleware + user context; filter repositories by tenant/user |
| Work item export | New Application command calling Azure DevOps client from analyzer output |

---

## Persistence model

| Table | Purpose |
|-------|---------|
| `Documents` | File metadata, status, blob path |
| `DocumentChunks` | Indexed text segments linked to documents |
| `ChatSessions` / `ChatMessages` | Copilot conversation state |
| `TicketAnalyses` | Persisted ticket triage results |
| `RequirementAnalyses` | Persisted breakdown results (JSON task columns) |

EF Core migrations live in `DevAssist.Infrastructure/Migrations/`.
