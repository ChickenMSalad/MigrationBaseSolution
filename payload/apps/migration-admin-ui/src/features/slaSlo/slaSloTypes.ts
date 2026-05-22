export type SlaSloSummary = {
  totalPolicies: number;
  activePolicies: number;
  warningBreaches: number;
  criticalBreaches: number;
  status: string;
};

export type SlaSloPolicy = {
  policyId: string;
  name: string;
  metric: string;
  threshold: string;
  severity: string;
  enabled: boolean;
  description?: string | null;
};

export type SlaSloPolicyCatalogResponse = {
  policies: SlaSloPolicy[];
};

export type SlaSloBreachPreviewItem = {
  breachId: string;
  detectedUtc: string;
  severity: string;
  metric: string;
  threshold: string;
  observedValue: string;
  scope: string;
  message: string;
};

export type SlaSloBreachPreviewResponse = {
  breaches: SlaSloBreachPreviewItem[];
};
