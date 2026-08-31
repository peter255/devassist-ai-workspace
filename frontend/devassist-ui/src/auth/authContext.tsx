import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { getCurrentUser } from '../api/auth'
import type { StoredAuth } from '../types/auth'

const STORAGE_KEY = 'devassist.auth'

interface AuthContextValue {
  auth: StoredAuth | null
  signIn: (auth: StoredAuth) => void
  signOut: () => void
  isAdmin: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return null
      const stored = JSON.parse(raw) as StoredAuth
      if (new Date(stored.expiresAt) < new Date()) {
        localStorage.removeItem(STORAGE_KEY)
        return null
      }
      return stored
    } catch {
      return null
    }
  })

  const signIn = useCallback((newAuth: StoredAuth) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(newAuth))
    setAuth(newAuth)
  }, [])

  const signOut = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY)
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith('devassist.copilot.sessionId.')) {
        localStorage.removeItem(key)
      }
    }
    setAuth(null)
  }, [])

  useEffect(() => {
    if (!auth) return

    let cancelled = false
    void getCurrentUser()
      .then((me) => {
        if (cancelled) return
        const refreshed: StoredAuth = {
          token: me.token,
          username: me.username,
          displayName: me.displayName,
          role: me.role,
          expiresAt: me.expiresAt,
        }
        localStorage.setItem(STORAGE_KEY, JSON.stringify(refreshed))
        setAuth(refreshed)
      })
      .catch(() => {
        if (!cancelled) signOut()
      })

    return () => {
      cancelled = true
    }
  }, [])

  const isAdmin = auth?.role === 'Admin'

  return (
    <AuthContext.Provider value={{ auth, signIn, signOut, isAdmin }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
