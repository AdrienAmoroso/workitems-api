import { animate, style, transition, trigger } from '@angular/animations';
import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { filter, skip, Subscription } from 'rxjs';
import {
    PaginatedResult,
    WorkItem,
    WorkItemFilter,
    WorkItemPriority,
    WorkItemStatus,
} from '../../../core/models';
import { AuthService, SignalRService, WorkItemService } from '../../../core/services';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-work-item-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatTooltipModule,
  ],
  animations: [
    trigger('rowAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(6px)' }),
        animate('180ms ease', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
    ]),
  ],
  template: `
    <div class="container">
      <div class="header">
        <h1>Work Items</h1>
        @if (authService.canManage()) {
          <a mat-flat-button class="new-item-btn" routerLink="/work-items/create">
            <mat-icon>add</mat-icon>
            New Work Item
          </a>
        }
      </div>

      <!-- Filters -->
      <mat-card class="filters-card">
        <mat-card-content>
          <div class="filters">
            <mat-form-field appearance="outline">
              <mat-label>Status</mat-label>
              <mat-select [(ngModel)]="filter.status" (selectionChange)="applyFilters()">
                <mat-option [value]="null">All</mat-option>
                @for (status of statuses; track status) {
                  <mat-option [value]="status">{{ status }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Priority</mat-label>
              <mat-select [(ngModel)]="filter.priority" (selectionChange)="applyFilters()">
                <mat-option [value]="null">All</mat-option>
                @for (priority of priorities; track priority) {
                  <mat-option [value]="priority">{{ priority }}</mat-option>
                }
              </mat-select>
            </mat-form-field>

            <button mat-stroked-button class="clear-btn" (click)="resetFilters()">
              <mat-icon>clear</mat-icon>
              Clear
            </button>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Loading -->
      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
        </div>
      }

      <!-- Table -->
      @if (!isLoading() && workItems().length > 0) {
        <div class="table-container">
          <table mat-table [dataSource]="workItems()" matSort (matSortChange)="onSort($event)">
            <!-- Title Column -->
            <ng-container matColumnDef="title">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Title</th>
              <td mat-cell *matCellDef="let item">
                <a [routerLink]="['/work-items', item.id]" class="title-link">{{ item.title }}</a>
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
              <td mat-cell *matCellDef="let item">
                <mat-chip [class]="'status-' + item.status.toLowerCase()">
                  {{ item.status }}
                </mat-chip>
              </td>
            </ng-container>

            <!-- Priority Column -->
            <ng-container matColumnDef="priority">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Priority</th>
              <td mat-cell *matCellDef="let item">
                <mat-chip [class]="'priority-' + item.priority.toLowerCase()">
                  {{ item.priority }}
                </mat-chip>
              </td>
            </ng-container>

            <!-- Created Column -->
            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef mat-sort-header>Created</th>
              <td mat-cell *matCellDef="let item">{{ item.createdAt | date: 'short' }}</td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let item">
                <a mat-icon-button [routerLink]="['/work-items', item.id]" matTooltip="View">
                  <mat-icon>visibility</mat-icon>
                </a>
                @if (authService.canManage()) {
                  <a
                    mat-icon-button
                    [routerLink]="['/work-items', item.id, 'edit']"
                    matTooltip="Edit"
                  >
                    <mat-icon>edit</mat-icon>
                  </a>
                }
                @if (authService.isAdmin()) {
                  <button
                    mat-icon-button
                    color="warn"
                    (click)="deleteWorkItem(item)"
                    matTooltip="Delete"
                  >
                    <mat-icon>delete</mat-icon>
                  </button>
                }
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns" [@rowAnimation]></tr>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageSize]="filter.pageSize"
            [pageIndex]="(filter.page || 1) - 1"
            [pageSizeOptions]="[5, 10, 25, 50]"
            (page)="onPageChange($event)"
            showFirstLastButtons
          >
          </mat-paginator>
        </div>
      }

      <!-- Empty State -->
      @if (!isLoading() && workItems().length === 0) {
        <mat-card class="empty-state">
          <mat-card-content>
            <mat-icon class="empty-icon">inbox</mat-icon>
            <h2>No work items found</h2>
            <p>Create your first work item to get started.</p>
            @if (authService.canManage()) {
              <a mat-flat-button class="empty-cta" routerLink="/work-items/create">
                Create Work Item
              </a>
            } @else {
              <a mat-flat-button class="empty-cta" routerLink="/auth/login">Login to Create</a>
            }
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
        max-width: 1200px;
        margin: 0 auto;
      }

      /* ── Header ───────────────────────────────────────────────── */
      .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 28px;
      }

      .header h1 {
        font-size: 1.625rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0;
        letter-spacing: -0.03em;
      }

      .new-item-btn {
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 8px !important;
        font-weight: 600 !important;
        font-size: 0.875rem !important;
        box-shadow: 0 0 0 0 var(--color-accent-glow);
        transition:
          box-shadow 0.2s ease,
          background-color 0.15s ease !important;
      }

      .new-item-btn:hover {
        background-color: #5254cc !important;
        box-shadow: 0 0 16px 0 var(--color-accent-glow) !important;
      }

      /* ── Filters ──────────────────────────────────────────────── */
      .filters-card {
        margin-bottom: 20px;
        background: var(--color-surface-2) !important;
        border: 1px solid var(--color-border) !important;
        border-radius: 12px !important;
        box-shadow: none !important;
      }

      .filters {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
        align-items: center;
        padding: 4px 0;
      }

      .filters mat-form-field {
        width: 148px;
      }

      .clear-btn {
        color: var(--color-text-secondary) !important;
        border-color: var(--color-border) !important;
        border-radius: 7px !important;
        font-size: 0.8125rem !important;
      }

      .clear-btn:hover {
        background-color: rgba(255, 255, 255, 0.04) !important;
      }

      /* ── Loading ──────────────────────────────────────────────── */
      .loading-container {
        display: flex;
        justify-content: center;
        padding: 64px;
      }

      /* ── Table ────────────────────────────────────────────────── */
      .table-container {
        overflow-x: auto;
        border-radius: 12px;
        border: 1px solid var(--color-border);
      }

      table {
        width: 100%;
      }

      .title-link {
        color: var(--color-text-primary);
        text-decoration: none;
        font-weight: 500;
        transition: color 0.15s ease;
      }

      .title-link:hover {
        color: var(--color-accent-text);
      }

      /* ── Empty state ──────────────────────────────────────────── */
      .empty-state {
        text-align: center;
        padding: 64px 48px !important;
        background: var(--color-surface) !important;
        border: 1px solid var(--color-border) !important;
        border-radius: 16px !important;
        box-shadow: none !important;
      }

      .empty-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: var(--color-text-muted);
        margin-bottom: 16px;
      }

      .empty-state h2 {
        font-size: 1.125rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 8px;
        letter-spacing: -0.01em;
      }

      .empty-state p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin-bottom: 24px;
      }

      .empty-cta {
        background-color: var(--color-accent) !important;
        color: #fff !important;
        border-radius: 8px !important;
        font-weight: 600 !important;
      }
    `,
  ],
})
export class WorkItemListComponent implements OnInit, OnDestroy {
  private workItemService = inject(WorkItemService);
  private signalRService = inject(SignalRService);
  authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private subscriptions = new Subscription();

  workItems = signal<WorkItem[]>([]);
  totalCount = signal(0);
  isLoading = signal(false);

  displayedColumns = ['title', 'status', 'priority', 'createdAt', 'actions'];
  statuses = Object.values(WorkItemStatus);
  priorities = Object.values(WorkItemPriority);

  filter: WorkItemFilter = {
    page: 1,
    pageSize: 10,
    status: null,
    priority: null,
    sortBy: 'createdAt',
    sortDir: 'desc',
  };

  ngOnInit(): void {
    this.loadWorkItems();
    this.subscriptions.add(
      this.signalRService.onWorkItemCreated$.subscribe((item) =>
        this.workItems.update((items) => [item, ...items]),
      ),
    );
    this.subscriptions.add(
      this.signalRService.onWorkItemUpdated$.subscribe((updated) =>
        this.workItems.update((items) => items.map((i) => (i.id === updated.id ? updated : i))),
      ),
    );
    this.subscriptions.add(
      this.signalRService.onWorkItemDeleted$.subscribe((id) => {
        this.workItems.update((items) => items.filter((i) => i.id !== id));
        this.totalCount.update((n) => n - 1);
      }),
    );
    // Show a snackbar only when the connection transitions from connected → disconnected.
    // skip(1) drops the BehaviorSubject's initial false emission (before any connection attempt).
    this.subscriptions.add(
      this.signalRService.isConnected$.pipe(
        skip(1),
        filter((connected) => !connected),
      ).subscribe(() => {
        this.snackBar.open('Live updates paused — reconnecting…', 'Dismiss', { duration: 5000 });
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  loadWorkItems(): void {
    this.isLoading.set(true);
    this.workItemService.getAll(this.filter).subscribe({
      next: (result: PaginatedResult<WorkItem>) => {
        this.workItems.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load work items', 'Close', { duration: 5000 });
      },
    });
  }

  applyFilters(): void {
    this.filter.page = 1;
    this.loadWorkItems();
  }

  resetFilters(): void {
    this.filter = {
      page: 1,
      pageSize: this.filter.pageSize,
      status: null,
      priority: null,
      sortBy: 'createdAt',
      sortDir: 'desc',
    };
    this.loadWorkItems();
  }

  onPageChange(event: PageEvent): void {
    this.filter.page = event.pageIndex + 1;
    this.filter.pageSize = event.pageSize;
    this.loadWorkItems();
  }

  onSort(sort: Sort): void {
    this.filter.sortBy = sort.active;
    this.filter.sortDir = (sort.direction as 'asc' | 'desc') || 'desc';
    this.loadWorkItems();
  }

  deleteWorkItem(item: WorkItem): void {
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
            this.loadWorkItems();
          },
          error: () => {
            this.snackBar.open('Failed to delete work item', 'Close', { duration: 5000 });
          },
        });
      }
    });
  }
}
