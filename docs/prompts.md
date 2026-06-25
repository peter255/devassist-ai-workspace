# Prompt Strategy

DevAssist uses **dedicated prompt builders** per module. Prompts live in Infrastructure (`*PromptBuilder.cs` classes) and are injected via Application interfaces.

No API keys or secrets belong in this document.

---

## Design principles

1. **Module-specific prompts** — copilot grounding differs from structured JSON extraction for analyzers.
2. **Structured outputs for analyzers** — ticket and requirement modules request JSON-only responses with fixed schemas.
3. **Grounding over creativity** — copilot must not invent architecture; analyzers must stay practical and engineering-focused.
4. **Local fallbacks** — when Azure OpenAI is not configured, heuristic services approximate behavior for demos (not for production quality).

---

## Knowledge Copilot

**Implementation:** `CopilotPromptBuilder` (`Infrastructure/Copilot/Prompting/`)

### System prompt goals
- Act as an internal assistant for software delivery teams
- Answer **only** from provided document context chunks
- State clearly when context is insufficient
- Do not invent APIs, integrations, or system behavior
- Be concise and engineering-focused
- Reference chunk keys in brackets, e.g. `[chunk-ref]`

### User prompt composition
1. **Recent conversation** — last 6 messages (user/assistant)
2. **Retrieved chunks** — each with reference ID, document name, document type, and content
3. **Current question**

### Expected output
- Natural language answer (not JSON)
- Citations derived server-side from retrieval metadata, stored with assistant messages

### Safety / grounding rules
| Rule | Rationale |
|------|-----------|
| No answer without context | Prevents generic ChatGPT-style guesses |
| Explicit "cannot answer" | Builds user trust when docs are incomplete |
| Chunk references in text | Traceability to source material |
| Limited history window | Controls token cost and drift |

### Future tuning
- Add explicit "confidence" or "sources used" metadata
- Tune retrieval `top-K` and chunk size for precision vs recall
- Add system prompt variants per document type (runbook vs ADR)
- Wire embeddings for semantic retrieval before prompt build

---

## Ticket Analyzer

**Implementation:** `TicketAnalyzerPromptBuilder` (`Infrastructure/Tickets/Prompting/`)

### System prompt goals
- Expert software engineering **triage** assistant
- Concise, practical, engineering-focused assessment
- JSON-only response — no markdown wrappers

### Expected JSON schema
```json
{
  "summary": "string",
  "severity": "Low|Medium|High|Critical",
  "category": "string",
  "impactedModule": "string",
  "suggestedAction": "string"
}
```

| Field | Intent |
|-------|--------|
| `summary` | One or two sentences capturing the issue |
| `severity` | Exactly one of Low, Medium, High, Critical |
| `category` | Problem domain (Auth, API, Database, UI, DevOps, …) |
| `impactedModule` | Likely system area or component |
| `suggestedAction` | Concrete next troubleshooting or fix step |

### Parsing
`AzureOpenAiTicketAnalyzerService` extracts JSON from model output (handles extra text), normalizes severity, maps to `TicketAnalysisOutput`.

### Local fallback
`LocalTicketAnalyzerService` uses keyword heuristics (e.g. "production", "logout", "500") when Azure is not configured.

### Future tuning
- Add examples (few-shot) for severity calibration per organization
- Include service catalog hints in system prompt
- Validate category against allowed taxonomy enum

---

## Requirement Breakdown

**Implementation:** `RequirementBreakdownPromptBuilder` (`Infrastructure/Requirements/Prompting/`)

### System prompt goals
- Expert engineering **lead** helping teams plan implementation
- Implementation-oriented — each list item should be actionable by a developer
- JSON-only response

### Expected JSON schema
```json
{
  "functionalSummary": "string",
  "backendTasks": ["string"],
  "frontendTasks": ["string"],
  "testingChecklist": ["string"],
  "risks": ["string"],
  "assumptions": ["string"],
  "acceptanceCriteria": ["string"]
}
```

| Section | Intent |
|---------|--------|
| `functionalSummary` | What the feature does in plain language |
| `backendTasks` | APIs, services, data, integrations |
| `frontendTasks` | UI flows, components, UX states |
| `testingChecklist` | Verifiable test scenarios |
| `risks` | Delivery, security, performance, integration risks |
| `assumptions` | Presumed existing systems or scope boundaries |
| `acceptanceCriteria` | Testable "done" conditions |

### Parsing
`AzureOpenAiRequirementBreakdownService` deserializes JSON, trims empty list items.

### Local fallback
`LocalRequirementBreakdownService` generates template task lists with keyword branches (auth, upload, API patterns).

### Future tuning
- Inject tech stack constraints (e.g. ".NET 8", "React", "SQL Server")
- Add story-point estimation hints
- Link output format to Azure DevOps work item fields

---

## Shared Azure OpenAI usage

All three modules share `IAzureOpenAiChatService`:
- **Copilot:** free-form completion
- **Tickets / Requirements:** completion with JSON parsing and validation

Configuration section: `AzureOpenAi` in `appsettings.json` (`Endpoint`, `ApiKey`, `DeploymentName`).

---

## What not to put in prompts

- API keys, connection strings, or PII
- Unbounded "use your general knowledge" instructions for copilot
- Vague output formats for analyzers (always specify JSON schema)

---

## Prompt change checklist

When editing prompts:
1. Update the corresponding `*PromptBuilder.cs`
2. Update this document's expected schema section
3. Test with sample files in `samples/`
4. Verify local fallback still behaves reasonably if Azure is disabled
5. Confirm frontend types still match API response shapes
