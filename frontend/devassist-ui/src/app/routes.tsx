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

const adminRoutes: AppRoute[] = [
  {
    path: '/admin/users',
    title: 'User Management',
    subtitle: 'Manage users and permissions',
    navLabel: 'User Management',
  },
]

const allRoutes = [...appRoutes, ...adminRoutes]

export function getRouteMeta(pathname: string): AppRoute {
  const match = allRoutes.find((route) =>
    route.path === '/' ? pathname === '/' : pathname.startsWith(route.path),
  )
  return match ?? appRoutes[0]
}
