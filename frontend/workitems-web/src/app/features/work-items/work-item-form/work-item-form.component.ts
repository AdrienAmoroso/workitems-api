import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { WorkItemService } from '../../../core/services';
import { WorkItem, WorkItemStatus, WorkItemPriority, CreateWorkItemRequest, UpdateWorkItemRequest } from '../../../core/models';

@Component({
  selector: 'app-work-item-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <div class="container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ isEditMode() ? 'Edit Work Item' : 'Create Work Item' }}</mat-card-title>
        </mat-card-header>

        @if (isLoadingItem()) {
          <mat-card-content>
            <div class="loading-container">
              <mat-spinner></mat-spinner>
            </div>
          </mat-card-content>
        } @else {
          <mat-card-content>
            <form [formGroup]="form" (ngSubmit)="onSubmit()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Title</mat-label>
                <input matInput formControlName="title" placeholder="Enter title">
                @if (form.get('title')?.hasError('required') && form.get('title')?.touched) {
                  <mat-error>Title is required</mat-error>
                }
                @if (form.get('title')?.hasError('minlength')) {
                  <mat-error>Title must be at least 3 characters</mat-error>
                }
                @if (form.get('title')?.hasError('maxlength')) {
                  <mat-error>Title cannot exceed 255 characters</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Description</mat-label>
                <textarea matInput formControlName="description" placeholder="Enter description" rows="4"></textarea>
                @if (form.get('description')?.hasError('maxlength')) {
                  <mat-error>Description cannot exceed 2000 characters</mat-error>
                }
              </mat-form-field>

              @if (isEditMode()) {
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Status</mat-label>
                  <mat-select formControlName="status">
                    @for (status of statuses; track status) {
                      <mat-option [value]="status">{{ status }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              }

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Priority</mat-label>
                <mat-select formControlName="priority">
                  @for (priority of priorities; track priority) {
                    <mat-option [value]="priority">{{ priority }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              <div class="form-actions">
                <a mat-button routerLink="/work-items">Cancel</a>
                <button mat-raised-button color="primary" type="submit" [disabled]="isSubmitting() || form.invalid">
                  @if (isSubmitting()) {
                    <mat-spinner diameter="20"></mat-spinner>
                  } @else {
                    <span class="button-content">
                      <mat-icon>save</mat-icon>
                      {{ isEditMode() ? 'Update' : 'Create' }}
                    </span>
                  }
                </button>
              </div>
            </form>
          </mat-card-content>
        }
      </mat-card>
    </div>
  `,
  styles: [`
    .container {
      padding: 24px;
      max-width: 600px;
      margin: 0 auto;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }

    .full-width {
      width: 100%;
    }

    mat-form-field {
      margin-bottom: 8px;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-top: 16px;
    }

    button mat-spinner {
      display: inline-block;
    }

    .button-content {
      display: flex;
      align-items: center;
      gap: 4px;
    }
  `]
})
export class WorkItemFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private workItemService = inject(WorkItemService);
  private snackBar = inject(MatSnackBar);

  isEditMode = signal(false);
  isLoadingItem = signal(false);
  isSubmitting = signal(false);
  workItemId: string | null = null;

  statuses = Object.values(WorkItemStatus);
  priorities = Object.values(WorkItemPriority);

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(255)]],
    description: ['', Validators.maxLength(2000)],
    status: [WorkItemStatus.Todo],
    priority: [WorkItemPriority.Medium, Validators.required]
  });

  ngOnInit(): void {
    this.workItemId = this.route.snapshot.paramMap.get('id');
    if (this.workItemId) {
      this.isEditMode.set(true);
      this.loadWorkItem(this.workItemId);
    }
  }

  loadWorkItem(id: string): void {
    this.isLoadingItem.set(true);
    this.workItemService.getById(id).subscribe({
      next: (item) => {
        this.form.patchValue({
          title: item.title,
          description: item.description,
          status: item.status,
          priority: item.priority
        });
        this.isLoadingItem.set(false);
      },
      error: () => {
        this.isLoadingItem.set(false);
        this.snackBar.open('Failed to load work item', 'Close', { duration: 5000 });
        this.router.navigate(['/work-items']);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isSubmitting.set(true);

    if (this.isEditMode() && this.workItemId) {
      const request: UpdateWorkItemRequest = this.form.value;
      this.workItemService.update(this.workItemId, request).subscribe({
        next: (item) => {
          this.snackBar.open('Work item updated', 'Close', { duration: 3000 });
          this.router.navigate(['/work-items', item.id]);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          const message = error.error?.message || 'Failed to update work item';
          this.snackBar.open(message, 'Close', { duration: 5000 });
        }
      });
    } else {
      const request: CreateWorkItemRequest = {
        title: this.form.value.title,
        description: this.form.value.description,
        priority: this.form.value.priority
      };
      this.workItemService.create(request).subscribe({
        next: (item) => {
          this.snackBar.open('Work item created', 'Close', { duration: 3000 });
          this.router.navigate(['/work-items', item.id]);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          const message = error.error?.message || 'Failed to create work item';
          this.snackBar.open(message, 'Close', { duration: 5000 });
        }
      });
    }
  }
}
