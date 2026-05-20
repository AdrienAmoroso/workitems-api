import { HttpClient } from '@angular/common/http';
import { computed, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User, UserRole } from '../models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';

  // Signals for reactive state
  private currentUserSignal = signal<User | null>(this.loadUserFromStorage());
  private isAuthenticatedSignal = signal<boolean>(this.hasValidToken());

  // Public computed signals
  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => this.isAuthenticatedSignal());

  /** The role stored in the current user's JWT. Null when not logged in. */
  readonly userRole = computed(() => this.currentUserSignal()?.role ?? null);

  /** True only for Admins — controls destructive actions (Delete). */
  readonly isAdmin = computed(() => this.userRole() === 'Admin');

  /** True for Members and Admins — controls write actions (Create, Edit). */
  readonly canManage = computed(() => this.userRole() === 'Admin' || this.userRole() === 'Member');

  constructor(private http: HttpClient) {}

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(tap((response) => this.handleAuthResponse(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, request)
      .pipe(tap((response) => this.handleAuthResponse(response)));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSignal.set(null);
    this.isAuthenticatedSignal.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    const role = this.decodeRoleFromToken(response.token);
    const user: User = {
      username: response.username,
      email: response.email,
      role,
    };
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUserSignal.set(user);
    this.isAuthenticatedSignal.set(true);
  }

  private loadUserFromStorage(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  }

  private hasValidToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Decodes the JWT payload (no signature verification — the API handles that).
   * The role is stored under the standard .NET claim key that maps to "role" in JWT.
   * Returns 'Member' as a safe default if the claim is absent or unrecognised.
   */
  private decodeRoleFromToken(token: string): UserRole {
    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
      // ASP.NET Core maps ClaimTypes.Role → "role" in the JWT payload
      const roleClaimKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
      const role: string = decoded[roleClaimKey] ?? decoded['role'] ?? 'Member';
      if (role === 'Admin' || role === 'Member' || role === 'Viewer') {
        return role;
      }
      return 'Member';
    } catch {
      return 'Member';
    }
  }
}
