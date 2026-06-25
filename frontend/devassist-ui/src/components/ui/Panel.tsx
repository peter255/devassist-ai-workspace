type PanelProps = {
  title: string
  count?: number
  children: React.ReactNode
  className?: string
}

export function Panel({ title, count, children, className = '' }: PanelProps) {
  return (
    <section className={`ui-panel ${className}`.trim()}>
      <h3 className="ui-panel__title">
        {title}
        {count !== undefined && <span className="ui-panel__count">{count}</span>}
      </h3>
      {children}
    </section>
  )
}
