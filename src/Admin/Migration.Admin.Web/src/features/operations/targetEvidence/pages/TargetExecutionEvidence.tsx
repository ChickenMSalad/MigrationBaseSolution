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

function safeCount(value?: number | null) {
  return value === undefined || value === null ? 0 : value;
}

export function TargetExecutionEvidence() {
  const { runId } = useParams();
  const [evidence, setEvidence] = useState<TargetExecutionEvidenceResponse | null>(null);
  const [statusFilter, setStatusFilter] = useState("all");
  const [searchText, setSearchText] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [take, setTake] = useState(500);
  const [skip, setSkip] = useState(0);
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
      setEvidence(await targetEvidenceApi.getRunEvidence(runId, statusFilter, take, skip, appliedSearch));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    setLoading(true);
    void load();
  }, [runId, statusFilter, take, skip, appliedSearch]);

  const rows = evidence?.rows ?? [];
  const totalCount = safeCount(evidence?.totalCount);
  const pageStart = totalCount === 0 ? 0 : skip + 1;
  const pageEnd = Math.min(skip + rows.length, totalCount);
  const canPageBack = skip > 0;
  const canPageForward = skip + rows.length < totalCount;

  const stampedColumns = useMemo(() => {
    const fields = new Set<string>();
    rows.forEach((row) => {
      Object.keys(row.stampedFields ?? {}).forEach((field) => fields.add(field));
      Object.keys(row.targetPayloadFields ?? {}).forEach((field) => fields.add(field));
    });
    return Array.from(fields).sort((a, b) => a.localeCompare(b));
  }, [rows]);

  const expandedRow = rows.find((row) => row.workItemId === expandedWorkItemId) ?? null;

  function applySearch() {
    setSkip(0);
    setAppliedSearch(searchText.trim());
  }

  function resetPagingForFilter(nextStatus: string) {
    setStatusFilter(nextStatus);
    setSkip(0);
    setExpandedWorkItemId(null);
  }

  function changeTake(nextTake: number) {
    setTake(nextTake);
    setSkip(0);
    setExpandedWorkItemId(null);
  }

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

  return (
    <>
      <p><Link to={runId ? `/runs/${encodeURIComponent(runId)}` : "/runs"}>Back to Run</Link></p>

      <Card
        title="Target execution evidence"
        subtitle="Success/retry view of target upsert results. This is the row-level bridge between source Origin_Id and target Id."
        action={
          <div className="actionRow wrapActions">
            <button type="button" onClick={() => void load()}>Refresh</button>
            <a className="secondaryButton" href={targetEvidenceApi.exportUrl(runId, "all", appliedSearch)}>Export All</a>
            <a className="secondaryButton" href={targetEvidenceApi.exportUrl(runId, "success", appliedSearch)}>Export Success</a>
            <a className="secondaryButton" href={targetEvidenceApi.exportUrl(runId, "retry", appliedSearch)}>Export Retry</a>
          </div>
        }
      >
        <LoadingError loading={loading} error={error} onRetry={() => void load()} />

        {evidence && (
          <>
            <div className="metricGrid compact">
              <div className="metric"><span>Filtered rows</span><strong>{evidence.totalCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Success</span><strong>{evidence.successCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Failed</span><strong>{evidence.failedCount.toLocaleString()}</strong></div>
              <div className="metric"><span>Retry</span><strong>{evidence.retryCount.toLocaleString()}</strong></div>
            </div>

            <div className="formGrid compact evidenceFilters">
              <label>
                Status
                <select value={statusFilter} onChange={(event) => resetPagingForFilter(event.target.value)}>
                  <option value="all">All</option>
                  <option value="success">Success</option>
                  <option value="failed">Failed / retry</option>
                </select>
              </label>
              <label>
                Search
                <span className="inlineInputAction">
                  <input
                    value={searchText}
                    onChange={(event) => setSearchText(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        event.preventDefault();
                        applySearch();
                      }
                    }}
                    placeholder="Origin_Id, target Id, file, error, stamped value"
                  />
                  <button type="button" onClick={applySearch}>Search</button>
                </span>
              </label>
              <label>
                Page size
                <select value={take} onChange={(event) => changeTake(Number(event.target.value))}>
                  <option value={100}>100</option>
                  <option value={500}>500</option>
                  <option value={1000}>1,000</option>
                  <option value={5000}>5,000</option>
                </select>
              </label>
            </div>

            <div className="paginationBar">
              <span>Showing {pageStart.toLocaleString()}-{pageEnd.toLocaleString()} of {totalCount.toLocaleString()}</span>
              <div className="actionRow">
                <button type="button" disabled={!canPageBack} onClick={() => setSkip(Math.max(skip - take, 0))}>Previous</button>
                <button type="button" disabled={!canPageForward} onClick={() => setSkip(skip + take)}>Next</button>
              </div>
            </div>
          </>
        )}
      </Card>

      {evidence && rows.length === 0 && (
        <Card>
          <EmptyState title="No evidence rows" message="No row-level target evidence matched this run/filter." />
        </Card>
      )}

      {evidence && rows.length > 0 && (
        <Card title="Upload rows" subtitle="Scroll inside the table to review large run result sets without expanding the page indefinitely.">
          <div className="tableScroll tableScrollTall evidenceTableScroll">
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
                  <tr key={row.workItemId} className={rowIsSuccess(row) ? "successEvidenceRow" : rowIsFailed(row) ? "failedEvidenceRow" : undefined}>
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
          {stampedColumns.length > 0 && (
            <>
              <h3>Flat stamped row</h3>
              <div className="tableScroll evidenceDetailScroll">
                <table>
                  <tbody>
                    {stampedColumns.map((field) => (
                      <tr key={field}>
                        <th>{field}</th>
                        <td>{fieldValue(expandedRow, field) || "-"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </Card>
      )}
    </>
  );
}
