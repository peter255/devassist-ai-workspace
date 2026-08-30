import { PublicClientApplication, type Configuration, type SilentRequest } from '@azure/msal-browser'
import { registerTokenProvider } from '../api/client'

const clientId = import.meta.env.VITE_AAD_CLIENT_ID as string | undefined
const tenantId = import.meta.env.VITE_AAD_TENANT_ID as string | undefined
const scope = import.meta.env.VITE_AAD_SCOPE as string | undefined

// Auth is optional — when env vars are absent, the app runs unauthenticated (local dev).
export const isAuthEnabled = Boolean(clientId && tenantId)

const msalConfig: Configuration = {
  auth: {
    clientId: clientId ?? 'not-configured',
    authority: `https://login.microsoftonline.com/${tenantId ?? 'common'}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
}

export const msalInstance = new PublicClientApplication(msalConfig)

export const loginRequest = {
  scopes: scope ? [scope] : ['openid', 'profile'],
}

// Register the MSAL token provider with the API client so all fetch calls
// automatically carry a Bearer token when auth is enabled.
if (isAuthEnabled) {
  registerTokenProvider(async () => {
    await msalInstance.initialize()
    const accounts = msalInstance.getAllAccounts()
    if (accounts.length === 0) return null

    const silentRequest: SilentRequest = {
      ...loginRequest,
      account: accounts[0],
    }

    try {
      const result = await msalInstance.acquireTokenSilent(silentRequest)
      return result.accessToken
    } catch {
      return null
    }
  })
}
