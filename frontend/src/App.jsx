import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ProtectedRoute } from "./components/layout/AppLayout";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import DashboardPage from "./pages/DashboardPage";
import ImportFlowPage from "./pages/ImportFlowPage";
import DocumentationListPage from "./pages/DocumentationListPage";
import DocumentationDetailPage from "./pages/DocumentationDetailPage";
import AdminUsersPage from "./pages/AdminUsersPage";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/connexion" element={<LoginPage />} />
          <Route path="/inscription" element={<RegisterPage />} />

          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/importer" element={<ImportFlowPage />} />
            <Route path="/documentations" element={<DocumentationListPage />} />
            <Route path="/documentations/:id" element={<DocumentationDetailPage />} />
          </Route>

          <Route element={<ProtectedRoute adminOnly />}>
            <Route path="/administration" element={<AdminUsersPage />} />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
