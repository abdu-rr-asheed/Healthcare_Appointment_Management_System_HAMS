import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HttpRequest } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiBaseUrl;

  // withCredentials: true ensures the browser sends the HttpOnly auth cookies
  // on every request (including cross-origin requests to the API in dev mode).
  private readonly defaultOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
    withCredentials: true
  };

  constructor(private http: HttpClient) {}

  get<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, {
      ...this.defaultOptions,
      params
    });
  }

  getBlob(endpoint: string, params?: HttpParams): Observable<Blob> {
    return this.http.get(`${this.baseUrl}${endpoint}`, {
      responseType: 'blob',
      withCredentials: true,
      params
    });
  }

  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body, this.defaultOptions);
  }

  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body, this.defaultOptions);
  }

  delete<T>(endpoint: string, body?: any): Observable<T> {
    if (body) {
      const req = new HttpRequest('DELETE', `${this.baseUrl}${endpoint}`, body, {
        headers: this.defaultOptions.headers,
        withCredentials: true
      });
      return this.http.request(req) as Observable<T>;
    }
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`, this.defaultOptions);
  }
}