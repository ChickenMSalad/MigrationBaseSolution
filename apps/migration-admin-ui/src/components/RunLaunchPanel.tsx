import { useEffect, useMemo, useRef, useState } from 'react';
import {
  cancelMigrationRun,
  getRunProgress,
  launchMigrationRun,
  type QueueFanOutMode,
  type RunExecutionMode,
  type RunLaunchResponse,
  type RunProgressSnapshot
} from '../lib/runLaunchApi';

type SubmitState = 'idle' | 'submitting' | 'launched' | 'failed';

function toNumber(value: string, fallback: number): number {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function formatProgress(snapshot: RunProgressSnapshot | null): string {
  if (!snapshot) {
    return 'No run launched yet.';
  }

  const total = snapshot.totalWorkItems ?? 0;
  const completed = snapshot.completedWorkItems ?? 0;
  const failed = snapshot.failedWorkItems ?? 0;

  if (total <= 0) {
    return `${snapshot.status} — waiting for queue fan-out`;
  }

  return `${snapshot.status} — ${completed}/${total} completed, ${failed} failed`;
}

export function RunLaunchPanel() {
  const [projectId, setProjectId] = useState('');
  const [manifestImportId, setManifestImportId] = useState('');
  const [mappingProfileId, setMappingProfileId] = useState('');
  const [executionMode, setExecutionMode] = useState<RunExecutionMode>('dry-run');
  const [queueFanOutMode, setQueueFanOutMode] = useState<QueueFanOutMode>('balanced');
  const [requestedConcurrency, setRequestedConcurrency] = useState('4');
  const [notes, setNotes] = useState('');
  const [submitState, setSubmitState] = useState<SubmitState>('idle');
  const [launchResponse, setLaunchResponse] = useState<RunLaunchResponse | null>(null);
  const [progress, setProgress] = useState<RunProgressSnapshot | null>(null);
  const [error, setError] = useState<string | null>(null);
  const pollingRunId = useRef<string | null>(null);

  const canLaunch = useMemo(() => {
    return projectId.trim().length > 0 && submitState !== 'submitting';
  }, [projectId, submitState]);

  async function launchRun() {
    if (!canLaunch) {
      return;
    }

    setSubmitState('submitting');
    setError(null);
    setLaunchResponse(null);
    setProgress(null);

    try {
      const response = await launchMigrationRun({
        projectId: projectId.trim(),
        manifestImportId: manifestImportId.trim() || undefined,
        mappingProfileId: mappingProfileId.trim() || undefined,
        executionMode,
        queueFanOutMode,
        requestedConcurrency: toNumber(requestedConcurrency, 4),
        notes: notes.trim() || undefined
      });

      setLaunchResponse(response);
      pollingRunId.current = response.runId;
      setSubmitState('launched');
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Run launch failed.');
      setSubmitState('failed');
    }
  }

  async function cancelRun() {
    if (!launchResponse?.runId) {
      return;
    }

    setError(null);

    try {
      const snapshot = await cancelMigrationRun(launchResponse.runId);
      setProgress(snapshot);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Run cancellation failed.');
    }
  }

  async function refreshProgress(signal?: AbortSignal) {
    const runId = pollingRunId.current;

    if (!runId) {
      return;
    }

    try {
      const snapshot = await getRunProgress(runId, signal);
      setProgress(snapshot);
    } catch (exception) {
      if (!signal?.aborted) {
        setError(exception instanceof Error ? exception.message : 'Run progress refresh failed.');
      }
    }
  }

  useEffect(() => {
    if (!launchResponse?.runId) {
      return;
    }

    pollingRunId.current = launchResponse.runId;
    const controller = new AbortController();

    void refreshProgress(controller.signal);
    const timer = window.setInterval(() => {
      void refreshProgress(controller.signal);
    }, 5000);

    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [launchResponse?.runId]);

  return (
    <section className="config-panel" aria-label="Migration run launch workspace">
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', alignItems: 'flex-start' }}>
        <div>
          <p className="eyebrow">Run orchestration</p>
          <h2>Launch migration run</h2>
          <p>
            Create a SQL-backed operational run, fan out manifest rows into work items, and monitor queue progress.
          </p>
        </div>
        <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#475569' }}>P4.15</span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Project ID</span>
          <input value={projectId} onChange={(event) => setProjectId(event.target.value)} placeholder="project-..." />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Manifest import ID</span>
          <input value={manifestImportId} onChange={(event) => setManifestImportId(event.target.value)} placeholder="optional" />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Mapping profile ID</span>
          <input value={mappingProfileId} onChange={(event) => setMappingProfileId(event.target.value)} placeholder="optional" />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Execution mode</span>
          <select value={executionMode} onChange={(event) => setExecutionMode(event.target.value as RunExecutionMode)}>
            <option value="dry-run">Dry run</option>
            <option value="full-run">Full run</option>
          </select>
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Queue fan-out</span>
          <select value={queueFanOutMode} onChange={(event) => setQueueFanOutMode(event.target.value as QueueFanOutMode)}>
            <option value="single-batch">Single batch</option>
            <option value="balanced">Balanced</option>
            <option value="maximum-throughput">Maximum throughput</option>
          </select>
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Requested concurrency</span>
          <input value={requestedConcurrency} onChange={(event) => setRequestedConcurrency(event.target.value)} inputMode="numeric" />
        </label>
      </div>

      <label style={{ display: 'block', marginTop: '1rem' }}>
        <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Operator notes</span>
        <textarea value={notes} onChange={(event) => setNotes(event.target.value)} rows={3} />
      </label>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', marginTop: '1rem' }}>
        <button type="button" onClick={launchRun} disabled={!canLaunch}>
          {submitState === 'submitting' ? 'Launching…' : 'Launch run'}
        </button>
        <button type="button" onClick={() => void refreshProgress()} disabled={!launchResponse?.runId}>
          Refresh progress
        </button>
        <button type="button" onClick={cancelRun} disabled={!launchResponse?.runId}>
          Cancel run
        </button>
      </div>

      {error && <pre style={{ whiteSpace: 'pre-wrap', color: '#991b1b', marginTop: '1rem' }}>{error}</pre>}

      <div style={{ marginTop: '1rem', padding: '1rem', borderRadius: '1rem', background: '#f8fafc', border: '1px solid #dbe3ef' }}>
        <strong>{formatProgress(progress)}</strong>
        {launchResponse && (
          <pre style={{ whiteSpace: 'pre-wrap', marginTop: '0.75rem' }}>{JSON.stringify({ launchResponse, progress }, null, 2)}</pre>
        )}
      </div>
    </section>
  );
}
