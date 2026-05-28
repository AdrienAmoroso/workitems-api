import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
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
    MatSnackBarModule
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
            Track your team's tasks in one place. Built as a .NET 10 and Angular 19 portfolio project.
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
            <h1 class="form-title">Create an account</h1>
            <p class="form-subtitle">Fill in the details below to get started</p>
          </div>

          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username</mat-label>
              <input matInput formControlName="username" placeholder="Choose a username" autocomplete="username">
              @if (registerForm.get('username')?.hasError('required') && registerForm.get('username')?.touched) {
                <mat-error>Username is required</mat-error>
              }
              @if (registerForm.get('username')?.hasError('minlength')) {
                <mat-error>Username must be at least 3 characters</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <input matInput type="email" formControlName="email" placeholder="Enter your email" autocomplete="email">
              @if (registerForm.get('email')?.hasError('required') && registerForm.get('email')?.touched) {
                <mat-error>Email is required</mat-error>
              }
              @if (registerForm.get('email')?.hasError('email')) {
                <mat-error>Enter a valid email address</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Password</mat-label>
              <input matInput type="password" formControlName="password" placeholder="Choose a password" autocomplete="new-password">
              @if (registerForm.get('password')?.hasError('required') && registerForm.get('password')?.touched) {
                <mat-error>Password is required</mat-error>
              }
              @if (registerForm.get('password')?.hasError('minlength')) {
                <mat-error>Password must be at least 6 characters</mat-error>
              }
            </mat-form-field>

            <button
              mat-flat-button
              color="primary"
              type="submit"
              class="full-width submit-btn"
              [disabled]="isLoading || registerForm.invalid"
            >
              @if (isLoading) {
                <mat-spinner diameter="20"></mat-spinner>
              } @else {
                Create account
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
  styles: [`
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
  `]
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  registerForm: FormGroup = this.fb.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
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
      }
    });
  }
}
      },
      error: (error) => {
        this.isLoading = false;
        const message = error.error?.message || 'Registration failed. Please try again.';
        this.snackBar.open(message, 'Close', { duration: 5000 });
      }
    });
  }
}
