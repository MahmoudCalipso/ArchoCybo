import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CreateProjectDto {
  name: string;
  description?: string;
  databaseType: number;
  useBaseRoles: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = '/api';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('jwt');
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }

  get<T>(url: string): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${url}`, { headers: this.getHeaders() });
  }

  post<T>(url: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${url}`, body, { headers: this.getHeaders() });
  }

  put<T>(url: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${url}`, body, { headers: this.getHeaders() });
  }

  delete<T>(url: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${url}`, { headers: this.getHeaders() });
  }

  createProject(dto: CreateProjectDto): Observable<any> {
    return this.post('project', dto);
  }
}
