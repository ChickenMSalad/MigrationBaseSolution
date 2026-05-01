import { useEffect, useState } from "react";
import { api } from "../api/client";
import { Card, EmptyState } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord } from "../types/api";

const artifactKindNames: Record<string, string> = {
  "0": "Other",
  "1": "Manifest",
  "2": "Mapping",
  "3": "Taxonomy",
  "4": "Staging",
  other: "Other",
  manifest: "Manifest",
  mapping: "Mapping",
  taxonomy: "Taxonomy",
  staging: "Staging"
};

function displayArtifactKind(artifact: ArtifactRecord) {
  const raw = artifact.kind ?? artifact.artifactType ?? "Other";
  return artifactKindNames[String(raw).toLowerCase()] ?? String(raw);
}

function artifactUploaded(artifact: ArtifactRecord) {
  return artifact.createdUtc ?? artifact.uploadedUtc ?? null;
}

function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleString() : "Unknown";
}

export function Artifacts() {
  const [artifacts, setArtifacts] = useState<ArtifactRecord[]>([]);
  const [artifactType, setArtifactType] = useState("Manifest");
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadArtifacts() {
    setLoading(true);
    setError(null);

    try {
      setArtifacts(await api.artifacts());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadArtifacts();
  }, []);

  async function uploadArtifact() {
    if (!file) {
      return;
    }

    setUploading(true);
    setError(null);
    setMessage(null);

    try {
      await api.uploadArtifact(file, { kind: artifactType });
      setMessage("Artifact uploaded.");
      setFile(null);
      await loadArtifacts();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setUploading(false);
    }
  }

  async function deleteArtifact(artifactId: string) {
    const confirmed = window.confirm("Delete this artifact? This cannot be undone.");

    if (!confirmed) {
      return;
    }

    setDeletingId(artifactId);
    setError(null);
    setMessage(null);

    try {
      await api.deleteArtifact(artifactId);
      setMessage("Artifact deleted.");
      await loadArtifacts();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="pageStack">
      <div className="pageHeader">
        <div>
          <h1>Artifacts</h1>
          <p className="muted">Upload, download, and manage artifacts for migration projects.</p>
        </div>
      </div>

      {error && <LoadingError message={error} />}
      {message && <div className="successBanner">{message}</div>}

      <Card title="Upload Artifact">
        <div className="formGrid wide">
          <label>
            Type
            <select value={artifactType} onChange={event => setArtifactType(event.target.value)}>
              <option value="Manifest">Manifest</option>
              <option value="Mapping">Mapping</option>
              <option value="Taxonomy">Taxonomy</option>
              <option value="Other">Other</option>
            </select>
          </label>

          <label>
            File
            <input type="file" onChange={event => setFile(event.target.files?.[0] ?? null)} />
          </label>

          <div className="buttonRow">
            <button
              type="button"
              className="primaryButton"
              onClick={() => void uploadArtifact()}
              disabled={!file || uploading}
            >
              {uploading ? "Uploading…" : "Upload"}
            </button>
          </div>
        </div>
      </Card>

      <Card title="Stored Artifacts">
        {loading ? (
          <p className="muted">Loading artifacts…</p>
        ) : artifacts.length === 0 ? (
          <EmptyState title="No artifacts uploaded yet" />
        ) : (
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Type</th>
                  <th>File</th>
                  <th>Uploaded</th>
                  <th>Artifact ID</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {artifacts.map(artifact => (
                  <tr key={artifact.artifactId}>
                    <td>{displayArtifactKind(artifact)}</td>
                    <td>{artifact.fileName}</td>
                    <td>{formatDate(artifactUploaded(artifact))}</td>
                    <td><small>{artifact.artifactId}</small></td>
                    <td>
                      <div className="inlineActions">
                        <a href={api.artifactDownloadUrl(artifact.artifactId)}>Download</a>
                        <button
                          type="button"
                          className="dangerButton"
                          onClick={() => void deleteArtifact(artifact.artifactId)}
                          disabled={deletingId === artifact.artifactId}
                        >
                          {deletingId === artifact.artifactId ? "Deleting…" : "Delete"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}