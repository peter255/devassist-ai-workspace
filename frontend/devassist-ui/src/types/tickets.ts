export type TicketSeverity = 'Low' | 'Medium' | 'High' | 'Critical'

export interface AnalyzeTicketResponse {
  id: string
  summary: string
  severity: TicketSeverity
  category: string
  impactedModule: string
  suggestedAction: string
  createdAt: string
}

export interface TicketAnalysisListItem {
  id: string
  summary: string
  severity: TicketSeverity
  category: string
  impactedModule: string
  suggestedAction: string
  createdAt: string
}
