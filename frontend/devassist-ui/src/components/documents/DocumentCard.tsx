import type { DocumentSummary } from '../../types/documents'

type DocumentCardProps = {
  document: DocumentSummary
  isSelected: boolean
  isIndexing: boolean
  onSelect: () => void
  onIndex: () => void
}

const statusLabels: Record<string, string> = {
  Uploaded: 'Uploaded',
  Processing: 'Indexing…',
  Indexed: 'Ready',
  Failed: 'Failed',
}

export function DocumentCard({
  document,
  isSelected,
  isIndexing,
  onSelect,
  onIndex,
}: DocumentCardProps) {
  const statusClass = document.status.toLowerCase()
  const canIndex = document.status === 'Uploaded' || document.status === 'Failed'
  const indexLabel = isIndexing ? '…' : document.status === 'Indexed' ? 'Indexed' : 'Index'

  return (
    <article className={`doc-card ${isSelected ? 'doc-card--selected' : ''}`}>
      <div className="doc-card__icon" aria-hidden="true">
        {document.fileName.endsWith('.md') ? '◈' : document.fileName.endsWith('.pdf') ? '▣' : '◇'}
      </div>
      <div className="doc-card__body">
        <h4 className="doc-card__name">{document.fileName}</h4>
        <p className="doc-card__type">{document.documentType.replace(/([A-Z])/g, ' $1').trim()}</p>
        <p className="doc-card__date">{new Date(document.uploadedAt).toLocaleString()}</p>
      </div>
      <div className="doc-card__aside">
        <span className={`status-pill status-${statusClass}`}>
          <span className="status-pill__dot" />
          {statusLabels[document.status] ?? document.status}
        </span>
        <div className="doc-card__actions">
          <button type="button" className="ghost-btn" onClick={onSelect}>
            Details
          </button>
          <button
            type="button"
            className="accent-btn"
            disabled={isIndexing || document.status === 'Processing' || !canIndex}
            onClick={onIndex}
            title={!canIndex && document.status === 'Indexed' ? 'Document is already indexed' : undefined}
          >
            {indexLabel}
          </button>
        </div>
      </div>
    </article>
  )
}
