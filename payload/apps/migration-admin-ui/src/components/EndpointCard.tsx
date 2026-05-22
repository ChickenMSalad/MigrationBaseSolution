import { EndpointProbe, summarize } from '../lib/adminApi';

interface EndpointCardProps {
  probe: EndpointProbe;
}

export function EndpointCard({ probe }: EndpointCardProps) {
  const state = probe.status.state;
  const className = `endpoint-card endpoint-card--${state}`;

  return (
    <article className={className}>
      <div className="endpoint-card__header">
        <div>
          <h3>{probe.label}</h3>
          <code>{probe.path}</code>
        </div>
        <span>{state}</span>
      </div>
      <p>
        {state === 'success'
          ? summarize(probe.status.value)
          : state === 'error'
            ? probe.status.error
            : 'Not checked yet.'}
      </p>
    </article>
  );
}
