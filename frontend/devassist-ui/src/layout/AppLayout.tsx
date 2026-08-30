import { NavLink, useLocation } from 'react-router-dom'
import type { PropsWithChildren } from 'react'
import { appRoutes, getRouteMeta } from '../app/routes'
import { useAuth } from '../auth/authContext'

export function AppLayout({ children }: PropsWithChildren) {
  const { pathname } = useLocation()
  const route = getRouteMeta(pathname)
  const { auth, signOut, isAdmin } = useAuth()

  return (
    <div className="workspace-shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <h1>DevAssist</h1>
          <p className="sidebar__tagline">AI delivery workspace</p>
        </div>
        <nav>
          {appRoutes.map((item) => (
            <NavLink key={item.path} to={item.path} end={item.path === '/'}>
              {item.navLabel}
            </NavLink>
          ))}
          {isAdmin && (
            <NavLink to="/admin/users">User Management</NavLink>
          )}
        </nav>

        {auth && (
          <div className="sidebar__user">
            <div className="sidebar__user-avatar">{auth.displayName[0].toUpperCase()}</div>
            <div className="sidebar__user-info">
              <span className="sidebar__user-name">{auth.displayName}</span>
              <span className="sidebar__user-role">{auth.role}</span>
            </div>
            <button type="button" className="sidebar__logout" onClick={signOut} title="Sign out">
              ⎋
            </button>
          </div>
        )}
      </aside>
      <section className="workspace-main">
        <header className="topbar">
          <div>
            <span className="topbar__title">{route.title}</span>
            <span className="topbar__subtitle">{route.subtitle}</span>
          </div>
        </header>
        <main>{children}</main>
      </section>
    </div>
  )
}
