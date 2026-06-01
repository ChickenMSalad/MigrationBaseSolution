import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorConfigurationValidationRequest,
  ConnectorConfigurationValidationResponse
} from "../types/connectorConfiguration";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: init?.body instanceof FormData
      ? init.headers
      : {
          "Content-Type": "application/json",
          ...(init?.headers ?? {})
        }
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      message = body?.error ?? body?.message ?? JSON.stringify(body);
    } catch {
      try {
        message = await response.text();
      } catch {
        // Keep the default status message.
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const connectorConfigurationApi = {
  summary: () => request<ConnectorConfigurationSummary>("/api/operational/connectors/configuration/summary"),
  catalog: () => request<ConnectorConfigurationCatalogItem[]>("/api/operational/connectors/configuration/catalog"),
  validate: (payload: ConnectorConfigurationValidationRequest) => request<ConnectorConfigurationValidationResponse>(
    "/api/operational/connectors/configuration/validate",
    {
      method: "POST",
      body: JSON.stringify(payload)
    }
  )
};
