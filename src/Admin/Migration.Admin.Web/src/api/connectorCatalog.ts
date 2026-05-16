export type ConnectorDescriptor = {
  type?: string;
  sourceType?: string;
  targetType?: string;
  manifestType?: string;
  name?: string;
  displayName?: string;
  description?: string;
  [key: string]: unknown;
};

export type ConnectorCatalogResponse = {
  sources: ConnectorDescriptor[];
  targets: ConnectorDescriptor[];
  manifestProviders: ConnectorDescriptor[];
};

const API_BASE_URL = (import.meta.env.VITE_ADMIN_API_BASE_URL ?? '').replace(/\/$/, '');

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`);

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function getConnectorCatalog(): Promise<ConnectorCatalogResponse> {
  return getJson<ConnectorCatalogResponse>('/api/connectors');
}

export async function getSourceConnectors(): Promise<ConnectorDescriptor[]> {
  return getJson<ConnectorDescriptor[]>('/api/connectors/sources');
}

export async function getTargetConnectors(): Promise<ConnectorDescriptor[]> {
  return getJson<ConnectorDescriptor[]>('/api/connectors/targets');
}

export async function getManifestProviders(): Promise<ConnectorDescriptor[]> {
  return getJson<ConnectorDescriptor[]>('/api/connectors/manifests');
}

export function getConnectorKey(descriptor: ConnectorDescriptor): string {
  return String(
    descriptor.type ??
      descriptor.sourceType ??
      descriptor.targetType ??
      descriptor.manifestType ??
      descriptor.name ??
      ''
  ).trim();
}

export function getConnectorLabel(descriptor: ConnectorDescriptor): string {
  return String(
    descriptor.displayName ??
      descriptor.name ??
      descriptor.type ??
      descriptor.sourceType ??
      descriptor.targetType ??
      descriptor.manifestType ??
      ''
  ).trim();
}
