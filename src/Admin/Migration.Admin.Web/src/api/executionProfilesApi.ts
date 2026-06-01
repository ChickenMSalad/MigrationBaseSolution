import { adminApiClient } from "./core/adminApiClient";
import type {
  ConnectorExecutionProfileCatalogItem,
  ConnectorExecutionProfileSummary,
  ConnectorExecutionProfileValidationRequest,
  ConnectorExecutionProfileValidationResponse,
} from "../types/executionProfiles";

export const executionProfilesApi = {
  getSummary(): Promise<ConnectorExecutionProfileSummary> {
    return adminApiClient.get<ConnectorExecutionProfileSummary>(
      "/api/operational/connectors/execution-profiles/summary",
    );
  },

  getCatalog(): Promise<ConnectorExecutionProfileCatalogItem[]> {
    return adminApiClient.get<ConnectorExecutionProfileCatalogItem[]>(
      "/api/operational/connectors/execution-profiles/catalog",
    );
  },

  validate(
    request: ConnectorExecutionProfileValidationRequest,
  ): Promise<ConnectorExecutionProfileValidationResponse> {
    return adminApiClient.post<ConnectorExecutionProfileValidationResponse>(
      "/api/operational/connectors/execution-profiles/validate",
      request,
    );
  },
};
