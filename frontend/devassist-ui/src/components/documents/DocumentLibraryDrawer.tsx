import { useEffect, type PropsWithChildren } from 'react'

type DocumentLibraryDrawerProps = PropsWithChildren<{
  open: boolean
  onClose: () => void
  title?: string
}>

export function DocumentLibraryDrawer({
  open,
  onClose,
  title = 'Document library',
  children,
}: DocumentLibraryDrawerProps) {
  useEffect(() => {
    if (!open) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }

    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open, onClose])

  if (!open) return null

  return (
    <aside className="doc-drawer" role="dialog" aria-modal="true" aria-labelledby="doc-drawer-title">
      <header className="doc-drawer__header">
        <div>
          <p className="doc-drawer__eyebrow">Knowledge base</p>
          <h2 id="doc-drawer-title" className="doc-drawer__title">{title}</h2>
        </div>
        <button type="button" className="doc-drawer__close" onClick={onClose} aria-label="Close">
          ✕
        </button>
      </header>
      <div className="doc-drawer__body">{children}</div>
    </aside>
  )
}
