import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { analyzeTicket, listTicketAnalyses, ticketListItemToResponse } from '../api/tickets'
import { queryKeys } from '../app/queryKeys'
import { StateMessage } from '../components/ui/StateMessage'
import type { AnalyzeTicketResponse, TicketSeverity } from '../types/tickets'
import './ticket-analyzer.css'

const SAMPLE_TICKET =
  'User clicks logout but remains logged in until automatic timeout. Logout button appears to do nothing.'

export function TicketAnalyzerPage() {
  const queryClient = useQueryClient()
  const [ticketText, setTicketText] = useState('')
  const [result, setResult] = useState<AnalyzeTicketResponse | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const historyQuery = useQuery({
    queryKey: queryKeys.ticketAnalyses,
    queryFn: () => listTicketAnalyses(15),
  })

  const analyzeMutation = useMutation({
    mutationFn: (text: string) => analyzeTicket(text),
    onSuccess: async (data) => {
      setResult(data)
      setSelectedId(data.id)
      await queryClient.invalidateQueries({ queryKey: queryKeys.ticketAnalyses })
    },
  })

  const handleAnalyze = (event: React.FormEvent) => {
    event.preventDefault()
    if (!ticketText.trim() || analyzeMutation.isPending) return
    analyzeMutation.mutate(ticketText.trim())
  }

  const handleSelectHistory = (item: AnalyzeTicketResponse) => {
    setResult(item)
    setSelectedId(item.id)
  }

  return (
    <div className="ticket-page">
      <header className="ticket-hero">
        <div className="ticket-hero__glow" aria-hidden="true" />
        <p className="ticket-hero__eyebrow">Incident triage</p>
        <h2 className="ticket-hero__title">Ticket & Incident Analyzer</h2>
        <p className="ticket-hero__subtitle">
          Paste a bug report, incident note, or support ticket — get structured engineering triage in seconds.
        </p>
      </header>

      <div className="ticket-layout">
        <section className="ticket-panel">
          <form className="ticket-form" onSubmit={handleAnalyze}>
            <label className="ticket-form__label" htmlFor="ticket-text">
              Ticket / incident description
            </label>
            <textarea
              id="ticket-text"
              className="ticket-form__textarea"
              rows={10}
              value={ticketText}
              onChange={(e) => setTicketText(e.target.value)}
              placeholder="Describe the issue, steps to reproduce, and impact…"
            />
            <div className="ticket-form__actions">
              <button
                type="button"
                className="ticket-btn ticket-btn--ghost"
                onClick={() => setTicketText(SAMPLE_TICKET)}
              >
                Load sample
              </button>
              <button
                type="submit"
                className="ticket-btn ticket-btn--primary"
                disabled={!ticketText.trim() || analyzeMutation.isPending}
              >
                {analyzeMutation.isPending ? 'Analyzing…' : 'Analyze ticket'}
              </button>
            </div>
          </form>

          {analyzeMutation.isError && (
            <p className="ticket-error">{(analyzeMutation.error as Error).message}</p>
          )}

          {analyzeMutation.isPending && !result && (
            <StateMessage variant="loading">Analyzing ticket…</StateMessage>
          )}

          {result && <AnalysisResultCard result={result} />}
        </section>

        <aside className="ticket-panel ticket-history">
          <h3 className="ticket-panel__title">
            Recent analyses
            {historyQuery.data && (
              <span className="ticket-panel__count">{historyQuery.data.length}</span>
            )}
          </h3>

          {historyQuery.isLoading && <StateMessage variant="loading">Loading history…</StateMessage>}
          {historyQuery.isError && (
            <StateMessage variant="error">{(historyQuery.error as Error).message}</StateMessage>
          )}

          {historyQuery.data?.length === 0 && !historyQuery.isLoading && (
            <StateMessage>No analyses yet. Run your first ticket above.</StateMessage>
          )}

          {historyQuery.data && historyQuery.data.length > 0 && (
            <ul className="history-list">
              {historyQuery.data.map((item) => {
                const mapped = ticketListItemToResponse(item)
                return (
                  <li key={item.id}>
                    <button
                      type="button"
                      className={`history-item ui-history-item--clickable ${selectedId === item.id ? 'ui-history-item--active' : ''}`}
                      onClick={() => handleSelectHistory(mapped)}
                    >
                      <div className="history-item__top">
                        <SeverityBadge severity={item.severity} />
                        <time className="history-item__date">
                          {new Date(item.createdAt).toLocaleString()}
                        </time>
                      </div>
                      <p className="history-item__summary">{item.summary}</p>
                      <p className="history-item__meta">
                        {item.category} · {item.impactedModule}
                      </p>
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
        </aside>
      </div>
    </div>
  )
}

function AnalysisResultCard({ result }: { result: AnalyzeTicketResponse }) {
  return (
    <article className="result-card">
      <header className="result-card__header">
        <h3>Analysis result</h3>
        <SeverityBadge severity={result.severity} large />
      </header>

      <div className="result-grid">
        <ResultField label="Summary" value={result.summary} full />
        <ResultField label="Category" value={result.category} />
        <ResultField label="Impacted module" value={result.impactedModule} />
        <ResultField label="Suggested action" value={result.suggestedAction} full highlight />
      </div>
    </article>
  )
}

function ResultField({
  label,
  value,
  full,
  highlight,
}: {
  label: string
  value: string
  full?: boolean
  highlight?: boolean
}) {
  return (
    <div className={`result-field ${full ? 'result-field--full' : ''} ${highlight ? 'result-field--highlight' : ''}`}>
      <span className="result-field__label">{label}</span>
      <p className="result-field__value">{value}</p>
    </div>
  )
}

function SeverityBadge({ severity, large }: { severity: TicketSeverity; large?: boolean }) {
  return (
    <span className={`severity-badge severity-${severity.toLowerCase()} ${large ? 'severity-badge--lg' : ''}`}>
      {severity}
    </span>
  )
}
