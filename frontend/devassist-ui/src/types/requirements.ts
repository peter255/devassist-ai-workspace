export interface BreakdownRequirementResponse {
  id: string
  functionalSummary: string
  backendTasks: string[]
  frontendTasks: string[]
  testingChecklist: string[]
  risks: string[]
  assumptions: string[]
  acceptanceCriteria: string[]
  createdAt: string
}

export interface RequirementAnalysisListItem {
  id: string
  functionalSummary: string
  createdAt: string
}
