import type { LoginResponse, UserProfile } from "./types";

const tokenKey = "corehr_token";

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export function authToken(): string | null {
  return localStorage.getItem(tokenKey);
}

export function saveToken(token: string) {
  localStorage.setItem(tokenKey, token);
}

export function clearToken() {
  localStorage.removeItem(tokenKey);
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  const token = authToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const base = import.meta.env.VITE_API_URL ?? "";
  const response = await fetch(`${base}${path}`, { ...options, headers });
  if (response.status === 401 && !path.startsWith("/api/auth/login")) {
    clearToken();
    if (!window.location.pathname.startsWith("/login")) window.location.assign("/login");
  }
  if (!response.ok) {
    throw new ApiError(response.status, await readError(response));
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

async function readError(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return response.statusText || "Request failed.";
  try {
    const json = JSON.parse(text) as { message?: string; title?: string; detail?: string; errors?: Record<string, string[]> };
    if (json.message) return json.message;
    if (json.detail) return json.detail;
    if (json.errors) return Object.values(json.errors).flat().join(" ");
    if (json.title) return json.title;
  } catch {
    return text;
  }
  return text;
}

export async function loginRequest(email: string, password: string): Promise<LoginResponse> {
  return api<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
}

export async function currentUser(): Promise<UserProfile> {
  return api<UserProfile>("/api/auth/me");
}

export async function downloadCsv(path: string, filename: string) {
  const headers = new Headers();
  const token = authToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const base = import.meta.env.VITE_API_URL ?? "";
  const response = await fetch(`${base}${path}`, { headers });
  if (!response.ok) throw new ApiError(response.status, await readError(response));
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}
