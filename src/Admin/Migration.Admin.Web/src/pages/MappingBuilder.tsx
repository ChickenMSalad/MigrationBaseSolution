import { useEffect, useMemo, useState } from "react";
import { Card, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";

type ArtifactKind = "Manifest" | "Mapping" | "Taxonomy" | "Binary" | "Report" | "Other" | string;

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
  taxonomyArtifactId?: string | null;
};

type ManifestPreview = {
  artifactId: string;
  fileName: string;
  columns: string[];
  sampleRows: Record<string, string>[];
};

type MappingType = "intermediate" | "target";

type MappingRow = {
  id: string;
  source: string;
  target: string;
  transform: string;
  required: boolean;
};

type TagRule = {
  id: string;
  sourceField: string;
  tagName: string;
  transform: string;
};

type MetadataRule = {
  id: string;
  sourceField: string;
  jsonPath: string;
  transform: string;
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

function isKind(a: ArtifactRecord, kind: string): boolean {
  return artifactKind(a).toLowerCase() === kind.toLowerCase();
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

function newTagRule(sourceField = "", tagName = ""): TagRule {
  return {
    id: crypto.randomUUID(),
    sourceField,
    tagName,
    transform: "trim"
  };
}

function newMetadataRule(sourceField = "", jsonPath = ""): MetadataRule {
  return {
    id: crypto.randomUUID(),
    sourceField,
    jsonPath,
    transform: "trim"
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

function defaultBlobNameTemplate(columns: string[]): string {
  const fileName = columns.find(c => /file\s*name|filename|name/i.test(c));
  const assetId = columns.find(c => /asset\s*id|assetid|id/i.test(c));
  if (fileName) return `{${fileName}}`;
  if (assetId) return `{${assetId}}`;
  return "{assetId}";
}

export function MappingBuilder() {
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [taxonomyArtifacts, setTaxonomyArtifacts] = useState<ArtifactRecord[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [selectedManifestArtifactId, setSelectedManifestArtifactId] = useState("");
  const [selectedTargetArtifactId, setSelectedTargetArtifactId] = useState("");
  const [mappingType, setMappingType] = useState<MappingType>("intermediate");
  const [preview, setPreview] = useState<ManifestPreview | null>(null);

  const [profileName, setProfileName] = useState("generated-mapping-profile");
  const [sourceType, setSourceType] = useState("LocalStorage");
  const [targetType, setTargetType] = useState("LocalStorage");
  const [fileName, setFileName] = useState("generated-mapping-profile.json");
  const [rows, setRows] = useState<MappingRow[]>([newRow()]);

  const [blobNameTemplate, setBlobNameTemplate] = useState("{assetId}");
  const [metadataJsonPathTemplate, setMetadataJsonPathTemplate] = useState("metadata/{assetId}.json");
  const [mapToFolderPathColumn, setMapToFolderPathColumn] = useState(false);
  const [folderPathColumn, setFolderPathColumn] = useState("");

  const [binaryOnly, setBinaryOnly] = useState(false);
  const [writeMetadataJson, setWriteMetadataJson] = useState(true);
  const [writeBlobTags, setWriteBlobTags] = useState(true);
  const [tagRules, setTagRules] = useState<TagRule[]>([newTagRule()]);
  const [metadataRules, setMetadataRules] = useState<MetadataRule[]>([newMetadataRule()]);

  const [bindToProject, setBindToProject] = useState(true);
  const [createdArtifact, setCreatedArtifact] = useState<ArtifactRecord | null>(null);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const selectedProject = useMemo(
    () => projects.find(x => x.projectId === selectedProjectId),
    [projects, selectedProjectId]
  );

  const hasProjectManifest = Boolean(selectedManifestArtifactId);
  const manifestMissingMessage = selectedProjectId
    ? "Selected project is missing a manifest artifact. Bind a manifest to the project before loading columns."
    : "Select a project with a manifest artifact to load manifest columns.";

  const mappingProfile = useMemo(() => {
    const cleanRows = rows
      .filter(r => r.source.trim() && r.target.trim())
      .map(r => ({
        source: r.source.trim(),
        target: r.target.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {})
      }));

    const cleanTagRules = tagRules
      .filter(r => r.sourceField.trim() && r.tagName.trim())
      .map(r => ({
        sourceField: r.sourceField.trim(),
        tagName: r.tagName.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {})
      }));

    const cleanMetadataRules = metadataRules
      .filter(r => r.sourceField.trim() && r.jsonPath.trim())
      .map(r => ({
        sourceField: r.sourceField.trim(),
        jsonPath: r.jsonPath.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {})
      }));

    if (mappingType === "intermediate") {
      return {
        profileName,
        sourceType,
        targetType,
        mappingType,
        manifestArtifactId: selectedManifestArtifactId || null,
        intermediateStorage: {
          provider: "AzureBlob",
          binaryOnly,
          blobNameTemplate,
          mapToFolderPathColumn,
          folderPathMode: mapToFolderPathColumn ? "manifestColumn" : "none",
          folderPathColumn: mapToFolderPathColumn ? folderPathColumn || null : null,
          folderPathSource: mapToFolderPathColumn ? folderPathColumn || null : null,
          writeBlobTags: !binaryOnly && writeBlobTags,
          writeMetadataJson: !binaryOnly && writeMetadataJson,
          metadataJsonPathTemplate: !binaryOnly && writeMetadataJson ? metadataJsonPathTemplate : null,
          tagRules: binaryOnly ? [] : cleanTagRules,
          metadataRules: binaryOnly ? [] : cleanMetadataRules
        },
        fieldMappings: [],
        requiredTargetFields: []
      };
    }

    return {
      profileName,
      sourceType,
      targetType,
      mappingType,
      manifestArtifactId: selectedManifestArtifactId || null,
      targetArtifactId: selectedTargetArtifactId || null,
      fieldMappings: cleanRows,
      requiredTargetFields: rows
        .filter(r => r.required && r.target.trim())
        .map(r => r.target.trim())
    };
  }, [
    rows,
    tagRules,
    metadataRules,
    profileName,
    sourceType,
    targetType,
    mappingType,
    selectedManifestArtifactId,
    selectedTargetArtifactId,
    binaryOnly,
    blobNameTemplate,
    mapToFolderPathColumn,
    folderPathColumn,
    writeBlobTags,
    writeMetadataJson,
    metadataJsonPathTemplate
  ]);

  async function loadInitial() {
    setLoading(true);
    setError(null);
    try {
      const [projectData, artifactData] = await Promise.all([
        request<ProjectRecord[]>("/api/projects"),
        request<ArtifactRecord[]>("/api/artifacts")
      ]);

      const artifacts = artifactData ?? [];
      setProjects(projectData ?? []);
      setManifestArtifacts(artifacts.filter(x => isKind(x, "Manifest") || !artifactKind(x)));
      setTaxonomyArtifacts(artifacts.filter(x => isKind(x, "Taxonomy") || isKind(x, "Manifest")));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { void loadInitial(); }, []);

  useEffect(() => {
    if (!selectedProject) {
      setSelectedManifestArtifactId("");
      setSelectedTargetArtifactId("");
      setPreview(null);
      return;
    }

    setSourceType(selectedProject.sourceType || sourceType);
    setTargetType(selectedProject.targetType || targetType);
    setSelectedManifestArtifactId(selectedProject.manifestArtifactId ?? "");
    setSelectedTargetArtifactId(selectedProject.taxonomyArtifactId ?? "");
    setPreview(null);

    const suffix = mappingType === "intermediate" ? "intermediate-storage-mapping" : "target-mapping";
    const safeName = `${selectedProject.displayName || selectedProject.projectId}-${suffix}`
      .replace(/\s+/g, "-")
      .replace(/[^a-zA-Z0-9_.-]/g, "")
      .toLowerCase();

    if (safeName) {
      setProfileName(safeName);
      setFileName(`${safeName}.json`);
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedProjectId, mappingType]);

  async function loadPreview() {
    if (!selectedManifestArtifactId) return;

    setError(null);
    try {
      const data = await request<ManifestPreview>(`/api/mapping-builder/manifests/${encodeURIComponent(selectedManifestArtifactId)}/columns`);
      setPreview(data);

      if (data.columns.length > 0) {
        setBlobNameTemplate(defaultBlobNameTemplate(data.columns));
        setRows(data.columns.map(c => newRow(c, defaultTargetName(c))));
        setTagRules(data.columns.slice(0, 5).map(c => newTagRule(c, defaultTargetName(c))));
        setMetadataRules(data.columns.map(c => newMetadataRule(c, defaultTargetName(c))));

        if (mapToFolderPathColumn && folderPathColumn && !data.columns.includes(folderPathColumn)) {
          setFolderPathColumn("");
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  }

  function updateRow(id: string, patch: Partial<MappingRow>) {
    setRows(current => current.map(row => row.id === id ? { ...row, ...patch } : row));
  }

  function updateTagRule(id: string, patch: Partial<TagRule>) {
    setTagRules(current => current.map(row => row.id === id ? { ...row, ...patch } : row));
  }

  function updateMetadataRule(id: string, patch: Partial<MetadataRule>) {
    setMetadataRules(current => current.map(row => row.id === id ? { ...row, ...patch } : row));
  }

  function removeRow(id: string) {
    setRows(current => current.length <= 1 ? current : current.filter(row => row.id !== id));
  }

  function removeTagRule(id: string) {
    setTagRules(current => current.length <= 1 ? current : current.filter(row => row.id !== id));
  }

  function removeMetadataRule(id: string) {
    setMetadataRules(current => current.length <= 1 ? current : current.filter(row => row.id !== id));
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
          mappingType,
          fileName,
          manifestArtifactId: selectedManifestArtifactId || null,
          targetArtifactId: mappingType === "target" ? selectedTargetArtifactId || null : null,
          projectId: selectedProjectId || null,
          fieldMappings: mappingType === "target" ? mappingProfile.fieldMappings : [],
          requiredTargetFields: mappingType === "target" ? mappingProfile.requiredTargetFields : [],
          intermediateStorage: mappingType === "intermediate" ? mappingProfile.intermediateStorage : null
        })
      });

      setCreatedArtifact(response.artifact);

      if (bindToProject && selectedProjectId) {
        await request(`/api/projects/${encodeURIComponent(selectedProjectId)}/artifacts`, {
          method: "PUT",
          body: JSON.stringify({
            manifestArtifactId: selectedManifestArtifactId || null,
            mappingArtifactId: response.artifact.artifactId,
            taxonomyArtifactId: mappingType === "target" ? selectedTargetArtifactId || null : undefined
          })
        });
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  const canSave = mappingType === "intermediate"
    ? Boolean(
        profileName.trim() &&
        blobNameTemplate.trim() &&
        hasProjectManifest &&
        (!mapToFolderPathColumn || folderPathColumn.trim()) &&
        (binaryOnly || tagRules.some(r => r.sourceField.trim() && r.tagName.trim()) || metadataRules.some(r => r.sourceField.trim() && r.jsonPath.trim()))
      )
    : mappingProfile.fieldMappings.length > 0;

  return (
    <div className="pageStack">
      <div className="pageTitle">
        <div>
          <h1>Mapping Builder</h1>
          <p>Build an intermediate storage mapping for Azure Blob staging, or a target mapping profile for final target systems.</p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />

      <Card title="Setup" subtitle="Choose whether this mapping describes intermediate storage behavior or final target field mapping.">
        <div className="formGrid wide">
          <label>
            Mapping type
            <select value={mappingType} onChange={(e) => setMappingType(e.target.value as MappingType)}>
              <option value="intermediate">Intermediate storage mapping</option>
              <option value="target">Target system mapping</option>
            </select>
          </label>

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

          {mappingType === "target" && (
            <label>
              Target taxonomy/manifest artifact
              <select value={selectedTargetArtifactId} onChange={(e) => setSelectedTargetArtifactId(e.target.value)}>
                <option value="">Select target artifact</option>
                {taxonomyArtifacts.map(artifact => (
                  <option key={artifact.artifactId} value={artifact.artifactId}>
                    {artifact.fileName} ({artifactKind(artifact) || "Artifact"})
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>

        {!hasProjectManifest && (
          <p className="muted">{manifestMissingMessage}</p>
        )}

        {hasProjectManifest && selectedProject && (
          <p className="muted">Using the manifest artifact already bound to {selectedProject.displayName}.</p>
        )}

        <button className="primary" disabled={!hasProjectManifest} onClick={loadPreview}>
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

      <Card title="Mapping profile" subtitle="The saved JSON stays a Mapping artifact. Intermediate profiles add storage behavior for Azure Blob runs.">
        <div className="formGrid wide">
          <label>Profile name<input value={profileName} onChange={(e) => setProfileName(e.target.value)} /></label>
          <label>File name<input value={fileName} onChange={(e) => setFileName(e.target.value)} /></label>
          <label>Source type<input value={sourceType} onChange={(e) => setSourceType(e.target.value)} /></label>
          <label>Target type<input value={targetType} onChange={(e) => setTargetType(e.target.value)} /></label>
        </div>
      </Card>

      {mappingType === "intermediate" && (
        <>
          <Card title="Intermediate storage behavior" subtitle="Define how the WebDam manifest should become Azure Blob objects, tags, and optional metadata JSON.">
            <div className="formGrid wide">
              <label>
                Blob name template
                <input value={blobNameTemplate} onChange={(e) => setBlobNameTemplate(e.target.value)} placeholder="assets/{Asset ID}/{Filename}" />
              </label>

              <label>
                Metadata JSON path template
                <input disabled={binaryOnly || !writeMetadataJson} value={metadataJsonPathTemplate} onChange={(e) => setMetadataJsonPathTemplate(e.target.value)} placeholder="metadata/{assetId}.json" />
              </label>

              <label>
                Folder path column
                <select disabled={!mapToFolderPathColumn || !preview} value={folderPathColumn} onChange={(e) => setFolderPathColumn(e.target.value)}>
                  <option value="">Select manifest column</option>
                  {preview?.columns.map(column => <option key={column} value={column}>{column}</option>)}
                </select>
              </label>
            </div>

            <label className="check">
              <input type="checkbox" checked={binaryOnly} onChange={(e) => setBinaryOnly(e.target.checked)} />
              Binary only; do not write blob tags or metadata JSON
            </label>

            <label className="check">
              <input type="checkbox" checked={mapToFolderPathColumn} onChange={(e) => setMapToFolderPathColumn(e.target.checked)} />
              Map to Folder Path Column
            </label>

            <label className="check">
              <input type="checkbox" disabled={binaryOnly} checked={writeBlobTags} onChange={(e) => setWriteBlobTags(e.target.checked)} />
              Store selected manifest values as Azure Blob index tags
            </label>

            <label className="check">
              <input type="checkbox" disabled={binaryOnly} checked={writeMetadataJson} onChange={(e) => setWriteMetadataJson(e.target.checked)} />
              Write selected manifest values to a metadata JSON sidecar
            </label>
          </Card>

          {!binaryOnly && writeBlobTags && (
            <Card title="Blob tag rules" subtitle="Choose manifest columns that should become Azure Blob index tags. Use tags for searchable lookup fields instead of path prefixes.">
              <div className="tableWrap">
                <table>
                  <thead>
                    <tr>
                      <th>Manifest field</th>
                      <th>Blob tag name</th>
                      <th>Transform</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {tagRules.map(row => (
                      <tr key={row.id}>
                        <td>
                          <select value={row.sourceField} onChange={(e) => updateTagRule(row.id, { sourceField: e.target.value })}>
                            <option value="">Select source</option>
                            {preview?.columns.map(column => <option key={column} value={column}>{column}</option>)}
                          </select>
                        </td>
                        <td><input value={row.tagName} onChange={(e) => updateTagRule(row.id, { tagName: e.target.value })} /></td>
                        <td>
                          <select value={row.transform} onChange={(e) => updateTagRule(row.id, { transform: e.target.value })}>
                            {transforms.map(transform => <option key={transform || "none"} value={transform}>{transform || "none"}</option>)}
                          </select>
                        </td>
                        <td><button className="ghost" onClick={() => removeTagRule(row.id)}>Remove</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <button className="ghost" onClick={() => setTagRules(current => [...current, newTagRule()])}>+ Add tag rule</button>
            </Card>
          )}

          {!binaryOnly && writeMetadataJson && (
            <Card title="Metadata JSON rules" subtitle="Choose manifest columns to include in the JSON sidecar stored alongside binaries.">
              <div className="tableWrap">
                <table>
                  <thead>
                    <tr>
                      <th>Manifest field</th>
                      <th>JSON property/path</th>
                      <th>Transform</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {metadataRules.map(row => (
                      <tr key={row.id}>
                        <td>
                          <select value={row.sourceField} onChange={(e) => updateMetadataRule(row.id, { sourceField: e.target.value })}>
                            <option value="">Select source</option>
                            {preview?.columns.map(column => <option key={column} value={column}>{column}</option>)}
                          </select>
                        </td>
                        <td><input value={row.jsonPath} onChange={(e) => updateMetadataRule(row.id, { jsonPath: e.target.value })} /></td>
                        <td>
                          <select value={row.transform} onChange={(e) => updateMetadataRule(row.id, { transform: e.target.value })}>
                            {transforms.map(transform => <option key={transform || "none"} value={transform}>{transform || "none"}</option>)}
                          </select>
                        </td>
                        <td><button className="ghost" onClick={() => removeMetadataRule(row.id)}>Remove</button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <button className="ghost" onClick={() => setMetadataRules(current => [...current, newMetadataRule()])}>+ Add metadata rule</button>
            </Card>
          )}
        </>
      )}

      {mappingType === "target" && (
        <Card title="Field mappings" subtitle="Map source manifest columns to target taxonomy fields and mark required target fields.">
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
      )}

      <Card title="Generated mapping JSON">
        <JsonBlock value={mappingProfile} />
        <label className="check">
          <input type="checkbox" checked={bindToProject} onChange={(e) => setBindToProject(e.target.checked)} />
          Bind saved mapping to selected project
        </label>
        <button className="primary" disabled={saving || !canSave} onClick={saveMapping}>
          {saving ? "Saving…" : "Save Mapping Artifact"}
        </button>
      </Card>

      {createdArtifact && (
        <Card title="Created mapping artifact">
          <JsonBlock value={createdArtifact} />
        </Card>
      )}
    </div>
  );
}
