type PageHeroProps = {
  eyebrow?: string
  title: string
  subtitle: string
  children?: React.ReactNode
}

export function PageHero({ eyebrow, title, subtitle, children }: PageHeroProps) {
  return (
    <header className="ui-hero">
      {eyebrow && <p className="ui-hero__eyebrow">{eyebrow}</p>}
      <h2 className="ui-hero__title">{title}</h2>
      <p className="ui-hero__subtitle">{subtitle}</p>
      {children}
    </header>
  )
}
