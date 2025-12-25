import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
export const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full'
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/layout/layout.component').then(m => m.LayoutComponent),
    children: [
      {
        path: 'projects',
        loadComponent: () => import('./features/projects/project-list.component').then(m => m.ProjectListComponent)
      },
      {
        path: 'projects/new',
        loadComponent: () => import('./features/project-create/project-create.component').then(m => m.ProjectCreateComponent)
      },
      {
        path: 'projects/:projectId',
        loadComponent: () => import('./features/project-detail/project-detail.component').then(m => m.ProjectDetailComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/user-list.component').then(m => m.UserListComponent)
      },
      {
        path: 'users/:id',
        loadComponent: () => import('./features/users/user-form.component').then(m => m.UserFormComponent)
      },
      {
        path: 'users/:id/permissions',
        loadComponent: () => import('./features/users/user-permissions.component').then(m => m.UserPermissionsComponent)
      }
    ]
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  }
];
