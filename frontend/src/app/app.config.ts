import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { routes } from './app.routes';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';
import { LoadingInterceptor } from './core/interceptors/loading.interceptor';
import { CacheInterceptor } from './core/interceptors/cache.interceptor';
import { LoggingInterceptor } from './core/interceptors/logging.interceptor';
import { RetryInterceptor } from './core/interceptors/retry.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    // Interceptor order matters: Auth → Logging → Cache → Retry → Error → Loading
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor,    multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoggingInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: CacheInterceptor,   multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: RetryInterceptor,   multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor,   multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true }
  ]
};
