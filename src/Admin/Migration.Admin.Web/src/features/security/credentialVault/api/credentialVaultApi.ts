import { apiGet, apiPost } from "../../../../api/core/adminApiClient";
import type {
  ConnectorCredentialCatalogItem,
  ConnectorCredentialValidationRequest,
  ConnectorCredentialValidationResponse,
  ConnectorCredentialVaultSummary
} from "../types/credentialVault";

export const credentialVaultApi = {
  summary: () =>
    apiGet<ConnectorCredentialVaultSummary>("/api/operational/connectors/credentials/summary"),

  catalog: () =>
    apiGet<ConnectorCredentialCatalogItem[]>("/api/operational/connectors/credentials/catalog"),

  validateReference: (request: ConnectorCredentialValidationRequest) =>
    apiPost<ConnectorCredentialValidationRequest, ConnectorCredentialValidationResponse>(
      "/api/operational/connectors/credentials/validate",
      request
    )
};

