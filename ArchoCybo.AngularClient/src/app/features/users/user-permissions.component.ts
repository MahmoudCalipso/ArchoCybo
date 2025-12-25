import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-user-permissions',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatListModule,
    MatCheckboxModule,
    MatButtonModule
  ],
  template: `
    <div class="container">
      <mat-card>
        <h2>User Permissions</h2>
        <div class="list">
          <div class="row header">
            <div class="col endpoint">Endpoint</div>
            <div class="col method">Method</div>
            <div class="col access">Has Access</div>
          </div>
          <div class="row" *ngFor="let e of endpoints">
            <div class="col endpoint">{{ e.endpoint }}</div>
            <div class="col method">{{ e.method }}</div>
            <div class="col access">
              <mat-checkbox [(ngModel)]="e.hasAccess"></mat-checkbox>
            </div>
          </div>
        </div>
        <div class="actions">
          <button mat-button (click)="back()">Cancel</button>
          <button mat-raised-button color="primary" (click)="save()">Save</button>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .container { max-width: 1000px; margin: 2rem auto; padding: 0 1rem; }
    .list { margin-top: 1rem; }
    .row { display: grid; grid-template-columns: 1fr 120px 120px; padding: .5rem; border-bottom: 1px solid #eee; align-items: center; }
    .header { font-weight: 600; background: #fafafa; }
    .actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 1rem; }
  `]
})
export class UserPermissionsComponent implements OnInit {
  userId = '';
  endpoints: any[] = [];

  constructor(private api: ApiService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    this.api.get<any[]>(`users/${this.userId}/endpoints`).subscribe(res => this.endpoints = res);
  }

  save() {
    const allowedIds = this.endpoints
      .filter(e => e.hasAccess && e.permissionId)
      .map(e => e.permissionId);
    this.api.put(`users/${this.userId}/permissions`, { userId: this.userId, allowedPermissionIds: allowedIds }).subscribe({
      next: () => this.router.navigate(['/users']),
      error: () => alert('Failed to save permissions')
    });
  }

  back() {
    this.router.navigate(['/users']);
  }
}
