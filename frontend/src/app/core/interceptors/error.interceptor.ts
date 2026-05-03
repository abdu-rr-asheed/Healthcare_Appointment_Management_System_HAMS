import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

/**
 * Handles HTTP error responses globally — normalises errors and re-throws.
 * Retry logic has been removed from here; it lives exclusively in RetryInterceptor
 * which uses the modern RxJS 7+ retry() operator. Having duplicate retry logic
 * in both interceptors caused requests to be retried up to 9 times (3 × 3).
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor() {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Re-throw so RetryInterceptor (earlier in the chain) can retry transient
        // errors, and so individual components / services receive the error.
        return throwError(() => error);
      })
    );
  }
}