import { AdminApiError } from './adminApiError';

const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? '').replace(/\/$/, '');

export type ApiRequestOptions = RequestInit & {
  parseJson?: boolean;
};

async function tryReadBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? '';

  if (contentType.includes('application/json')) {
    try {
      return await response.json();
    } catch {
      return undefined;
    }
  }

  try {
    return await response.text();
  } catch {
    return undefined;
  }
}

export async function apiRequest<T>(
  path: string,
  options?: ApiRequestOptions
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      Accept: 'application/json',
      ...(options?.headers ?? {})
    },
    ...options
  });

  const responseBody = await tryReadBody(response);

  if (!response.ok) {
    const message =
      typeof responseBody === 'object' &&
      responseBody !== null &&
      'error' in responseBody &&
      typeof (responseBody as { error?: unknown }).error === 'string'
        ? String((responseBody as { error: string }).error)
        : `Request failed with status ${response.status}`;

    throw new AdminApiError(
      message,
      response.status,
      response.statusText,
      responseBody
    );
  }

  if (options?.parseJson === false) {
    return responseBody as T;
  }

  return responseBody as T;
}

export async function apiGet<T>(path: string): Promise<T> {
  return apiRequest<T>(path, {
    method: 'GET'
  });
}

export async function apiPost<TResponse, TRequest>(
  path: string,
  body: TRequest
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });
}

export async function apiPut<TResponse, TRequest>(
  path: string,
  body: TRequest
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });
}

export async function apiDelete(path: string): Promise<void> {
  await apiRequest<void>(path, {
    method: 'DELETE',
    parseJson: false
  });
}
