export type DocumentType =
  | 'EngineeringSpecification'
  | 'ArchitectureDecisionRecord'
  | 'IncidentPostmortem'
  | 'Runbook'
  | 'TicketAttachment'
  | 'RequirementDocument'
  | 'Other'

export type DocumentStatus = 'Uploaded' | 'Processing' | 'Indexed' | 'Failed'

export interface DocumentSummary {
  id: string
  fileName: string
  contentType: string
  status: DocumentStatus
  documentType: DocumentType
  uploadedAt: string
  uploadedBy: string
}

export interface DocumentDetails extends DocumentSummary {
  blobPath: string
  chunkCount: number
}

export interface UploadDocumentResponse {
  id: string
  fileName: string
  status: DocumentStatus
}

export interface IndexDocumentResponse {
  id: string
  status: DocumentStatus
  chunkCount: number
}

export const documentTypeOptions: { value: DocumentType; label: string }[] = [
  { value: 'EngineeringSpecification', label: 'Engineering Specification' },
  { value: 'ArchitectureDecisionRecord', label: 'Architecture Decision Record' },
  { value: 'IncidentPostmortem', label: 'Incident Postmortem' },
  { value: 'Runbook', label: 'Runbook' },
  { value: 'TicketAttachment', label: 'Ticket Attachment' },
  { value: 'RequirementDocument', label: 'Requirement Document' },
  { value: 'Other', label: 'Other' },
]
