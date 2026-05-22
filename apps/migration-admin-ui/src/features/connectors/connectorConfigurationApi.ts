import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorConfigurationValidationRequest,
  ConnectorConfigurationValidationResponse,
} from './connectorConfigurationTypes';

export async function fetchConnectorConfigurationSummary(): Promise<ConnectorConfigurationSummary> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/connectors/configuration/summary`,
  );

  return response.json();
}

export async function fetchConnectorConfigurationCatalog(): Promise<ConnectorConfigurationCatalogItem[]> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/connectors/configuration/catalog`,
  );

  return response.json();
}

export async function validateConnectorConfiguration(
  request: ConnectorConfigurationValidationRequest,
): Promise<ConnectorConfigurationValidationResponse> {
  const response = await fetch(
    `${adminApiBaseUrl}/api/operational/connectors/configuration/validate`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  );

  return response.json();
}