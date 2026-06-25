import type { ApiResponse } from '../types/api'

export async function parseApiResponse<T>(response: Response): Promise<T> {
  const payload = (await response.json()) as ApiResponse<T>
  if (!response.ok || !payload.success || payload.data === null) {
    throw new Error(payload.error ?? `Request failed with status ${response.status}`)
  }
  return payload.data
}
