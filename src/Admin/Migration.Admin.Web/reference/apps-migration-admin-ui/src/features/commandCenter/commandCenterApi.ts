import { adminApiBaseUrl } from '../../lib/adminApi';
import type { CommandCenterSummary } from './commandCenterTypes';

export async function fetchCommandCenterSummary(): Promise<CommandCenterSummary> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/command-center/summary`);

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return response.json() as Promise<CommandCenterSummary>;
}
