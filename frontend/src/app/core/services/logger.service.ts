import { Injectable } from '@angular/core';

export interface LogEntry {
  level: 'debug' | 'info' | 'warn' | 'error';
  message: string;
  data?: any;
  timestamp: Date;
  correlationId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LoggerService {
  private logs: LogEntry[] = [];
  private maxLogs = 1000;

  debug(message: string, data?: any, correlationId?: string): void {
    this.log('debug', message, data, correlationId);
  }

  info(message: string, data?: any, correlationId?: string): void {
    this.log('info', message, data, correlationId);
  }

  warn(message: string, data?: any, correlationId?: string): void {
    this.log('warn', message, data, correlationId);
  }

  error(message: string, data?: any, correlationId?: string): void {
    this.log('error', message, data, correlationId);
  }

  private log(level: LogEntry['level'], message: string, data?: any, correlationId?: string): void {
    const entry: LogEntry = {
      level,
      message,
      data,
      timestamp: new Date(),
      correlationId
    };

    this.logs.push(entry);
    if (this.logs.length > this.maxLogs) {
      this.logs.shift();
    }

    const formattedMessage = correlationId 
      ? `[${correlationId}] ${message}` 
      : message;

    switch (level) {
      case 'debug':
        console.debug(formattedMessage, data);
        break;
      case 'info':
        console.info(formattedMessage, data);
        break;
      case 'warn':
        console.warn(formattedMessage, data);
        break;
      case 'error':
        console.error(formattedMessage, data);
        break;
    }
  }

  getLogs(): LogEntry[] {
    return [...this.logs];
  }

  clearLogs(): void {
    this.logs = [];
  }

  exportLogs(): string {
    return JSON.stringify(this.logs, null, 2);
  }
}