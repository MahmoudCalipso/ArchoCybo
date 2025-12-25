import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { ApiService, CreateProjectDto } from '../../core/api.service';

@Component({
  selector: 'app-project-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatInputModule,
    MatIconModule,
    MatStepperModule
  ],
  template: `
    <div class="container">
      <h1>Create New Project</h1>
      
      <mat-stepper [linear]="true" #stepper>
        <!-- Step 1: Basic Info -->
        <mat-step [stepControl]="infoForm">
          <form [formGroup]="infoForm">
            <ng-template matStepLabel>Project Info</ng-template>
            
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Project Name</mat-label>
              <input matInput formControlName="name" placeholder="MyAwesomeApi">
              <mat-error *ngIf="infoForm.get('name')?.hasError('required')">Name is required</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="3"></textarea>
            </mat-form-field>

            <div class="actions">
              <button mat-button routerLink="/projects">Cancel</button>
              <button mat-raised-button color="primary" matStepperNext>Next</button>
            </div>
          </form>
        </mat-step>

        <!-- Step 2: Database Selection -->
        <mat-step [stepControl]="dbForm">
          <form [formGroup]="dbForm">
            <ng-template matStepLabel>Database</ng-template>
            
            <h3>Select Database Type</h3>
            <div class="db-grid">
              <div class="db-card" 
                   [class.selected]="dbForm.get('databaseType')?.value === 0"
                   (click)="selectDb(0)">
                <mat-icon class="db-icon">storage</mat-icon>
                <div class="db-name">SQL Server</div>
                <div class="db-desc">Microsoft SQL Server (Default)</div>
              </div>

              <div class="db-card" 
                   [class.selected]="dbForm.get('databaseType')?.value === 1"
                   (click)="selectDb(1)">
                <mat-icon class="db-icon">dns</mat-icon>
                <div class="db-name">PostgreSQL</div>
                <div class="db-desc">Advanced Open Source DB</div>
              </div>

              <div class="db-card" 
                   [class.selected]="dbForm.get('databaseType')?.value === 2"
                   (click)="selectDb(2)">
                <mat-icon class="db-icon">table_chart</mat-icon>
                <div class="db-name">MySQL</div>
                <div class="db-desc">Popular Open Source DB</div>
              </div>

              <div class="db-card" 
                   [class.selected]="dbForm.get('databaseType')?.value === 3"
                   (click)="selectDb(3)">
                <mat-icon class="db-icon">smartphone</mat-icon>
                <div class="db-name">SQLite</div>
                <div class="db-desc">Lightweight embedded DB</div>
              </div>
            </div>
            <input type="hidden" formControlName="databaseType">

            <div class="actions">
              <button mat-button matStepperPrevious>Back</button>
              <button mat-raised-button color="primary" matStepperNext [disabled]="dbForm.invalid">Next</button>
            </div>
          </form>
        </mat-step>

        <!-- Step 3: Review & Create -->
        <mat-step>
          <ng-template matStepLabel>Review</ng-template>
          
          <div class="review-section">
            <p><strong>Name:</strong> {{ infoForm.get('name')?.value }}</p>
            <p><strong>Description:</strong> {{ infoForm.get('description')?.value || 'N/A' }}</p>
            <p><strong>Database:</strong> {{ getDbName(dbForm.get('databaseType')?.value) }}</p>
          </div>

          <div class="actions">
            <button mat-button matStepperPrevious>Back</button>
            <button mat-raised-button color="accent" (click)="createProject()" [disabled]="isSubmitting">
              {{ isSubmitting ? 'Creating...' : 'Create Project' }}
            </button>
          </div>
        </mat-step>
      </mat-stepper>
    </div>
  `,
  styles: [`
    .container { max-width: 800px; margin: 2rem auto; padding: 1rem; }
    .full-width { width: 100%; margin-bottom: 1rem; }
    .actions { margin-top: 1rem; display: flex; gap: 1rem; justify-content: flex-end; }
    
    .db-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
      gap: 1.5rem;
      margin: 2rem 0;
    }
    
    .db-card {
      border: 2px solid #e0e0e0;
      border-radius: 8px;
      padding: 1.5rem;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      
      &:hover {
        border-color: #3f51b5;
        background: #f5f5f5;
      }
      
      &.selected {
        border-color: #3f51b5;
        background: #e8eaf6;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      }
      
      .db-icon {
        font-size: 48px;
        height: 48px;
        width: 48px;
        margin-bottom: 1rem;
        color: #555;
      }
      
      .db-name {
        font-weight: bold;
        margin-bottom: 0.5rem;
      }
      
      .db-desc {
        font-size: 0.8rem;
        color: #777;
      }
    }
  `]
})
export class ProjectCreateComponent {
  infoForm: FormGroup;
  dbForm: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private router: Router
  ) {
    this.infoForm = this.fb.group({
      name: ['', Validators.required],
      description: ['']
    });

    this.dbForm = this.fb.group({
      databaseType: [0, Validators.required]
    });
  }

  selectDb(type: number) {
    this.dbForm.patchValue({ databaseType: type });
  }

  getDbName(type: number): string {
    switch(type) {
      case 0: return 'SQL Server';
      case 1: return 'PostgreSQL';
      case 2: return 'MySQL';
      case 3: return 'SQLite';
      default: return 'Unknown';
    }
  }

  createProject() {
    if (this.infoForm.invalid || this.dbForm.invalid) return;

    this.isSubmitting = true;
    const dto: CreateProjectDto = {
      ...this.infoForm.value,
      ...this.dbForm.value,
      useBaseRoles: true
    };

    this.api.createProject(dto).subscribe({
      next: (res) => {
        this.router.navigate(['/projects', res.id]);
      },
      error: (err) => {
        console.error(err);
        this.isSubmitting = false;
        alert('Failed to create project');
      }
    });
  }
}
