import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { WorkItemService, AuthService } from '../../../core/services';
import { WorkItem, WorkItemStatus, WorkItemPriority, WorkItemFilter, PaginatedResult } from '../../../core/models';
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
    MatTooltipModule
  ],
  template: `
    <div class="container">
      <div class="header">
        <h1>Work Items</h1>
        @if (authService.isAuthenticated()) {
          <a mat-raised-button color="primary" routerLink="/work-items/create">
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

            <button mat-stroked-button (click)="resetFilters()">
              <mat-icon>clear</mat-icon>
              Clear Filters
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
        <div class="table-container mat-elevation-z2">
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
              <td mat-cell *matCellDef="let item">{{ item.createdAt | date:'short' }}</td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let item">
                <a mat-icon-button [routerLink]="['/work-items', item.id]" matTooltip="View">
                  <mat-icon>visibility</mat-icon>
                </a>
                @if (authService.isAuthenticated()) {
                  <a mat-icon-button [routerLink]="['/work-items', item.id, 'edit']" matTooltip="Edit">
                    <mat-icon>edit</mat-icon>
                  </a>
                  <button mat-icon-button color="warn" (click)="deleteWorkItem(item)" matTooltip="Delete">
                    <mat-icon>delete</mat-icon>
                  </button>
                }
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageSize]="filter.pageSize"
            [pageIndex]="(filter.page || 1) - 1"
            [pageSizeOptions]="[5, 10, 25, 50]"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>
        </div>
      }

      <!-- Empty State -->
      @if (!isLoading() && workItems().length === 0) {
        <mat-card class="empty-state">
          <mat-card-content>
            <mat-icon>inbox</mat-icon>
            <h2>No work items found</h2>
            <p>Create your first work item to get started.</p>
            @if (authService.isAuthenticated()) {
              <a mat-raised-button color="primary" routerLink="/work-items/create">
                Create Work Item
              </a>
            } @else {
              <a mat-raised-button color="primary" routerLink="/auth/login">
                Login to Create
              </a>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .filters-card {
      margin-bottom: 24px;
    }

    .filters {
      display: flex;
      gap: 16px;
      flex-wrap: wrap;
      align-items: center;
    }

    .filters mat-form-field {
      width: 150px;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }

    .table-container {
      overflow-x: auto;
      border-radius: 4px;
    }

    table {
      width: 100%;
    }

    .title-link {
      color: inherit;
      text-decoration: none;
      font-weight: 500;
    }

    .title-link:hover {
      text-decoration: underline;
    }

    .status-todo { background-color: #e3f2fd !important; color: #1976d2 !important; }
    .status-inprogress { background-color: #fff3e0 !important; color: #f57c00 !important; }
    .status-done { background-color: #e8f5e9 !important; color: #388e3c !important; }

    .priority-low { background-color: #f5f5f5 !important; color: #757575 !important; }
    .priority-medium { background-color: #fff8e1 !important; color: #ffa000 !important; }
    .priority-high { background-color: #ffebee !important; color: #d32f2f !important; }

    .empty-state {
      text-align: center;
      padding: 48px;
    }

    .empty-state mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #9e9e9e;
    }

    .empty-state h2 {
      margin: 16px 0 8px;
    }

    .empty-state p {
      color: #757575;
      margin-bottom: 24px;
    }
  `]
})
export class WorkItemListComponent implements OnInit {
  private workItemService = inject(WorkItemService);
  authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

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
    sortDir: 'desc'
  };

  ngOnInit(): void {
    this.loadWorkItems();
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
      }
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
      sortDir: 'desc'
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
    this.filter.sortDir = sort.direction as 'asc' | 'desc' || 'desc';
    this.loadWorkItems();
  }

  deleteWorkItem(item: WorkItem): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Work Item',
        message: `Are you sure you want to delete "${item.title}"?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.workItemService.delete(item.id).subscribe({
          next: () => {
            this.snackBar.open('Work item deleted', 'Close', { duration: 3000 });
            this.loadWorkItems();
          },
          error: () => {
            this.snackBar.open('Failed to delete work item', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }
}
