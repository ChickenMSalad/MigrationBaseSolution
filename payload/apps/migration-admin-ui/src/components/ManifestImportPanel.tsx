import { useMemo, useState } from 'react';
import { importManifestFile, type ManifestImportResult } from '../lib/manifestImportApi';

type ImportMode = 'validate-only' | 'import';

type SubmitState = 'idle' | 'submitting' | 'succeeded' | 'failed';

function formatBytes(value: number): string {
  if (value < 1024) {
    return `${value} B`;
  }

  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`;
  }

  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}

export function ManifestImportPanel() {
  const [projectId, setProjectId] = useState('');
  const [mappingProfileId, setMappingProfileId] = useState('');
  const [importMode, setImportMode] = useState<ImportMode>('validate-only');
  const [file, setFile] = useState<File | null>(null);
  const [submitState, setSubmitState] = useState<SubmitState>('idle');
  const [result, setResult] = useState<ManifestImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = useMemo(() => {
    return projectId.trim().length > 0 && file !== null && submitState !== 'submitting';
  }, [file, projectId, submitState]);

  async function submitImport() {
    if (!file || !canSubmit) {
      return;
    }

    setSubmitState('submitting');
    setError(null);
    setResult(null);

    try {
      const response = await importManifestFile({
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
        projectId: projectId.trim(),
        mappingProfileId: mappingProfileId.trim() || undefined,
        importMode
      }, file);

      setResult(response);
      setSubmitState('succeeded');
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Manifest import failed.');
      setSubmitState('failed');
    }
  }

  return (
    <section className="config-panel" aria-label="Manifest import workspace">
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', alignItems: 'flex-start' }}>
        <div>
          <p className="eyebrow">Manifest intake</p>
          <h2>SQL manifest import workspace</h2>
          <p>
            Validate or import CSV/Excel manifest artifacts into the SQL-backed operational runtime.
          </p>
        </div>
        <span style={{ fontSize: '0.8rem', fontWeight: 700, color: '#475569' }}>P4.14</span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem', marginTop: '1rem' }}>
        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Project ID</span>
          <input
            type="text"
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
            placeholder="project-..."
            style={{ width: '100%', padding: '0.7rem', borderRadius: '0.75rem', border: '1px solid #cbd5e1' }}
          />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Mapping profile ID</span>
          <input
            type="text"
            value={mappingProfileId}
            onChange={(event) => setMappingProfileId(event.target.value)}
            placeholder="optional"
            style={{ width: '100%', padding: '0.7rem', borderRadius: '0.75rem', border: '1px solid #cbd5e1' }}
          />
        </label>

        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Mode</span>
          <select
            value={importMode}
            onChange={(event) => setImportMode(event.target.value as ImportMode)}
            style={{ width: '100%', padding: '0.7rem', borderRadius: '0.75rem', border: '1px solid #cbd5e1' }}
          >
            <option value="validate-only">Validate only</option>
            <option value="import">Import into SQL</option>
          </select>
        </label>
      </div>

      <div style={{ marginTop: '1rem' }}>
        <label>
          <span style={{ display: 'block', fontWeight: 700, marginBottom: '0.35rem' }}>Manifest file</span>
          <input
            type="file"
            accept=".csv,.xlsx,.xls,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,text/csv"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </label>
        {file && (
          <p style={{ color: '#475569' }}>
            Selected <strong>{file.name}</strong> ({formatBytes(file.size)})
          </p>
        )}
      </div>

      <button type="button" onClick={submitImport} disabled={!canSubmit} style={{ marginTop: '1rem' }}>
        {submitState === 'submitting' ? 'Submitting…' : importMode === 'import' ? 'Import manifest' : 'Validate manifest'}
      </button>

      {error && (
        <pre style={{ whiteSpace: 'pre-wrap', color: '#991b1b', marginTop: '1rem' }}>{error}</pre>
      )}

      {result && (
        <pre style={{ whiteSpace: 'pre-wrap', background: '#0f172a', color: '#e2e8f0', padding: '1rem', borderRadius: '1rem', marginTop: '1rem' }}>
          {JSON.stringify(result, null, 2)}
        </pre>
      )}
    </section>
  );
}
