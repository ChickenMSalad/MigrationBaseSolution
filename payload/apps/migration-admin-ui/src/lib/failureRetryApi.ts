import { adminApiBaseUrl } from './adminApi';

export type FailureRetryScope = 'selected' | 'all-retryable';

export interface FailureSearchRequest {
  runId: string;
  searchText?: string;
  includeNonRetryable: boolean;
  limit: number;
}

export interface FailureRecord {
  failureId: string;
  runId: string;
  workItemId?: string;
  assetId?: string;
  sourceIdentifier?: string;
  failureCategory?: string;
  failureCode?: string;
  message: string;
  isRetryable: boolean;
  attemptCount?: number;
  lastAttemptUtc?: string;
  nextAttemptUtc?: string;
}

export interface FailureSearchResponse {
  runId: string;
  totalFailures?: number;
  retryableFailures?: number;
  returnedFailures: number;
  failures: FailureRecord[];
  message?: string;
}

export interface RetryFailureRequest {
  runId: string;
  scope: FailureRetryScope;
  failureIds: string[];
  operatorNote?: string;
}

export interface RetryFailureResponse {
  runId: string;
  retryRequestId?: string;
  requestedFailures: number;
  acceptedFailures: number;
  rejectedFailures?: number;
  message?: string;
}

async function parseJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  return (text ? JSON.parse(text) : {}) as T;
}

export async function searchRunFailures(
  request: FailureSearchRequest,
  signal?: AbortSignal
): Promise<FailureSearchResponse> {
  const parameters = new URLSearchParams();
  parameters.set('includeNonRetryable', String(request.includeNonRetryable));
  parameters.set('limit', String(request.limit));

  if (request.searchText?.trim()) {
    parameters.set('searchText', request.searchText.trim());
  }

  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/runs/${encodeURIComponent(request.runId)}/failures?${parameters.toString()}`,
    {
      method: 'GET',
      headers: {
        Accept: 'application/json'
      },
      signal
    }
  );

  const parsed = await parseJson<FailureSearchResponse>(response);

  if (!response.ok) {
    throw new Error(parsed.message || `Failure search failed: ${response.status} ${response.statusText}`);
  }

  return parsed;
}

export async function retryRunFailures(
  request: RetryFailureRequest,
  signal?: AbortSignal
): Promise<RetryFailureResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/runs/${encodeURIComponent(request.runId)}/failures/retry`,
    {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        scope: request.scope,
        failureIds: request.failureIds,
        operatorNote: request.operatorNote
      }),
      signal
    }
  );

  const parsed = await parseJson<RetryFailureResponse>(response);

  if (!response.ok) {
    throw new Error(parsed.message || `Failure retry failed: ${response.status} ${response.statusText}`);
  }

  return parsed;
}
