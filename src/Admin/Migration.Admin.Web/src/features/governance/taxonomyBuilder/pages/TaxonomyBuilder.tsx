import { useState } from "react";
import { Card } from "../../../../components/Card";
import { LoadingError } from "../../../../components/LoadingError";

type ProbeState = "idle" | "running" | "success" | "error";

type BuilderWorkspaceProps = {
  title: string;
  apiPath: string;
};

function BuilderWorkspace({ title, apiPath }: BuilderWorkspaceProps) {
  const [status, setStatus] = useState<ProbeState>("idle");
  const [message, setMessage] = useState<string | null>(null);

  async function probeEndpoint() {
    setStatus("running");
    setMessage(null);

    try {
      const response = await fetch(apiPath, { method: "OPTIONS" });
      if (response.ok || response.status === 204 || response.status === 405) {
        setStatus("success");
        setMessage("Builder endpoint is reachable. HTTP " + String(response.status) + ".");
        return;
      }

      setStatus("error");
      setMessage("Request failed with HTTP " + String(response.status) + ".");
    } catch (error) {
      setStatus("error");
      setMessage(error instanceof Error ? error.message : String(error));
    }
  }

  return (
    <div className="pageStack">
      <Card title={title} description="Builder workspace restored from the Admin Web consolidation pass.">
        <p>
          This workspace is reachable from the canonical Admin Web route. Use the endpoint probe to confirm the Admin API route is visible in the local stack before wiring deeper builder workflows.
        </p>
        <div className="detailGrid">
          <span>API endpoint</span>
          <strong>{apiPath}</strong>
          <span>Status</span>
          <strong>{status}</strong>
        </div>
        <div className="buttonRow">
          <button type="button" className="primaryButton" onClick={() => void probeEndpoint()} disabled={status === "running"}>
            {status === "running" ? "Checking..." : "Check endpoint"}
          </button>
        </div>
      </Card>
      {status === "error" && message && <LoadingError message={message} />}
      {status === "success" && message && <Card title="Endpoint reachable" message={message} />}
    </div>
  );
}

export function TaxonomyBuilder() {
  return <BuilderWorkspace title="Taxonomy Builder" apiPath="/api/taxonomy-builder/build" />;
}
