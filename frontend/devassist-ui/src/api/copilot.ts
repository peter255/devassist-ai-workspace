import type { AskCopilotResponse, CreateChatSessionResponse } from '../types/copilot'
import { apiBaseUrl } from './client'
import { parseApiResponse } from './parseResponse'

export async function createChatSession(title?: string): Promise<CreateChatSessionResponse> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/sessions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, createdBy: 'system' }),
  })
  return parseApiResponse<CreateChatSessionResponse>(response)
}

export async function askCopilot(sessionId: string, question: string): Promise<AskCopilotResponse> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, question }),
  })
  return parseApiResponse<AskCopilotResponse>(response)
}
