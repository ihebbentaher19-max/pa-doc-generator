import { createContext, useMemo, useState } from "react";
import { getStoredToken, getStoredUser } from "../services/api";
import * as authService from "../services/authService";

export const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => getStoredToken());
  const [user, setUser] = useState(() => getStoredUser());

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: !!token,
      isAdmin: user?.role === "Administrateur",
      async login(email, password) {
        const result = await authService.login(email, password);
        setToken(getStoredToken());
        setUser(getStoredUser());
        return result;
      },
      async register(fullName, email, password) {
        const result = await authService.register(fullName, email, password);
        setToken(getStoredToken());
        setUser(getStoredUser());
        return result;
      },
      logout() {
        authService.logout();
        setToken(null);
        setUser(null);
      },
    }),
    [user, token]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
