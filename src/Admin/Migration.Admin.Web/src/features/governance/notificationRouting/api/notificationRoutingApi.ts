import { apiGet } from '../../../../api/core/adminApiClient';
import type {
  AlertPreviewResponse,
  NotificationRoutesResponse,
  NotificationSummary,
} from '../types/notificationRouting';

export const notificationRoutingApi = {
  getSummary(): Promise<NotificationSummary> {
    return apiGet<NotificationSummary>('/api/operational/notifications/summary');
  },

  getRoutes(): Promise<NotificationRoutesResponse> {
    return apiGet<NotificationRoutesResponse>('/api/operational/notifications/routes');
  },

  getAlertPreview(): Promise<AlertPreviewResponse> {
    return apiGet<AlertPreviewResponse>('/api/operational/notifications/alert-preview');
  },
};
