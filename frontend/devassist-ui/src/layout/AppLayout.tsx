import { NavLink, useLocation } from 'react-router-dom'
import type { PropsWithChildren } from 'react'
import { appRoutes, getRouteMeta } from '../app/routes'

export function AppLayout({ children }: PropsWithChildren) {
  const { pathname } = useLocation()
  const route = getRouteMeta(pathname)

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
        </nav>
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
