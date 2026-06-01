import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WorkItem } from '../models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private connection: signalR.HubConnection;

  readonly onWorkItemCreated$ = new Subject<WorkItem>();
  readonly onWorkItemUpdated$ = new Subject<WorkItem>();
  readonly onWorkItemDeleted$ = new Subject<string>();

  // Emits true when connected, false when disconnected or reconnecting.
  // Components subscribe to show a "Live updates unavailable" indicator.
  readonly isConnected$ = new BehaviorSubject<boolean>(false);

  constructor(
    private authService: AuthService,
    private zone: NgZone,
  ) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        // Return null (not empty string) when there is no token.
        // An empty string is still sent as a bearer token, causing a 401.
        // Returning null lets SignalR omit the Authorization header entirely.
        accessTokenFactory: () => this.authService.getToken() ?? null,
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('WorkItemCreated', (item: WorkItem) =>
      this.zone.run(() => this.onWorkItemCreated$.next(item)),
    );
    this.connection.on('WorkItemUpdated', (item: WorkItem) =>
      this.zone.run(() => this.onWorkItemUpdated$.next(item)),
    );
    this.connection.on('WorkItemDeleted', (id: string) =>
      this.zone.run(() => this.onWorkItemDeleted$.next(id)),
    );

    this.connection.onreconnecting(() =>
      this.zone.run(() => this.isConnected$.next(false)),
    );
    this.connection.onreconnected(() =>
      this.zone.run(() => this.isConnected$.next(true)),
    );
    this.connection.onclose(() =>
      this.zone.run(() => this.isConnected$.next(false)),
    );
  }

  async startConnection(): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Disconnected) return;
    try {
      await this.connection.start();
      this.zone.run(() => this.isConnected$.next(true));
    } catch {
      // Connection failed (e.g. server unreachable, 401).
      // isConnected$ stays false; withAutomaticReconnect will retry on resume.
      this.zone.run(() => this.isConnected$.next(false));
    }
  }

  async stopConnection(): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
  }
}
