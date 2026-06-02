import type { ReactNode } from "react";

export function Card({
  title,
  subtitle,
  description,
  action,
  children,
}: {
  title?: string;
  subtitle?: string;
  description?: string;
  action?: ReactNode;
  children?: ReactNode;
}) {
  const supportingText = subtitle ?? description;

  return (
    <section className="card">
      {(title || supportingText || action) && (
        <div className="card-header">
          <div>
            {title && <h2>{title}</h2>}
            {supportingText && <p>{supportingText}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </section>
  );
}

export function StatusPill({ status, value }: { status?: string; value?: string }) {
  const displayValue = status ?? value ?? "Unknown";
  const normalized = displayValue.toLowerCase();
  const kind = normalized.includes("fail")
    ? "bad"
    : normalized.includes("complete")
      ? "good"
      : normalized.includes("run") || normalized.includes("queue")
        ? "warn"
        : "neutral";

  return <span className={`pill ${kind}`}>{displayValue}</span>;
}

export function EmptyState({
  title,
  message,
  description,
}: {
  title: string;
  message?: string;
  description?: string;
}) {
  const text = message ?? description;

  return (
    <div className="empty-state">
      <h3>{title}</h3>
      {text && <p>{text}</p>}
    </div>
  );
}

export function JsonBlock({ value }: { value: unknown }) {
  return <pre className="json-block">{JSON.stringify(value, null, 2)}</pre>;
}
