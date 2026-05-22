export interface OperatorSessionSnapshot {
  isAuthenticated: boolean;
  displayName?: string;
  accessToken?: string;
  roles: string[];
}

const storageKey = 'migration-admin-ui.operator-session';

export function readOperatorSession(): OperatorSessionSnapshot {
  const raw = window.sessionStorage.getItem(storageKey);

  if (!raw) {
    return { isAuthenticated: false, roles: [] };
  }

  try {
    const parsed = JSON.parse(raw) as OperatorSessionSnapshot;
    return {
      isAuthenticated: parsed.isAuthenticated === true,
      displayName: parsed.displayName,
      accessToken: parsed.accessToken,
      roles: Array.isArray(parsed.roles) ? parsed.roles : [],
    };
  } catch {
    window.sessionStorage.removeItem(storageKey);
    return { isAuthenticated: false, roles: [] };
  }
}

export function writeOperatorSession(snapshot: OperatorSessionSnapshot): void {
  window.sessionStorage.setItem(storageKey, JSON.stringify(snapshot));
}

export function clearOperatorSession(): void {
  window.sessionStorage.removeItem(storageKey);
}
