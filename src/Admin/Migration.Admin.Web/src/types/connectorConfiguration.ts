export type ConnectorDirection = "Source" | "Target" | "SourceTarget";

export type ConnectorConfigurationSummary = {
  registeredConnectors: number;
  readyConnectors: number;
  sourceConnectors: number;
  targetConnectors: number;
  attentionRequired: number;
  lastUpdatedUtc?: string | null;
  notes: string[];
};

export type ConnectorConfigurationCatalogItem = {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  recommendedForFirstProductionLane: boolean;
  requiredSettings: string[];
};

export type ConnectorConfigurationValidationRequest = {
  connectorKey: string;
  displayName: string;
  direction: ConnectorDirection;
  settings: Record<string, string | null>;
};

export type ConnectorConfigurationValidationResponse = {
  isValid: boolean;
  errors: string[];
  validatedAtUtc?: string | null;
};
