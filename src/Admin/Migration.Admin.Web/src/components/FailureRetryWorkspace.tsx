import { useMemo, useState } from 'react';
import {
  retryRunFailures,
  searchRunFailures,
  type FailureRecord,
  type FailureRetryScope,
  type FailureSearchResponse,
  type RetryFailureResponse
} from '../lib/failureRetryApi';

type WorkspaceState = 'idle' | 'loading' | 'loaded' | 'submitting' | 'failed';

function summarizeFailures(response: FailureSearchResponse | null): string {
  if (!response) {
    return 'No failure search has been run.';
  }

  const total = response.totalFailures ?? response.returnedFailures;
  const retryable = response.retryableFailures ?? response.failures.filter((failure) => failure.isRetryable).length;
  return `${response.returnedFailures} displayed / ${total} total, ${retryable} retryable`;
}

function formatFailureTitle(failure: FailureRecord): string {
  const asset = failure.assetId || failure.sourceIdentifier || failure.workItemId || failure.failureId;
  const code = failure.failureCode || failure.failureCategory || 'failure';
  return `${asset} — ${code}`;
}

export function FailureRetryWorkspace() {
  const [runId, setRunId] = useState('');
  const [searchText, setSearchText] = useState('');
  const [includeNonRetryable, setIncludeNonRetryable] = useState(false);
  const [limit, setLimit] = useState('50');
  const [operatorNote, setOperatorNote] = useState('');
  const [state, setState] = useState<WorkspaceState>('idle');
  const [response, setResponse] = useState<FailureSearchResponse | null>(null);
  const [retryResponse, setRetryResponse] = useState<RetryFailureResponse | null>(null);
  const [selectedFailureIds, setSelectedFailureIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  const retryableFailures = useMemo(() => {
    return response?.failures.filter((failure) => failure.isRetryable) ?? [];
  }, [response]);

  const selectedRetryableCount = useMemo(() => {
    const selected = new Set(selectedFailureIds);
    return retryableFailures.filter((failure) => selected.has(failure.failureId)).length;
  }, [retryableFailures, selectedFailureIds]);

  function parseLimit(): number {
    const parsed = Number.parseInt(limit, 10);
    return Number.isFinite(parsed) && parsed > 0 ? Math.min(parsed, 500) : 50;
  }

  function toggleFailure(failureId: string) {
    setSelectedFailureIds((current) => {
      if (current.includes(failureId)) {
        return current.filter((id) => id !== failureId);
      }

      return [...current, failureId];
    });
  }

  async function loadFailures() {
    if (!runId.trim()) {
      setError('Run ID is required.');
      return;
    }

    setState('loading');
    setError(null);
    setRetryResponse(null);

    try {
      const result = await searchRunFailures({
        runId: runId.trim(),
        searchText: searchText.trim() || undefined,
        includeNonRetryable,
        limit: parseLimit()
      });

      setResponse(result);
      setSelectedFailureIds(result.failures.filter((failure) => failure.isRetryable).map((failure) => failure.failureId));
      setState('loaded');
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Failure search failed.');
      setState('failed');
    }
  }

  async function submitRetry(scope: FailureRetryScope) {
    if (!runId.trim()) {
      setError('Run ID is required.');
      return;
    }

    const failureIds = scope === 'selected' ? selectedFailureIds : [];

    if (scope === 'selected' && failureIds.length === 0) {
      setError('Select at least one retryable failure.');
      return;
    }

    setState('submitting');
    setError(null);

    try {
      const result = await retryRunFailures({
        runId: runId.trim(),
        scope,
        failureIds,
        operatorNote: operatorNote.trim() || undefined
      });

      setRetryResponse(result);
      setState('loaded');
      await loadFailures();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Retry request failed.');
      setState('failed');
    }
  }

  return (
    <section className="config-panel" aria-label="Failure review and retry workspace">
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', alignItems: 'flex-start' }}>
        <div>
          <p className="eyebrow">Failure recovery</p>
          <h2>Review and retry failed work items</h2>
          <p>
            Search run failures, select retryable records, and submit SQL-backed retry requests for the coordinated runtime.
          </p>
        </div>
        <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#475569' }}>P4.16</span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Run ID</span>
          <input value={runId} onChange={(event) => setRunId(event.target.value)} placeholder="run-..." />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Search text</span>
          <input value={searchText} onChange={(event) => setSearchText(event.target.value)} placeholder="asset, code, message" />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Limit</span>
          <input value={limit} onChange={(event) => setLimit(event.target.value)} inputMode="numeric" />
        </label>

        <label style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '1.55rem' }}>
          <input
            type="checkbox"
            checked={includeNonRetryable}
            onChange={(event) => setIncludeNonRetryable(event.target.checked)}
          />
          Include non-retryable failures
        </label>
      </div>

      <label style={{ display: 'block', marginTop: '1rem' }}>
        <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Retry note</span>
        <textarea value={operatorNote} onChange={(event) => setOperatorNote(event.target.value)} rows={3} />
      </label>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', marginTop: '1rem' }}>
        <button type="button" onClick={loadFailures} disabled={state === 'loading' || state === 'submitting'}>
          {state === 'loading' ? 'Searching…' : 'Search failures'}
        </button>
        <button type="button" onClick={() => void submitRetry('selected')} disabled={selectedRetryableCount === 0 || state === 'submitting'}>
          Retry selected ({selectedRetryableCount})
        </button>
        <button type="button" onClick={() => void submitRetry('all-retryable')} disabled={retryableFailures.length === 0 || state === 'submitting'}>
          Retry all retryable
        </button>
      </div>

      {error && <pre style={{ whiteSpace: 'pre-wrap', color: '#991b1b', marginTop: '1rem' }}>{error}</pre>}

      <div style={{ marginTop: '1rem', padding: '1rem', borderRadius: '1rem', background: '#f8fafc', border: '1px solid #dbe3ef' }}>
        <strong>{summarizeFailures(response)}</strong>
        {retryResponse && (
          <pre style={{ whiteSpace: 'pre-wrap', marginTop: '0.75rem' }}>{JSON.stringify(retryResponse, null, 2)}</pre>
        )}
      </div>

      {response && (
        <div style={{ display: 'grid', gap: '0.75rem', marginTop: '1rem' }}>
          {response.failures.map((failure) => (
            <article key={failure.failureId} style={{ padding: '1rem', borderRadius: '1rem', border: '1px solid #dbe3ef', background: 'white' }}>
              <label style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-start' }}>
                <input
                  type="checkbox"
                  checked={selectedFailureIds.includes(failure.failureId)}
                  onChange={() => toggleFailure(failure.failureId)}
                  disabled={!failure.isRetryable}
                />
                <span>
                  <strong>{formatFailureTitle(failure)}</strong>
                  <span style={{ display: 'block', color: failure.isRetryable ? '#166534' : '#991b1b', fontWeight: 700 }}>
                    {failure.isRetryable ? 'Retryable' : 'Not retryable'}
                    {typeof failure.attemptCount === 'number' ? ` · attempts: ${failure.attemptCount}` : ''}
                  </span>
                  <span style={{ display: 'block', marginTop: '0.35rem', color: '#334155' }}>{failure.message}</span>
                </span>
              </label>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
