import { adminApiClient } from '../../../../api/core/adminApiClient';
import { apiGet } from "../../../../api/core/adminApiClient";
import type { OperationalEventTimelineResponse } from "../types/operationalEvents";

export async function getOperationalEventTimeline(runId?: string): Promise<OperationalEventTimelineResponse> {
  const query = runId ? `?runId=${encodeURIComponent(runId)}` : "";
  return adminApiClient.get<OperationalEventTimelineResponse>(`/api/operational/events/timeline${query}`);
}


