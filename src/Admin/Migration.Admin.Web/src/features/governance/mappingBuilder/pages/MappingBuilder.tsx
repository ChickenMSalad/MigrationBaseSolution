import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "../../../../api/core/adminApiClient";

import { Card, JsonBlock } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";

type ArtifactKind =
  | "Manifest"
  | "Mapping"
  | "Taxonomy"
  | "Binary"
  | "Report"
  | "Other"
  | string
  | number;

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
  "empty-to-null",
];

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    return apiRequest<T>(path, {
        ...init,
        headers: init?.body instanceof FormData
            ? init.headers
            : {
                "Content-Type": "application/json",
                ...(init?.headers ?? {})
            }
    });
}

function artifactKind(a: ArtifactRecord): string {
  const raw = a.kind ?? a.artifactType ?? "";
  const value = String(raw);
  if (value === "0") return "Manifest";
  if (value === "1") return "Mapping";
  if (value === "2") return "Binary";
  if (value === "3") return "Report";
  if (value === "5") return "Taxonomy";
  return value;
}

function isKind(a: ArtifactRecord, kind: string): boolean {
  return artifactKind(a).toLowerCase() === kind.toLowerCase();
}

function normalizeColumnName(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[_\-./\\]+/g, " ")
    .replace(/[()\[\]{}]/g, " ")
    .replace(/[^a-z0-9 ]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function compactColumnName(value: string): string {
  return normalizeColumnName(value).replace(/\s+/g, "");
}

function columnTokens(value: string): string[] {
  return normalizeColumnName(value)
    .split(" ")
    .filter(
      (token) =>
        token.length > 1 && !["the", "and", "for", "with"].includes(token),
    );
}

function bestTargetColumn(source: string, targetColumns: string[]): string {
  if (!source.trim() || targetColumns.length === 0) return "";

  const sourceNormalized = normalizeColumnName(source);
  const sourceCompact = compactColumnName(source);
  const sourceTokens = columnTokens(source);

  const exact = targetColumns.find(
    (target) => normalizeColumnName(target) === sourceNormalized,
  );
  if (exact) return exact;

  const compactExact = targetColumns.find(
    (target) => compactColumnName(target) === sourceCompact,
  );
  if (compactExact) return compactExact;

  const sourceSynonyms: Record<string, string[]> = {
    id: ["id", "asset id", "assetid", "bynder id", "identifier"],
    filename: [
      "filename",
      "file name",
      "name",
      "original filename",
      "original file name",
      "asset name",
    ],
    collection: ["collection", "collections", "folder", "folder path", "path"],
    tags: ["tags", "tag", "keywords", "keyword"],
  };

  for (const [targetName, synonyms] of Object.entries(sourceSynonyms)) {
    if (
      synonyms.some(
        (synonym) =>
          sourceNormalized === normalizeColumnName(synonym) ||
          sourceCompact === compactColumnName(synonym),
      )
    ) {
      const synonymMatch = targetColumns.find(
        (target) =>
          normalizeColumnName(target) === targetName ||
          compactColumnName(target) === compactColumnName(targetName),
      );
      if (synonymMatch) return synonymMatch;
    }
  }

  let best = "";
  let bestScore = 0;
  for (const target of targetColumns) {
    const targetTokens = columnTokens(target);
    if (targetTokens.length === 0 || sourceTokens.length === 0) continue;
    const overlap = sourceTokens.filter((token) =>
      targetTokens.includes(token),
    ).length;
    const score = overlap / Math.max(sourceTokens.length, targetTokens.length);
    if (score > bestScore) {
      bestScore = score;
      best = target;
    }
  }

  return bestScore >= 0.6 ? best : "";
}

function buildRowsFromColumns(
  sourceColumns: string[],
  targetColumns: string[],
): MappingRow[] {
  if (targetColumns.length === 0) {
    return sourceColumns.map((source) =>
      newRow(source, defaultTargetName(source)),
    );
  }

  return targetColumns.map((target) => {
    const source = bestTargetColumn(target, sourceColumns);
    return newRow(source, target);
  });
}

function CompactColumnPreview({
  preview,
  title,
  subtitle,
}: {
  preview: ManifestPreview;
  title: string;
  subtitle?: string;
}) {
  return (
    <Card
      title={title}
      subtitle={
        subtitle ??
        `${preview.fileName} — ${preview.columns.length} columns detected`
      }
    >
      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          gap: "0.35rem",
          maxHeight: "10rem",
          overflow: "auto",
        }}
      >
        {preview.columns.map((column) => (
          <span
            className="pill"
            key={column}
            style={{
              fontSize: "0.72rem",
              lineHeight: 1.2,
              padding: "0.2rem 0.45rem",
            }}
            title={column}
          >
            {column}
          </span>
        ))}
      </div>
      {preview.sampleRows.length > 0 && (
        <JsonBlock value={preview.sampleRows.slice(0, 2)} />
      )}
    </Card>
  );
}

function newRow(source = "", target = ""): MappingRow {
  return {
    id: crypto.randomUUID(),
    source,
    target,
    transform: "",
    required: false,
  };
}

function newTagRule(sourceField = "", tagName = ""): TagRule {
  return {
    id: crypto.randomUUID(),
    sourceField,
    tagName,
    transform: "trim",
  };
}

function newMetadataRule(sourceField = "", jsonPath = ""): MetadataRule {
  return {
    id: crypto.randomUUID(),
    sourceField,
    jsonPath,
    transform: "trim",
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
  const fileName = columns.find((c) => /file\s*name|filename|name/i.test(c));
  const assetId = columns.find((c) => /asset\s*id|assetid|id/i.test(c));
  if (fileName) return `{${fileName}}`;
  if (assetId) return `{${assetId}}`;
  return "{assetId}";
}

function normalizeConnectorType(value: string): string {
  return value.replace(/[^a-z0-9]/gi, "").toLowerCase();
}

function intermediateProviderForTarget(
  targetType: string,
): "AzureBlob" | "S3" | "LocalStorage" | "Unknown" {
  const normalized = normalizeConnectorType(targetType);
  if (
    ["azure", "azureblob", "blob", "blobstorage", "azureblobstorage"].includes(
      normalized,
    )
  )
    return "AzureBlob";
  if (["s3", "amazons3", "awss3"].includes(normalized)) return "S3";
  if (
    ["local", "localstorage", "filesystem", "file", "files"].includes(
      normalized,
    )
  )
    return "LocalStorage";
  return "Unknown";
}

export function MappingBuilder() {
  const [projects, setProjects] = useState<ProjectRecord[]>([]);
  const [, setManifestArtifacts] = useState<ArtifactRecord[]>([]);
  const [taxonomyArtifacts, setTaxonomyArtifacts] = useState<ArtifactRecord[]>(
    [],
  );
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [selectedManifestArtifactId, setSelectedManifestArtifactId] =
    useState("");
  const [selectedTargetArtifactId, setSelectedTargetArtifactId] = useState("");
  const [mappingType, setMappingType] = useState<MappingType>("intermediate");
  const [preview, setPreview] = useState<ManifestPreview | null>(null);
  const [targetPreview, setTargetPreview] = useState<ManifestPreview | null>(
    null,
  );
  const [profileName, setProfileName] = useState("generated-mapping-profile");
  const [sourceType, setSourceType] = useState("LocalStorage");
  const [targetType, setTargetType] = useState("LocalStorage");
  const [fileName, setFileName] = useState("generated-mapping-profile.json");
  const [rows, setRows] = useState<MappingRow[]>([newRow()]);
  const [blobNameTemplate, setBlobNameTemplate] = useState("{assetId}");
  const [metadataJsonPathTemplate, setMetadataJsonPathTemplate] = useState(
    "metadata/{assetId}.json",
  );
  const [mapToFolderPathColumn, setMapToFolderPathColumn] = useState(false);
  const [folderPathColumn, setFolderPathColumn] = useState("");
  const [binaryOnly, setBinaryOnly] = useState(false);
  const [writeMetadataJson, setWriteMetadataJson] = useState(true);
  const [writeBlobTags, setWriteBlobTags] = useState(true);
  const [tagRules, setTagRules] = useState<TagRule[]>([newTagRule()]);
  const [metadataRules, setMetadataRules] = useState<MetadataRule[]>([
    newMetadataRule(),
  ]);
  const [localOutputPath, setLocalOutputPath] = useState("");
  const [sharePointRcloneFolderPath, setSharePointRcloneFolderPath] =
    useState("");
  const [sharePointTargetRcloneRemoteName, setSharePointTargetRcloneRemoteName] =
    useState("");
  const [s3ObjectKeyTemplate, setS3ObjectKeyTemplate] = useState(
    "{sourceRelativePath}",
  );
  const [bindToProject, setBindToProject] = useState(true);
  const [createdArtifact, setCreatedArtifact] = useState<ArtifactRecord | null>(
    null,
  );
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const selectedProject = useMemo(
    () => projects.find((x) => x.projectId === selectedProjectId),
    [projects, selectedProjectId],
  );

  const targetColumns = targetPreview?.columns ?? [];
  const hasProjectManifest = Boolean(selectedManifestArtifactId);
  const hasSelectedProject = Boolean(selectedProject);
  const intermediateProvider = intermediateProviderForTarget(targetType);
  const isSharePointSource =
    normalizeConnectorType(sourceType) === "sharepoint";
  const showIntermediateStorage =
    mappingType === "intermediate" && hasSelectedProject;
  const showSharePointRcloneIntermediate =
    showIntermediateStorage &&
    isSharePointSource &&
    ["AzureBlob", "S3", "LocalStorage"].includes(intermediateProvider);
  const showAzureIntermediate =
    showIntermediateStorage &&
    intermediateProvider === "AzureBlob" &&
    !showSharePointRcloneIntermediate;
  const showS3Intermediate =
    showIntermediateStorage &&
    intermediateProvider === "S3" &&
    !showSharePointRcloneIntermediate;
  const showLocalStorageIntermediate =
    showIntermediateStorage &&
    intermediateProvider === "LocalStorage" &&
    !showSharePointRcloneIntermediate;
  const showBlobTagRules =
    showAzureIntermediate && !binaryOnly && writeBlobTags;
  const showMetadataJsonRules =
    showAzureIntermediate && !binaryOnly && writeMetadataJson;

  const manifestMissingMessage = selectedProjectId
    ? "Selected project is missing a manifest artifact. Bind a manifest to the project before loading columns."
    : "Select a project with a manifest artifact to load manifest columns.";

  const mappingProfile = useMemo<any>(() => {
    const cleanRows = rows
      .filter((r) => r.source.trim() && r.target.trim())
      .map((r) => ({
        source: r.source.trim(),
        target: r.target.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {}),
      }));

    const cleanTagRules = tagRules
      .filter((r) => r.sourceField.trim() && r.tagName.trim())
      .map((r) => ({
        sourceField: r.sourceField.trim(),
        tagName: r.tagName.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {}),
      }));

    const cleanMetadataRules = metadataRules
      .filter((r) => r.sourceField.trim() && r.jsonPath.trim())
      .map((r) => ({
        sourceField: r.sourceField.trim(),
        jsonPath: r.jsonPath.trim(),
        ...(r.transform.trim() ? { transform: r.transform.trim() } : {}),
      }));

    if (mappingType === "intermediate") {
      let intermediateStorage: Record<string, unknown> | null = null;

      if (hasSelectedProject && showSharePointRcloneIntermediate) {
        intermediateStorage = {
          provider: intermediateProvider,
          binaryOnly: true,
          blobNameTemplate: "{sourceRelativePath}",
          objectKeyTemplate: "{sourceRelativePath}",
          outputPath: sharePointRcloneFolderPath,
          folderPath: sharePointRcloneFolderPath,
          destinationPath: sharePointRcloneFolderPath,
          targetRcloneRemoteName:
            intermediateProvider === "LocalStorage"
              ? null
              : sharePointTargetRcloneRemoteName,
          preserveSourceFolderPath: true,
          writeBlobTags: false,
          writeMetadataJson: false,
          tagRules: [],
          metadataRules: [],
        };
      } else if (hasSelectedProject && intermediateProvider === "AzureBlob") {
        intermediateStorage = {
          provider: "AzureBlob",
          binaryOnly,
          blobNameTemplate,
          mapToFolderPathColumn,
          folderPathMode: mapToFolderPathColumn ? "manifestColumn" : "none",
          folderPathColumn: mapToFolderPathColumn
            ? folderPathColumn || null
            : null,
          folderPathSource: mapToFolderPathColumn
            ? folderPathColumn || null
            : null,
          writeBlobTags: !binaryOnly && writeBlobTags,
          writeMetadataJson: !binaryOnly && writeMetadataJson,
          metadataJsonPathTemplate:
            !binaryOnly && writeMetadataJson ? metadataJsonPathTemplate : null,
          preserveSourceFolderPath: mapToFolderPathColumn,
          sourceFolderPathField: mapToFolderPathColumn
            ? folderPathColumn
            : null,
          tagRules: binaryOnly ? [] : cleanTagRules,
          metadataRules: binaryOnly ? [] : cleanMetadataRules,
        };
      } else if (hasSelectedProject && intermediateProvider === "S3") {
        intermediateStorage = {
          provider: "S3",
          binaryOnly: true,
          objectKeyTemplate: s3ObjectKeyTemplate,
          preserveSourceFolderPath: true,
          tagRules: [],
          metadataRules: [],
        };
      } else if (
        hasSelectedProject &&
        intermediateProvider === "LocalStorage"
      ) {
        intermediateStorage = {
          provider: "LocalStorage",
          binaryOnly: true,
          outputPath: localOutputPath,
          preserveSourceFolderPath: true,
          tagRules: [],
          metadataRules: [],
        };
      }

      return {
        profileName,
        sourceType,
        targetType,
        mappingType,
        manifestArtifactId: selectedManifestArtifactId || null,
        intermediateStorage,
        fieldMappings: [],
        requiredTargetFields: [],
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
        .filter((r) => r.required && r.target.trim())
        .map((r) => r.target.trim()),
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
    metadataJsonPathTemplate,
    hasSelectedProject,
    intermediateProvider,
    localOutputPath,
    sharePointRcloneFolderPath,
    showSharePointRcloneIntermediate,
    s3ObjectKeyTemplate,
  ]);

  async function loadInitial() {
    setLoading(true);
    setError(null);

    try {
      const [projectData, artifactData] = await Promise.all([
        request<ProjectRecord[]>("/api/projects"),
        request<ArtifactRecord[]>("/api/artifacts"),
      ]);

      const artifacts = artifactData ?? [];
      setProjects(projectData ?? []);
      setManifestArtifacts(
        artifacts.filter((x) => isKind(x, "Manifest") || !artifactKind(x)),
      );
      setTaxonomyArtifacts(
        artifacts.filter((x) => isKind(x, "Taxonomy") || isKind(x, "Manifest")),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadInitial();
  }, []);

  useEffect(() => {
    if (!selectedProject) {
      setSelectedManifestArtifactId("");
      setSelectedTargetArtifactId("");
      setPreview(null);
      setTargetPreview(null);
      return;
    }

    setSourceType(selectedProject.sourceType || sourceType);
    setTargetType(selectedProject.targetType || targetType);
    setSelectedManifestArtifactId(selectedProject.manifestArtifactId ?? "");
    setSelectedTargetArtifactId(selectedProject.taxonomyArtifactId ?? "");
    setPreview(null);
    setTargetPreview(null);

    const suffix =
      mappingType === "intermediate"
        ? "intermediate-storage-mapping"
        : "target-mapping";
    const safeName =
      `${selectedProject.displayName || selectedProject.projectId}-${suffix}`
        .replace(/\s+/g, "-")
        .replace(/[^a-zA-Z0-9_.-]/g, "")
        .toLowerCase();

    if (safeName) {
      setProfileName(safeName);
      setFileName(`${safeName}.json`);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedProjectId, mappingType]);

  useEffect(() => {
    if (mappingType !== "target" || !selectedTargetArtifactId) {
      setTargetPreview(null);
      return;
    }

    void loadTargetPreview(selectedTargetArtifactId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mappingType, selectedTargetArtifactId]);

  async function loadPreview() {
    if (!selectedManifestArtifactId) return;
    setError(null);

    try {
      const data = await request<ManifestPreview>(
        `/api/mapping-builder/manifests/${encodeURIComponent(selectedManifestArtifactId)}/columns`,
      );
      setPreview(data);

      if (data.columns.length > 0) {
        setBlobNameTemplate(defaultBlobNameTemplate(data.columns));
        setRows(
          mappingType === "target"
            ? buildRowsFromColumns(data.columns, targetPreview?.columns ?? [])
            : data.columns.map((c) => newRow(c, defaultTargetName(c))),
        );
        setTagRules(
          data.columns
            .slice(0, 5)
            .map((c) => newTagRule(c, defaultTargetName(c))),
        );
        setMetadataRules(
          data.columns.map((c) => newMetadataRule(c, defaultTargetName(c))),
        );

        if (
          mapToFolderPathColumn &&
          folderPathColumn &&
          !data.columns.includes(folderPathColumn)
        ) {
          setFolderPathColumn("");
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  }

  async function loadTargetPreview(artifactId = selectedTargetArtifactId) {
    if (!artifactId) {
      setTargetPreview(null);
      return;
    }

    setError(null);

    try {
      const data = await request<ManifestPreview>(
        `/api/mapping-builder/manifests/${encodeURIComponent(artifactId)}/columns`,
      );
      setTargetPreview(data);
      setRows(
        preview?.columns.length
          ? buildRowsFromColumns(preview.columns, data.columns)
          : data.columns.map((target) => newRow("", target)),
      );
    } catch (err) {
      setTargetPreview(null);
      setError(err instanceof Error ? err.message : String(err));
    }
  }

  function updateRow(id: string, patch: Partial<MappingRow>) {
    setRows((current) =>
      current.map((row) => (row.id === id ? { ...row, ...patch } : row)),
    );
  }

  function updateTagRule(id: string, patch: Partial<TagRule>) {
    setTagRules((current) =>
      current.map((row) => (row.id === id ? { ...row, ...patch } : row)),
    );
  }

  function updateMetadataRule(id: string, patch: Partial<MetadataRule>) {
    setMetadataRules((current) =>
      current.map((row) => (row.id === id ? { ...row, ...patch } : row)),
    );
  }

  function removeRow(id: string) {
    setRows((current) =>
      current.length <= 1 ? current : current.filter((row) => row.id !== id),
    );
  }

  function removeTagRule(id: string) {
    setTagRules((current) =>
      current.length <= 1 ? current : current.filter((row) => row.id !== id),
    );
  }

  function removeMetadataRule(id: string) {
    setMetadataRules((current) =>
      current.length <= 1 ? current : current.filter((row) => row.id !== id),
    );
  }

  async function saveMapping() {
    setSaving(true);
    setError(null);
    setCreatedArtifact(null);

    try {
      const response = await request<SaveMappingResponse>(
        "/api/mapping-builder/mappings",
        {
          method: "POST",
          body: JSON.stringify({
            profileName,
            sourceType,
            targetType,
            mappingType,
            fileName,
            manifestArtifactId: selectedManifestArtifactId || null,
            targetArtifactId:
              mappingType === "target"
                ? selectedTargetArtifactId || null
                : null,
            projectId: selectedProjectId || null,
            fieldMappings:
              mappingType === "target" ? mappingProfile.fieldMappings : [],
            requiredTargetFields:
              mappingType === "target"
                ? mappingProfile.requiredTargetFields
                : [],
            intermediateStorage:
              mappingType === "intermediate"
                ? mappingProfile.intermediateStorage
                : null,
          }),
        },
      );

      setCreatedArtifact(response.artifact);

      if (bindToProject && selectedProjectId) {
        await request(
          `/api/projects/${encodeURIComponent(selectedProjectId)}/artifacts`,
          {
            method: "PUT",
            body: JSON.stringify({
              manifestArtifactId: selectedManifestArtifactId || null,
              mappingArtifactId: response.artifact.artifactId,
              taxonomyArtifactId:
                mappingType === "target"
                  ? selectedTargetArtifactId || null
                  : undefined,
            }),
          },
        );
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  }

  const canSave =
    mappingType === "intermediate"
      ? Boolean(
          hasSelectedProject &&
          profileName.trim() &&
          hasProjectManifest &&
          ((showSharePointRcloneIntermediate &&
            sharePointRcloneFolderPath.trim() &&
            (intermediateProvider === "LocalStorage" ||
              sharePointTargetRcloneRemoteName.trim())) ||
            (intermediateProvider === "AzureBlob" &&
              blobNameTemplate.trim() &&
              (!mapToFolderPathColumn || folderPathColumn.trim()) &&
              (binaryOnly ||
                tagRules.some(
                  (r) => r.sourceField.trim() && r.tagName.trim(),
                ) ||
                metadataRules.some(
                  (r) => r.sourceField.trim() && r.jsonPath.trim(),
                ))) ||
            (intermediateProvider === "S3" && s3ObjectKeyTemplate.trim()) ||
            (intermediateProvider === "LocalStorage" &&
              localOutputPath.trim())),
        )
      : mappingProfile.fieldMappings.length > 0 &&
        Boolean(selectedTargetArtifactId);

  return (
    <div className="pageStack mappingBuilder">
      <div className="pageTitle">
        <div>
          <h1>Mapping Builder</h1>
          <p>
            Build an intermediate storage mapping for Azure Blob staging, or a
            target mapping profile for final target systems.
          </p>
        </div>
      </div>

      <LoadingError loading={loading} error={error} />

      <Card
        title="Setup"
        subtitle="Choose whether this mapping describes intermediate storage behavior or final target field mapping."
      >
        <div className="formGrid wide">
          <label>
            Mapping type
            <select
              value={mappingType}
              onChange={(e) => setMappingType(e.target.value as MappingType)}
            >
              <option value="intermediate">Intermediate storage mapping</option>
              <option value="target">Target system mapping</option>
            </select>
          </label>

          <label>
            Project
            <select
              value={selectedProjectId}
              onChange={(e) => setSelectedProjectId(e.target.value)}
            >
              <option value="">No project selected</option>
              {projects.map((project) => (
                <option key={project.projectId} value={project.projectId}>
                  {project.displayName} ({project.sourceType} →{" "}
                  {project.targetType})
                </option>
              ))}
            </select>
          </label>

          {mappingType === "target" && (
            <label>
              Target taxonomy/manifest artifact
              <select
                value={selectedTargetArtifactId}
                onChange={(e) => setSelectedTargetArtifactId(e.target.value)}
              >
                <option value="">Select target artifact</option>
                {taxonomyArtifacts.map((artifact) => (
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
          <p className="muted">
            Using the manifest artifact already bound to{" "}
            {selectedProject.displayName}.
          </p>
        )}

        {mappingType === "target" &&
          selectedTargetArtifactId &&
          !targetPreview && (
            <p className="muted">
              Target taxonomy/manifest selected. Its columns will load
              automatically, or you can reload them after changing the artifact.
            </p>
          )}

        <button
          className="primary"
          disabled={!hasProjectManifest}
          onClick={loadPreview}
        >
          {mappingType === "target"
            ? "Load Source Manifest Columns"
            : "Load Manifest Columns"}
        </button>
      </Card>

      {preview && (
        <CompactColumnPreview
          preview={preview}
          title="Source manifest preview"
          subtitle={`${preview.fileName} — ${preview.columns.length} source columns detected`}
        />
      )}

      {mappingType === "target" && targetPreview && (
        <CompactColumnPreview
          preview={targetPreview}
          title="Target taxonomy/manifest preview"
          subtitle={`${targetPreview.fileName} — ${targetPreview.columns.length} target columns detected`}
        />
      )}

      <Card
        title="Mapping profile"
        subtitle="The saved JSON stays a Mapping artifact. Intermediate profiles add storage behavior for migration runs."
      >
        <div className="formGrid wide">
          <label>
            Profile name
            <input
              value={profileName}
              onChange={(e) => setProfileName(e.target.value)}
            />
          </label>
          <label>
            File name
            <input
              value={fileName}
              onChange={(e) => setFileName(e.target.value)}
            />
          </label>
          <label>
            Source type
            <input
              value={sourceType}
              onChange={(e) => setSourceType(e.target.value)}
            />
          </label>
          <label>
            Target type
            <input
              value={targetType}
              onChange={(e) => setTargetType(e.target.value)}
            />
          </label>
        </div>
      </Card>

      {showSharePointRcloneIntermediate && (
        <Card
          title="Intermediate storage behavior"
          subtitle="Define the destination folder/path for the rclone direct copy. Binaries keep the SharePoint folder structure."
        >
          <div className="formGrid wide">
            {intermediateProvider !== "LocalStorage" && (
              <label>
                Target rclone remote
                <input
                  value={sharePointTargetRcloneRemoteName}
                  onChange={(e) =>
                    setSharePointTargetRcloneRemoteName(e.target.value)
                  }
                  placeholder={
                    intermediateProvider === "AzureBlob"
                      ? "az-placecats"
                      : "s3-placecats"
                  }
                />
              </label>
            )}
            <label>
              Folder path
              <input
                value={sharePointRcloneFolderPath}
                onChange={(e) => setSharePointRcloneFolderPath(e.target.value)}
                placeholder={
                  intermediateProvider === "LocalStorage"
                    ? "C:\\Exports\\Migration"
                    : "exports/placecats"
                }
              />
            </label>
          </div>

          <label className="check">
            <input type="checkbox" checked readOnly />
            Binary only; preserve source folder structure
          </label>
        </Card>
      )}

      {showAzureIntermediate && (
        <>
          <Card
            title="Intermediate storage behavior"
            subtitle="Define how the manifest should become Azure Blob objects, tags, and optional metadata JSON."
          >
            <div className="formGrid wide">
              <label>
                Blob name template
                <input
                  value={blobNameTemplate}
                  onChange={(e) => setBlobNameTemplate(e.target.value)}
                  placeholder="assets/{Asset ID}/{Filename}"
                />
              </label>

              <label>
                Metadata JSON path template
                <input
                  disabled={binaryOnly || !writeMetadataJson}
                  value={metadataJsonPathTemplate}
                  onChange={(e) => setMetadataJsonPathTemplate(e.target.value)}
                  placeholder="metadata/{assetId}.json"
                />
              </label>

              <label>
                Folder path column
                <select
                  disabled={!mapToFolderPathColumn || !preview}
                  value={folderPathColumn}
                  onChange={(e) => setFolderPathColumn(e.target.value)}
                >
                  <option value="">Select manifest column</option>
                  {preview?.columns.map((column) => (
                    <option key={column} value={column}>
                      {column}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <label className="check">
              <input
                type="checkbox"
                checked={binaryOnly}
                onChange={(e) => setBinaryOnly(e.target.checked)}
              />
              Binary only; do not write blob tags or metadata JSON
            </label>

            <label className="check">
              <input
                type="checkbox"
                checked={mapToFolderPathColumn}
                onChange={(e) => setMapToFolderPathColumn(e.target.checked)}
              />
              Map to Folder Path Column
            </label>

            <label className="check">
              <input
                type="checkbox"
                disabled={binaryOnly}
                checked={writeBlobTags}
                onChange={(e) => setWriteBlobTags(e.target.checked)}
              />
              Store selected manifest values as Azure Blob index tags
            </label>

            <label className="check">
              <input
                type="checkbox"
                disabled={binaryOnly}
                checked={writeMetadataJson}
                onChange={(e) => setWriteMetadataJson(e.target.checked)}
              />
              Write selected manifest values to a metadata JSON sidecar
            </label>
          </Card>

          {showBlobTagRules && (
            <Card
              title="Blob tag rules"
              subtitle="Choose manifest columns that should become Azure Blob index tags. Use tags for searchable lookup fields instead of path prefixes."
            >
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
                    {tagRules.map((row) => (
                      <tr key={row.id}>
                        <td>
                          <select
                            value={row.sourceField}
                            onChange={(e) =>
                              updateTagRule(row.id, {
                                sourceField: e.target.value,
                              })
                            }
                          >
                            <option value="">-- select source --</option>
                            {preview?.columns.map((column) => (
                              <option key={column} value={column}>
                                {column}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <input
                            value={row.tagName}
                            onChange={(e) =>
                              updateTagRule(row.id, { tagName: e.target.value })
                            }
                          />
                        </td>
                        <td>
                          <select
                            value={row.transform}
                            onChange={(e) =>
                              updateTagRule(row.id, {
                                transform: e.target.value,
                              })
                            }
                          >
                            {transforms.map((transform) => (
                              <option
                                key={transform || "none"}
                                value={transform}
                              >
                                {transform || "none"}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <button
                            className="ghost"
                            onClick={() => removeTagRule(row.id)}
                          >
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <button
                className="ghost"
                onClick={() =>
                  setTagRules((current) => [...current, newTagRule()])
                }
              >
                + Add tag rule
              </button>
            </Card>
          )}

          {showMetadataJsonRules && (
            <Card
              title="Metadata JSON rules"
              subtitle="Choose manifest columns to include in the JSON sidecar stored alongside binaries."
            >
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
                    {metadataRules.map((row) => (
                      <tr key={row.id}>
                        <td>
                          <select
                            value={row.sourceField}
                            onChange={(e) =>
                              updateMetadataRule(row.id, {
                                sourceField: e.target.value,
                              })
                            }
                          >
                            <option value="">-- select source --</option>
                            {preview?.columns.map((column) => (
                              <option key={column} value={column}>
                                {column}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <input
                            value={row.jsonPath}
                            onChange={(e) =>
                              updateMetadataRule(row.id, {
                                jsonPath: e.target.value,
                              })
                            }
                          />
                        </td>
                        <td>
                          <select
                            value={row.transform}
                            onChange={(e) =>
                              updateMetadataRule(row.id, {
                                transform: e.target.value,
                              })
                            }
                          >
                            {transforms.map((transform) => (
                              <option
                                key={transform || "none"}
                                value={transform}
                              >
                                {transform || "none"}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <button
                            className="ghost"
                            onClick={() => removeMetadataRule(row.id)}
                          >
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <button
                className="ghost"
                onClick={() =>
                  setMetadataRules((current) => [...current, newMetadataRule()])
                }
              >
                + Add metadata rule
              </button>
            </Card>
          )}
        </>
      )}

      {showS3Intermediate && (
        <Card
          title="Intermediate storage behavior"
          subtitle="Define how binaries should be written to S3 intermediate storage."
        >
          <div className="formGrid wide">
            <label>
              S3 object key template
              <input
                value={s3ObjectKeyTemplate}
                onChange={(e) => setS3ObjectKeyTemplate(e.target.value)}
                placeholder="{sourceRelativePath}"
              />
            </label>
          </div>

          <label className="check">
            <input type="checkbox" checked readOnly />
            Binary only; preserve source folder structure
          </label>
        </Card>
      )}

      {showLocalStorageIntermediate && (
        <Card
          title="Intermediate storage behavior"
          subtitle="Define where binaries should be copied on local storage."
        >
          <div className="formGrid wide">
            <label>
              Output path
              <input
                value={localOutputPath}
                onChange={(e) => setLocalOutputPath(e.target.value)}
                placeholder="C:\\Exports\\Migration"
              />
            </label>
          </div>

          <label className="check">
            <input type="checkbox" checked readOnly />
            Binary only; preserve source folder structure
          </label>
        </Card>
      )}

      {mappingType === "target" && (
        <Card
          title="Field mappings"
          subtitle="Map target taxonomy/manifest columns to source manifest columns. Auto-map lists every target field and selects the best source field when a reasonable match exists."
          action={
            preview && targetPreview ? (
              <button
                className="ghost"
                onClick={() =>
                  setRows(
                    buildRowsFromColumns(
                      preview.columns,
                      targetPreview.columns,
                    ),
                  )
                }
              >
                Auto-map fields
              </button>
            ) : undefined
          }
        >
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
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <select
                        value={row.source}
                        onChange={(e) =>
                          updateRow(row.id, { source: e.target.value })
                        }
                      >
                        <option value="">-- select source --</option>
                        {preview?.columns.map((column) => (
                          <option key={column} value={column}>
                            {column}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      {targetColumns.length > 0 ? (
                        <select
                          value={row.target}
                          onChange={(e) =>
                            updateRow(row.id, { target: e.target.value })
                          }
                        >
                          <option value="">-- select target --</option>
                          {targetColumns.map((column) => (
                            <option key={column} value={column}>
                              {column}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <input
                          value={row.target}
                          onChange={(e) =>
                            updateRow(row.id, { target: e.target.value })
                          }
                          placeholder="Load a target taxonomy/manifest to select fields"
                        />
                      )}
                    </td>
                    <td>
                      <select
                        value={row.transform}
                        onChange={(e) =>
                          updateRow(row.id, { transform: e.target.value })
                        }
                      >
                        {transforms.map((transform) => (
                          <option key={transform || "none"} value={transform}>
                            {transform || "none"}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        type="checkbox"
                        checked={row.required}
                        onChange={(e) =>
                          updateRow(row.id, { required: e.target.checked })
                        }
                      />
                    </td>
                    <td>
                      <button
                        className="ghost"
                        onClick={() => removeRow(row.id)}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button
            className="ghost"
            onClick={() => setRows((current) => [...current, newRow()])}
          >
            + Add mapping row
          </button>
        </Card>
      )}

      <Card title="Generated mapping JSON">
        <JsonBlock value={mappingProfile} />
        <label className="check">
          <input
            type="checkbox"
            checked={bindToProject}
            onChange={(e) => setBindToProject(e.target.checked)}
          />
          Bind saved mapping to selected project
        </label>
        <button
          className="primary"
          disabled={saving || !canSave}
          onClick={saveMapping}
        >
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
