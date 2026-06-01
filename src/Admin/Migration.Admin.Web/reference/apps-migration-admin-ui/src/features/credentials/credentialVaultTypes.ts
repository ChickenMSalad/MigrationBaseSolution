export type ConnectorCredentialVaultSummary = {
  registeredCredentialReferences: number;
  missingSecretReferences: number;
  connectorsRequiringCredentials: number;
  supportedSecretProviders: string[];
};

export type ConnectorCredentialCatalogItem = {
  connectorKey: string;
  displayName: string;
  direction: string;
  requiredSecretNames: string[];
};

export type ConnectorCredentialValidationRequest = {
  connectorKey: string;
  secretProvider: string;
  secretReferenceName: string;
};

export type ConnectorCredentialValidationResponse = {
  isValid: boolean;
  message: string;
  findings: string[];
};
