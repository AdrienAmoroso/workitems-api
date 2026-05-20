export type UserRole = 'Admin' | 'Member' | 'Viewer';

export interface User {
  username: string;
  email: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
  expiresAt: string;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}
