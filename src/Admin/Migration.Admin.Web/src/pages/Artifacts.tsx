import { useEffect, useState } from "react";

type ArtifactRecord = {
  artifactId: string;
  artifactType: string;
  fileName: string;
  uploadedUtc?: string;
};

export function Artifacts() {
  const [artifacts, setArtifacts] = useState<ArtifactRecord[]>([]);
  const [artifactType, setArtifactType] = useState("Manifest");
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadArtifacts() {
    const response = await fetch("/api/artifacts");

    if (!response.ok) {
      throw new Error(await response.text());
    }

    setArtifacts(await response.json());
  }

  useEffect(() => {
    loadArtifacts().catch((e) => setError(e.message));
  }, []);

  async function uploadArtifact() {
    if (!file) return;

    const form = new FormData();

    form.append("artifactType", artifactType);
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

    await loadArtifacts();
  }

  async function deleteArtifact(artifactId: string) {
    const response = await fetch(`/api/artifacts/${artifactId}`, {
      method: "DELETE"
    });

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    await loadArtifacts();
  }

  return (
    <section className="page">
      <div className="page-header">
        <div>
          <h1>Artifacts</h1>
          <p>Upload manifests and mappings for migration projects.</p>
        </div>
      </div>

      {error && (
        <div className="error">
          {error}
        </div>
      )}

      <div className="card">
        <h2>Upload Artifact</h2>

        <label>
          Type
          <select
            value={artifactType}
            onChange={(e) => setArtifactType(e.target.value)}
          >
            <option value="Manifest">Manifest</option>
            <option value="Mapping">Mapping</option>
          </select>
        </label>

        <label>
          File
          <input
            type="file"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </label>

        <button disabled={!file} onClick={uploadArtifact}>
          Upload
        </button>
      </div>

      <div className="card">
        <h2>Stored Artifacts</h2>

        <table>
          <thead>
            <tr>
              <th>Type</th>
              <th>File</th>
              <th>Uploaded</th>
              <th />
            </tr>
          </thead>

          <tbody>
            {artifacts.map((artifact) => (
              <tr key={artifact.artifactId}>
                <td>{artifact.artifactType}</td>
                <td>{artifact.fileName}</td>
                <td>{artifact.uploadedUtc ?? ""}</td>
                <td>
                  <button
                    onClick={() =>
                      window.open(
                        `/api/artifacts/${artifact.artifactId}`,
                        "_blank"
                      )
                    }
                  >
                    Download
                  </button>

                  <button
                    onClick={() => deleteArtifact(artifact.artifactId)}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}

            {artifacts.length === 0 && (
              <tr>
                <td colSpan={4}>
                  No artifacts uploaded yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}