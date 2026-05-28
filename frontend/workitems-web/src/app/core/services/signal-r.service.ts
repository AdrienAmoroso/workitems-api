import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
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

  constructor(private authService: AuthService) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('WorkItemCreated', (item: WorkItem) => this.onWorkItemCreated$.next(item));
    this.connection.on('WorkItemUpdated', (item: WorkItem) => this.onWorkItemUpdated$.next(item));
    this.connection.on('WorkItemDeleted', (id: string) => this.onWorkItemDeleted$.next(id));
  }

  async startConnection(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async stopConnection(): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
  }
}
