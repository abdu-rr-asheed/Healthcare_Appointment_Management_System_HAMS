import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
}

export interface TableConfig {
  columns: TableColumn[];
  pageSize?: number;
  showPagination?: boolean;
  showSearch?: boolean;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="data-table-container">
      <div class="table-header" *ngIf="config.showSearch">
        <input 
          type="text" 
          class="search-input"
          placeholder="Search..."
          [(ngModel)]="searchTerm"
          (ngModelChange)="onSearch()">
      </div>
      
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th 
                *ngFor="let col of config.columns"
                [style.width]="col.width"
                (click)="col.sortable && toggleSort(col.key)"
                [class.sortable]="col.sortable">
                {{ col.label }}
                <span *ngIf="col.sortable && sortKey === col.key" class="sort-icon">
                  {{ sortDirection === 'asc' ? '↑' : '↓' }}
                </span>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of paginatedData()" (click)="rowClicked.emit(row)">
              <td *ngFor="let col of config.columns">
                <ng-container *ngIf="!col.key.includes('.'); else nested">
                  {{ row[col.key] }}
                </ng-container>
                <ng-template #nested>
                  {{ getNestedValue(row, col.key) }}
                </ng-template>
              </td>
            </tr>
            <tr *ngIf="filteredData().length === 0">
              <td [attr.colspan]="config.columns.length" class="no-data">
                No data available
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <div class="table-footer" *ngIf="config.showPagination">
        <span class="page-info">
          Showing {{ startIndex() + 1 }}-{{ endIndex() }} of {{ filteredData().length }}
        </span>
        <div class="pagination">
          <button 
            class="page-btn" 
            [disabled]="currentPage() === 1"
            (click)="goToPage(currentPage() - 1)">
            Previous
          </button>
          <span class="page-numbers">
            <button 
              *ngFor="let page of visiblePages()"
              class="page-num"
              [class.active]="page === currentPage()"
              (click)="goToPage(page)">
              {{ page }}
            </button>
          </span>
          <button 
            class="page-btn" 
            [disabled]="currentPage() >= totalPages()"
            (click)="goToPage(currentPage() + 1)">
            Next
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .data-table-container {
      background: var(--surface-color, #fff);
      border-radius: 12px;
      overflow: hidden;
    }
    .table-header {
      padding: 16px;
      border-bottom: 1px solid #e5e7eb;
    }
    .search-input {
      width: 100%;
      max-width: 300px;
      padding: 10px 16px;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      font-size: 14px;
    }
    .search-input:focus {
      outline: none;
      border-color: #2563eb;
    }
    .table-wrapper {
      overflow-x: auto;
    }
    .data-table {
      width: 100%;
      border-collapse: collapse;
    }
    .data-table th, .data-table td {
      padding: 12px 16px;
      text-align: left;
      border-bottom: 1px solid #e5e7eb;
    }
    .data-table th {
      background: #f8fafc;
      font-weight: 600;
      font-size: 13px;
      color: #6b7280;
      text-transform: uppercase;
    }
    .data-table th.sortable {
      cursor: pointer;
    }
    .data-table th.sortable:hover {
      background: #f1f5f9;
    }
    .sort-icon {
      margin-left: 4px;
    }
    .data-table tbody tr {
      transition: background 0.2s;
      cursor: pointer;
    }
    .data-table tbody tr:hover {
      background: #f9fafb;
    }
    .no-data {
      text-align: center;
      color: #9ca3af;
      padding: 32px !important;
    }
    .table-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px;
      border-top: 1px solid #e5e7eb;
    }
    .page-info {
      font-size: 14px;
      color: #6b7280;
    }
    .pagination {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .page-btn {
      padding: 8px 16px;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      background: white;
      cursor: pointer;
      font-size: 14px;
    }
    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .page-btn:hover:not(:disabled) {
      background: #f3f4f6;
    }
    .page-numbers {
      display: flex;
      gap: 4px;
    }
    .page-num {
      width: 32px;
      height: 32px;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      background: white;
      cursor: pointer;
      font-size: 14px;
    }
    .page-num.active {
      background: #2563eb;
      color: white;
      border-color: #2563eb;
    }
  `]
})
export class DataTableComponent {
  @Input() data: any[] = [];
  @Input() config: TableConfig = { columns: [], showPagination: true, showSearch: true };
  @Output() rowClicked = new EventEmitter<any>();

  searchTerm = '';
  sortKey = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentPage = signal(1);
  pageSize = 10;

  filteredData = signal<any[]>([]);

  ngOnChanges(): void {
    this.applyFilter();
  }

  applyFilter(): void {
    let result = [...this.data];
    
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(row => 
        Object.values(row).some(val => 
          String(val).toLowerCase().includes(term)
        )
      );
    }

    if (this.sortKey) {
      result.sort((a, b) => {
        const aVal = this.getNestedValue(a, this.sortKey);
        const bVal = this.getNestedValue(b, this.sortKey);
        const comparison = String(aVal).localeCompare(String(bVal));
        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    this.filteredData.set(result);
    this.currentPage.set(1);
  }

  onSearch(): void {
    this.applyFilter();
  }

  toggleSort(key: string): void {
    if (this.sortKey === key) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = key;
      this.sortDirection = 'asc';
    }
    this.applyFilter();
  }

  getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((acc, part) => acc && acc[part], obj);
  }

  startIndex = () => (this.currentPage() - 1) * this.pageSize;
  endIndex = () => Math.min(this.startIndex() + this.pageSize, this.filteredData().length);
  totalPages = () => Math.ceil(this.filteredData().length / this.pageSize);

  paginatedData = () => {
    const start = this.startIndex();
    const end = this.endIndex();
    return this.filteredData().slice(start, end);
  };

  visiblePages = () => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages = [];
    
    let start = Math.max(1, current - 2);
    let end = Math.min(total, current + 2);
    
    if (end - start < 4) {
      if (start === 1) {
        end = Math.min(5, total);
      } else {
        start = Math.max(1, total - 4);
      }
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  };

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }
}