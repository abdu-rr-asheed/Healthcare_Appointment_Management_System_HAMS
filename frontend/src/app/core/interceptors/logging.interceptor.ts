import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpResponse
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { LoggerService } from '../services/logger.service';

@Injectable()
export class LoggingInterceptor implements HttpInterceptor {
  constructor(private logger: LoggerService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const startTime = Date.now();
    const correlationId = this.generateCorrelationId();

    this.logger.debug(`[${correlationId}] HTTP ${request.method} ${request.url}`, {
      method: request.method,
      url: request.url,
      correlationId
    });

    return next.handle(request).pipe(
      tap({
        next: (event) => {
          if (event instanceof HttpResponse) {
            const duration = Date.now() - startTime;
            this.logger.debug(`[${correlationId}] Response: ${event.status} in ${duration}ms`);
          }
        },
        error: (error) => {
          const duration = Date.now() - startTime;
          this.logger.error(`[${correlationId}] Error: ${error.status} in ${duration}ms`, error);
        }
      })
    );
  }

  private generateCorrelationId(): string {
    return `corr_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}