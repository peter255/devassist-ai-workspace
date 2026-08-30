import type { BreakdownRequirementResponse, RequirementAnalysisListItem } from '../types/requirements'
import { apiBaseUrl, getAuthHeaders } from './client'
import { parseApiResponse } from './parseResponse'

export async function breakdownRequirement(text: string): Promise<BreakdownRequirementResponse> {
  const authHeaders = await getAuthHeaders()
  const response = await fetch(`${apiBaseUrl}/api/requirements/breakdown`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders },
    body: JSON.stringify({ text }),
  })
  return parseApiResponse<BreakdownRequirementResponse>(response)
}

export async function listRequirementAnalyses(limit = 20): Promise<RequirementAnalysisListItem[]> {
  const response = await fetch(`${apiBaseUrl}/api/requirements/analyses?limit=${limit}`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<RequirementAnalysisListItem[]>(response)
}

export async function getRequirementAnalysis(id: string): Promise<BreakdownRequirementResponse> {
  const response = await fetch(`${apiBaseUrl}/api/requirements/analyses/${id}`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<BreakdownRequirementResponse>(response)
}
