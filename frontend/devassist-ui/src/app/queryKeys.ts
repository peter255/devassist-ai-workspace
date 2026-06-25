export const queryKeys = {
  status: ['status'] as const,
  health: ['health'] as const,
  documents: ['documents'] as const,
  document: (id: string) => ['documents', id] as const,
  ticketAnalyses: ['ticketAnalyses'] as const,
  requirementAnalyses: ['requirementAnalyses'] as const,
  requirementAnalysis: (id: string) => ['requirementAnalyses', id] as const,
}
