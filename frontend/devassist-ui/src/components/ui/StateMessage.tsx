type StateMessageProps = {
  variant?: 'default' | 'loading' | 'error'
  children: React.ReactNode
}

export function StateMessage({ variant = 'default', children }: StateMessageProps) {
  return <p className={`ui-state ui-state--${variant}`}>{children}</p>
}
