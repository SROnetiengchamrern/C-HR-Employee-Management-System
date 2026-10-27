import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { clearToken, currentUser, loginRequest, saveToken } from "./api";
import type { UserProfile } from "./types";

interface AuthState {
  user: UserProfile | null;
  ready: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isHr: boolean;
  isAdmin: boolean;
  canApprove: boolean;
  canReport: boolean;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let active = true;
    currentUser()
      .then((profile) => {
        if (active) setUser(profile);
      })
      .catch(() => {
        clearToken();
        if (active) setUser(null);
      })
      .finally(() => {
        if (active) setReady(true);
      });
    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<AuthState>(() => {
    const roles = user?.roles ?? [];
    return {
      user,
      ready,
      isHr: roles.includes("Admin") || roles.includes("HR"),
      isAdmin: roles.includes("Admin"),
      canApprove: roles.some((role) => role === "Admin" || role === "HR" || role === "Manager"),
      canReport: roles.some((role) => role === "Admin" || role === "HR" || role === "Manager"),
      login: async (email: string, password: string) => {
        const result = await loginRequest(email, password);
        saveToken(result.token);
        setUser(result.user);
      },
      logout: () => {
        clearToken();
        setUser(null);
      }
    };
  }, [ready, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const value = useContext(AuthContext);
  if (!value) throw new Error("AuthProvider is missing.");
  return value;
}
