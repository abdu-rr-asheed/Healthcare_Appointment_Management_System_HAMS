import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpResponse
} from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap, shareReplay } from 'rxjs/operators';

interface CacheEntry {
  response: HttpResponse<unknown>;
  timestamp: number;
}

@Injectable()
export class CacheInterceptor implements HttpInterceptor {
  private cache = new Map<string, CacheEntry>();
  private readonly defaultTtl = 5 * 60 * 1000;
  private readonly maxCacheSize = 100;

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!this.isGetRequest(request) || !this.isCacheable(request.url)) {
      return next.handle(request);
    }

    const cacheKey = this.getCacheKey(request);
    const cached = this.cache.get(cacheKey);

    if (cached && this.isValid(cached)) {
      return of(cached.response.clone());
    }

    return next.handle(request).pipe(
      tap(event => {
        if (event instanceof HttpResponse) {
          this.setCache(cacheKey, event);
        }
      }),
      shareReplay(1)
    );
  }

  private isGetRequest(request: HttpRequest<unknown>): boolean {
    return request.method === 'GET';
  }

  private isCacheable(url: string): boolean {
    const cacheablePatterns = [
      '/api/clinicians',
      '/api/specialties',
      '/api/appointments/slots'
    ];
    return cacheablePatterns.some(pattern => url.includes(pattern));
  }

  private getCacheKey(request: HttpRequest<unknown>): string {
    return `${request.urlWithParams}`;
  }

  private isValid(entry: CacheEntry): boolean {
    return Date.now() - entry.timestamp < this.defaultTtl;
  }

  private setCache(key: string, response: HttpResponse<unknown>): void {
    if (this.cache.size >= this.maxCacheSize) {
      const firstKey = this.cache.keys().next().value;
      if (firstKey) {
        this.cache.delete(firstKey);
      }
    }

    this.cache.set(key, {
      response: response.clone(),
      timestamp: Date.now()
    });
  }

  clearCache(): void {
    this.cache.clear();
  }

  invalidate(pattern: string): void {
    for (const key of this.cache.keys()) {
      if (key.includes(pattern)) {
        this.cache.delete(key);
      }
    }
  }
}