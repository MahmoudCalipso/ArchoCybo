import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { ApiService } from '../../core/api.service';
import { SignalRService } from '../../core/signalr.service';
import { Subscription } from 'rxjs';
import { SchemaDesignerComponent } from '../schema-designer/schema-designer.component';
import { QueryBuilderComponent } from '../query-builder/query-builder.component';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    SchemaDesignerComponent,
    QueryBuilderComponent
  ],
  template: `
    <mat-toolbar color="primary">
      <span>Project: {{ project?.name }}</span>
      <span style="flex: 1 1 auto;"></span>
      <button mat-icon-button (click)="refresh()"><mat-icon>refresh</mat-icon></button>
      <button mat-raised-button color="accent" (click)="generate()">Generate Code</button>
    </mat-toolbar>
    
    <div class="container" *ngIf="projectId">
      <mat-tab-group>
        <mat-tab label="Schema Designer">
          <app-schema-designer 
            [projectId]="projectId" 
            [entities]="entities">
          </app-schema-designer>
        </mat-tab>
        
        <mat-tab label="Query Builder">
          <app-query-builder 
            [projectId]="projectId" 
            [entities]="entities"
            [queries]="queries">
          </app-query-builder>
        </mat-tab>

        <mat-tab label="Settings">
          <div class="p-3">
             <h3>Project Settings</h3>
             <p>Database: {{ project?.databaseType }}</p>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .container {
      height: calc(100vh - 64px);
      display: flex;
      flex-direction: column;
    }
    mat-tab-group { flex: 1; }
    .p-3 { padding: 1rem; }
  `]
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);
  private signal = inject(SignalRService);

  projectId?: string;
  project?: any;
  entities: any[] = [];
  queries: any[] = [];
  sub?: Subscription;

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    this.load();
    this.signal.start();
    this.sub = this.signal.onProjectUpdated().subscribe(id => {
      if (id === this.projectId) {
        this.load();
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  refresh(): void {
    this.load();
  }

  generate(): void {
    if (!this.projectId) return;
    this.api.post(`projects/${this.projectId}/generate`, {}).subscribe(res => {
      // In real app, this would return a blob/download url
      // For now we assume the backend handles the generation trigger
      alert('Generation started! Download will be available shortly.');
    });
  }

  private load(): void {
    if (!this.projectId) return;
    
    this.api.get<any>(`projects/${this.projectId}`).subscribe(res => this.project = res);
    
    this.api.get<any[]>(`projects/${this.projectId}/entities`).subscribe({
      next: (res) => this.entities = res
    });
    this.api.get<any[]>(`projects/${this.projectId}/queries`).subscribe({
      next: (res) => this.queries = res
    });
  }
}

