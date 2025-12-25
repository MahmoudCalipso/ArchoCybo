import { Component, Input, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DragDropModule, CdkDragEnd } from '@angular/cdk/drag-drop';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ApiService } from '../../core/api.service';

interface EntityNode {
  id: string;
  name: string;
  tableName: string;
  fields: any[];
  x: number;
  y: number;
}

@Component({
  selector: 'app-schema-designer',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  template: `
    <div class="designer-container">
      <div class="toolbar">
        <button mat-raised-button color="primary" (click)="addEntity()">
          <mat-icon>add</mat-icon> Add Entity
        </button>
        <button mat-button (click)="saveLayout()">
          <mat-icon>save</mat-icon> Save Layout
        </button>
        <button mat-button (click)="toggleTableMode()">
          <mat-icon>table_chart</mat-icon> Table Mode
        </button>
      </div>

      <div class="canvas" #canvas>
        <div *ngFor="let node of nodes"
             class="entity-node"
             [class.table]="tableMode"
             cdkDrag
             [cdkDragFreeDragPosition]="{x: node.x, y: node.y}"
             (cdkDragEnded)="onDragEnd($event, node)">
          
          <div class="node-header" cdkDragHandle>
            <span class="node-title">{{ node.name }}</span>
            <button mat-icon-button class="node-menu"><mat-icon>more_vert</mat-icon></button>
          </div>
          
          <div class="node-content">
            <div class="field-list">
              <div *ngFor="let field of node.fields" class="field-item">
                <span class="field-name">{{ field.name }}</span>
                <span class="field-type">{{ field.dataType }}</span>
              </div>
            </div>
            <button mat-button class="add-field-btn" (click)="addField(node)">
              <mat-icon>add</mat-icon> Field
            </button>
          </div>
        </div>
        
        <!-- SVG Layer for relations (placeholder) -->
        <svg class="connections-layer">
          <!-- Lines would go here -->
        </svg>
      </div>
    </div>
  `,
  styles: [`
    .designer-container {
      height: 100%;
      display: flex;
      flex-direction: column;
      background: #f0f2f5;
    }
    
    .toolbar {
      padding: 1rem;
      background: white;
      border-bottom: 1px solid #ddd;
      display: flex;
      gap: 1rem;
    }
    
    .canvas {
      flex: 1;
      position: relative;
      overflow: auto;
      min-height: 600px;
    }
    
    .entity-node {
      position: absolute;
      width: 250px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      border: 1px solid #e0e0e0;
      z-index: 10;
    }
    .entity-node.table {
      border-radius: 4px;
      width: 300px;
    }
    
    .node-header {
      padding: 0.5rem 1rem;
      background: #3f51b5;
      color: white;
      border-top-left-radius: 8px;
      border-top-right-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      cursor: move;
    }
    
    .node-title {
      font-weight: bold;
    }
    
    .node-content {
      padding: 0.5rem;
    }
    
    .field-item {
      display: flex;
      justify-content: space-between;
      padding: 4px 8px;
      border-bottom: 1px solid #eee;
      font-size: 0.9rem;
    }
    
    .field-type {
      color: #888;
      font-size: 0.8rem;
    }
    
    .add-field-btn {
      width: 100%;
      margin-top: 0.5rem;
      font-size: 0.8rem;
    }
    
    .connections-layer {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 0;
    }
  `]
})
export class SchemaDesignerComponent implements OnInit, OnChanges {
  @Input() projectId!: string;
  @Input() entities: any[] = [];
  
  nodes: EntityNode[] = [];
  tableMode = true;

  constructor(private api: ApiService, private dialog: MatDialog) {}

  ngOnInit() {
    this.initNodes();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['entities']) {
      this.initNodes();
    }
  }

  initNodes() {
    // Convert entities to nodes with positions
    // In a real app, positions would be saved/loaded.
    // Here we auto-layout simple grid
    this.nodes = this.entities.map((e, i) => ({
      ...e,
      x: (i % 3) * 300 + 50,
      y: Math.floor(i / 3) * 300 + 50
    }));
  }

  onDragEnd(event: CdkDragEnd, node: EntityNode) {
    const element = event.source.getRootElement();
    const boundingClientRect = element.getBoundingClientRect();
    const parentPosition = this.getPosition(element.parentElement!);
    
    node.x = boundingClientRect.x - parentPosition.x;
    node.y = boundingClientRect.y - parentPosition.y;
  }

  getPosition(el: HTMLElement) {
    let x = 0;
    let y = 0;
    while (el && !isNaN(el.offsetLeft) && !isNaN(el.offsetTop)) {
      x += el.offsetLeft - el.scrollLeft;
      y += el.offsetTop - el.scrollTop;
      el = el.offsetParent as HTMLElement;
    }
    return { x, y };
  }

  addEntity() {
    const name = prompt("Entity Name:");
    if (!name) return;
    
    this.api.post(`projects/${this.projectId}/entities`, { name, tableName: name }).subscribe(() => {
      // SignalR will trigger refresh in parent
    });
  }

  addField(node: EntityNode) {
    const name = prompt("Field Name:");
    if (!name) return;
    
    this.api.post(`projects/${this.projectId}/entities/${node.id}/fields`, { 
      name, 
      dataType: 'string', 
      isNullable: true 
    }).subscribe(() => {
      // Refresh handled by SignalR
    });
  }

  saveLayout() {
    // TODO: Save positions to backend (metadata or separate endpoint)
    console.log('Layout saved', this.nodes.map(n => ({ id: n.id, x: n.x, y: n.y })));
  }

  toggleTableMode() {
    this.tableMode = !this.tableMode;
  }
}
