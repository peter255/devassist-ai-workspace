import type { DocumentType } from '../../types/documents'
import { documentTypeOptions } from '../../types/documents'

type DocumentTypePickerProps = {
  value: DocumentType
  onChange: (value: DocumentType) => void
}

const typeIcons: Record<DocumentType, string> = {
  EngineeringSpecification: '⚙️',
  ArchitectureDecisionRecord: '🏛️',
  IncidentPostmortem: '🔥',
  Runbook: '📋',
  TicketAttachment: '🎫',
  RequirementDocument: '📝',
  Other: '📄',
}

export function DocumentTypePicker({ value, onChange }: DocumentTypePickerProps) {
  return (
    <div className="type-picker">
      <span className="type-picker__label">Document category</span>
      <div className="type-picker__grid" role="radiogroup" aria-label="Document type">
        {documentTypeOptions.map((option) => {
          const selected = value === option.value
          return (
            <button
              key={option.value}
              type="button"
              role="radio"
              aria-checked={selected}
              className={`type-chip ${selected ? 'type-chip--active' : ''}`}
              onClick={() => onChange(option.value)}
            >
              <span className="type-chip__icon" aria-hidden="true">
                {typeIcons[option.value]}
              </span>
              <span className="type-chip__text">{option.label}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}
