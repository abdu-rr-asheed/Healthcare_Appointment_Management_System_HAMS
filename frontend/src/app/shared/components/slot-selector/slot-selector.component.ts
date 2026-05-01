import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SlotOption {
  id: string;
  startTime: Date;
  endTime: Date;
  clinicianName?: string;
  isAvailable: boolean;
}

@Component({
  selector: 'app-slot-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="slot-selector">
      <div class="slot-header">
        <button class="nav-btn" (click)="previousWeek()" [disabled]="isPastWeek()">
          &lt; Previous
        </button>
        <span class="week-label">{{ getWeekLabel() }}</span>
        <button class="nav-btn" (click)="nextWeek()">
          Next &gt;
        </button>
      </div>
      
      <div class="slots-grid">
        <div class="day-column" *ngFor="let day of weekDays()">
          <div class="day-header">
            <span class="day-name">{{ day.name }}</span>
            <span class="day-date">{{ day.date | date:'d MMM' }}</span>
          </div>
          
          <div class="slots-container">
            <button
              *ngFor="let slot of getSlotsForDay(day.date)"
              class="slot-btn"
              [class.available]="slot.isAvailable"
              [class.unavailable]="!slot.isAvailable"
              [class.selected]="isSelected(slot)"
              [disabled]="!slot.isAvailable"
              (click)="selectSlot(slot)">
              {{ slot.startTime | date:'HH:mm' }}
            </button>
            
            <p *ngIf="getSlotsForDay(day.date).length === 0" class="no-slots">
              No slots available
            </p>
          </div>
        </div>
      </div>
      
      <div class="selection-info" *ngIf="selectedSlot()">
        <span>Selected: </span>
        <strong>{{ selectedSlot()!.startTime | date:'EEEE, d MMMM yyyy HH:mm' }}</strong>
        <button class="confirm-btn" (click)="confirmSelection()">Confirm</button>
      </div>
    </div>
  `,
  styles: [`
    .slot-selector {
      background: var(--surface-color, #fff);
      border-radius: 12px;
      padding: 20px;
    }
    .slot-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .week-label {
      font-size: 18px;
      font-weight: 600;
    }
    .nav-btn {
      padding: 8px 16px;
      background: #f3f4f6;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 500;
    }
    .nav-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .nav-btn:hover:not(:disabled) {
      background: #e5e7eb;
    }
    .slots-grid {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 8px;
    }
    .day-column {
      min-width: 100px;
    }
    .day-header {
      text-align: center;
      padding: 8px;
      background: #f8fafc;
      border-radius: 8px;
      margin-bottom: 8px;
    }
    .day-name {
      display: block;
      font-weight: 600;
      font-size: 13px;
    }
    .day-date {
      display: block;
      font-size: 12px;
      color: #6b7280;
    }
    .slots-container {
      display: flex;
      flex-direction: column;
      gap: 4px;
      max-height: 300px;
      overflow-y: auto;
    }
    .slot-btn {
      padding: 8px;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      background: white;
      cursor: pointer;
      font-size: 13px;
      transition: all 0.2s;
    }
    .slot-btn.available:hover {
      background: #dbeafe;
      border-color: #2563eb;
    }
    .slot-btn.selected {
      background: #2563eb;
      color: white;
      border-color: #2563eb;
    }
    .slot-btn.unavailable {
      background: #f3f4f6;
      color: #9ca3af;
      cursor: not-allowed;
      text-decoration: line-through;
    }
    .no-slots {
      text-align: center;
      color: #9ca3af;
      font-size: 12px;
      padding: 16px;
    }
    .selection-info {
      margin-top: 20px;
      padding: 16px;
      background: #f0f9ff;
      border-radius: 8px;
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .confirm-btn {
      margin-left: auto;
      padding: 10px 24px;
      background: #2563eb;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 600;
    }
    .confirm-btn:hover {
      background: #1d4ed8;
    }
    @media (max-width: 768px) {
      .slots-grid {
        grid-template-columns: repeat(1, 1fr);
      }
    }
  `]
})
export class SlotSelectorComponent {
  @Input() slots: SlotOption[] = [];
  @Output() slotSelected = new EventEmitter<SlotOption>();

  selectedSlot = signal<SlotOption | null>(null);
  currentWeekStart = signal(this.getStartOfWeek(new Date()));

  weekDays = computed(() => {
    const start = this.currentWeekStart();
    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(start);
      date.setDate(start.getDate() + i);
      return {
        date: new Date(date),
        name: date.toLocaleDateString('en-GB', { weekday: 'short' })
      };
    });
  });

  getSlotsForDay(date: Date): SlotOption[] {
    return this.slots.filter(slot => {
      const slotDate = new Date(slot.startTime);
      return slotDate.toDateString() === date.toDateString();
    }).sort((a, b) => 
      new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
    );
  }

  getWeekLabel(): string {
    const start = this.currentWeekStart();
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    return `${start.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })} - ${end.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}`;
  }

  isPastWeek(): boolean {
    const weekStart = this.currentWeekStart();
    const todayStart = this.getStartOfWeek(new Date());
    return weekStart < todayStart;
  }

  previousWeek(): void {
    const current = this.currentWeekStart();
    const newDate = new Date(current);
    newDate.setDate(current.getDate() - 7);
    this.currentWeekStart.set(newDate);
  }

  nextWeek(): void {
    const current = this.currentWeekStart();
    const newDate = new Date(current);
    newDate.setDate(current.getDate() + 7);
    this.currentWeekStart.set(newDate);
  }

  selectSlot(slot: SlotOption): void {
    if (slot.isAvailable) {
      this.selectedSlot.set(slot);
    }
  }

  isSelected(slot: SlotOption): boolean {
    return this.selectedSlot()?.id === slot.id;
  }

  confirmSelection(): void {
    const slot = this.selectedSlot();
    if (slot) {
      this.slotSelected.emit(slot);
    }
  }

  private getStartOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  }
}