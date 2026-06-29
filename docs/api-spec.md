# API Specification

Base URL (local development): **`http://localhost:5147`**

Frontend dev server (`http://localhost:5173`) proxies `/api` and `/health` to this host.

---

## Response envelope

Most module endpoints return:

```json
{
  "success": true,
  "data": { },
  "error": null
}
```

On failure: `success: false`, `data: null`, `error: "message"`.

Unhandled exceptions are caught by `ApiExceptionHandler` and returned in the same envelope with appropriate HTTP status codes (`400`, `404`, `500`).

**Exceptions (raw JSON, no envelope):**
- `GET /` — `{ "service": "DevAssist.Api", "status": "Running" }`
- `GET /api/status` — `{ "service": "...", "environment": "...", "utcNow": "..." }`
- `GET /health` — ASP.NET health check (200 when healthy)

---

## Status

### `GET /api/status`
Returns API metadata for dashboard health display.

**Response:**
```json
{
  "service": "DevAssist AI Workspace API",
  "environment": "Development",
  "utcNow": "2026-06-25T12:00:00+00:00"
}
```

---

## Documents

### `POST /api/documents/upload`
Upload an engineering document. **Phase 2:** Indexing starts automatically in the background — the HTTP response returns immediately with status `Uploaded`.

**Content-Type:** `multipart/form-data`

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `file` | file | yes | Max 10 MB |
| `documentType` | enum string | yes | e.g. `EngineeringSpecification`, `Runbook`, `ArchitectureDecisionRecord` |
| `uploadedBy` | string | no | Default `system` |

**Response `data`:**
```json
{
  "id": "guid",
  "fileName": "auth-architecture.md",
  "status": "Uploaded"
}
```

**Phase 2 pipeline (background, after response):**
1. Text extraction (`.txt`, `.md` supported; PDF/DOCX return extraction error, status set to `Failed`)
2. Sliding-window chunking (1000 chars, 200 overlap)
3. Embedding vector generation (Azure OpenAI, or placeholder zeros)
4. Azure AI Search upsert with vectors (or no-op if not configured)
5. SQL chunk persistence; document status updated to `Indexed` or `Failed`

Poll `GET /api/documents/{id}` to check when status transitions to `Indexed`.

---

### `GET /api/documents`
List all uploaded documents (newest first).

**Response `data`:** array of
```json
{
  "id": "guid",
  "fileName": "string",
  "contentType": "string",
  "status": "Uploaded|Processing|Indexed|Failed",
  "documentType": "string",
  "uploadedAt": "ISO-8601",
  "uploadedBy": "string"
}
```

---

### `GET /api/documents/{documentId}`
Document details including chunk count.

**Response `data`:**
```json
{
  "id": "guid",
  "fileName": "string",
  "contentType": "string",
  "blobPath": "string",
  "status": "string",
  "documentType": "string",
  "uploadedAt": "ISO-8601",
  "uploadedBy": "string",
  "chunkCount": 12
}
```

**Errors:** `404` if document not found.

---

### `POST /api/documents/{documentId}/index`
Re-queue an existing document for background re-indexing (e.g. after an initial failure or to refresh vectors). Returns immediately.

**Response `data`:**
```json
{
  "id": "guid",
  "status": "Queued",
  "chunkCount": 0
}
```

**Notes:** Actual indexing runs asynchronously — poll document status to confirm completion.

**Errors:** `404` document not found; `400` validation failures.

---

## Copilot

### `POST /api/copilot/sessions`
Create a new chat session.

**Request:**
```json
{
  "title": "optional session title",
  "createdBy": "system"
}
```

**Response `data`:**
```json
{
  "sessionId": "guid",
  "title": "string",
  "createdAt": "ISO-8601"
}
```

---

### `POST /api/copilot/ask`
Ask a grounded question within a session.

**Request:**
```json
{
  "sessionId": "guid",
  "question": "How does session logout work?"
}
```

**Response `data`:**
```json
{
  "answer": "string",
  "citations": [
    {
      "documentId": "guid",
      "documentName": "authentication-architecture.md",
      "chunkReference": "chunk-guid-or-ref"
    }
  ]
}
```

**Phase 2 behavior:**
1. Persists user message
2. Translates non-Latin questions to English for retrieval (LLM or Latin-term extraction)
3. Generates query embedding (Azure OpenAI) for vector retrieval
4. `HybridDocumentSearchRetriever`: Azure AI Search (BM25 + KNN + optional semantic re-ranking), falls back to SQL keyword search
5. Builds grounded prompt (last 6 messages + top-K chunks)
6. `IAiAgent.CompleteAsync` → `AzureFoundryAgent` (Azure) or `LocalGroundedChatService` (local)
7. Persists assistant message with citations JSON

**Errors:** `404` if session not found; `400` validation errors.

---

## Tickets

### `POST /api/tickets/analyze`
Analyze ticket or incident text.

**Request:**
```json
{
  "text": "User clicks logout but remains logged in..."
}
```

**Response `data`:**
```json
{
  "id": "guid",
  "summary": "string",
  "severity": "Low|Medium|High|Critical",
  "category": "string",
  "impactedModule": "string",
  "suggestedAction": "string",
  "createdAt": "ISO-8601"
}
```

**Notes:** Analysis is persisted. Azure OpenAI returns structured JSON when configured; otherwise local keyword heuristics apply.

**Errors:** `400` validation or JSON parse failure.

---

### `GET /api/tickets/analyses?limit=20`
List recent ticket analyses, ordered by `createdAt` descending.

**Response `data`:** array of same shape as analyze response (full fields per item).

---

## Requirements

### `POST /api/requirements/breakdown`
Break down a feature request into an implementation plan.

**Request:**
```json
{
  "text": "Add OTP login with SMS fallback..."
}
```

**Response `data`:**
```json
{
  "id": "guid",
  "functionalSummary": "string",
  "backendTasks": ["string"],
  "frontendTasks": ["string"],
  "testingChecklist": ["string"],
  "risks": ["string"],
  "assumptions": ["string"],
  "acceptanceCriteria": ["string"],
  "createdAt": "ISO-8601"
}
```

**Errors:** `400` validation or JSON parse failure.

---

### `GET /api/requirements/analyses?limit=20`
List recent requirement analyses (summary only).

**Response `data`:**
```json
[
  {
    "id": "guid",
    "functionalSummary": "string",
    "createdAt": "ISO-8601"
  }
]
```

---

### `GET /api/requirements/analyses/{id}`
Load a full persisted breakdown by ID (for history reload in UI).

**Response `data`:** same shape as `POST /api/requirements/breakdown` response.

**Errors:** `404` if not found.

---

## Document type enum values

`EngineeringSpecification`, `ArchitectureDecisionRecord`, `IncidentPostmortem`, `Runbook`, `TicketAttachment`, `RequirementDocument`, `Other`

---

## Swagger

Interactive docs available in Development: `http://localhost:5147/swagger`
