import { apiGet } from './core/adminApiClient';

export type WorkspaceContextDescriptor = {
  workspaceId: string;
  displayName: string;
  tenantMode: string;
  isDefaultWorkspace: boolean;
  isTenantEnforced: boolean;
  tenantId?: string | null;
  allowedConnectorRoles: string[];
  warnings: string[];
};

export async function getWorkspaceContext(): Promise<WorkspaceContextDescriptor> {
  return apiGet<WorkspaceContextDescriptor>('/api/workspace/context');
}
