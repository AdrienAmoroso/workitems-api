import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService, SignalRService } from '../../../core/services';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
  ],
  template: `
    <mat-toolbar class="app-navbar">
      <a routerLink="/" class="nav-logo">
        <span class="nav-logo-mark">
          <mat-icon class="nav-logo-icon">task_alt</mat-icon>
        </span>
        <span class="nav-logo-text">Work Items</span>
      </a>

      <span class="spacer"></span>

      <nav class="nav-links">
        <a mat-button routerLink="/work-items" routerLinkActive="nav-link-active" class="nav-link">
          All Items
        </a>
      </nav>

      @if (authService.isAuthenticated()) {
        <button mat-button [matMenuTriggerFor]="userMenu" class="user-button">
          <span class="user-avatar">{{ getUserInitial() }}</span>
          <span class="user-name">{{ authService.currentUser()?.username }}</span>
          <mat-icon class="chevron-icon">expand_more</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu" class="user-menu-panel">
          <div class="menu-user-info">
            <p class="menu-username">{{ authService.currentUser()?.username }}</p>
            <p class="menu-role">{{ authService.currentUser()?.role }}</p>
          </div>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>Sign out</span>
          </button>
        </mat-menu>
      } @else {
        <a mat-button routerLink="/auth/login" class="nav-btn-ghost">Sign in</a>
        <a mat-flat-button routerLink="/auth/register" class="nav-btn-cta">Get started</a>
      }
    </mat-toolbar>
  `,
  styles: [
    `
      mat-toolbar.app-navbar {
        position: sticky;
        top: 0;
        z-index: 1000;
        background-color: rgba(9, 9, 13, 0.85);
        backdrop-filter: blur(12px);
        -webkit-backdrop-filter: blur(12px);
        color: var(--color-text-primary);
        padding: 0 24px;
        height: 60px;
        border-bottom: 1px solid var(--color-border);
        gap: 0;
      }

      /* Logo */
      .nav-logo {
        display: flex;
        align-items: center;
        gap: 10px;
        color: var(--color-text-primary);
        text-decoration: none;
        font-weight: 700;
        font-size: 0.9375rem;
        letter-spacing: -0.01em;
        flex-shrink: 0;
      }

      .nav-logo-mark {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 28px;
        height: 28px;
        border-radius: 7px;
        background: var(--color-accent-dim);
        border: 1px solid rgba(99, 102, 241, 0.3);
        transition: background 0.15s ease;
      }

      .nav-logo:hover .nav-logo-mark {
        background: rgba(99, 102, 241, 0.22);
      }

      .nav-logo-icon {
        color: var(--color-accent-text);
        font-size: 16px;
        width: 16px;
        height: 16px;
      }

      /* Spacer */
      .spacer {
        flex: 1 1 auto;
      }

      /* Nav links */
      .nav-links {
        display: flex;
        gap: 2px;
        margin-right: 16px;
      }

      .nav-link {
        color: var(--color-text-secondary) !important;
        font-weight: 500;
        font-size: 0.875rem;
        border-radius: 6px !important;
        transition:
          color 0.15s ease,
          background-color 0.15s ease;
      }

      .nav-link:hover {
        color: var(--color-text-primary) !important;
        background-color: rgba(255, 255, 255, 0.05) !important;
      }

      .nav-link.nav-link-active {
        color: var(--color-accent-text) !important;
        background-color: var(--color-accent-dim) !important;
      }

      /* User button */
      .user-button {
        color: var(--color-text-primary) !important;
        border-radius: 8px !important;
        padding: 0 10px !important;
        transition: background-color 0.15s ease;
      }

      .user-button:hover {
        background-color: rgba(255, 255, 255, 0.05) !important;
      }

      .user-name {
        font-weight: 500;
        font-size: 0.875rem;
        margin: 0 4px;
      }

      .chevron-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
        color: var(--color-text-muted);
        transition: transform 0.15s ease;
      }

      .user-avatar {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 26px;
        height: 26px;
        border-radius: 50%;
        background: linear-gradient(135deg, var(--color-accent) 0%, #818cf8 100%);
        color: #fff;
        font-size: 0.7rem;
        font-weight: 700;
        text-transform: uppercase;
        flex-shrink: 0;
        letter-spacing: 0;
      }

      /* User menu content */
      .menu-user-info {
        padding: 12px 16px 8px;
      }

      .menu-username {
        margin: 0;
        font-weight: 600;
        font-size: 0.875rem;
        color: var(--color-text-primary);
      }

      .menu-role {
        margin: 2px 0 0;
        font-size: 0.75rem;
        color: var(--color-text-muted);
        text-transform: capitalize;
      }

      /* Ghost + CTA buttons (unauthenticated state) */
      .nav-btn-ghost {
        color: var(--color-text-secondary) !important;
        font-size: 0.875rem;
        font-weight: 500;
        border-radius: 6px !important;
        transition:
          color 0.15s ease,
          background-color 0.15s ease;
      }

      .nav-btn-ghost:hover {
        color: var(--color-text-primary) !important;
        background-color: rgba(255, 255, 255, 0.05) !important;
      }

      .nav-btn-cta {
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 6px !important;
        margin-left: 8px;
        font-size: 0.875rem;
        font-weight: 600;
        letter-spacing: -0.01em;
        box-shadow: 0 0 0 0 var(--color-accent-glow);
        transition:
          box-shadow 0.2s ease,
          background-color 0.15s ease !important;
      }

      .nav-btn-cta:hover {
        background-color: #5254cc !important;
        box-shadow: 0 0 16px 0 var(--color-accent-glow) !important;
      }
    `,
  ],
})
export class NavbarComponent {
  authService = inject(AuthService);
  private signalRService = inject(SignalRService);

  logout(): void {
    this.signalRService.stopConnection();
    this.authService.logout();
  }

  getUserInitial(): string {
    return (this.authService.currentUser()?.username ?? 'U').charAt(0).toUpperCase();
  }
}
