import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  SlaSloBreachPreviewResponse,
  SlaSloPolicyCatalogResponse,
  SlaSloSummary,
} from './slaSloTypes';

async function readJson<T>(path: string): Promise<T> {
  const response = await fetch(`${adminApiBaseUrl}${path}`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function fetchSlaSloSummary(): Promise<SlaSloSummary> {
  return readJson<SlaSloSummary>('/api/operational/sla-slo/summary');
}

export async function fetchSlaSloPolicies(): Promise<SlaSloPolicyCatalogResponse> {
  return readJson<SlaSloPolicyCatalogResponse>('/api/operational/sla-slo/policies');
}

export async function fetchSlaSloBreachPreview(): Promise<SlaSloBreachPreviewResponse> {
  return readJson<SlaSloBreachPreviewResponse>('/api/operational/sla-slo/breach-preview');
}
