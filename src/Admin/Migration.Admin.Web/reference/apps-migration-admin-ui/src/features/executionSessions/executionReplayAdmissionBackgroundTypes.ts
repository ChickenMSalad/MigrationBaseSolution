export type ExecutionReplayAdmissionBackgroundStatus = {
  enabled: boolean;
  intervalSeconds: number;
  take: number;
  admissionEnabled: boolean;
  maxConcurrentReplays: number;
  allowedStartHourUtc: number;
  allowedEndHourUtc: number;
  generatedUtc: string;
};
