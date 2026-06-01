import { adminApiBaseUrl } from '../../lib/adminApi';

export async function pauseExecutionSession(executionSessionId: string, reason?: string): Promise<void> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-control/pause`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ executionSessionId, reason: reason || null }),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
}

export async function resumeExecutionSession(executionSessionId: string, reason?: string): Promise<void> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-control/resume`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ executionSessionId, reason: reason || null }),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
}

export async function cancelExecutionSession(executionSessionId: string, reason?: string): Promise<void> {
  const response = await fetch(`${adminApiBaseUrl}/api/operational/execution-control/cancel`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ executionSessionId, reason: reason || null }),
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
}
