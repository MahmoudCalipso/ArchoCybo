import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private projectUpdated$ = new Subject<string>();
  private userChanged$ = new Subject<string>();

  constructor(private zone: NgZone) {}

  start(): void {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => localStorage.getItem('jwt') ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ProjectUpdated', (projectId: string) => {
      this.zone.run(() => this.projectUpdated$.next(projectId));
    });
    this.hubConnection.on('UserChanged', (userId: string) => {
      this.zone.run(() => this.userChanged$.next(userId));
    });

    this.hubConnection.start().catch(() => {});
  }

  stop(): void {
    this.hubConnection?.stop().catch(() => {});
    this.hubConnection = undefined;
  }

  onProjectUpdated(): Observable<string> {
    return this.projectUpdated$.asObservable();
  }

  onUserChanged(): Observable<string> {
    return this.userChanged$.asObservable();
  }
}

