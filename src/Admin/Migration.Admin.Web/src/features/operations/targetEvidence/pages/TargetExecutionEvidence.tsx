import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Card, EmptyState, JsonBlock, StatusPill } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";
import { targetEvidenceApi } from "../api/targetEvidenceApi";
import type { TargetExecutionEvidenceResponse, TargetExecutionEvidenceRow } from "../types/targetEvidence";

function formatDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function csvValue(value: unknown) {
  if (value === undefined || value === null) {
    return "";
  }

  const text = String(value);
  if (text.includes("\"") || text.includes(",") || text.includes("\n") || text.includes("\r")) {
    return `"${text.replace(/"/g, '""')}"`;
  }

  return text;
}

function downloadCsv(fileName: string, rows: unknown[][]) {
  const csv = rows.map((row) => row.map(csvValue).join(",")).join("\r\n");
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

function safeFileName(value: string) {
  return value.replace(/[^a-zA-Z0-9._-]+/g, "-").replace(/^-+|-+$/g, "") || "run";
}

function fieldValue(row: TargetExecutionEvidenceRow, field: string) {
  return row.stampedFields?.[field] ?? row.targetPayloadFields?.[field] ?? "";
}

function rowIsSuccess(row: TargetExecutionEvidenceRow) {
  const status = String(row.status ?? "").toLowerCase();
  return status.includes("succeed") || status.includes("success");
}

function rowIsFailed(row: TargetExecutionEvidenceRow) {
  const status = String(row.status ?? "").toLowerCase();
  return status.includes("fail") || Boolean(row.error);
}

export function TargetExecutionEvidence() {
  const { runId } = useParams();
  const [evidence, setEvidence] = useState<TargetExecutionEvidenceResponse | null>(null);
  const [statusFilter, setStatusFilter] = useState("all");
  const [take, setTake] = useState(500);
  const [expandedWorkItemId, setExpandedWorkItemId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    if (!runId) {
      setEvidence(null);
      setError(null);
      setLoading(false);
      return;
    }

    setError(null);

    try {
      setEvidence(await targetEvidenceApi.getRunEvidence(runId, statusFilter, take));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    setLoading(true);
    void load();
  }, [runId, statusFilter, take]);

  const rows = evidence?.rows ?? [];

  const stampedColumns = useMemo(() => {
    const fields = new Set<string>();
    rows.forEach((row) => {
      Object.keys(row.stampedFields ?? {}).forEach((field) => fields.add(field));
      Object.keys(row.targetPayloadFields ?? {}).forEach((field) => fields.add(field));
    });
    return Array.from(fields).sort((a, b) => a.localeCompare(b));
  }, [rows]);

  const expandedRow = rows.find((row) => row.workItemId === expandedWorkItemId) ?? null;
  if (!runId) {
    return (
      <Card
        title="Target execution evidence"
        subtitle="Open a specific run to review source-to-target upload success, retry rows, and stamped metadata."
      >
        <EmptyState title="Choose a run" message="Go to Runs, open a run detail page, then select Target Evidence." />
        <p><Link className="secondaryButton" to="/runs">Open Runs</Link></p>
      </Card>
    );
  }


  function exportRows(kind: "all" | "success" | "retry") {
    if (!evidence) {
      return;
    }

    const exportRows = rows.filter((row) => {
      if (kind === "success") {
        return rowIsSuccess(row);
      }
      if (kind === "retry") {
        return rowIsFailed(row);
      }
      return true;
    });

    const fileName = `${safeFileName(evidence.jobName)}-${kind === "retry" ? "retry" : kind}-target-evidence.csv`;
    downloadCsv(fileName, [
      ["Status", "Origin_Id", "Id", "TargetAssetId", "WorkItemId", "FileName", "Message", "Error", "UpdatedUtc", ...stampedColumns],
      ...exportRows.map((row) => [
        row.status,
        row.originId ?? "",
        row.id ?? "",
        row.targetAssetId ?? "",
        row.workItemId,
        row.fileName ?? "",
        row.message ?? "",
        row.error ?? "",
        row.updatedUtc ?? "",
        ...stampedColumns.map((field) => fieldValue(row, field))
      ])
    ]);
  }

  return (
    <>
      <p><Link to={runId ? `/runs/${encodeURIComponent(runId)}` : "/runs"}>Back to Run</Link></p>

      <Card
        title="Target execution evidence"
        subtitle="Success/retry view of target upsert results. This is the row-level bridge between source Origin_Id and target Id."
        action={
          <div className="actionRow">
            <button type="button" onClick={() => void load()}>Refresh</button>
            <button type="button" onClick={() => exportRows("all")} disabled={rows.length === 0}>Export All</button>
            <button type="button" onClick={() => exportRows("success")} disabled={rows.length === 0}>Export Success</button>
            <button type="button" onClick={() => exportRows("retry")} disabled={rows.length === 0}>Export Retry</button>
          </div>
        }
      >
        <LoadingError loading={loading} error={error} onRetry={() => void load()} />

        {evidence && (
          <>
            <div className="metricGrid compact">
              <div className="metric"><span>Total rows</span><strong>{evidence.totalCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Success</span><strong>{evidence.successCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Failed</span><strong>{evidence.failedCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Retry</span><strong>{evidence.retryCount.toLocaleString()}</strong></div>
            </div>

            <div className="formGrid compact evidenceFilters">
              <label>
                Status
                <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                  <option value="all">All</option>
                  <option value="success">Success</option>
                  <option value="failed">Failed / retry</option>
                </select>
              </label>
              <label>
                Max rows
                <select value={take} onChange={(event) => setTake(Number(event.target.value))}>
                  <option value={100}>100</option>
                  <option value={500}>500</option>
                  <option value={1000}>1,000</option>
                  <option value={5000}>5,000</option>
                </select>
              </label>
            </div>
          </>
        )}
      </Card>

      {evidence && rows.length === 0 && (
        <Card>
          <EmptyState title="No evidence rows" message="No row-level target evidence has been recorded for this run yet." />
        </Card>
      )}

      {evidence && rows.length > 0 && (
        <Card title="Upload rows" subtitle="Scroll inside the table to review large run result sets without expanding the page indefinitely.">
          <div className="tableScroll tableScrollTall">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Origin_Id</th>
                  <th>Id</th>
                  <th>File</th>
                  <th>Message</th>
                  <th>Error</th>
                  <th>Updated</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.workItemId}>
                    <td><StatusPill status={row.status} /></td>
                    <td>{row.originId ?? "-"}</td>
                    <td>{row.id ?? row.targetAssetId ?? "-"}</td>
                    <td>{row.fileName ?? "-"}</td>
                    <td>{row.message ?? "-"}</td>
                    <td>{row.error ?? "-"}</td>
                    <td>{formatDate(row.updatedUtc)}</td>
                    <td><button type="button" onClick={() => setExpandedWorkItemId(row.workItemId)}>Open</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {expandedRow && (
        <Card
          title={`Evidence detail ${expandedRow.originId ?? expandedRow.workItemId}`}
          subtitle="Stamped fields and target payload captured for restamping and reconciliation."
          action={<button type="button" onClick={() => setExpandedWorkItemId(null)}>Close</button>}
        >
          <div className="detailGrid">
            <span>Status</span><strong>{expandedRow.status}</strong>
            <span>Origin_Id</span><strong>{expandedRow.originId ?? "-"}</strong>
            <span>Id</span><strong>{expandedRow.id ?? expandedRow.targetAssetId ?? "-"}</strong>
            <span>Message</span><strong>{expandedRow.message ?? "-"}</strong>
            <span>Error</span><strong>{expandedRow.error ?? "-"}</strong>
          </div>
          <h3>Stamped fields</h3>
          <JsonBlock value={expandedRow.stampedFields} />
          <h3>Target payload fields</h3>
          <JsonBlock value={expandedRow.targetPayloadFields} />
          <h3>Warnings</h3>
          <JsonBlock value={expandedRow.warnings} />
        </Card>
      )}
    </>
  );
}
