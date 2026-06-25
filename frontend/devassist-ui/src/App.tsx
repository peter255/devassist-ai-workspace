import { Navigate, Route, Routes } from 'react-router-dom'
import './app/styles.css'
import { AppLayout } from './layout/AppLayout'
import { DashboardPage } from './pages/DashboardPage'
import { KnowledgeCopilotPage } from './pages/KnowledgeCopilotPage'
import { RequirementBreakdownPage } from './pages/RequirementBreakdownPage'
import { TicketAnalyzerPage } from './pages/TicketAnalyzerPage'

function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/copilot" element={<KnowledgeCopilotPage />} />
        <Route path="/tickets" element={<TicketAnalyzerPage />} />
        <Route path="/requirements" element={<RequirementBreakdownPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppLayout>
  )
}

export default App
