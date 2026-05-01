import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { retryWhen, delay, scan, mergeMap } from 'rxjs/operators';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor() {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      retryWhen(errors =>
        errors.pipe(
          mergeMap((error: HttpErrorResponse, i) => {
            const retryAttempt = i + 1;
            
            if (retryAttempt > 3) {
              return throwError(() => error);
            }

            if (error.status === 0 || error.status >= 500) {
              return timer(1000 * retryAttempt);
            }

            return throwError(() => error);
          })
        )
      )
    );
  }
}