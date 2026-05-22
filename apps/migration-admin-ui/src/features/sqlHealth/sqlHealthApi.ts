import { adminApiBaseUrl } from '../../lib/adminApi';
import type { OperationalSqlHealth } from './sqlHealthTypes';

export async function fetchOperationalSqlHealth(): Promise<OperationalSqlHealth> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/sql/health`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<OperationalSqlHealth>;
}
