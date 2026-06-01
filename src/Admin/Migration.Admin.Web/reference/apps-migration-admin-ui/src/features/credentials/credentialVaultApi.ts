import { adminApiBaseUrl } from '../../lib/adminApi';
import type {
  ConnectorCredentialCatalogItem,
  ConnectorCredentialValidationRequest,
  ConnectorCredentialValidationResponse,
  ConnectorCredentialVaultSummary,
} from './credentialVaultTypes';

export async function fetchConnectorCredentialVaultSummary(): Promise<ConnectorCredentialVaultSummary> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/credentials/summary`);
  return response.json();
}

export async function fetchConnectorCredentialCatalog(): Promise<ConnectorCredentialCatalogItem[]> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/credentials/catalog`);
  return response.json();
}

export async function validateConnectorCredentialReference(
  request: ConnectorCredentialValidationRequest,
): Promise<ConnectorCredentialValidationResponse> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/connectors/credentials/validate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  return response.json();
}
