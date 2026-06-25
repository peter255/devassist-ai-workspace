import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { breakdownRequirement, getRequirementAnalysis, listRequirementAnalyses } from '../api/requirements'
import { queryKeys } from '../app/queryKeys'
import { StateMessage } from '../components/ui/StateMessage'
import type { BreakdownRequirementResponse } from '../types/requirements'
import './requirement-breakdown.css'

const SAMPLE_REQUIREMENT =
  'Add OTP login with SMS fallback and account lock after repeated failures.'

export function RequirementBreakdownPage() {
  const queryClient = useQueryClient()
  const [requirementText, setRequirementText] = useState('')
  const [result, setResult] = useState<BreakdownRequirementResponse | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [loadingHistoryId, setLoadingHistoryId] = useState<string | null>(null)
  const [historyLoadError, setHistoryLoadError] = useState<string | null>(null)

  const historyQuery = useQuery({
    queryKey: queryKeys.requirementAnalyses,
    queryFn: () => listRequirementAnalyses(15),
  })

  const breakdownMutation = useMutation({
    mutationFn: (text: string) => breakdownRequirement(text),
    onSuccess: async (data) => {
      setResult(data)
      setSelectedId(data.id)
      await queryClient.invalidateQueries({ queryKey: queryKeys.requirementAnalyses })
    },
  })

  const handleBreakdown = (event: React.FormEvent) => {
    event.preventDefault()
    if (!requirementText.trim() || breakdownMutation.isPending) return
    breakdownMutation.mutate(requirementText.trim())
  }

  const handleSelectHistory = async (id: string) => {
    setSelectedId(id)
    setLoadingHistoryId(id)
    setHistoryLoadError(null)
    try {
      const data = await getRequirementAnalysis(id)
      setResult(data)
    } catch (error) {
      setHistoryLoadError((error as Error).message)
    } finally {
      setLoadingHistoryId(null)
    }
  }

  return (
    <div className="req-page">
      <header className="req-hero">
        <div className="req-hero__glow" aria-hidden="true" />
        <p className="req-hero__eyebrow">Implementation planning</p>
        <h2 className="req-hero__title">Requirement Breakdown</h2>
        <p className="req-hero__subtitle">
          Paste a feature request and get an engineering-oriented breakdown — backend tasks, frontend work,
          testing, risks, and acceptance criteria.
        </p>
      </header>

      <div className="req-layout">
        <section className="req-panel">
          <form className="req-form" onSubmit={handleBreakdown}>
            <label className="req-form__label" htmlFor="requirement-text">
              Feature request / requirement
            </label>
            <textarea
              id="requirement-text"
              className="req-form__textarea"
              rows={10}
              value={requirementText}
              onChange={(e) => setRequirementText(e.target.value)}
              placeholder="Describe the feature, user goals, and constraints…"
            />
            <div className="req-form__actions">
              <button
                type="button"
                className="req-btn req-btn--ghost"
                onClick={() => setRequirementText(SAMPLE_REQUIREMENT)}
              >
                Load sample
              </button>
              <button
                type="submit"
                className="req-btn req-btn--primary"
                disabled={!requirementText.trim() || breakdownMutation.isPending}
              >
                {breakdownMutation.isPending ? 'Breaking down…' : 'Break down requirement'}
              </button>
            </div>
          </form>

          {breakdownMutation.isError && (
            <p className="req-error">{(breakdownMutation.error as Error).message}</p>
          )}

          {breakdownMutation.isPending && !result && (
            <StateMessage variant="loading">Breaking down requirement…</StateMessage>
          )}

          {loadingHistoryId && !breakdownMutation.isPending && (
            <StateMessage variant="loading">Loading analysis…</StateMessage>
          )}

          {historyLoadError && (
            <p className="req-error">{historyLoadError}</p>
          )}

          {result && !loadingHistoryId && <BreakdownResultCard result={result} />}
        </section>

        <aside className="req-panel req-history">
          <h3 className="req-panel__title">
            Recent analyses
            {historyQuery.data && (
              <span className="req-panel__count">{historyQuery.data.length}</span>
            )}
          </h3>

          {historyQuery.isLoading && <StateMessage variant="loading">Loading history…</StateMessage>}
          {historyQuery.isError && (
            <StateMessage variant="error">{(historyQuery.error as Error).message}</StateMessage>
          )}

          {historyQuery.data?.length === 0 && !historyQuery.isLoading && (
            <StateMessage>No analyses yet. Run your first breakdown above.</StateMessage>
          )}

          {historyQuery.data && historyQuery.data.length > 0 && (
            <ul className="req-history-list">
              {historyQuery.data.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className={`req-history-item ui-history-item--clickable ${selectedId === item.id ? 'ui-history-item--active' : ''}`}
                    onClick={() => handleSelectHistory(item.id)}
                    disabled={loadingHistoryId === item.id}
                  >
                    <time className="req-history-item__date">
                      {new Date(item.createdAt).toLocaleString()}
                    </time>
                    <p className="req-history-item__summary">{item.functionalSummary}</p>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </aside>
      </div>
    </div>
  )
}

function BreakdownResultCard({ result }: { result: BreakdownRequirementResponse }) {
  return (
    <article className="req-result-card">
      <header className="req-result-card__header">
        <h3>Breakdown result</h3>
        <time className="req-result-card__date">
          {new Date(result.createdAt).toLocaleString()}
        </time>
      </header>

      <section className="req-summary-block">
        <h4>Functional Summary</h4>
        <p>{result.functionalSummary}</p>
      </section>

      <div className="req-sections-grid">
        <TaskListSection title="Backend Tasks" items={result.backendTasks} variant="backend" />
        <TaskListSection title="Frontend Tasks" items={result.frontendTasks} variant="frontend" />
        <TaskListSection title="Testing Checklist" items={result.testingChecklist} variant="testing" />
        <TaskListSection title="Risks" items={result.risks} variant="risks" />
        <TaskListSection title="Assumptions" items={result.assumptions} variant="assumptions" />
        <TaskListSection title="Acceptance Criteria" items={result.acceptanceCriteria} variant="acceptance" />
      </div>
    </article>
  )
}

function TaskListSection({
  title,
  items,
  variant,
}: {
  title: string
  items: string[]
  variant: 'backend' | 'frontend' | 'testing' | 'risks' | 'assumptions' | 'acceptance'
}) {
  return (
    <section className={`req-section req-section--${variant}`}>
      <h4 className="req-section__title">{title}</h4>
      {items.length === 0 ? (
        <p className="req-section__empty">None identified</p>
      ) : (
        <ul className="req-section__list">
          {items.map((item, index) => (
            <li key={`${title}-${index}`}>{item}</li>
          ))}
        </ul>
      )}
    </section>
  )
}
