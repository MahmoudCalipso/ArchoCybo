import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="container">
      <div class="header">
        <h2>My Projects</h2>
        <button mat-raised-button color="primary" routerLink="/projects/new">
          <mat-icon>add</mat-icon> New Project
        </button>
      </div>
      <div class="grid">
        <mat-card *ngFor="let p of projects" class="card" (click)="open(p.id)">
          <div class="name">{{ p.name }}</div>
          <div class="meta">Created: {{ p.createdAt | date:'medium' }}</div>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 1000px; margin: 2rem auto; padding: 0 1rem; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill,minmax(240px,1fr)); gap: 1rem; }
    .card { cursor: pointer; padding: 1rem; }
    .name { font-weight: 600; }
    .meta { color: #666; font-size: 0.85rem; margin-top: .5rem; }
  `]
})
export class ProjectListComponent implements OnInit {
  projects: any[] = [];
  constructor(private api: ApiService, private router: Router) {}
  ngOnInit() {
    this.api.get<any[]>('project').subscribe(res => this.projects = res);
  }
  open(id: string) {
    this.router.navigate(['/projects', id]);
  }
}
