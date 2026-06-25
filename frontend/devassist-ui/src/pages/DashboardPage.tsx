import { useQueries, useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listDocuments } from '../api/documents'
import { listRequirementAnalyses } from '../api/requirements'
import { checkHealth, getStatus } from '../api/status'
import { listTicketAnalyses } from '../api/tickets'
import { queryKeys } from '../app/queryKeys'
import { ModuleNavCard } from '../components/ui/ModuleNavCard'
import { PageHero } from '../components/ui/PageHero'
import { Panel } from '../components/ui/Panel'
import { StateMessage } from '../components/ui/StateMessage'
import { StatCard } from '../components/ui/StatCard'
import './dashboard.css'

export function DashboardPage() {
  const statusQuery = useQuery({
    queryKey: queryKeys.status,
    queryFn: getStatus,
    staleTime: 30_000,
  })

  const healthQuery = useQuery({
    queryKey: queryKeys.health,
    queryFn: checkHealth,
    staleTime: 30_000,
  })

  const [documentsQuery, ticketsQuery, requirementsQuery] = useQueries({
    queries: [
      { queryKey: queryKeys.documents, queryFn: listDocuments },
      { queryKey: queryKeys.ticketAnalyses, queryFn: () => listTicketAnalyses(5) },
      { queryKey: queryKeys.requirementAnalyses, queryFn: () => listRequirementAnalyses(5) },
    ],
  })

  const documents = documentsQuery.data ?? []
  const indexedCount = documents.filter((d) => d.status === 'Indexed').length
  const isLoadingStats =
    documentsQuery.isLoading || ticketsQuery.isLoading || requirementsQuery.isLoading
  const statsError =
    documentsQuery.error ?? ticketsQuery.error ?? requirementsQuery.error

  const apiHealthy = healthQuery.data === true
  const environment = statusQuery.data?.environment ?? '—'

  return (
    <div className="ui-page dashboard-page">
      <PageHero
        eyebrow="Internal engineering workspace"
        title="DevAssist AI Workspace"
        subtitle="Upload engineering knowledge, triage incidents, and break down feature requests — all in one place for delivery teams."
      >
        <div className="dashboard-status">
          <span className={`dashboard-status__dot ${apiHealthy ? 'dashboard-status__dot--ok' : 'dashboard-status__dot--bad'}`} />
          <span>
            API {apiHealthy ? 'healthy' : 'unreachable'}
            {statusQuery.data && ` · ${environment}`}
          </span>
        </div>
      </PageHero>

      <section className="ui-stat-grid">
        {isLoadingStats && (
          <StateMessage variant="loading">Loading workspace stats…</StateMessage>
        )}
        {statsError && !isLoadingStats && (
          <StateMessage variant="error">{(statsError as Error).message}</StateMessage>
        )}
        {!isLoadingStats && !statsError && (
          <>
            <StatCard label="Documents" value={documents.length} hint="Total uploaded" />
            <StatCard label="Indexed" value={indexedCount} hint="Ready for copilot Q&A" />
            <StatCard
              label="Ticket analyses"
              value={ticketsQuery.data?.length ?? 0}
              hint="Recent (last 5 loaded)"
            />
            <StatCard
              label="Requirement breakdowns"
              value={requirementsQuery.data?.length ?? 0}
              hint="Recent (last 5 loaded)"
            />
          </>
        )}
      </section>

      <section>
        <h3 className="dashboard-section-title">Modules</h3>
        <div className="ui-nav-grid">
          <ModuleNavCard
            to="/copilot"
            label="Knowledge Copilot"
            description="Upload docs, index content, and ask grounded questions with citations."
          />
          <ModuleNavCard
            to="/tickets"
            label="Ticket Analyzer"
            description="Paste incidents or bug reports for structured severity and triage guidance."
          />
          <ModuleNavCard
            to="/requirements"
            label="Requirement Breakdown"
            description="Turn feature requests into backend tasks, frontend work, and acceptance criteria."
          />
        </div>
      </section>

      <div className="dashboard-activity-grid">
        <Panel title="Recent ticket analyses">
          {ticketsQuery.isLoading && <StateMessage variant="loading">Loading…</StateMessage>}
          {ticketsQuery.isError && (
            <StateMessage variant="error">{(ticketsQuery.error as Error).message}</StateMessage>
          )}
          {ticketsQuery.data?.length === 0 && !ticketsQuery.isLoading && (
            <StateMessage>No ticket analyses yet.</StateMessage>
          )}
          {ticketsQuery.data && ticketsQuery.data.length > 0 && (
            <ul className="dashboard-activity-list">
              {ticketsQuery.data.map((item) => (
                <li key={item.id}>
                  <Link to="/tickets" className="dashboard-activity-item">
                    <span className="dashboard-activity-item__title">{item.summary}</span>
                    <span className="dashboard-activity-item__meta">
                      {item.severity} · {new Date(item.createdAt).toLocaleString()}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Panel>

        <Panel title="Recent requirement breakdowns">
          {requirementsQuery.isLoading && <StateMessage variant="loading">Loading…</StateMessage>}
          {requirementsQuery.isError && (
            <StateMessage variant="error">{(requirementsQuery.error as Error).message}</StateMessage>
          )}
          {requirementsQuery.data?.length === 0 && !requirementsQuery.isLoading && (
            <StateMessage>No requirement breakdowns yet.</StateMessage>
          )}
          {requirementsQuery.data && requirementsQuery.data.length > 0 && (
            <ul className="dashboard-activity-list">
              {requirementsQuery.data.map((item) => (
                <li key={item.id}>
                  <Link to="/requirements" className="dashboard-activity-item">
                    <span className="dashboard-activity-item__title">{item.functionalSummary}</span>
                    <span className="dashboard-activity-item__meta">
                      {new Date(item.createdAt).toLocaleString()}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>
    </div>
  )
}
