import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ConnectorExecutionProfileCatalogItem,
  ConnectorExecutionProfileSummary,
  ConnectorExecutionProfileValidationRequest,
  ConnectorExecutionProfileValidationResponse,
} from './executionProfileTypes';

export async function fetchConnectorExecutionProfileSummary(): Promise<ConnectorExecutionProfileSummary> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/execution-profiles/summary`);
  return response.json();
}

export async function fetchConnectorExecutionProfileCatalog(): Promise<ConnectorExecutionProfileCatalogItem[]> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/execution-profiles/catalog`);
  return response.json();
}

export async function validateConnectorExecutionProfile(
  request: ConnectorExecutionProfileValidationRequest,
): Promise<ConnectorExecutionProfileValidationResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/execution-profiles/validate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  return response.json();
}
