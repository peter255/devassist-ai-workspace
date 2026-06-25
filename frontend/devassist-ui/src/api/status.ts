import { apiBaseUrl } from './client'

export interface StatusResponse {
  service: string
  environment: string
  utcNow: string
}

export async function getStatus(): Promise<StatusResponse> {
  const response = await fetch(`${apiBaseUrl}/api/status`)
  if (!response.ok) {
    throw new Error(`Status check failed: ${response.status}`)
  }
  return response.json() as Promise<StatusResponse>
}

export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(`${apiBaseUrl}/health`)
    return response.ok
  } catch {
    return false
  }
}
