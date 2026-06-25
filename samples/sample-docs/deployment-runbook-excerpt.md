# Deployment Runbook — Customer Portal (Excerpt)

**Document type:** Runbook  
**Application:** customer-portal (React SPA + auth-service + customer-api)  
**Last updated:** 2026-01-08

---

## Pre-deployment checklist

- [ ] All migrations applied in staging
- [ ] Smoke tests green on staging
- [ ] Feature flags reviewed (LaunchDarkly)
- [ ] On-call engineer assigned for 2-hour watch window

---

## Standard deployment (Azure App Service)

1. Merge to `release/*` branch — pipeline `portal-deploy` triggers
2. Pipeline builds SPA artifact + API packages
3. Deploy to **staging** slot → run automated smoke tests
4. Swap staging → production (blue-green)
5. Monitor App Insights for 15 minutes: error rate, response time, login success rate

---

## Rollback procedure (failed deployment)

**When to rollback:** Error rate > 2% for 5 minutes, login failure spike, or critical functional regression.

### Steps

1. **Immediate:** Swap production slot back to previous staging artifact (slot swap reverse)
   ```bash
   az webapp deployment slot swap --name customer-portal-api --resource-group rg-portal --slot staging --action swap
   ```
2. Notify `#incidents` channel with deployment ID and symptom
3. If database migration ran, assess whether backward-compatible:
   - Compatible: no further action
   - Breaking: execute documented down migration script from `db/migrations/rollback/`
4. Create incident ticket with timeline
5. Block redeploy until root cause documented

### Frontend-only rollback

- Redeploy previous SPA build from pipeline run artifact `build-{id}`
- Purge CDN cache for `static/*` paths

---

## Post-deployment verification

| Check | Expected |
|-------|----------|
| `GET /health` | 200 |
| Login + logout flow | Session cleared after logout API |
| Sample checkout | Order reaches `Paid` in test account |

---

## Contacts

| Role | Channel |
|------|---------|
| Platform on-call | PagerDuty `portal-oncall` |
| DBA | `#database-team` |
| Identity | `#platform-identity` |
