import { useEffect, useState } from "react";
import { api } from "../api/client";
import { Card, EmptyState } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ArtifactRecord } from "../types/api";

function artifactType(artifact: ArtifactRecord) {
  return artifact.kind || artifact.artifactType || "Unknown";
}

function uploadedAt(artifact: ArtifactRecord) {
  return artifact.createdUtc || artifact.uploadedUtc || "";
}

export function Artifacts() {
  const [artifacts, setArtifacts] = useState<ArtifactRecord[]>([]);
  const [artifactTypeValue, setArtifactTypeValue] = useState("Manifest");
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
      const form = new FormData();
      form.append("kind", artifactTypeValue);
      form.append("file", file);

      const response = await fetch("/api/artifacts", {
        method: "POST",
        body: form
      });

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      setFile(null);
      setMessage("Artifact uploaded.");
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
          <p className="muted">
            Upload, download, and manage manifest and mapping artifacts for migration projects.
          </p>
        </div>
      </div>

      {error && <LoadingError message={error} />}
      {message && <div className="successBanner">{message}</div>}

      <Card title="Upload Artifact">
        <div className="formGrid">
          <label>
            Type
            <select value={artifactTypeValue} onChange={event => setArtifactTypeValue(event.target.value)}>
              <option value="Manifest">Manifest</option>
              <option value="Mapping">Mapping</option>
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
              {uploading ? "Uploading..." : "Upload"}
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
                {artifacts.map(artifact => {
                  const uploaded = uploadedAt(artifact);

                  return (
                    <tr key={artifact.artifactId}>
                      <td>{artifactType(artifact)}</td>
                      <td>{artifact.fileName}</td>
                      <td>
                        {uploaded ? new Date(uploaded).toLocaleString() : <span className="muted">Unknown</span>}
                      </td>
                      <td>
                        <small>{artifact.artifactId}</small>
                      </td>
                      <td>
                        <a href={api.artifactDownloadUrl(artifact.artifactId)}>Download</a>{" "}
                        <button
                          type="button"
                          onClick={() => void deleteArtifact(artifact.artifactId)}
                          disabled={deletingId === artifact.artifactId}
                        >
                          {deletingId === artifact.artifactId ? "Deleting..." : "Delete"}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}