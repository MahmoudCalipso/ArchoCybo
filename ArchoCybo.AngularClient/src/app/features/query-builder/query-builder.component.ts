import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-query-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatSelectModule,
    MatInputModule,
    MatIconModule,
    MatTabsModule,
    MatExpansionModule
  ],
  template: `
    <div class="qb-container">
      <div class="sidebar">
        <h3>Queries</h3>
        <button mat-raised-button color="primary" class="full-width" (click)="newQuery()">
          <mat-icon>add</mat-icon> New Query
        </button>
        <div class="query-list">
          <div *ngFor="let q of queries" class="query-item" (click)="selectQuery(q)">
            {{ q.name }}
          </div>
        </div>
      </div>
      
      <div class="main-content" *ngIf="currentQuery">
        <mat-card>
          <div class="header">
            <mat-form-field appearance="outline">
              <mat-label>Query Name</mat-label>
              <input matInput [(ngModel)]="currentQuery.name">
            </mat-form-field>
            <button mat-raised-button color="primary" (click)="saveQuery()">Save</button>
          </div>

          <div class="builder-section">
            <h4>From Table</h4>
            <mat-form-field appearance="outline">
              <mat-label>Select Entity</mat-label>
              <mat-select [(ngModel)]="sourceEntityId" (selectionChange)="onEntityChange()">
                <mat-option *ngFor="let e of entities" [value]="e.id">{{ e.name }}</mat-option>
              </mat-select>
            </mat-form-field>

            <h4>Fields</h4>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Select Fields</mat-label>
              <mat-select multiple [(ngModel)]="selectedFields" (selectionChange)="updatePreview()">
                <mat-option *ngFor="let f of availableFields" [value]="f">{{ f }}</mat-option>
              </mat-select>
            </mat-form-field>

            <h4>Filter</h4>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>WHERE</mat-label>
              <input matInput [(ngModel)]="filterText" (input)="updatePreview()" placeholder="e.g. Name LIKE '%john%'">
            </mat-form-field>
          </div>

          <!-- Joins would go here -->
          
          <!-- Filters would go here -->

          <div class="output-section">
            <h4>Generated Code</h4>
            <mat-tab-group (selectedTabChange)="onTabChange($event)">
              <mat-tab label="SQL">
                <div class="code-block">{{ generatedSql }}</div>
              </mat-tab>
              <mat-tab label="MongoDB Shell">
                <div class="code-block">{{ generatedMongo }}</div>
              </mat-tab>
              <mat-tab label="C# LINQ">
                <div class="code-block">{{ generatedLinq }}</div>
              </mat-tab>
            </mat-tab-group>
          </div>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .qb-container {
      display: flex;
      height: 100%;
      background: #f5f5f5;
    }
    .sidebar {
      width: 250px;
      background: white;
      border-right: 1px solid #ddd;
      padding: 1rem;
    }
    .main-content {
      flex: 1;
      padding: 1rem;
      overflow: auto;
    }
    .full-width { width: 100%; margin-bottom: 1rem; }
    .query-item {
      padding: 0.5rem;
      cursor: pointer;
      &:hover { background: #eee; }
    }
    .header { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; }
    .code-block {
      background: #1e1e1e;
      color: #d4d4d4;
      padding: 1rem;
      font-family: monospace;
      border-radius: 4px;
      min-height: 100px;
      white-space: pre-wrap;
    }
  `]
})
export class QueryBuilderComponent implements OnInit {
  @Input() projectId!: string;
  @Input() entities: any[] = [];
  @Input() queries: any[] = [];

  currentQuery: any = null;
  sourceEntityId: string = '';
  availableFields: string[] = [];
  selectedFields: string[] = [];
  filterText = '';

  generatedSql = '';
  generatedMongo = '';
  generatedLinq = '';

  constructor(private api: ApiService) {}

  ngOnInit() {}

  newQuery() {
    this.currentQuery = { name: 'New Query', sql: '' };
    this.sourceEntityId = '';
    this.generatedSql = '';
  }

  selectQuery(q: any) {
    this.currentQuery = { ...q };
    // Load definition...
  }

  onEntityChange() {
    const entity = this.entities.find(e => e.id === this.sourceEntityId);
    this.availableFields = entity ? entity.fields.map((f: any) => f.name) : [];
    this.selectedFields = [];
    this.filterText = '';
    this.updatePreview();
  }

  updatePreview() {
    if (!this.sourceEntityId) return;
    const entity = this.entities.find(e => e.id === this.sourceEntityId);
    if (!entity) return;

    const fields = this.selectedFields.length ? this.selectedFields : ['Id', 'Name', 'CreatedDate'];
    const fieldList = fields.join(', ');
    const fieldListLinq = fields.map(f => `x.${f}`).join(', ');
    const whereSql = this.filterText ? `\nWHERE ${this.filterText}` : '';

    this.generatedSql = `SELECT ${fieldList} \nFROM [${entity.tableName}] AS t0${whereSql}`;
    
    this.generatedMongo = `db.${entity.tableName}.find(\n  {},\n  { ${fields.map(f => `${f}: 1`).join(', ')} }\n)`;

    this.generatedLinq = `_context.${entity.name}s\n  .Select(x => new { ${fieldListLinq} })\n  .ToList()`;
  }

  onTabChange(event: any) {
    // Re-generate if needed
  }

  saveQuery() {
    if (!this.currentQuery) return;
    this.api.post(`projects/${this.projectId}/queries`, {
      ...this.currentQuery,
      projectId: this.projectId,
      sql: this.generatedSql
    }).subscribe();
  }
}
