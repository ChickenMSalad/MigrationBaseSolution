export interface CostAnalyticsSummary {
  estimatedTotalCost?: number | null;
  estimatedStorageCost?: number | null;
  estimatedTransferCost?: number | null;
  estimatedOperationCost?: number | null;
  currency?: string | null;
  generatedAtUtc?: string | null;
}

export interface CostAnalyticsBreakdownItem {
  category: string;
  name: string;
  estimatedCost?: number | null;
  currency?: string | null;
  notes?: string | null;
}

export interface CostAnalyticsResponse {
  summary?: CostAnalyticsSummary | null;
  breakdown?: CostAnalyticsBreakdownItem[] | null;
  warnings?: string[] | null;
}
