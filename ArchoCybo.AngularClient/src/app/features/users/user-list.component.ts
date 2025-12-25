import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ],
  template: `
    <div class="container">
      <div class="header">
        <h2>Users</h2>
        <button mat-raised-button color="primary" (click)="add()">Add User</button>
      </div>
      <div class="filters">
        <mat-form-field appearance="outline">
          <mat-label>Search</mat-label>
          <input matInput [(ngModel)]="search" (ngModelChange)="load()">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Role</mat-label>
          <mat-select [(ngModel)]="roleFilter">
            <mat-option value="">All</mat-option>
            <mat-option *ngFor="let r of roles" [value]="r">{{ r }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput [(ngModel)]="emailFilter">
        </mat-form-field>
        <button mat-button (click)="applyClientFilters()">Apply Filters</button>
      </div>
      <mat-card>
        <table mat-table [dataSource]="filtered">
          <ng-container matColumnDef="username">
            <th mat-header-cell *matHeaderCellDef>Username</th>
            <td mat-cell *matCellDef="let u">{{ u.username }}</td>
          </ng-container>
          <ng-container matColumnDef="email">
            <th mat-header-cell *matHeaderCellDef>Email</th>
            <td mat-cell *matCellDef="let u">{{ u.email }}</td>
          </ng-container>
          <ng-container matColumnDef="roles">
            <th mat-header-cell *matHeaderCellDef>Roles</th>
            <td mat-cell *matCellDef="let u">{{ u.roles?.join(', ') }}</td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let u">
              <button mat-button (click)="edit(u.id)">Edit</button>
              <button mat-button (click)="permissions(u.id)">Permissions</button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
        </table>
      </mat-card>
    </div>
  `,
  styles: [`
    .container { max-width: 1000px; margin: 2rem auto; padding: 0 1rem; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .filters { display: flex; gap: 1rem; align-items: center; margin-bottom: 1rem; }
  `]
})
export class UserListComponent implements OnInit {
  search = '';
  roleFilter = '';
  emailFilter = '';
  users: any[] = [];
  filtered: any[] = [];
  roles: string[] = [];
  displayedColumns = ['username', 'email', 'roles', 'actions'];

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.get<any>(`users?page=1&pageSize=50&q=${encodeURIComponent(this.search)}`).subscribe(res => {
      this.users = res.items ?? res; 
      this.filtered = [...this.users];
      this.roles = Array.from(new Set(this.users.flatMap(u => u.roles || [])));
    });
  }

  applyClientFilters() {
    this.filtered = this.users.filter(u => {
      const roleOk = !this.roleFilter || (u.roles || []).includes(this.roleFilter);
      const emailOk = !this.emailFilter || (u.email || '').toLowerCase().includes(this.emailFilter.toLowerCase());
      return roleOk && emailOk;
    });
  }

  add() {
    this.router.navigate(['/users', 'new']);
  }

  edit(id: string) {
    this.router.navigate(['/users', id]);
  }

  permissions(id: string) {
    this.router.navigate(['/users', id, 'permissions']);
  }
}
