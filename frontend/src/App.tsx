import { Navigate, Route, Routes } from "react-router-dom";
import { Layout } from "./components/Layout";
import { RequireAuth } from "./RequireAuth";
import { AttendancePage } from "./pages/AttendancePage";
import { DashboardPage } from "./pages/DashboardPage";
import { EmployeesPage } from "./pages/EmployeesPage";
import { LeavePage } from "./pages/LeavePage";
import { LoginPage } from "./pages/LoginPage";
import { AdminPage, BenefitsPage, OnboardingPage, PerformancePage, RecruitmentPage, TemplatesPage, TrainingPage } from "./pages/ModulesPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { OrganizationPage } from "./pages/OrganizationPage";
import { ReportsPage } from "./pages/ReportsPage";

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="people" element={<EmployeesPage />} />
          <Route path="leave" element={<LeavePage />} />
          <Route path="recruitment" element={<RecruitmentPage />} />
          <Route path="onboarding" element={<OnboardingPage />} />
          <Route path="templates" element={<TemplatesPage />} />
          <Route path="training" element={<TrainingPage />} />
          <Route path="benefits" element={<BenefitsPage />} />
          <Route path="performance" element={<PerformancePage />} />
          <Route path="attendance" element={<AttendancePage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="organization" element={<OrganizationPage />} />
          <Route path="administration" element={<AdminPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
