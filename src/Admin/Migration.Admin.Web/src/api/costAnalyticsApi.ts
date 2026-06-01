import { adminApiClient } from "./core/adminApiClient";
import type { CostAnalyticsResponse } from "../types/costAnalytics";

export async function getCostAnalytics(): Promise<CostAnalyticsResponse> {
  return adminApiClient.get<CostAnalyticsResponse>("/api/operational/cost/analytics");
}
