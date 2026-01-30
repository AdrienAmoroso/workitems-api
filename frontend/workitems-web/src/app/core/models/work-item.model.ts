export interface WorkItem {
  id: string;
  title: string;
  description: string | null;
  status: WorkItemStatus;
  priority: WorkItemPriority;
  createdAt: string;
  updatedAt: string;
}

export enum WorkItemStatus {
  Todo = 'Todo',
  InProgress = 'InProgress',
  Done = 'Done'
}

export enum WorkItemPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High'
}

export interface CreateWorkItemRequest {
  title: string;
  description?: string;
  priority: WorkItemPriority;
}

export interface UpdateWorkItemRequest {
  title: string;
  description?: string;
  status: WorkItemStatus;
  priority: WorkItemPriority;
}

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface WorkItemFilter {
  page?: number;
  pageSize?: number;
  status?: WorkItemStatus | null;
  priority?: WorkItemPriority | null;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}
