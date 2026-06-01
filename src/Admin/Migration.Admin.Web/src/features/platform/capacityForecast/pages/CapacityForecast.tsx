import { useEffect, useState } from "react";
import { Activity, AlertTriangle, RefreshCw } from "lucide-react";
import { getCapacityForecast } from "../api/capacityForecastApi";
import type { CapacityForecastSummary } from "../types/capacityForecast";

export function CapacityForecast() {
  const [forecast, setForecast] = useState<CapacityForecastSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadForecast() {
    setIsLoading(true);
    setError(null);
    try {
      setForecast(await getCapacityForecast());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load capacity forecast.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadForecast();
  }, []);

  return (
    <main className="page">
      <section className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Capacity Forecast</h1>
          <p className="muted">Review runtime capacity signals and operator recommendations.</p>
        </div>
        <button className="button secondary" onClick={() => void loadForecast()} disabled={isLoading}>
          <RefreshCw size={16} /> Refresh
        </button>
      </section>

      {error && (
        <section className="card warning">
          <AlertTriangle size={18} />
          <span>{error}</span>
        </section>
      )}

      <section className="grid cards">
        <article className="card">
          <h2>Forecast Window</h2>
          <p>{forecast?.window.label ?? (isLoading ? "Loading..." : "Unavailable")}</p>
          <p className="muted">Generated UTC: {forecast?.generatedUtc ?? "n/a"}</p>
        </article>
      </section>

      <section className="card">
        <h2>Capacity Metrics</h2>
        {forecast?.metrics.length ? (
          <table>
            <thead>
              <tr><th>Name</th><th>Value</th><th>Unit</th><th>Status</th></tr>
            </thead>
            <tbody>
              {forecast.metrics.map((metric) => (
                <tr key={metric.name}>
                  <td>{metric.name}</td>
                  <td>{metric.value}</td>
                  <td>{metric.unit}</td>
                  <td>{metric.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p className="muted"><Activity size={16} /> No capacity metrics are available yet.</p>
        )}
      </section>

      <section className="card">
        <h2>Recommendations</h2>
        {forecast?.recommendations.length ? (
          <ul>
            {forecast.recommendations.map((item) => (
              <li key={item.id}>
                <strong>{item.severity}: {item.title}</strong>
                <p>{item.detail}</p>
              </li>
            ))}
          </ul>
        ) : (
          <p className="muted">No recommendations are available yet.</p>
        )}
      </section>
    </main>
  );
}
