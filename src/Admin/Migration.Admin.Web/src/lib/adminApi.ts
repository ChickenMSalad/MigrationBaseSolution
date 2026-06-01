export type ApiStatus<T> =
  | { state: 'idle' | 'loading' }
  | { state: 'success'; value: T }
  | { state: 'error'; error: string };

export interface EndpointProbe {
  label: string;
  path: string;
  status: ApiStatus<unknown>;
}

export const adminApiBaseUrl =
  (import.meta.env.VITE_ADMIN_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ||
  'https://localhost:55436';

export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${adminApiBaseUrl}${path}`, {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}

export function summarize(value: unknown): string {
  if (value === null || value === undefined) {
    return 'No response body.';
  }

  if (Array.isArray(value)) {
    return `${value.length} item(s)`;
  }

  if (typeof value === 'object') {
    const keys = Object.keys(value as Record<string, unknown>);
    return keys.length === 0 ? 'Empty object.' : keys.slice(0, 6).join(', ');
  }

  return String(value);
}
