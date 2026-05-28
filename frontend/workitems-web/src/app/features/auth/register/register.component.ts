import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services';

@Component({
  selector: 'app-register',
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
        <div class="brand-glow"></div>
        <div class="brand-content">
          <div class="brand-logo">
            <span class="brand-logo-mark">
              <mat-icon>task_alt</mat-icon>
            </span>
            <span class="brand-name">Work Items</span>
          </div>

          <h2 class="brand-headline">Join the demo. Explore the stack.</h2>

          <ul class="brand-features">
            <li>
              <span class="feature-dot feature-dot-accent"></span>
              Real-time updates via SignalR WebSockets
            </li>
            <li>
              <span class="feature-dot feature-dot-orange"></span>
              Role-based access — Admin, Member, Viewer
            </li>
            <li>
              <span class="feature-dot feature-dot-green"></span>
              Clean Architecture + JWT Bearer auth
            </li>
          </ul>

          <div class="brand-stack">
            <span class="stack-badge">.NET 10</span>
            <span class="stack-badge">Angular 19</span>
            <span class="stack-badge">EF Core</span>
            <span class="stack-badge">PostgreSQL</span>
          </div>
        </div>
      </div>

      <!-- Right form panel -->
      <div class="form-panel">
        <div class="form-content">
          <div class="form-header">
            <h1 class="form-title">Create an account</h1>
            <p class="form-subtitle">Fill in the details below to get started</p>
          </div>

          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username</mat-label>
              <input
                matInput
                formControlName="username"
                placeholder="Choose a username"
                autocomplete="username"
              />
              @if (
                registerForm.get('username')?.hasError('required') &&
                registerForm.get('username')?.touched
              ) {
                <mat-error>Username is required</mat-error>
              }
              @if (registerForm.get('username')?.hasError('minlength')) {
                <mat-error>Username must be at least 3 characters</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <input
                matInput
                type="email"
                formControlName="email"
                placeholder="Enter your email"
                autocomplete="email"
              />
              @if (
                registerForm.get('email')?.hasError('required') &&
                registerForm.get('email')?.touched
              ) {
                <mat-error>Email is required</mat-error>
              }
              @if (registerForm.get('email')?.hasError('email')) {
                <mat-error>Enter a valid email address</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Password</mat-label>
              <input
                matInput
                type="password"
                formControlName="password"
                placeholder="Choose a password"
                autocomplete="new-password"
              />
              @if (
                registerForm.get('password')?.hasError('required') &&
                registerForm.get('password')?.touched
              ) {
                <mat-error>Password is required</mat-error>
              }
              @if (registerForm.get('password')?.hasError('minlength')) {
                <mat-error>Password must be at least 6 characters</mat-error>
              }
            </mat-form-field>

            <button
              mat-flat-button
              type="submit"
              class="full-width submit-btn"
              [disabled]="isLoading || registerForm.invalid"
            >
              @if (isLoading) {
                <mat-spinner diameter="18"></mat-spinner>
              } @else {
                <ng-container>Create account</ng-container>
                <mat-icon class="btn-arrow">arrow_forward</mat-icon>
              }
            </button>
          </form>

          <p class="form-footer">
            Already have an account?
            <a routerLink="/auth/login" class="form-link">Sign in</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      /* ── Layout ───────────────────────────────────────────────── */
      :host {
        display: block;
        animation: pageFadeIn 0.15s ease both;
      }

      @keyframes pageFadeIn {
        from {
          opacity: 0;
          transform: translateY(4px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .auth-layout {
        display: flex;
        min-height: calc(100dvh - 60px);
      }

      /* ── Brand panel (left, hidden on mobile) ─────────────────── */
      .brand-panel {
        display: none;
        flex: 1;
        position: relative;
        overflow: hidden;
        background-color: var(--color-surface);
        border-right: 1px solid var(--color-border);
        padding: 56px 48px;
        align-items: center;
        justify-content: center;
      }

      @media (min-width: 768px) {
        .brand-panel {
          display: flex;
        }
      }

      .brand-glow {
        position: absolute;
        top: -80px;
        left: -80px;
        width: 400px;
        height: 400px;
        border-radius: 50%;
        background: radial-gradient(circle, rgba(99, 102, 241, 0.18) 0%, transparent 70%);
        pointer-events: none;
      }

      .brand-content {
        position: relative;
        max-width: 360px;
        z-index: 1;
      }

      .brand-logo {
        display: flex;
        align-items: center;
        gap: 10px;
        margin-bottom: 40px;
      }

      .brand-logo-mark {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        border-radius: 8px;
        background: var(--color-accent-dim);
        border: 1px solid rgba(99, 102, 241, 0.3);
      }

      .brand-logo-mark mat-icon {
        font-size: 17px;
        width: 17px;
        height: 17px;
        color: var(--color-accent-text);
      }

      .brand-name {
        font-size: 1rem;
        font-weight: 700;
        color: var(--color-text-primary);
        letter-spacing: -0.01em;
      }

      .brand-headline {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--color-text-primary);
        line-height: 1.35;
        letter-spacing: -0.02em;
        margin: 0 0 32px;
      }

      .brand-features {
        list-style: none;
        padding: 0;
        margin: 0 0 36px;
        display: flex;
        flex-direction: column;
        gap: 14px;
      }

      .brand-features li {
        display: flex;
        align-items: center;
        gap: 12px;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        font-weight: 500;
      }

      .feature-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        flex-shrink: 0;
      }

      .feature-dot-accent {
        background: var(--color-status-todo);
      }
      .feature-dot-orange {
        background: var(--color-status-progress);
      }
      .feature-dot-green {
        background: var(--color-status-done);
      }

      .brand-stack {
        display: flex;
        gap: 6px;
        flex-wrap: wrap;
      }

      .stack-badge {
        padding: 3px 9px;
        border-radius: 4px;
        font-size: 0.7rem;
        font-weight: 700;
        background: rgba(255, 255, 255, 0.04);
        color: var(--color-text-muted);
        border: 1px solid var(--color-border);
        letter-spacing: 0.03em;
        font-family: 'Plus Jakarta Sans', monospace;
      }

      /* ── Form panel (right) ───────────────────────────────────── */
      .form-panel {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 40px 24px;
        background-color: var(--color-bg);
      }

      .form-content {
        width: 100%;
        max-width: 360px;
      }

      .form-header {
        margin-bottom: 28px;
      }

      .form-title {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 6px;
        letter-spacing: -0.02em;
      }

      .form-subtitle {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        margin: 0;
      }

      .full-width {
        width: 100%;
        margin-bottom: 4px;
      }

      /* Submit button */
      .submit-btn {
        margin-top: 8px;
        height: 44px;
        font-size: 0.875rem;
        font-weight: 600;
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 8px !important;
        letter-spacing: -0.01em;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        box-shadow: 0 0 0 0 var(--color-accent-glow);
        transition:
          box-shadow 0.2s ease,
          background-color 0.15s ease !important;
      }

      .submit-btn:not([disabled]):hover {
        background-color: #5254cc !important;
        box-shadow: 0 0 20px 0 var(--color-accent-glow) !important;
      }

      .submit-btn[disabled] {
        opacity: 0.45;
      }

      .btn-arrow {
        font-size: 16px;
        width: 16px;
        height: 16px;
        transition: transform 0.15s ease;
      }

      .submit-btn:not([disabled]):hover .btn-arrow {
        transform: translateX(2px);
      }

      .form-footer {
        margin-top: 20px;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        text-align: center;
      }

      .form-link {
        color: var(--color-accent-text);
        text-decoration: none;
        font-weight: 600;
        transition: color 0.15s ease;
      }

      .form-link:hover {
        color: #fff;
      }
    `,
  ],
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  registerForm: FormGroup = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  isLoading = false;

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.snackBar.open('Registration successful! Welcome!', 'Close', { duration: 3000 });
        this.router.navigate(['/work-items']);
      },
      error: (error) => {
        this.isLoading = false;
        const message = error.error?.message || 'Registration failed. Please try again.';
        this.snackBar.open(message, 'Close', { duration: 5000 });
      },
    });
  }
}
