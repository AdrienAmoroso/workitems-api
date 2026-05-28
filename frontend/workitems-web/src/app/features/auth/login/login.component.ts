import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, SignalRService } from '../../../core/services';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  template: `
    <div class="auth-layout">
      <!-- Left brand panel -->
      <div class="brand-panel">
        <div class="brand-content">
          <div class="brand-logo">
            <mat-icon class="brand-icon">task_alt</mat-icon>
            <span class="brand-name">Work Items</span>
          </div>
          <p class="brand-tagline">
            Track your team's tasks in one place. Built as a .NET 10 and Angular 19 portfolio
            project.
          </p>
          <ul class="brand-features">
            <li>
              <mat-icon class="feature-icon">bolt</mat-icon>
              Real-time updates via SignalR
            </li>
            <li>
              <mat-icon class="feature-icon">shield</mat-icon>
              Role-based access control
            </li>
            <li>
              <mat-icon class="feature-icon">code</mat-icon>
              Clean Architecture + JWT auth
            </li>
          </ul>
          <div class="brand-stack">
            <span class="stack-badge">.NET 10</span>
            <span class="stack-badge">Angular 19</span>
            <span class="stack-badge">PostgreSQL</span>
          </div>
        </div>
      </div>

      <!-- Right form panel -->
      <div class="form-panel">
        <div class="form-content">
          <div class="form-header">
            <h1 class="form-title">Welcome back</h1>
            <p class="form-subtitle">Sign in to your account to continue</p>
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username or Email</mat-label>
              <input
                matInput
                formControlName="usernameOrEmail"
                placeholder="Enter username or email"
                autocomplete="username"
              />
              @if (
                loginForm.get('usernameOrEmail')?.hasError('required') &&
                loginForm.get('usernameOrEmail')?.touched
              ) {
                <mat-error>Username or email is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Password</mat-label>
              <input
                matInput
                type="password"
                formControlName="password"
                placeholder="Enter password"
                autocomplete="current-password"
              />
              @if (
                loginForm.get('password')?.hasError('required') &&
                loginForm.get('password')?.touched
              ) {
                <mat-error>Password is required</mat-error>
              }
            </mat-form-field>

            <button
              mat-flat-button
              color="primary"
              type="submit"
              class="full-width submit-btn"
              [disabled]="isLoading || loginForm.invalid"
            >
              @if (isLoading) {
                <mat-spinner diameter="20"></mat-spinner>
              } @else {
                Sign in
              }
            </button>
          </form>

          <!-- Demo account shortcuts -->
          <div class="demo-section">
            <p class="demo-label">Try a demo account</p>
            <div class="demo-buttons">
              <button
                type="button"
                mat-stroked-button
                class="demo-btn"
                (click)="fillDemo('admin@demo.com', 'Admin1234!')"
              >
                Admin
              </button>
              <button
                type="button"
                mat-stroked-button
                class="demo-btn"
                (click)="fillDemo('viewer@demo.com', 'Viewer1234!')"
              >
                Viewer
              </button>
            </div>
          </div>

          <p class="form-footer">
            No account?
            <a routerLink="/auth/register" class="form-link">Create one</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .auth-layout {
        display: flex;
        min-height: calc(100dvh - 64px);
      }

      /* Left brand panel */
      .brand-panel {
        display: none;
        flex: 1;
        background-color: #0f172a;
        padding: 48px;
        align-items: center;
        justify-content: center;
      }

      @media (min-width: 768px) {
        .brand-panel {
          display: flex;
        }
      }

      .brand-content {
        max-width: 380px;
      }

      .brand-logo {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 32px;
      }

      .brand-icon {
        font-size: 36px;
        width: 36px;
        height: 36px;
        color: var(--mat-sys-primary);
      }

      .brand-name {
        font-size: 1.75rem;
        font-weight: 700;
        color: #ffffff;
        letter-spacing: -0.02em;
      }

      .brand-tagline {
        font-size: 1rem;
        line-height: 1.6;
        color: rgba(255, 255, 255, 0.6);
        margin: 0 0 36px;
      }

      .brand-features {
        list-style: none;
        padding: 0;
        margin: 0 0 36px;
        display: flex;
        flex-direction: column;
        gap: 16px;
      }

      .brand-features li {
        display: flex;
        align-items: center;
        gap: 12px;
        font-size: 0.9rem;
        color: rgba(255, 255, 255, 0.75);
        font-weight: 500;
      }

      .feature-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
        color: var(--mat-sys-primary);
        flex-shrink: 0;
      }

      .brand-stack {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }

      .stack-badge {
        padding: 4px 10px;
        border-radius: 4px;
        font-size: 0.75rem;
        font-weight: 600;
        background-color: rgba(255, 255, 255, 0.08);
        color: rgba(255, 255, 255, 0.55);
        border: 1px solid rgba(255, 255, 255, 0.1);
        letter-spacing: 0.01em;
      }

      /* Right form panel */
      .form-panel {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 32px 24px;
        background-color: #ffffff;
      }

      .form-content {
        width: 100%;
        max-width: 380px;
      }

      .form-header {
        margin-bottom: 28px;
      }

      .form-title {
        font-size: 1.625rem;
        font-weight: 700;
        color: #0f172a;
        margin: 0 0 6px;
        letter-spacing: -0.02em;
      }

      .form-subtitle {
        font-size: 0.9rem;
        color: #64748b;
        margin: 0;
      }

      .full-width {
        width: 100%;
        margin-bottom: 4px;
      }

      .submit-btn {
        margin-top: 8px;
        height: 44px;
        font-size: 0.9rem;
        font-weight: 600;
      }

      .submit-btn mat-spinner {
        display: inline-block;
      }

      /* Demo shortcuts */
      .demo-section {
        margin-top: 24px;
        padding-top: 20px;
        border-top: 1px solid #f1f5f9;
      }

      .demo-label {
        font-size: 0.75rem;
        font-weight: 600;
        color: #94a3b8;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin: 0 0 10px;
      }

      .demo-buttons {
        display: flex;
        gap: 8px;
      }

      .demo-btn {
        flex: 1;
        font-size: 0.8125rem;
        font-weight: 500;
      }

      .form-footer {
        margin-top: 20px;
        font-size: 0.875rem;
        color: #64748b;
        text-align: center;
      }

      .form-link {
        color: var(--mat-sys-primary);
        text-decoration: none;
        font-weight: 600;
      }

      .form-link:hover {
        text-decoration: underline;
      }
    `,
  ],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private signalRService = inject(SignalRService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  loginForm: FormGroup = this.fb.group({
    usernameOrEmail: ['', Validators.required],
    password: ['', Validators.required],
  });

  isLoading = false;

  fillDemo(email: string, password: string): void {
    this.loginForm.patchValue({ usernameOrEmail: email, password });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.signalRService.startConnection();
        this.snackBar.open('Login successful!', 'Close', { duration: 3000 });
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/work-items';
        this.router.navigateByUrl(returnUrl);
      },
      error: (error) => {
        this.isLoading = false;
        const message = error.error?.message || 'Login failed. Please check your credentials.';
        this.snackBar.open(message, 'Close', { duration: 5000 });
      },
    });
  }
}
