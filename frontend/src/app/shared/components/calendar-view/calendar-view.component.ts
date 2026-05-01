import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface CalendarEvent {
  id: string;
  title: string;
  start: Date;
  end?: Date;
  color?: string;
  type?: string;
}

@Component({
  selector: 'app-calendar-view',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="calendar-view">
      <div class="calendar-header">
        <button class="nav-btn" (click)="previousMonth()">&lt;</button>
        <h3>{{ currentMonthYear() }}</h3>
        <button class="nav-btn" (click)="nextMonth()">&gt;</button>
        <button class="today-btn" (click)="goToToday()">Today</button>
      </div>
      
      <div class="calendar-grid">
        <div class="weekday-header" *ngFor="let day of weekdays">
          {{ day }}
        </div>
        
        <div 
          class="calendar-day" 
          *ngFor="let day of calendarDays()"
          [class.other-month]="!day.isCurrentMonth"
          [class.today]="day.isToday"
          [class.has-events]="day.events.length > 0"
          (click)="onDayClick(day)">
          <span class="day-number">{{ day.date.getDate() }}</span>
          <div class="events-container">
            <div 
              class="event" 
              *ngFor="let event of day.events.slice(0, 3)"
              [style.background-color]="event.color || '#2563eb'"
              (click)="onEventClick(event, $event)">
              {{ event.title }}
            </div>
            <span *ngIf="day.events.length > 3" class="more-events">
              +{{ day.events.length - 3 }} more
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .calendar-view {
      background: var(--surface-color, #fff);
      border-radius: 12px;
      padding: 20px;
    }
    .calendar-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 20px;
    }
    .calendar-header h3 {
      flex: 1;
      text-align: center;
      margin: 0;
    }
    .nav-btn {
      width: 36px;
      height: 36px;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      background: white;
      cursor: pointer;
      font-size: 18px;
    }
    .nav-btn:hover { background: #f3f4f6; }
    .today-btn {
      padding: 8px 16px;
      background: #2563eb;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
    }
    .today-btn:hover { background: #1d4ed8; }
    .calendar-grid {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 1px;
      background: #e5e7eb;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      overflow: hidden;
    }
    .weekday-header {
      background: #f8fafc;
      padding: 12px;
      text-align: center;
      font-weight: 600;
      font-size: 13px;
      color: #6b7280;
    }
    .calendar-day {
      background: white;
      min-height: 100px;
      padding: 8px;
      cursor: pointer;
      transition: background 0.2s;
    }
    .calendar-day:hover { background: #f9fafb; }
    .calendar-day.other-month { background: #f9fafb; }
    .calendar-day.other-month .day-number { color: #9ca3af; }
    .calendar-day.today { background: #eff6ff; }
    .calendar-day.today .day-number {
      background: #2563eb;
      color: white;
      border-radius: 50%;
      width: 28px;
      height: 28px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .day-number {
      font-weight: 600;
      font-size: 14px;
      margin-bottom: 4px;
    }
    .events-container {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .event {
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 11px;
      color: white;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      cursor: pointer;
    }
    .event:hover { opacity: 0.9; }
    .more-events {
      font-size: 11px;
      color: #6b7280;
      cursor: pointer;
    }
  `]
})
export class CalendarViewComponent {
  @Input() events: CalendarEvent[] = [];
  @Input() viewMode: 'month' | 'week' = 'month';
  @Output() dayClicked = new EventEmitter<Date>();
  @Output() eventClicked = new EventEmitter<CalendarEvent>();

  weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  currentDate = signal(new Date());

  currentMonthYear = computed(() => {
    return this.currentDate().toLocaleDateString('en-GB', { 
      month: 'long', 
      year: 'numeric' 
    });
  });

  calendarDays = computed(() => {
    const current = this.currentDate();
    const year = current.getFullYear();
    const month = current.getMonth();
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    
    const days: { date: Date; isCurrentMonth: boolean; isToday: boolean; events: CalendarEvent[] }[] = [];
    
    const startDay = firstDay.getDay() || 7;
    for (let i = startDay - 1; i > 0; i--) {
      const date = new Date(year, month, 1 - i);
      days.push({
        date,
        isCurrentMonth: false,
        isToday: this.isToday(date),
        events: this.getEventsForDate(date)
      });
    }
    
    for (let i = 1; i <= lastDay.getDate(); i++) {
      const date = new Date(year, month, i);
      days.push({
        date,
        isCurrentMonth: true,
        isToday: this.isToday(date),
        events: this.getEventsForDate(date)
      });
    }
    
    const remaining = 42 - days.length;
    for (let i = 1; i <= remaining; i++) {
      const date = new Date(year, month + 1, i);
      days.push({
        date,
        isCurrentMonth: false,
        isToday: this.isToday(date),
        events: this.getEventsForDate(date)
      });
    }
    
    return days;
  });

  getEventsForDate(date: Date): CalendarEvent[] {
    return this.events.filter(event => {
      const eventDate = new Date(event.start);
      return eventDate.toDateString() === date.toDateString();
    });
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  previousMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
  }

  nextMonth(): void {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
  }

  goToToday(): void {
    this.currentDate.set(new Date());
  }

  onDayClick(day: { date: Date }): void {
    this.dayClicked.emit(day.date);
  }

  onEventClick(event: CalendarEvent, e: Event): void {
    e.stopPropagation();
    this.eventClicked.emit(event);
  }
}