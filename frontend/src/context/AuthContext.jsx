import { createContext, useEffect, useMemo, useState } from "react";
import { getStoredToken, setStoredToken } from "../services/api";
import * as authService from "../services/authService";

export const AuthContext = createContext(null);

function decodeUserFromToken(token) {
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return {
      fullName: payload["name"] || payload["unique_name"] || "",
      email: payload["email"] || "",
      role: payload["role"] || "Utilisateur",
    };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => getStoredToken());
  const [user, setUser] = useState(() => decodeUserFromToken(getStoredToken()));

  useEffect(() => {
    setUser(decodeUserFromToken(token));
  }, [token]);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: !!token,
      isAdmin: user?.role === "Administrateur",
      async login(email, password) {
        const result = await authService.login(email, password);
        setToken(getStoredToken());
        return result;
      },
      async register(fullName, email, password) {
        const result = await authService.register(fullName, email, password);
        setToken(getStoredToken());
        return result;
      },
      logout() {
        setStoredToken(null);
        setToken(null);
      },
    }),
    [user, token]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
