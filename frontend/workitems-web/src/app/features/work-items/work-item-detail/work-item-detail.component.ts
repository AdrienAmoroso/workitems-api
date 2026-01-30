import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { WorkItemService, AuthService } from '../../../core/services';
import { WorkItem } from '../../../core/models';
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
    MatDialogModule
  ],
  template: `
    <div class="container">
      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
        </div>
      }

      @if (!isLoading() && workItem()) {
        <mat-card>
          <mat-card-header>
            <mat-card-title>{{ workItem()!.title }}</mat-card-title>
            <mat-card-subtitle>
              Created: {{ workItem()!.createdAt | date:'medium' }} |
              Updated: {{ workItem()!.updatedAt | date:'medium' }}
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
              <h3>Description</h3>
              <p>{{ workItem()!.description || 'No description provided.' }}</p>
            </div>
          </mat-card-content>

          <mat-card-actions align="end">
            <a mat-button routerLink="/work-items">
              <mat-icon>arrow_back</mat-icon>
              Back to List
            </a>
            @if (authService.isAuthenticated()) {
              <a mat-raised-button color="primary" [routerLink]="['/work-items', workItem()!.id, 'edit']">
                <mat-icon>edit</mat-icon>
                Edit
              </a>
              <button mat-raised-button color="warn" (click)="deleteWorkItem()">
                <mat-icon>delete</mat-icon>
                Delete
              </button>
            }
          </mat-card-actions>
        </mat-card>
      }

      @if (!isLoading() && !workItem()) {
        <mat-card class="not-found">
          <mat-card-content>
            <mat-icon>error_outline</mat-icon>
            <h2>Work Item Not Found</h2>
            <p>The requested work item could not be found.</p>
            <a mat-raised-button color="primary" routerLink="/work-items">
              Back to List
            </a>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .container {
      padding: 24px;
      max-width: 800px;
      margin: 0 auto;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }

    .chips {
      display: flex;
      gap: 8px;
      margin-bottom: 24px;
    }

    .description {
      margin-top: 16px;
    }

    .description h3 {
      margin-bottom: 8px;
      color: #666;
    }

    .description p {
      white-space: pre-wrap;
      line-height: 1.6;
    }

    .status-todo { background-color: #e3f2fd !important; color: #1976d2 !important; }
    .status-inprogress { background-color: #fff3e0 !important; color: #f57c00 !important; }
    .status-done { background-color: #e8f5e9 !important; color: #388e3c !important; }

    .priority-low { background-color: #f5f5f5 !important; color: #757575 !important; }
    .priority-medium { background-color: #fff8e1 !important; color: #ffa000 !important; }
    .priority-high { background-color: #ffebee !important; color: #d32f2f !important; }

    .not-found {
      text-align: center;
      padding: 48px;
    }

    .not-found mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #f44336;
    }

    .not-found h2 {
      margin: 16px 0 8px;
    }

    .not-found p {
      color: #757575;
      margin-bottom: 24px;
    }
  `]
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
      }
    });
  }

  deleteWorkItem(): void {
    const item = this.workItem();
    if (!item) return;

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
            this.router.navigate(['/work-items']);
          },
          error: () => {
            this.snackBar.open('Failed to delete work item', 'Close', { duration: 5000 });
          }
        });
      }
    });
  }
}
