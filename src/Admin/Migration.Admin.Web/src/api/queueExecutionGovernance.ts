import { apiGet } from './core/adminApiClient';

export type QueueExecutionGovernanceDecision = {
  generatedUtc: string;
  canEnableLiveQueueExecution: boolean;
  canCompleteMessages: boolean;
  requiresManualApproval: boolean;
  recommendedMode: string;
  requiredConditions: string[];
  blockingIssues: string[];
  warnings: string[];
};

export async function getQueueExecutionGovernance(): Promise<QueueExecutionGovernanceDecision> {
  return apiGet<QueueExecutionGovernanceDecision>('/api/cloud/operations/queue-execution-governance');
}
