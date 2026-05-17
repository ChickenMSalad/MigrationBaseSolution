import { apiGet } from './core/adminApiClient';

export type EndpointPolicyInventoryItem = {
  area: string;
  routePrefix: string;
  recommendedPolicy: string;
  requiredScope: string;
  mutatesState: boolean;
  exposesSecretsOrCredentials: boolean;
  operationallySensitive: boolean;
  notes: string;
};

export type EndpointPolicyInventorySnapshot = {
  generatedUtc: string;
  items: EndpointPolicyInventoryItem[];
  readOnlyCount: number;
  mutatingCount: number;
  credentialSensitiveCount: number;
  operationallySensitiveCount: number;
  warnings: string[];
};

export async function getEndpointPolicyInventory(): Promise<EndpointPolicyInventorySnapshot> {
  return apiGet<EndpointPolicyInventorySnapshot>('/api/cloud/auth/endpoint-policy-inventory');
}
