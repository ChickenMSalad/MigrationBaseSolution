import { apiGet } from './core/adminApiClient';

export type AuthorizationRoleDescriptor = {
  role: string;
  displayName: string;
  description: string;
};

export type AuthorizationScopeDescriptor = {
  scope: string;
  displayName: string;
  description: string;
};

export type AuthorizationRoutePolicyDescriptor = {
  routePattern: string;
  policy: string;
  requiredRoles: string[];
  requiredScopes: string[];
};

export type AuthorizationPolicyPlanDescriptor = {
  environmentName: string;
  authMode: string;
  authRequired: boolean;
  tenantEnforced: boolean;
  authority?: string | null;
  audience?: string | null;
  roles: AuthorizationRoleDescriptor[];
  scopes: AuthorizationScopeDescriptor[];
  routePolicies: AuthorizationRoutePolicyDescriptor[];
  warnings: string[];
};

export async function getAuthorizationPolicyPlan(): Promise<AuthorizationPolicyPlanDescriptor> {
  return apiGet<AuthorizationPolicyPlanDescriptor>('/api/cloud/auth/policy-plan');
}
