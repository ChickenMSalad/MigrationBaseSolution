import { useEffect, useMemo, useState } from "react";
import { Card, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";

type ArtifactKind = "Manifest" | "Mapping" | "Binary" | "Report" | "Other" | string;

type ArtifactRecord = {
  artifactId: string;
  kind?: ArtifactKind;
  artifactType?: ArtifactKind;
  fileName: string;
  createdUtc?: string;
  uploadedUtc?: string;
  projectId?: string | null;
};

type ProjectRecord = {
  projectId: string;
  displayName: string;
  sourceType: string;
  targetType: string;
  manifestType: string;
  manifestArtifactId?: string | null;
  mappingArtifactId?: string | null;
};

type ManifestPreview = {
  artifactId: string;
  fileName: string;
  columns: string[];
  sampleRows: Record<string, string>[];
};

type MappingRow = {
  id: string;
  source: string;
  target: string;
  transform: string;
  required: boolean;
};

type SaveMappingResponse = {
  artifact: ArtifactRecord;
  mappingProfile: unknown;
};

const transforms = [
  "",
  "trim",
  "lower",
  "upper",
  "split:semicolon",
  "split:comma",
  "normalize-date",
  "boolean",
  "integer",
  "decimal",
  "empty-to-null"
];

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      message = body?.error ?? JSON.stringify(body);
    } catch {
      try {
        message = await response.text();
      } catch {
        // keep default
      }
    }
    throw new Error(message);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

function artifactKind(a: ArtifactRecord): string {
  return String(a.kind ?? a.artifactType ?? "");
}

function newRow(source = "", target = ""): MappingRow {
  return {
    id: crypto.randomUUID(),
    source,
    target,
    transform: "",
    required: false
  };
}

function defaultTargetName(source: string): string {
  return source
    .trim()
    .replace(/\s+/g, "_")
    .replace(/[()\[\]{}]/g, "")
    .replace(/__+/g, "_")
    .replace(/^_+|_+$/g, "")
    .toLowerCase();
}

export function MappingBuilder() {
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [manifestArtifacts, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [selectedManifestArtifactId, setSelectedManifestArtifactId] = useState("");
  const [preview, setPreview] = useState<ManifestPreview | null>(null);
  const [profileName, setProfileName] = useState("generated-mapping-profile");
  const [sourceType, setSourceType] = useState("LocalStorage");
  const [targetType, setTargetType] = useState("LocalStorage");
  const [fileName, setFileName] = useState("generated-mapping-profile.json");
  const [rows, setRows] = useState<MappingRow[]>([newRow()]);
  const [bindToProject, setBindToProject] = useState(true);
  const [createdArtifact, setCreatedArtifact] = useState<ArtifactRecord | null>(null);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const selectedProject = useMemo(
    () => projects.find(x => x.projectId === selectedProjectId),
    [projects, selectedProjectId]
  );

  const mappingProfile = useMemo(() => {
    const cleanRows = rows
      .filter(r => r.source.trim() && r.target.trim())
      .map(r => ({
        source: r.source.trim(),
        target: r.target.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {})
      }));

    return {
      profileName,
      sourceType,
      targetType,
      fieldMappings: cleanRows,
      requiredTargetFields: rows
        .filter(r => r.required && r.target.trim())
        .map(r => r.target.trim())
    };
  }, [rows, profileName, sourceType, targetType]);

  async function loadInitial() {
    setLoading(true);
    setError(null);
    try {
      const [projectData, artifactData] = await Promise.all([
        request<ProjectRecord[]>("/api/projects"),
        request<ArtifactRecord[]>("/api/artifacts?kind=Manifest")
      ]);
      setProjects(projectData ?? []);
      setManifestArtifacts((artifactData ?? []).filter(x => artifactKind(x).toLowerCase() === "manifest" || !artifactKind(x)));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void loadInitial(); }, []);

  useEffect(() => {
    if (!selectedProject) return;

    setSourceType(selectedProject.sourceType || sourceType);
    setTargetType(selectedProject.targetType || targetType);

    if (selectedProject.manifestArtifactId) {
      setSelectedManifestArtifactId(selectedProject.manifestArtifactId);
    }

    const safeName = `${selectedProject.displayName || selectedProject.projectId}-mapping`
      .replace(/\s+/g, "-")
      .replace(/[^a-zA-Z0-9_.-]/g, "")
      .toLowerCase();

    if (safeName) {
      setProfileName(safeName);
      setFileName(`${safeName}.json`);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedProjectId]);

  async function loadPreview() {
    if (!selectedManifestArtifactId) return;

    setError(null);
    try {
      const data = await request<ManifestPreview>(`/api/mapping-builder/manifests/${encodeURIComponent(selectedManifestArtifactId)}/columns`);
      setPreview(data);

      if (data.columns.length > 0) {
        setRows(data.columns.map(c => newRow(c, defaultTargetName(c))));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  }

  function updateRow(id: string, patch: Partial<MappingRow>) {
    setRows(current => current.map(row => row.id === id ? { ...row, ...patch } : row));
  }

  function removeRow(id: string) {
    setRows(current => current.length <= 1 ? current : current.filter(row => row.id !== id));
  }

  async function saveMapping() {
    setSaving(true);
    setError(null);
    setCreatedArtifact(null);
    try {
      const response = await request<SaveMappingResponse>("/api/mapping-builder/mappings", {
        method: "POST",
        body: JSON.stringify({
          profileName,
          sourceType,
          targetType,
          fileName,
          manifestArtifactId: selectedManifestArtifactId || null,
          projectId: selectedProjectId || null,
          fieldMappings: mappingProfile.fieldMappings,
          requiredTargetFields: mappingProfile.requiredTargetFields
        })
      });

      setCreatedArtifact(response.artifact);

      if (bindToProject && selectedProjectId) {
        await request(`/api/projects/${encodeURIComponent(selectedProjectId)}/artifacts`, {
          method: "PUT",
          body: JSON.stringify({
            manifestArtifactId: selectedManifestArtifactId || null,
            mappingArtifactId: response.artifact.artifactId
          })
        });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <h1>Mapping Builder</h1>
          <p>Build a generic mapping profile from an uploaded manifest. Saved mappings are stored as Mapping artifacts and can be managed from the Artifacts page.</p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />

      <Card title="Setup" subtitle="Choose a project and manifest. Project values are optional but help pre-fill source and target types.">
        <div className="formGrid wide">
          <label>
            Project
            <select value={selectedProjectId} onChange={(e) => setSelectedProjectId(e.target.value)}>
              <option value="">No project selected</option>
              {projects.map(project => (
                <option key={project.projectId} value={project.projectId}>
                  {project.displayName} ({project.sourceType} → {project.targetType})
                </option>
              ))}
            </select>
          </label>

          <label>
            Manifest artifact
            <select value={selectedManifestArtifactId} onChange={(e) => setSelectedManifestArtifactId(e.target.value)}>
              <option value="">Select a manifest artifact</option>
              {manifestArtifacts.map(artifact => (
                <option key={artifact.artifactId} value={artifact.artifactId}>
                  {artifact.fileName} ({artifact.artifactId})
                </option>
              ))}
            </select>
          </label>
        </div>

        <button className="primary" disabled={!selectedManifestArtifactId} onClick={loadPreview}>
          Load Manifest Columns
        </button>
      </Card>

      {preview && (
        <Card title="Manifest preview" subtitle={`${preview.fileName} — ${preview.columns.length} columns detected`}>
          <div className="pillRow">
            {preview.columns.map(column => <span className="pill" key={column}>{column}</span>)}
          </div>
          {preview.sampleRows.length > 0 && <JsonBlock value={preview.sampleRows.slice(0, 3)} />}
        </Card>
      )}

      <Card title="Mapping profile" subtitle="The generated JSON matches Migration.Application.Models.MappingProfile.">
        <div className="formGrid wide">
          <label>Profile name<input value={profileName} onChange={(e) => setProfileName(e.target.value)} /></label>
          <label>File name<input value={fileName} onChange={(e) => setFileName(e.target.value)} /></label>
          <label>Source type<input value={sourceType} onChange={(e) => setSourceType(e.target.value)} /></label>
          <label>Target type<input value={targetType} onChange={(e) => setTargetType(e.target.value)} /></label>
        </div>
      </Card>

      <Card title="Field mappings" subtitle="Mark required target fields when the target must receive a value.">
        <div className="tableWrap">
          <table>
            <thead>
              <tr>
                <th>Source</th>
                <th>Target</th>
                <th>Transform</th>
                <th>Required target</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rows.map(row => (
                <tr key={row.id}>
                  <td>
                    <select value={row.source} onChange={(e) => updateRow(row.id, { source: e.target.value })}>
                      <option value="">Select source</option>
                      {preview?.columns.map(column => <option key={column} value={column}>{column}</option>)}
                    </select>
                  </td>
                  <td><input value={row.target} onChange={(e) => updateRow(row.id, { target: e.target.value })} /></td>
                  <td>
                    <select value={row.transform} onChange={(e) => updateRow(row.id, { transform: e.target.value })}>
                      {transforms.map(transform => (
                        <option key={transform || "none"} value={transform}>{transform || "none"}</option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input type="checkbox" checked={row.required} onChange={(e) => updateRow(row.id, { required: e.target.checked })} />
                  </td>
                  <td><button className="ghost" onClick={() => removeRow(row.id)}>Remove</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <button className="ghost" onClick={() => setRows(current => [...current, newRow()])}>+ Add mapping row</button>
      </Card>

      <Card title="Generated mapping JSON">
        <JsonBlock value={mappingProfile} />
        <label className="check">
          <input type="checkbox" checked={bindToProject} onChange={(e) => setBindToProject(e.target.checked)} />
          Bind saved mapping to selected project
        </label>
        <button className="primary" disabled={saving || mappingProfile.fieldMappings.length === 0} onClick={saveMapping}>
          {saving ? "Saving…" : "Save Mapping Artifact"}
        </button>
      </Card>

{createdArtifact && (
  <div className="successBanner">
    <strong>Mapping artifact created.</strong>
    <p>
      Saved <code>{createdArtifact.fileName}</code> to Artifacts as kind <strong>Mapping</strong>.
    </p>
    <div className="buttonRow">
      <a className="secondaryButton" href="/artifacts">
        View in Artifacts
      </a>
    </div>
  </div>
)}
    </div>
  );
}
