import { adminApiClient } from '../../../../api/core/adminApiClient';
import { apiGet } from "../../../../api/core/adminApiClient";
import type { CapacityForecastSummary } from "../types/capacityForecast";

const fallbackCapacityForecast: CapacityForecastSummary = {
  generatedUtc: new Date(0).toISOString(),
  window: {
    label: "Unavailable",
    startUtc: new Date(0).toISOString(),
    endUtc: new Date(0).toISOString(),
  },
  metrics: [],
  recommendations: [],
};

export async function getCapacityForecast(): Promise<CapacityForecastSummary> {
  try {
    return await adminApiClient.get<CapacityForecastSummary>("/api/operational/capacity/forecast");
  } catch {
    return fallbackCapacityForecast;
  }
}

