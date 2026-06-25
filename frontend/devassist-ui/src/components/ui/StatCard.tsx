type StatCardProps = {
  label: string
  value: string | number
  hint?: string
}

export function StatCard({ label, value, hint }: StatCardProps) {
  return (
    <article className="ui-stat-card">
      <span className="ui-stat-card__label">{label}</span>
      <p className="ui-stat-card__value">{value}</p>
      {hint && <p className="ui-stat-card__hint">{hint}</p>}
    </article>
  )
}
