import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  AlertPreviewResponse,
  NotificationRoutesResponse,
  NotificationSummary,
} from './notificationTypes';

async function readJson<T>(path: string): Promise<T> {
  const response = await fetch(`${adminApiBaseUrl}${path}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function fetchNotificationSummary(): Promise<NotificationSummary> {
  return readJson<NotificationSummary>('/api/operational/notifications/summary');
}

export async function fetchNotificationRoutes(): Promise<NotificationRoutesResponse> {
  return readJson<NotificationRoutesResponse>('/api/operational/notifications/routes');
}

export async function fetchAlertPreview(): Promise<AlertPreviewResponse> {
  return readJson<AlertPreviewResponse>('/api/operational/notifications/alert-preview');
}
