import { useEffect, useMemo, useState } from "react";
import { api, connectorValue, displayConnectorName } from "../api/client";
import { Card } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorDescriptor, CredentialSetSummary } from "../types/api";

type NoticeKind = "success" | "error" | "info";
type ExportFormat = "csv" | "excel";

type PageNotice = {
  kind: NoticeKind;
  message: string;
};

type TaxonomyRow = {
  source: string;
  name: string;
  displayName: string;
  description: string;
  required: string;
  type: string;
};

function noticeClassName(kind: NoticeKind) {
  return `notice ${kind === "error" ? "error" : ""}`;
}

function descriptorArray(value: unknown): Array<Record<string, unknown>> {
  return Array.isArray(value) ? value.filter(x => typeof x === "object" && x !== null) as Array<Record<string, unknown>> : [];
}

function valueOf(row: Record<string, unknown>, keys: string[]) {
  for (const key of keys) {
    const value = row[key];

    if (value !== undefined && value !== null && value !== "") {
      return String(value);
    }
  }

  return "";
}

function buildRows(connector: ConnectorDescriptor): TaxonomyRow[] {
  const options = descriptorArray(connector.options).map(row => ({
    source: "Options",
    name: valueOf(row, ["name", "key", "field", "propertyName", "optionName"]),
    displayName: valueOf(row, ["displayName", "label", "name", "key"]),
    description: valueOf(row, ["description", "helpText"]),
    required: valueOf(row, ["required", "isRequired"]),
    type: valueOf(row, ["type", "valueType", "dataType"])
  }));

  const mappingFields = descriptorArray(connector.mappingFields).map(row => ({
    source: "MappingFields",
    name: valueOf(row, ["name", "key", "field", "source", "target"]),
    displayName: valueOf(row, ["displayName", "label", "name", "key"]),
    description: valueOf(row, ["description", "helpText"]),
    required: valueOf(row, ["required", "isRequired"]),
    type: valueOf(row, ["type", "valueType", "dataType"])
  }));

  const manifestColumns = descriptorArray(connector.manifestColumns).map(row => ({
    source: "ManifestColumns",
    name: valueOf(row, ["name", "key", "field", "column"]),
    displayName: valueOf(row, ["displayName", "label", "name", "key"]),
    description: valueOf(row, ["description", "helpText"]),
    required: valueOf(row, ["required", "isRequired"]),
    type: valueOf(row, ["type", "valueType", "dataType"])
  }));

  return [...options, ...mappingFields, ...manifestColumns];
}

function csvEscape(value: string) {
  const text = value ?? "";
  const mustQuote = text.includes(",") || text.includes("\"") || text.includes("\r") || text.includes("\n");

  if (!mustQuote) {
    return text;
  }

  return `"${text.replace(/"/g, "\"\"")}"`;
}

function buildCsv(rows: TaxonomyRow[]) {
  const header = ["Source", "Name", "DisplayName", "Description", "Required", "Type"];
  const lines = [
    header.join(","),
    ...rows.map(row => [
      row.source,
      row.name,
      row.displayName,
      row.description,
      row.required,
      row.type
    ].map(csvEscape).join(","))
  ];

  return lines.join("\r\n") + "\r\n";
}

function xmlEscape(value: string) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function worksheetXml(name: string, rows: string[][]) {
  const rowXml = rows.map(row =>
    `<Row>${row.map(cell => `<Cell><Data ss:Type="String">${xmlEscape(cell ?? "")}</Data></Cell>`).join("")}</Row>`
  ).join("");

  return `<Worksheet ss:Name="${xmlEscape(name)}"><Table>${rowXml}</Table></Worksheet>`;
}

function buildExcelXml(connector: ConnectorDescriptor, rows: TaxonomyRow[]) {
  const taxonomyRows = [
    ["Source", "Name", "DisplayName", "Description", "Required", "Type"],
    ...rows.map(row => [row.source, row.name, row.displayName, row.description, row.required, row.type])
  ];

  const metadataRows = [
    ["Key", "Value"],
    ...Object.entries(connector.metadata ?? {}).map(([key, value]) => [key, String(value)])
  ];

  const schemaRows = [
    ["Section", "Json"],
    ["Options", JSON.stringify(connector.options ?? [], null, 2)],
    ["MappingFields", JSON.stringify(connector.mappingFields ?? [], null, 2)],
    ["ManifestColumns", JSON.stringify(connector.manifestColumns ?? [], null, 2)]
  ];

  return `<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
  xmlns:o="urn:schemas-microsoft-com:office:office"
  xmlns:x="urn:schemas-microsoft-com:office:excel"
  xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
  ${worksheetXml("Taxonomy", taxonomyRows)}
  ${worksheetXml("Metadata", metadataRows)}
  ${worksheetXml("Connector Schema", schemaRows)}
</Workbook>`;
}

function safeFilePart(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, "-")
    .replace(/^-+|-+$/g, "") || "connector";
}

export function TaxonomyBuilder() {
  const [targets, setTargets] = useState<ConnectorDescriptor[]>([]);
  const [credentials, setCredentials] = useState<CredentialSetSummary[]>([]);
  const [targetType, setTargetType] = useState("");
  const [credentialSetId, setCredentialSetId] = useState("");
  const [format, setFormat] = useState<ExportFormat>("excel");
  const [loading, setLoading] = useState(true);
  const [building, setBuilding] = useState(false);
  const [notice, setNotice] = useState<PageNotice | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [createdArtifact, setCreatedArtifact] = useState<{ artifactId: string; fileName: string } | null>(null);

  const selectedTarget = useMemo(
    () => targets.find(target => connectorValue(target).toLowerCase() === targetType.toLowerCase()) ?? null,
    [targets, targetType]
  );

  const matchingCredentials = useMemo(
    () => credentials.filter(credential =>
      credential.connectorRole?.toLowerCase() === "target" &&
      credential.connectorType?.toLowerCase() === targetType.toLowerCase()),
    [credentials, targetType]
  );

  const taxonomyRows = useMemo(
    () => selectedTarget ? buildRows(selectedTarget) : [],
    [selectedTarget]
  );

  useEffect(() => {
    async function load() {
      setLoading(true);
      setError(null);

      try {
        const [connectorResult, credentialResult] = await Promise.all([
          api.connectors(),
          api.credentials()
        ]);

        setTargets(connectorResult.targets ?? []);
        setCredentials(credentialResult);

        if ((connectorResult.targets ?? []).length > 0) {
          setTargetType(connectorValue(connectorResult.targets[0]));
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err));
      } finally {
        setLoading(false);
      }
    }

    void load();
  }, []);

  async function createTaxonomyArtifact() {
    setError(null);
    setNotice(null);
    setCreatedArtifact(null);

    if (!selectedTarget) {
      setNotice({ kind: "error", message: "Choose a target connector first." });
      return;
    }

    setBuilding(true);

    try {
      const connectorType = safeFilePart(connectorValue(selectedTarget));
      const timestamp = new Date().toISOString().replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");

      const file = format === "excel"
        ? new File(
          [buildExcelXml(selectedTarget, taxonomyRows)],
          `${connectorType}-taxonomy-${timestamp}.xls`,
          { type: "application/vnd.ms-excel" }
        )
        : new File(
          [buildCsv(taxonomyRows)],
          `${connectorType}-taxonomy-${timestamp}.csv`,
          { type: "text/csv" }
        );

      const artifact = await api.uploadArtifact(file, {
        kind: "Taxonomy",
        description: `Generated by Taxonomy Builder from ${displayConnectorName(selectedTarget)}`
      });

      setCreatedArtifact({
        artifactId: artifact.artifactId,
        fileName: artifact.fileName
      });

      setNotice({
        kind: "success",
        message: `Taxonomy artifact created: ${artifact.fileName}. It is available under Artifacts.`
      });
    } catch (err) {
      setNotice({
        kind: "error",
        message: `Failed to create taxonomy artifact: ${err instanceof Error ? err.message : String(err)}`
      });
    } finally {
      setBuilding(false);
    }
  }

  return (
    <div className="pageStack taxonomyBuilderPage">
      <div className="pageHeader">
        <div>
          <h1>Taxonomy Builder</h1>
          <p className="muted">
            Create a taxonomy/metaproperty artifact for a target DAM. Phase 1 exports the connector catalog field shape as CSV or Excel workbook.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}

      {notice && (
        <div className={noticeClassName(notice.kind)}>
          {notice.message}
        </div>
      )}

      <Card title="Build Taxonomy Artifact">
        {loading ? (
          <p className="muted">Loading target connectors…</p>
        ) : targets.length === 0 ? (
          <p className="muted">No target connectors are registered.</p>
        ) : (
          <div className="formGrid">
            <label>
              Target connector
              <select value={targetType} onChange={event => {
                setTargetType(event.target.value);
                setCredentialSetId("");
                setCreatedArtifact(null);
                setNotice(null);
              }}>
                {targets.map(target => (
                  <option key={connectorValue(target)} value={connectorValue(target)}>
                    {displayConnectorName(target)}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Credentials
              <select value={credentialSetId} onChange={event => setCredentialSetId(event.target.value)}>
                <option value="">No credential set selected</option>
                {matchingCredentials.map(credential => (
                  <option key={credential.credentialSetId} value={credential.credentialSetId}>
                    {credential.displayName}
                  </option>
                ))}
              </select>
              <span className="helpText">
                Phase 1 does not call the target API yet; the selected credential is for traceability.
              </span>
            </label>

            <label>
              Format
              <select value={format} onChange={event => setFormat(event.target.value as ExportFormat)}>
                <option value="excel">Excel workbook (.xls)</option>
                <option value="csv">CSV</option>
              </select>
              <span className="helpText">
                Excel output contains multiple sheets. Live connector taxonomy pulls can later use this same page.
              </span>
            </label>

            <div className="buttonRow">
              <button
                type="button"
                className="primaryButton"
                onClick={() => void createTaxonomyArtifact()}
                disabled={building || !selectedTarget}
              >
                {building ? "Creating…" : "Create Taxonomy Artifact"}
              </button>

              {createdArtifact && (
                <>
                  <a className="secondaryButton" href={api.artifactDownloadUrl(createdArtifact.artifactId)}>
                    Download Taxonomy
                  </a>
                  <a className="secondaryButton" href="/artifacts">
                    View in Artifacts
                  </a>
                </>
              )}
            </div>
          </div>
        )}
      </Card>

      <Card title="Taxonomy Summary">
        <div className="metricGrid compact">
          <div className="metric">
            <span>Rows</span>
            <strong>{taxonomyRows.length}</strong>
          </div>
          <div className="metric">
            <span>Connector</span>
            <strong>{selectedTarget ? displayConnectorName(selectedTarget) : "None"}</strong>
          </div>
          <div className="metric">
            <span>Format</span>
            <strong>{format === "excel" ? "Excel" : "CSV"}</strong>
          </div>
        </div>
      </Card>
    </div>
  );
}