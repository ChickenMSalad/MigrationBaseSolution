export class AdminApiError extends Error {
  readonly status: number;
  readonly statusText: string;
  readonly responseBody?: unknown;

  constructor(
    message: string,
    status: number,
    statusText: string,
    responseBody?: unknown
  ) {
    super(message);

    this.name = 'AdminApiError';
    this.status = status;
    this.statusText = statusText;
    this.responseBody = responseBody;
  }
}

export function isAdminApiError(value: unknown): value is AdminApiError {
  return value instanceof AdminApiError;
}
