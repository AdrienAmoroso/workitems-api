import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { WorkItem } from '../../../core/models';
import { AuthService, WorkItemService } from '../../../core/services';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-work-item-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
  ],
  template: `
    <div class="container">
      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
        </div>
      }

      @if (!isLoading() && workItem()) {
        <div class="detail-layout">
          <!-- Back link -->
          <a mat-button routerLink="/work-items" class="back-link">
            <mat-icon>arrow_back</mat-icon>
            Back to list
          </a>

          <mat-card class="detail-card">
            <mat-card-header>
              <mat-card-title class="item-title">{{ workItem()!.title }}</mat-card-title>
              <mat-card-subtitle class="item-meta">
                Created {{ workItem()!.createdAt | date: 'mediumDate' }} &nbsp;·&nbsp; Updated
                {{ workItem()!.updatedAt | date: 'mediumDate' }}
              </mat-card-subtitle>
            </mat-card-header>

            <mat-card-content>
              <div class="chips">
                <mat-chip [class]="'status-' + workItem()!.status.toLowerCase()">
                  {{ workItem()!.status }}
                </mat-chip>
                <mat-chip [class]="'priority-' + workItem()!.priority.toLowerCase()">
                  {{ workItem()!.priority }} Priority
                </mat-chip>
              </div>

              <div class="description">
                <p class="description-label">Description</p>
                <p class="description-body">
                  {{ workItem()!.description || 'No description provided.' }}
                </p>
              </div>
            </mat-card-content>

            <mat-card-actions class="card-actions">
              @if (authService.canManage()) {
                <a
                  mat-flat-button
                  class="edit-btn"
                  [routerLink]="['/work-items', workItem()!.id, 'edit']"
                >
                  <mat-icon>edit</mat-icon>
                  Edit
                </a>
              }
              @if (authService.isAdmin()) {
                <button mat-stroked-button class="delete-btn" (click)="deleteWorkItem()">
                  <mat-icon>delete</mat-icon>
                  Delete
                </button>
              }
            </mat-card-actions>
          </mat-card>
        </div>
      }

      @if (!isLoading() && !workItem()) {
        <mat-card class="not-found">
          <mat-card-content>
            <mat-icon class="not-found-icon">error_outline</mat-icon>
            <h2>Work Item Not Found</h2>
            <p>The requested work item could not be found.</p>
            <a mat-flat-button class="back-btn" routerLink="/work-items">Back to List</a>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [
    `
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

      .container {
        padding: 32px 24px;
        max-width: 760px;
        margin: 0 auto;
      }

      /* ── Layout ───────────────────────────────────────────────── */
      .detail-layout {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }

      .back-link {
        align-self: flex-start;
        color: var(--color-text-secondary) !important;
        font-size: 0.875rem !important;
        font-weight: 500 !important;
        border-radius: 6px !important;
        padding: 0 8px !important;
        transition: color 0.15s ease !important;
      }

      .back-link:hover {
        color: var(--color-text-primary) !important;
        background-color: rgba(255, 255, 255, 0.05) !important;
      }

      /* ── Card ─────────────────────────────────────────────────── */
      .detail-card {
        border-radius: 14px !important;
        border: 1px solid var(--color-border) !important;
        box-shadow: none !important;
      }

      .item-title {
        font-size: 1.375rem !important;
        font-weight: 700 !important;
        color: var(--color-text-primary) !important;
        letter-spacing: -0.02em !important;
        line-height: 1.3 !important;
        margin-bottom: 6px !important;
      }

      .item-meta {
        font-size: 0.8125rem !important;
        color: var(--color-text-muted) !important;
      }

      /* ── Chips ────────────────────────────────────────────────── */
      .chips {
        display: flex;
        gap: 8px;
        margin: 8px 0 24px;
        flex-wrap: wrap;
      }

      /* ── Description ──────────────────────────────────────────── */
      .description {
        padding-top: 20px;
        border-top: 1px solid var(--color-border);
      }

      .description-label {
        font-size: 0.7rem;
        font-weight: 700;
        color: var(--color-text-muted);
        text-transform: uppercase;
        letter-spacing: 0.07em;
        margin: 0 0 10px;
      }

      .description-body {
        font-size: 0.9375rem;
        color: var(--color-text-secondary);
        white-space: pre-wrap;
        line-height: 1.7;
        margin: 0;
      }

      /* ── Actions ──────────────────────────────────────────────── */
      .card-actions {
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        padding: 12px 16px !important;
        border-top: 1px solid var(--color-border);
      }

      .edit-btn {
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 7px !important;
        font-weight: 600 !important;
        font-size: 0.875rem !important;
        box-shadow: 0 0 0 0 var(--color-accent-glow);
        transition:
          box-shadow 0.2s ease,
          background-color 0.15s ease !important;
      }

      .edit-btn:hover {
        background-color: #5254cc !important;
        box-shadow: 0 0 14px 0 var(--color-accent-glow) !important;
      }

      .delete-btn {
        border-color: rgba(248, 113, 113, 0.3) !important;
        color: #f87171 !important;
        border-radius: 7px !important;
        font-size: 0.875rem !important;
        transition:
          background-color 0.15s ease,
          border-color 0.15s ease !important;
      }

      .delete-btn:hover {
        background-color: rgba(248, 113, 113, 0.08) !important;
        border-color: rgba(248, 113, 113, 0.5) !important;
      }

      /* ── Loading ──────────────────────────────────────────────── */
      .loading-container {
        display: flex;
        justify-content: center;
        padding: 64px;
      }

      /* ── Not-found ────────────────────────────────────────────── */
      .not-found {
        text-align: center;
        padding: 64px 48px !important;
        border: 1px solid var(--color-border) !important;
        border-radius: 16px !important;
        box-shadow: none !important;
      }

      .not-found-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: #f87171;
        margin-bottom: 16px;
      }

      .not-found h2 {
        font-size: 1.125rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 8px;
      }

      .not-found p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin-bottom: 24px;
      }

      .back-btn {
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 8px !important;
        font-weight: 600 !important;
      }
    `,
  ],
})
export class WorkItemDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private workItemService = inject(WorkItemService);
  authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  workItem = signal<WorkItem | null>(null);
  isLoading = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadWorkItem(id);
    }
  }

  loadWorkItem(id: string): void {
    this.isLoading.set(true);
    this.workItemService.getById(id).subscribe({
      next: (item) => {
        this.workItem.set(item);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.workItem.set(null);
      },
    });
  }

  deleteWorkItem(): void {
    const item = this.workItem();
    if (!item) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Work Item',
        message: `Are you sure you want to delete "${item.title}"?`,
      },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.workItemService.delete(item.id).subscribe({
          next: () => {
            this.snackBar.open('Work item deleted', 'Close', { duration: 3000 });
            this.router.navigate(['/work-items']);
          },
          error: () => {
            this.snackBar.open('Failed to delete work item', 'Close', { duration: 5000 });
          },
        });
      }
    });
  }
}
