import type { RuntimeReadinessStatus } from '../lib/operationalRuntimeApi';

interface RuntimeStatusBadgeProps {
  status: RuntimeReadinessStatus | string | undefined;
}

function getStatusLabel(status: RuntimeReadinessStatus | string | undefined): string {
  if (!status) {
    return 'unknown';
  }

  return status;
}

export function RuntimeStatusBadge({ status }: RuntimeStatusBadgeProps) {
  const label = getStatusLabel(status);

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        borderRadius: '999px',
        border: '1px solid #c8d3e1',
        padding: '0.2rem 0.55rem',
        fontSize: '0.78rem',
        fontWeight: 700,
        textTransform: 'uppercase',
        letterSpacing: '0.04em'
      }}
    >
      {label}
    </span>
  );
}
