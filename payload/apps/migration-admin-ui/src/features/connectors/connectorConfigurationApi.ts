import { adminApiFetch } from '../../lib/adminApi';
import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorConfigurationValidationRequest,
  ConnectorConfigurationValidationResponse,
} from './connectorConfigurationTypes';

export async function fetchConnectorConfigurationSummary(): Promise<ConnectorConfigurationSummary> {
  return adminApiFetch<ConnectorConfigurationSummary>('/api/operational/connectors/configuration/summary');
}

export async function fetchConnectorConfigurationCatalog(): Promise<ConnectorConfigurationCatalogItem[]> {
  return adminApiFetch<ConnectorConfigurationCatalogItem[]>('/api/operational/connectors/configuration/catalog');
}

export async function validateConnectorConfiguration(
  request: ConnectorConfigurationValidationRequest,
): Promise<ConnectorConfigurationValidationResponse> {
  return adminApiFetch<ConnectorConfigurationValidationResponse>('/api/operational/connectors/configuration/validate', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}
