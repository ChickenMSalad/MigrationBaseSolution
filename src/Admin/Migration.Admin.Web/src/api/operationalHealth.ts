import { apiGet } from './core/adminApiClient';

export type OperationalHealthCheckDescriptor = {
  name: string;
  status: string;
  description: string;
  warnings: string[];
};

export type OperationalHealthDescriptor = {
  status: string;
  environmentName: string;
  checkedUtc: string;
  checks: OperationalHealthCheckDescriptor[];
  warnings: string[];
};

export async function getHealthReady(): Promise<OperationalHealthDescriptor> {
  return apiGet<OperationalHealthDescriptor>('/health/ready');
}

export async function getHealthCloud(): Promise<OperationalHealthDescriptor> {
  return apiGet<OperationalHealthDescriptor>('/health/cloud');
}
