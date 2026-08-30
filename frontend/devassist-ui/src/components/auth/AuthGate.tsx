import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { isAuthEnabled, loginRequest } from '../../auth/msalConfig'

type AuthGateProps = {
  children: React.ReactNode
}

/**
 * Wraps the app content. When Entra ID is configured, unauthenticated users
 * see a login prompt instead of the app. When auth is disabled (local dev),
 * children are always rendered.
 */
export function AuthGate({ children }: AuthGateProps) {
  if (!isAuthEnabled) return <>{children}</>

  return (
    <>
      <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <LoginScreen />
      </UnauthenticatedTemplate>
    </>
  )
}

function LoginScreen() {
  const { instance } = useMsal()

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch(console.error)
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh', gap: '1rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 600 }}>DevAssist AI Workspace</h1>
      <p style={{ color: '#64748b' }}>Sign in with your Microsoft account to continue.</p>
      <button
        onClick={handleLogin}
        style={{
          padding: '0.625rem 1.5rem',
          background: '#0078d4',
          color: '#fff',
          border: 'none',
          borderRadius: '0.375rem',
          cursor: 'pointer',
          fontSize: '0.95rem',
          fontWeight: 500,
        }}
      >
        Sign in with Microsoft
      </button>
    </div>
  )
}
