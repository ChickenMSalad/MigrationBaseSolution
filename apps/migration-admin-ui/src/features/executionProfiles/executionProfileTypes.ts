export interface ConnectorExecutionProfileSummary {
  totalProfiles: number;
  sourceProfiles: number;
  targetProfiles: number;
  policyFamilies: string[];
  defaultProfileId: string;
}

export interface ConnectorExecutionProfileCatalogItem {
  profileId: string;
  displayName: string;
  connectorScope: string;
  maxConcurrency: number;
  maxAttempts: number;
  retryDelaySeconds: number;
  throttlePerMinute: number;
  isDefault: boolean;
}

export interface ConnectorExecutionProfileValidationRequest {
  profileId: string;
  connectorScope: string;
  maxConcurrency: number;
  maxAttempts: number;
  retryDelaySeconds: number;
  throttlePerMinute: number;
}

export interface ConnectorExecutionProfileValidationResponse {
  isValid: boolean;
  findings: string[];
}
