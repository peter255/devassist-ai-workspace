import type { ApiResponse } from '../types/api'

export async function parseApiResponse<T>(response: Response): Promise<T> {
  // Read as text first so we can handle empty bodies (e.g. 401 with no content)
  // without crashing on response.json().
  let text: string
  try {
    text = await response.text()
  } catch {
    throw new Error(`Request failed with status ${response.status}`)
  }

  if (!text.trim()) {
    // Empty body — treat as auth error or generic failure.
    if (response.status === 401) throw new Error('Unauthorized — please sign in again.')
    if (response.status === 403) throw new Error('Access denied.')
    throw new Error(`Request failed with status ${response.status}`)
  }

  let payload: ApiResponse<T>
  try {
    payload = JSON.parse(text) as ApiResponse<T>
  } catch {
    throw new Error(`Unexpected response from server (status ${response.status})`)
  }

  if (!response.ok || !payload.success || payload.data === null || payload.data === undefined) {
    const message = payload.error ?? `Request failed with status ${response.status}`
    if (response.status === 401) throw new Error(message || 'Unauthorized — please sign in again.')
    throw new Error(message)
  }

  return payload.data
}
