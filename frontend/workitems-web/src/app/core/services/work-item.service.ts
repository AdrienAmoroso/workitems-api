import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  WorkItem,
  CreateWorkItemRequest,
  UpdateWorkItemRequest,
  PaginatedResult,
  WorkItemFilter
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class WorkItemService {
  private readonly apiUrl = `${environment.apiUrl}/work-items`;

  constructor(private http: HttpClient) {}

  getAll(filter: WorkItemFilter = {}): Observable<PaginatedResult<WorkItem>> {
    let params = new HttpParams();

    if (filter.page) {
      params = params.set('page', filter.page.toString());
    }
    if (filter.pageSize) {
      params = params.set('pageSize', filter.pageSize.toString());
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }
    if (filter.priority) {
      params = params.set('priority', filter.priority);
    }
    if (filter.sortBy) {
      params = params.set('sortBy', filter.sortBy);
    }
    if (filter.sortDir) {
      params = params.set('sortDir', filter.sortDir);
    }

    return this.http.get<PaginatedResult<WorkItem>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<WorkItem> {
    return this.http.get<WorkItem>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateWorkItemRequest): Observable<WorkItem> {
    return this.http.post<WorkItem>(this.apiUrl, request);
  }

  update(id: string, request: UpdateWorkItemRequest): Observable<WorkItem> {
    return this.http.put<WorkItem>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
