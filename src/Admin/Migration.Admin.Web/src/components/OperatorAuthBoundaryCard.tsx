import { isOperatorAuthConfigured, operatorAuthConfig } from '../auth/authConfig';
import { readOperatorSession } from '../auth/authSession';

export function OperatorAuthBoundaryCard() {
  const session = readOperatorSession();
  const configured = isOperatorAuthConfigured();

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Operator access</h2>
          <p>Authentication boundary readiness for the migration control plane.</p>
        </div>
        <span className={configured ? 'status-pill status-ok' : 'status-pill status-warn'}>
          {configured ? 'Configured' : 'Local/dev mode'}
        </span>
      </div>
      <dl className="metric-grid">
        <div>
          <dt>Mode</dt>
          <dd>{operatorAuthConfig.mode}</dd>
        </div>
        <div>
          <dt>Tenant</dt>
          <dd>{operatorAuthConfig.tenantId ?? 'not configured'}</dd>
        </div>
        <div>
          <dt>API scope</dt>
          <dd>{operatorAuthConfig.apiScope ?? 'not configured'}</dd>
        </div>
        <div>
          <dt>Session</dt>
          <dd>{session.isAuthenticated ? session.displayName ?? 'authenticated' : 'not signed in'}</dd>
        </div>
      </dl>
    </section>
  );
}
