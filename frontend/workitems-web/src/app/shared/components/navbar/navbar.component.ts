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
        <mat-icon class="nav-logo-icon">task_alt</mat-icon>
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
          <mat-icon>expand_more</mat-icon>
        </button>
        <mat-menu #userMenu="matMenu">
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
        <a mat-flat-button routerLink="/auth/register" class="nav-btn-register">Get started</a>
      }
    </mat-toolbar>
  `,
  styles: [
    `
      mat-toolbar.app-navbar {
        position: sticky;
        top: 0;
        z-index: 1000;
        background-color: #0f172a;
        color: #ffffff;
        padding: 0 24px;
        height: 64px;
        border-bottom: 1px solid rgba(255, 255, 255, 0.07);
        gap: 0;
      }

      .nav-logo {
        display: flex;
        align-items: center;
        gap: 10px;
        color: #ffffff;
        text-decoration: none;
        font-weight: 700;
        font-size: 1.05rem;
        letter-spacing: -0.01em;
        flex-shrink: 0;
      }

      .nav-logo-icon {
        color: var(--mat-sys-primary);
        font-size: 24px;
        width: 24px;
        height: 24px;
      }

      .spacer {
        flex: 1 1 auto;
      }

      .nav-links {
        display: flex;
        gap: 2px;
        margin-right: 12px;
      }

      .nav-link {
        color: rgba(255, 255, 255, 0.65) !important;
        font-weight: 500;
        font-size: 0.875rem;
        border-radius: 6px !important;
        transition: color 0.15s ease, background-color 0.15s ease;
      }

      .nav-link.nav-link-active {
        color: #ffffff !important;
        background-color: rgba(255, 255, 255, 0.1) !important;
      }

      .user-button {
        color: #ffffff !important;
        display: flex;
        align-items: center;
        gap: 8px;
        border-radius: 8px !important;
      }

      .user-name {
        font-weight: 500;
        font-size: 0.875rem;
      }

      .user-avatar {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 26px;
        height: 26px;
        border-radius: 50%;
        background-color: var(--mat-sys-primary);
        color: var(--mat-sys-on-primary);
        font-size: 0.7rem;
        font-weight: 700;
        text-transform: uppercase;
        flex-shrink: 0;
      }

      .menu-user-info {
        padding: 12px 16px 8px;
      }

      .menu-username {
        margin: 0;
        font-weight: 600;
        font-size: 0.875rem;
        color: #0f172a;
      }

      .menu-role {
        margin: 2px 0 0;
        font-size: 0.75rem;
        color: #64748b;
        text-transform: capitalize;
      }

      .nav-btn-ghost {
        color: rgba(255, 255, 255, 0.8) !important;
        font-size: 0.875rem;
        font-weight: 500;
      }

      .nav-btn-register {
        background-color: var(--mat-sys-primary) !important;
        color: var(--mat-sys-on-primary) !important;
        border-radius: 6px !important;
        margin-left: 8px;
        font-size: 0.875rem;
        font-weight: 600;
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
}
