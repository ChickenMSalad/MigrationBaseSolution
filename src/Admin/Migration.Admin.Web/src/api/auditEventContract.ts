import { apiGet } from './core/adminApiClient';

export type AuditEventContractDescriptor = {
  environmentName: string;
  auditMode: string;
  workspaceId: string;
  tenantId?: string | null;
  persistenceEnabled: boolean;
  providerKind: string;
  auditStorageRoot?: string | null;
  supportedEventTypes: string[];
  requiredProperties: string[];
  redactedProperties: string[];
  warnings: string[];
};

export async function getAuditEventContract(): Promise<AuditEventContractDescriptor> {
  return apiGet<AuditEventContractDescriptor>('/api/cloud/audit/event-contract');
}
