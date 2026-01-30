import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'work-items',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'work-items',
    loadChildren: () => import('./features/work-items/work-items.routes').then(m => m.WORK_ITEMS_ROUTES)
  },
  {
    path: '**',
    redirectTo: 'work-items'
  }
];
