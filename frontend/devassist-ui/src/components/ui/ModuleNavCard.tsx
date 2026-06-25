import { Link } from 'react-router-dom'

type ModuleNavCardProps = {
  to: string
  label: string
  description: string
}

export function ModuleNavCard({ to, label, description }: ModuleNavCardProps) {
  return (
    <Link to={to} className="ui-nav-card">
      <span className="ui-nav-card__label">{label}</span>
      <p className="ui-nav-card__desc">{description}</p>
      <span className="ui-nav-card__cta">Open module →</span>
    </Link>
  )
}
