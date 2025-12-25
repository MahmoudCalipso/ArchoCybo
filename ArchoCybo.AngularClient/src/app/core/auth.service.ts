import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = '/api';
  currentToken = signal<string | null>(localStorage.getItem('jwt'));

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string) {
    return this.http.post<{ token: string }>(`${this.baseUrl}/auth/login`, { username, password });
  }

  setToken(token: string) {
    localStorage.setItem('jwt', token);
    this.currentToken.set(token);
  }

  logout() {
    localStorage.removeItem('jwt');
    this.currentToken.set(null);
    this.router.navigate(['/auth/login']);
  }
}
