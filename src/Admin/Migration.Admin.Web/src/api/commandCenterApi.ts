import { apiGet } from './core/adminApiClient';
import type { CommandCenterHealthResponse, CommandCenterSummary } from '../types/commandCenter';

export const commandCenterApi = {
  getSummary(): Promise<CommandCenterSummary> {
    return apiGet<CommandCenterSummary>('/api/operational/command-center/summary');
  },

  getHealth(): Promise<CommandCenterHealthResponse> {
    return apiGet<CommandCenterHealthResponse>('/api/operational/command-center/health');
  },
};
