import { adminApiBaseUrl } from '../../lib/adminApi';

export function buildExecutionDiagnosticBundleUrl(executionSessionId: string): string {
  return `${adminApiBaseUrl}/api/operational/execution-diagnostics/${executionSessionId}/bundle.json`;
}
