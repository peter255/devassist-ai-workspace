import { Navigate, Route, Routes } from 'react-router-dom'
import './app/styles.css'
import { useAuth } from './auth/authContext'
import { AppLayout } from './layout/AppLayout'
import { AdminUsersPage } from './pages/AdminUsersPage'
import { DashboardPage } from './pages/DashboardPage'
import { KnowledgeCopilotPage } from './pages/KnowledgeCopilotPage'
import { LoginPage } from './pages/LoginPage'
import { RequirementBreakdownPage } from './pages/RequirementBreakdownPage'
import { TicketAnalyzerPage } from './pages/TicketAnalyzerPage'

function App() {
  const { auth, isAdmin } = useAuth()

  if (!auth) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    )
  }

  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/copilot" element={<KnowledgeCopilotPage />} />
        <Route path="/tickets" element={<TicketAnalyzerPage />} />
        <Route path="/requirements" element={<RequirementBreakdownPage />} />
        {isAdmin && <Route path="/admin/users" element={<AdminUsersPage />} />}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppLayout>
  )
}

export default App
