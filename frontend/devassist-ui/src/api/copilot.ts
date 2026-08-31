import type { AskCopilotResponse, ChatMessageItem, ChatSessionSummary, CreateChatSessionResponse } from '../types/copilot'
import { apiBaseUrl, getAuthHeaders } from './client'
import { parseApiResponse } from './parseResponse'

export async function listChatSessions(): Promise<ChatSessionSummary[]> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/sessions`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<ChatSessionSummary[]>(response)
}

export async function createChatSession(title?: string): Promise<CreateChatSessionResponse> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/sessions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ title, createdBy: 'system' }),
  })
  return parseApiResponse<CreateChatSessionResponse>(response)
}

export async function deleteChatSession(sessionId: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/sessions/${sessionId}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  })
  await parseApiResponse<object>(response)
}

export async function getChatMessages(sessionId: string): Promise<ChatMessageItem[]> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/sessions/${sessionId}/messages`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<ChatMessageItem[]>(response)
}

export async function askCopilot(sessionId: string, question: string): Promise<AskCopilotResponse> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ sessionId, question }),
  })
  return parseApiResponse<AskCopilotResponse>(response)
}

export async function askCopilotStream(
  sessionId: string,
  question: string,
  onToken: (token: string) => void,
  onDone: (citations: import('../types/copilot').Citation[]) => void,
  signal?: AbortSignal,
): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/copilot/ask-stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ sessionId, question }),
    signal,
  })

  if (!response.ok || !response.body) {
    const text = await response.text().catch(() => response.statusText)
    throw new Error(text || `Request failed with status ${response.status}`)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    const lines = buffer.split('\n')
    buffer = lines.pop() ?? ''

    for (const line of lines) {
      if (!line.startsWith('data:')) continue
      const payload = line.slice(5).trim()
      if (payload === '[DONE]') return
      try {
        const parsed = JSON.parse(payload) as { token?: string; citations?: import('../types/copilot').Citation[] }
        if (parsed.token !== undefined) onToken(parsed.token)
        if (parsed.citations !== undefined) onDone(parsed.citations)
      } catch {
        // ignore malformed SSE lines
      }
    }
  }
}
