import { apiGet, apiPost } from '../../../../api/core/adminApiClient';
import type {
  ConnectorConfigurationCatalogItem,
  ConnectorConfigurationSummary,
  ConnectorConfigurationValidationRequest,
  ConnectorConfigurationValidationResponse
} from "../types/connectorConfiguration";

export const connectorConfigurationApi = {
  summary: () => apiGet<ConnectorConfigurationSummary>("/api/operational/connectors/configuration/summary"),
  catalog: () => apiGet<ConnectorConfigurationCatalogItem[]>("/api/operational/connectors/configuration/catalog"),
  validate: (payload: ConnectorConfigurationValidationRequest) => apiPost<ConnectorConfigurationValidationResponse>(
    "/api/operational/connectors/configuration/validate",
    payload
  )
};
