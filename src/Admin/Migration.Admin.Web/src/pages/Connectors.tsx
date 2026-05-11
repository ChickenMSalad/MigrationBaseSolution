import { useEffect, useState } from "react";
import { api, displayConnectorName } from "../api/client";
import { Card, EmptyState, JsonBlock } from "../components/Card";
import { LoadingError } from "../components/LoadingError";
import type { ConnectorsResponse, ConnectorDescriptor } from "../types/api";

function ConnectorList({ title, items }: { title: string; items: ConnectorDescriptor[] }) {
  return (
    <Card title={title}>
      {items.length === 0 ? <EmptyState title="No connectors returned" /> : (
        <div className="connectorGrid">
          {items.map((item, index) => (
            <details className="connectorCard" key={`${displayConnectorName(item)}-${index}`}>
              <summary>{displayConnectorName(item)}</summary>
              <JsonBlock value={item} />
            </details>
          ))}
        </div>
      )}
    </Card>
  );
}

export function Connectors() {
  const [data, setData] = useState<ConnectorsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    api.connectors()
      .then((result) => { if (active) setData(result); })
      .catch((err) => { if (active) setError(err instanceof Error ? err.message : String(err)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  return (
    <div className="pageStack">
      <div className="pageTitle"><div><h1>Connector Catalog</h1><p>Descriptors exposed by the Admin API.</p></div></div>
      <LoadingError loading={loading} error={error} />
      {data && (
        <>
          <ConnectorList title="Sources" items={data.sources ?? []} />
          <ConnectorList title="Targets" items={data.targets ?? []} />
          <ConnectorList title="Manifest Providers" items={data.manifestProviders ?? []} />
        </>
      )}
    </div>
  );
}
