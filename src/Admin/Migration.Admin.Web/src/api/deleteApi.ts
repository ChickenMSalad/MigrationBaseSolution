export async function deleteProject(projectId: string, includeRuns = false) {
  const response = await fetch(`/api/projects/${encodeURIComponent(projectId)}?includeRuns=${includeRuns}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function deleteRun(runId: string) {
  const response = await fetch(`/api/runs/${encodeURIComponent(runId)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function deleteCredential(credentialId: string) {
  const response = await fetch(`/api/credentials/${encodeURIComponent(credentialId)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function deleteArtifact(artifactId: string) {
  const response = await fetch(`/api/artifacts/${encodeURIComponent(artifactId)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function deleteConnector(connectorType: string) {
  const response = await fetch(`/api/connectors/${encodeURIComponent(connectorType)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}
