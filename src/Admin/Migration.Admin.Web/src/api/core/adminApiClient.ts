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

function hasBody(body: unknown): body is BodyInit {
  return body !== undefined && body !== null;
}

function toRequestBody(body: unknown): BodyInit | undefined {
  if (!hasBody(body)) {
    return undefined;
  }

  if (
    typeof body === 'string' ||
    body instanceof Blob ||
    body instanceof FormData ||
    body instanceof URLSearchParams ||
    body instanceof ArrayBuffer
  ) {
    return body;
  }

  return JSON.stringify(body);
}

function withJsonHeaders(options?: ApiRequestOptions, body?: unknown): ApiRequestOptions {
  const headers = new Headers(options?.headers ?? undefined);

  if (hasBody(body) && !(body instanceof Blob) && !(body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }

  return {
    ...(options ?? {}),
    headers,
  };
}

export async function apiRequest<TResponse = unknown>(
  path: string,
  options?: ApiRequestOptions,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...(options ?? {}),
    headers: {
      Accept: 'application/json',
      ...(options?.headers ?? {}),
    },
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

    throw new AdminApiError(message, response.status, response.statusText, responseBody);
  }

  if (options?.parseJson === false) {
    return responseBody as TResponse;
  }

  return responseBody as TResponse;
}

export async function apiGet<TResponse = unknown>(path: string, options?: ApiRequestOptions): Promise<TResponse> {
  return apiRequest<TResponse>(path, { ...(options ?? {}), method: 'GET' });
}

export async function apiPost<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPost<TRequest, TResponse>(path: string, body: TRequest, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPost<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse> {
  const requestOptions = withJsonHeaders(options, body);

  return apiRequest<TResponse>(path, {
    ...requestOptions,
    method: 'POST',
    body: toRequestBody(body),
  });
}

export async function apiPut<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPut<TRequest, TResponse>(path: string, body: TRequest, options?: ApiRequestOptions): Promise<TResponse>;
export async function apiPut<TResponse = unknown>(path: string, body?: unknown, options?: ApiRequestOptions): Promise<TResponse> {
  const requestOptions = withJsonHeaders(options, body);

  return apiRequest<TResponse>(path, {
    ...requestOptions,
    method: 'PUT',
    body: toRequestBody(body),
  });
}

export async function apiDelete<TResponse = unknown>(path: string, options?: ApiRequestOptions): Promise<TResponse> {
  return apiRequest<TResponse>(path, { ...(options ?? {}), method: 'DELETE' });
}

export const adminApiClient = {
  request: apiRequest,
  get: apiGet,
  post: apiPost,
  put: apiPut,
  delete: apiDelete,
};
