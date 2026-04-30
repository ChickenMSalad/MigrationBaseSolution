import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { Card, JsonBlock, StatusPill } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { RunEventsResponse, RunFailuresResponse, RunRecord, RunSummary, RunWorkItemsResponse } from "../types/api";

export function RunDetail() {
  const navigate = useNavigate();
  const { runId: routeRunId } = useParams();
  const runId = routeRunId ?? "";
  const back = () => navigate("/runs");
  const [run, setRun] = useState<RunRecord | null>(null);
  const [summary, setSummary] = useState<RunSummary | null>(null);
  const [events, setEvents] = useState<RunEventsResponse | null>(null);
  const [failures, setFailures] = useState<RunFailuresResponse | null>(null);
  const [workItems, setWorkItems] = useState<RunWorkItemsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const [runResult, summaryResult, eventResult, failureResult, workItemResult] = await Promise.allSettled([
        api.run(runId), api.runSummary(runId), api.runEvents(runId, 250), api.runFailures(runId), api.runWorkItems(runId)
      ]);
      if (runResult.status === "fulfilled") setRun(runResult.value);
      if (summaryResult.status === "fulfilled") setSummary(summaryResult.value);
      if (eventResult.status === "fulfilled") setEvents(eventResult.value);
      if (failureResult.status === "fulfilled") setFailures(failureResult.value);
      if (workItemResult.status === "fulfilled") setWorkItems(workItemResult.value);
      const firstError = [runResult, summaryResult, eventResult, failureResult, workItemResult].find((x) => x.status === "rejected");
      if (firstError && firstError.status === "rejected") setError(firstError.reason instanceof Error ? firstError.reason.message : String(firstError.reason));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    const timer = window.setInterval(load, 4000);
    return () => window.clearInterval(timer);
  }, [runId]);

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div><button className="ghost" onClick={back}>← Runs</button><h1>{run?.jobName ?? "Run"}</h1><p className="mono">{runId}</p></div>
        <div className="actions"><button onClick={load}>Refresh</button>{run && <StatusPill status={run.status} />}</div>
      </div>
      <LoadingError loading={loading} error={error} />
      <div className="metricGrid">
        <Card><div className="metric"><span>Total</span><strong>{String(summary?.total ?? workItems?.count ?? "—")}</strong></div></Card>
        <Card><div className="metric"><span>Completed</span><strong>{String(summary?.completed ?? "—")}</strong></div></Card>
        <Card><div className="metric"><span>Failed</span><strong>{String(summary?.failed ?? failures?.count ?? "—")}</strong></div></Card>
        <Card><div className="metric"><span>Events</span><strong>{String(events?.count ?? "—")}</strong></div></Card>
      </div>
      <Card title="Run record"><JsonBlock value={run} /></Card>
      <Card title="Summary"><JsonBlock value={summary} /></Card>
      <Card title="Failures"><JsonBlock value={failures} /></Card>
      <Card title="Recent events"><JsonBlock value={events} /></Card>
      <Card title="Work items"><JsonBlock value={workItems} /></Card>
    </div>
  );
}
