# Demo Scenarios

A **5–7 minute** presenter script for demonstrating DevAssist AI Workspace to a Head of Team or engineering leadership audience.

**Before you start:**
- `docker compose up -d`
- API running: `dotnet run --project src/DevAssist.Api`
- UI running: `cd frontend/devassist-ui && npm run dev`
- Open `http://localhost:5173`
- Have sample files ready in `samples/`

---

## 0. Opening — Dashboard (30 seconds)

**Navigate to:** `/` (Dashboard)

**Say:**
> "DevAssist is an internal AI workspace for delivery teams. It connects three common workflows — finding answers in engineering docs, triaging incidents, and breaking down feature requests — in one place."

**Show:**
- API health indicator (green = API reachable)
- Stat cards: documents, indexed count, recent analyses
- Module navigation cards

**Value line:** One entry point instead of three separate tools or ad-hoc ChatGPT sessions without company context.

---

## 1. Knowledge Copilot — Upload & index (90 seconds)

**Navigate to:** `/copilot`

**Say:**
> "Engineers spend time hunting through architecture notes and runbooks. The Knowledge Copilot lets us upload internal docs and ask questions with citations."

**Steps:**
1. Click upload zone → select `samples/sample-docs/authentication-architecture.md`
2. Choose document type: **Engineering Specification** (or Architecture Decision Record)
3. Click upload → document appears in library with status **Uploaded**
4. Click **Index** on the document → status becomes **Ready/Indexed**
5. Optional: select document → Inspector shows chunk count and metadata

**Value line:** Documents are chunked and searchable; answers can reference the source material.

---

## 2. Knowledge Copilot — Ask a question (60 seconds)

**Steps:**
1. Click **Start chat session** (or **New session**)
2. Ask one of these demo questions:
   - *"How does user logout work in our authentication service?"*
   - *"What is the session timeout policy?"*
   - *"What should we check if a user remains logged in after clicking logout?"*
3. Show the answer and **citations** linking to document chunks

**Say:**
> "The copilot is grounded — it answers from retrieved chunks, not from general internet knowledge. If context is missing, it should say so."

**If using local fallback (no Azure OpenAI):** Mention that answers use retrieved chunk text; with Azure OpenAI configured, responses are more natural while still grounded.

---

## 3. Ticket Analyzer (90 seconds)

**Navigate to:** `/tickets`

**Say:**
> "Incident reports arrive as free text. Different engineers triage differently. The Ticket Analyzer produces a consistent structure every time."

**Steps:**
1. Click **Load sample** (or paste from `samples/sample-ticket.txt`)
2. Click **Analyze ticket**
3. Walk through result cards:
   - **Summary** — one-line problem statement
   - **Severity** badge
   - **Category** and **Impacted module**
   - **Suggested action** — concrete next step
4. Point to **Recent analyses** sidebar — click a history item to reload

**Value line:** Faster handoff to the right team; consistent severity language for standups and escalation.

---

## 4. Requirement Breakdown (90 seconds)

**Navigate to:** `/requirements`

**Say:**
> "Feature requests often land as a paragraph in email or a ticket. Before sprint planning, someone manually splits them into tasks. This module does that decomposition in seconds."

**Steps:**
1. Click **Load sample** (or paste from `samples/sample-requirement.txt`)
2. Click **Break down requirement**
3. Walk through sections:
   - Functional summary
   - Backend tasks / Frontend tasks
   - Testing checklist
   - Risks and assumptions
   - Acceptance criteria
4. Click a history item to reload a previous breakdown

**Value line:** Implementation-ready output for backlog refinement and estimation conversations.

---

## 5. Closing — Tie it together (60 seconds)

**Return to:** Dashboard

**Say:**
> "DevAssist is a modular monolith — one API, one database, clear boundaries. Today it runs locally with fallbacks; in production we wire Azure OpenAI for quality, Azure AI Search for semantic retrieval, and Blob Storage for files."

**Highlight architecture strengths:**
- Clean separation: Domain → Application → Infrastructure
- Each module can evolve independently
- Persistence means history is auditable

**Honest limitations (builds trust):**
- No SSO yet — internal MVP
- PDF/DOCX not supported for upload
- Copilot chat thread not restored on page refresh
- Azure Search and embeddings are scaffolded for production hardening

**Future hooks:**
- Azure DevOps work item creation from analyzer output
- Proposal Assistant for technical design drafts
- Teams integration for in-chat access

---

## Backup talking points

| Question | Answer |
|----------|--------|
| "Does it hallucinate?" | Copilot prompts require answers from retrieved chunks only; analyzers use structured JSON schemas. |
| "Can we use our own docs?" | Yes — upload `.md` or `.txt` engineering material. |
| "What if Azure is down?" | Local fallbacks allow dev/demo; production should require Azure for quality. |
| "How long to productionize?" | Auth, secret management, Search/embedding wiring, and observability are the main increments. |

---

## Sample files reference

| File | Use in demo |
|------|-------------|
| `samples/sample-docs/authentication-architecture.md` | Copilot upload + Q&A |
| `samples/sample-ticket.txt` | Ticket Analyzer |
| `samples/sample-requirement.txt` | Requirement Breakdown |
