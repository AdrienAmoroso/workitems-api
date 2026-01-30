import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards';

export const WORK_ITEMS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./work-item-list/work-item-list.component').then(m => m.WorkItemListComponent)
  },
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () => import('./work-item-form/work-item-form.component').then(m => m.WorkItemFormComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./work-item-detail/work-item-detail.component').then(m => m.WorkItemDetailComponent)
  },
  {
    path: ':id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./work-item-form/work-item-form.component').then(m => m.WorkItemFormComponent)
  }
];
