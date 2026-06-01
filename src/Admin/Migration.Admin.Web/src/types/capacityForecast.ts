export interface CapacityForecastWindow {
  label: string;
  startUtc: string;
  endUtc: string;
}

export interface CapacityForecastMetric {
  name: string;
  value: number;
  unit: string;
  status: "Normal" | "Warning" | "Critical" | "Unknown";
}

export interface CapacityForecastRecommendation {
  id: string;
  severity: "Info" | "Warning" | "Critical";
  title: string;
  detail: string;
}

export interface CapacityForecastSummary {
  generatedUtc: string;
  window: CapacityForecastWindow;
  metrics: CapacityForecastMetric[];
  recommendations: CapacityForecastRecommendation[];
}
