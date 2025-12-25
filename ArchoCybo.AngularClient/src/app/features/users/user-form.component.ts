import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  template: `
    <div class="container">
      <mat-card>
        <h2>{{ isNew ? 'Add User' : 'Edit User' }}</h2>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field appearance="outline" class="full">
            <mat-label>Username</mat-label>
            <input matInput formControlName="username" [readonly]="!isNew">
          </mat-form-field>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email">
          </mat-form-field>
          <mat-form-field appearance="outline" class="full" *ngIf="isNew">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password">
          </mat-form-field>
          <div class="actions">
            <button mat-button (click)="back()">Cancel</button>
            <button mat-raised-button color="primary" [disabled]="form.invalid || loading">
              {{ loading ? 'Saving...' : 'Save' }}
            </button>
          </div>
        </form>
      </mat-card>
    </div>
  `,
  styles: [`
    .container { display: flex; justify-content: center; padding: 2rem; }
    .full { width: 100%; }
    mat-card { width: 600px; }
    .actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 1rem; }
  `]
})
export class UserFormComponent implements OnInit {
  form: FormGroup;
  isNew = false;
  loading = false;

  constructor(private fb: FormBuilder, private api: ApiService, private route: ActivatedRoute, private router: Router) {
    this.form = this.fb.group({
      id: [''],
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['']
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    this.isNew = id === 'new';
    if (!this.isNew && id) {
      // Load user details if an endpoint exists; otherwise leave form editable
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    if (this.isNew) {
      const { username, email, password } = this.form.value;
      this.api.post('users', { username, email, password }).subscribe({
        next: () => this.router.navigate(['/users']),
        error: () => { this.loading = false; alert('Failed to create user'); }
      });
    } else {
      const { id, email } = this.form.value;
      this.api.put('users', { id, email }).subscribe({
        next: () => this.router.navigate(['/users']),
        error: () => { this.loading = false; alert('Failed to update user'); }
      });
    }
  }

  back() {
    this.router.navigate(['/users']);
  }
}
