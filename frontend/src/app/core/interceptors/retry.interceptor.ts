import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { retry, catchError, switchMap } from 'rxjs/operators';
import { LoggerService } from '../services/logger.service';

@Injectable()
export class RetryInterceptor implements HttpInterceptor {
  private readonly maxRetries = 3;
  private readonly retryDelay = 1000;

  constructor(private logger: LoggerService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (this.isExcludedUrl(request.url)) {
      return next.handle(request);
    }

    return next.handle(request).pipe(
      retry({
        count: this.maxRetries,
        delay: (error, retryCount) => {
          this.logger.warn(`Retry attempt ${retryCount} for ${request.url}`, {
            url: request.url,
            retryCount,
            status: error.status
          });
          return timer(retryCount * this.retryDelay);
        }
      }),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 0) {
          this.logger.error(`Network error for ${request.url}: ${error.message}`);
        } else if (error.status >= 500) {
          this.logger.error(`Server error ${error.status} for ${request.url}`, error);
        }
        return throwError(() => error);
      })
    );
  }

  private isExcludedUrl(url: string): boolean {
    const excludedPatterns = ['/health', '/metrics', '/hangfire'];
    return excludedPatterns.some(pattern => url.includes(pattern));
  }
}