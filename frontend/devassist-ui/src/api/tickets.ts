import type { AnalyzeTicketResponse, TicketAnalysisListItem } from '../types/tickets'
import { apiBaseUrl, getAuthHeaders } from './client'
import { parseApiResponse } from './parseResponse'

export async function analyzeTicket(text: string): Promise<AnalyzeTicketResponse> {
  const authHeaders = await getAuthHeaders()
  const response = await fetch(`${apiBaseUrl}/api/tickets/analyze`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders },
    body: JSON.stringify({ text }),
  })
  return parseApiResponse<AnalyzeTicketResponse>(response)
}

export async function listTicketAnalyses(limit = 20): Promise<TicketAnalysisListItem[]> {
  const response = await fetch(`${apiBaseUrl}/api/tickets/analyses?limit=${limit}`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<TicketAnalysisListItem[]>(response)
}

export function ticketListItemToResponse(item: TicketAnalysisListItem): AnalyzeTicketResponse {
  return {
    id: item.id,
    summary: item.summary,
    severity: item.severity,
    category: item.category,
    impactedModule: item.impactedModule,
    suggestedAction: item.suggestedAction,
    createdAt: item.createdAt,
  }
}
