export interface AppRoute {
  path: string
  title: string
  subtitle: string
  navLabel: string
}

export const appRoutes: AppRoute[] = [
  {
    path: '/',
    title: 'Dashboard',
    subtitle: 'DevAssist AI Workspace overview',
    navLabel: 'Dashboard',
  },
  {
    path: '/copilot',
    title: 'Knowledge Copilot',
    subtitle: 'Upload documents and ask grounded engineering questions',
    navLabel: 'Knowledge Copilot',
  },
  {
    path: '/tickets',
    title: 'Ticket Analyzer',
    subtitle: 'Structured incident and bug triage',
    navLabel: 'Ticket Analyzer',
  },
  {
    path: '/requirements',
    title: 'Requirement Breakdown',
    subtitle: 'Implementation planning from feature requests',
    navLabel: 'Requirement Breakdown',
  },
]

export function getRouteMeta(pathname: string): AppRoute {
  const match = appRoutes.find((route) =>
    route.path === '/' ? pathname === '/' : pathname.startsWith(route.path),
  )
  return match ?? appRoutes[0]
}
