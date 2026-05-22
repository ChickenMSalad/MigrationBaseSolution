export type ConnectorDirection = 'Source' | 'Target' | 'SourceTarget';

export interface ConnectorConfigurationSummary {
  registeredConnectors: number;
  readyConnectors: number;
  sourceConnectors: number;
  targetConnectors: number;
  attentionRequired: number;
  lastUpdatedUtc: string;
  notes: string[];
}

export interface ConnectorConfigurationCatalogItem {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  recommendedForFirstProductionLane: boolean;
  requiredSettings: string[];
}

export interface ConnectorConfigurationValidationRequest {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  settings: Record<string, string | null>;
}

export interface ConnectorConfigurationValidationResponse {
  isValid: boolean;
  errors: string[];
  validatedAtUtc: string;
}
