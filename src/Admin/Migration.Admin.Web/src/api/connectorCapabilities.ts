import { apiGet } from './core/adminApiClient';

export type ConnectorRole = 'source' | 'target' | 'manifestProvider';

export type ConnectorFieldType =
  | 'text'
  | 'password'
  | 'url'
  | 'path'
  | 'boolean'
  | 'number'
  | 'select'
  | 'multiText'
  | 'json';

export type ConnectorSecretKind =
  | 'username'
  | 'password'
  | 'bearerToken'
  | 'apiKey'
  | 'apiSecret'
  | 'oauthClientId'
  | 'oauthClientSecret'
  | 'connectionString'
  | 'accessKeyId'
  | 'secretAccessKey';

export type ConnectorConfigurationFieldDescriptor = {
  name: string;
  label: string;
  fieldType: ConnectorFieldType | string;
  required: boolean;
  description?: string | null;
  defaultValue?: string | null;
  options?: string[] | null;
};

export type ConnectorCredentialRequirementDescriptor = {
  name: string;
  label: string;
  secretKind: ConnectorSecretKind | string;
  required: boolean;
  description?: string | null;
};

export type ConnectorCapabilityDescriptor = {
  key: string;
  displayName: string;
  role: ConnectorRole;
  description?: string | null;
  aliases: string[];
  supportedOperations: string[];
  configurationFields: ConnectorConfigurationFieldDescriptor[];
  credentialRequirements: ConnectorCredentialRequirementDescriptor[];
  supportsManifestGeneration: boolean;
  supportsValidation: boolean;
  supportsDryRun: boolean;
};

export type ConnectorCapabilitiesResponse = {
  sources: ConnectorCapabilityDescriptor[];
  targets: ConnectorCapabilityDescriptor[];
  manifestProviders: ConnectorCapabilityDescriptor[];
};

export async function getConnectorCapabilities(): Promise<ConnectorCapabilitiesResponse> {
  return apiGet<ConnectorCapabilitiesResponse>('/api/connectors/capabilities');
}

export async function getConnectorCapability(
  role: ConnectorRole,
  key: string
): Promise<ConnectorCapabilityDescriptor> {
  return apiGet<ConnectorCapabilityDescriptor>(
    `/api/connectors/capabilities/${encodeURIComponent(role)}/${encodeURIComponent(key)}`
  );
}

export function hasConnectorOperation(
  descriptor: ConnectorCapabilityDescriptor | undefined | null,
  operation: string
): boolean {
  if (!descriptor) {
    return false;
  }

  return descriptor.supportedOperations.some(
    (candidate) => candidate.toLocaleLowerCase() === operation.toLocaleLowerCase()
  );
}

export function getRequiredConfigurationFields(
  descriptor: ConnectorCapabilityDescriptor | undefined | null
): ConnectorConfigurationFieldDescriptor[] {
  return descriptor?.configurationFields.filter((field) => field.required) ?? [];
}

export function getRequiredCredentialRequirements(
  descriptor: ConnectorCapabilityDescriptor | undefined | null
): ConnectorCredentialRequirementDescriptor[] {
  return descriptor?.credentialRequirements.filter((requirement) => requirement.required) ?? [];
}
