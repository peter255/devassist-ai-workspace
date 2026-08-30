export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

const AUTH_STORAGE_KEY = 'devassist.auth'

// Returns Authorization header — reads directly from localStorage so it works
// synchronously on first render before any React context or effect has run.
export async function getAuthHeaders(): Promise<Record<string, string>> {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) return {}
    const stored = JSON.parse(raw) as { token: string; expiresAt: string }
    if (!stored?.token) return {}
    if (new Date(stored.expiresAt) < new Date()) {
      localStorage.removeItem(AUTH_STORAGE_KEY)
      return {}
    }
    return { Authorization: `Bearer ${stored.token}` }
  } catch {
    return {}
  }
}

// Kept for backward-compatibility (msalConfig.ts still calls this).
// The local-auth path no longer needs it since getAuthHeaders reads localStorage directly.
export function registerTokenProvider(_provider: () => Promise<string | null>): void {
  // no-op — token is read directly from localStorage
}
