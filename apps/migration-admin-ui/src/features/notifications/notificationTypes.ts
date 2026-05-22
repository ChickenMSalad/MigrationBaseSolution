export type NotificationSummary = {
  totalRoutes: number;
  enabledRoutes: number;
  pendingAlerts: number;
  criticalAlerts: number;
  status: string;
};

export type NotificationRoute = {
  routeId: string;
  name: string;
  severity: string;
  channel: string;
  enabled: boolean;
  destinationReference: string;
  description?: string | null;
};

export type NotificationRoutesResponse = {
  routes: NotificationRoute[];
};

export type AlertPreviewItem = {
  alertId: string;
  createdUtc: string;
  severity: string;
  category: string;
  title: string;
  message: string;
  source: string;
};

export type AlertPreviewResponse = {
  alerts: AlertPreviewItem[];
};
