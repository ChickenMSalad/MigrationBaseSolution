import { useEffect, useState } from "react";
import { getCostAnalytics } from "../api/costAnalyticsApi";
import type { CostAnalyticsResponse } from "../types/costAnalytics";

function formatMoney(value?: number | null, currency?: string | null): string {
  if (value === null || value === undefined) {
    return "—";
  }

  const code = currency || "USD";
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: code,
    maximumFractionDigits: 2
  }).format(value);
}

export function CostAnalytics() {
  const [data, setData] = useState<CostAnalyticsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        setIsLoading(true);
        setError(null);
        const result = await getCostAnalytics();
        if (!cancelled) {
          setData(result);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Unable to load cost analytics.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const summary = data?.summary;
  const currency = summary?.currency || "USD";
  const breakdown = data?.breakdown || [];
  const warnings = data?.warnings || [];

  return (
    <main className="page-shell">
      <header className="page-header">
        <div>
          <p className="eyebrow">Operations</p>
          <h1>Cost Analytics</h1>
          <p className="page-description">
            Review estimated runtime, storage, transfer, and operation costs for migration execution.
          </p>
        </div>
      </header>

      {isLoading && <section className="card">Loading cost analytics…</section>}
      {error && <section className="card error">{error}</section>}

      {!isLoading && !error && (
        <>
          <section className="metric-grid">
            <article className="card metric-card">
              <span>Total</span>
              <strong>{formatMoney(summary?.estimatedTotalCost, currency)}</strong>
            </article>
            <article className="card metric-card">
              <span>Storage</span>
              <strong>{formatMoney(summary?.estimatedStorageCost, currency)}</strong>
            </article>
            <article className="card metric-card">
              <span>Transfer</span>
              <strong>{formatMoney(summary?.estimatedTransferCost, currency)}</strong>
            </article>
            <article className="card metric-card">
              <span>Operations</span>
              <strong>{formatMoney(summary?.estimatedOperationCost, currency)}</strong>
            </article>
          </section>

          {warnings.length > 0 && (
            <section className="card warning-card">
              <h2>Warnings</h2>
              <ul>
                {warnings.map((warning) => (
                  <li key={warning}>{warning}</li>
                ))}
              </ul>
            </section>
          )}

          <section className="card">
            <h2>Breakdown</h2>
            {breakdown.length === 0 ? (
              <p>No cost breakdown is available.</p>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Category</th>
                      <th>Name</th>
                      <th>Estimated Cost</th>
                      <th>Notes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {breakdown.map((item) => (
                      <tr key={`${item.category}-${item.name}`}>
                        <td>{item.category}</td>
                        <td>{item.name}</td>
                        <td>{formatMoney(item.estimatedCost, item.currency || currency)}</td>
                        <td>{item.notes || "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </main>
  );
}
