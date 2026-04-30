import type { ReactNode } from "react";

export function Card({ title, subtitle, action, children }: { title?: string; subtitle?: string; action?: ReactNode; children: ReactNode }) {
  return (
    <section className="card">
      {(title || subtitle || action) && (
        <div className="cardHeader">
          <div>
            {title && <h2>{title}</h2>}
            {subtitle && <p>{subtitle}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </section>
  );
}

export function StatusPill({ status }: { status?: string }) {
  const value = status || "Unknown";
  const normalized = value.toLowerCase();
  const kind = normalized.includes("fail") ? "bad" : normalized.includes("complete") ? "good" : normalized.includes("run") || normalized.includes("queue") ? "warn" : "neutral";
  return <span className={`pill ${kind}`}>{value}</span>;
}

export function EmptyState({ title, message }: { title: string; message?: string }) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      {message && <span>{message}</span>}
    </div>
  );
}

export function JsonBlock({ value }: { value: unknown }) {
  return <pre className="jsonBlock">{JSON.stringify(value, null, 2)}</pre>;
}
