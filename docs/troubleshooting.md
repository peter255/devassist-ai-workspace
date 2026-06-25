# Troubleshooting

Common issues when running DevAssist AI Workspace locally.

---

## NuGet restore warnings (NU1900 / unreachable feed)

**Symptom:** `Unable to load the service index for source https://devops.nupco.com/...`

**Cause:** A machine-wide or corporate NuGet feed is configured but unreachable.

**Fix:** This repository includes `nuget.config` at the root, which clears extra feeds and uses **nuget.org** only when restoring from the repo directory.

```bash
dotnet restore DevAssist.sln
```

If you need a corporate feed, add it to `nuget.config` without removing nuget.org.

---

## SQL Server connection failed

**Symptom:** API fails on startup with database connection errors.

**Checks:**
1. Docker is running: `docker compose up -d`
2. Port `1433` is not blocked or used by another instance
3. Connection string in `appsettings.json` matches `docker-compose.yml` SA password
4. Wait ~30 seconds after first `docker compose up` for SQL Server to initialize

**Test connection:**
```bash
docker logs devassist-sqlserver
```

---

## API runs but frontend shows "API unreachable"

**Checks:**
1. API is running on `http://localhost:5147`
2. Frontend dev server is on `http://localhost:5173`
3. Vite proxy is configured in `frontend/devassist-ui/vite.config.ts`
4. `VITE_API_BASE_URL` is empty in local dev (uses proxy)

---

## Document indexing fails

| Cause | Resolution |
|-------|------------|
| Unsupported file type (`.pdf`, `.docx`) | Use `.txt` or `.md` sample files |
| Empty file | Upload non-empty content |
| Storage path permissions | Ensure `./data/documents` is writable (local fallback) |

---

## Copilot returns generic / empty answers

| Cause | Resolution |
|-------|------------|
| No indexed documents | Upload and **Index** a document first |
| Azure OpenAI not configured | Expected in local mode — uses `LocalGroundedChatService` |
| No matching chunks | Try questions aligned with uploaded doc content |

---

## Ticket / requirement analysis looks heuristic

**Expected** when `AzureOpenAi__Endpoint` and `AzureOpenAi__ApiKey` are empty. Local fallback services use keyword-based templates.

Configure Azure OpenAI in `appsettings.json` or environment variables for production-quality structured output.

---

## Migrations

Migrations apply automatically in **Development** on API startup.

Manual apply:
```bash
dotnet ef database update --project src/DevAssist.Infrastructure --startup-project src/DevAssist.Api
```

---

## HTTPS redirect in local dev

The API calls `UseHttpsRedirection()`. If you hit HTTPS issues, use the `http` launch profile or access `http://localhost:5147` directly (matches Vite proxy).

---

## Azure integration placeholders

These components are intentionally scaffolded until credentials are provided:

- Azure AI Search indexer and retriever
- Azure OpenAI embeddings
- Full Azure OpenAI embedding service implementation

Local fallbacks are documented in [architecture.md](architecture.md) and [README.md](../README.md).
