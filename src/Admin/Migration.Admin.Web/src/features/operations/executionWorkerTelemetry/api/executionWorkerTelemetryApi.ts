import { apiGet } from "../../../../api/core/adminApiClient";
import type { ExecutionWorkerTelemetrySummary } from "../types/executionWorkerTelemetry";

function clampStaleAfterSeconds(value: number): number {
  if (!Number.isFinite(value)) {
    return 120;
  }

  if (value < 15) {
    return 15;
  }

  if (value > 3600) {
    return 3600;
  }

  return Math.trunc(value);
}

export const executionWorkerTelemetryApi = {
  summary(staleAfterSeconds = 120): Promise<ExecutionWorkerTelemetrySummary> {
    const seconds = clampStaleAfterSeconds(staleAfterSeconds);
    return apiGet<ExecutionWorkerTelemetrySummary>(
      `/api/operational/execution-workers/summary?staleAfterSeconds=${seconds}`,
    );
  },
};
