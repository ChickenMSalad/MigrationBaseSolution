INSERT INTO migration.WorkItems
(
    RunId,
    WorkType,
    Status,
    AttemptCount,
    MaxAttempts,
    PayloadJson,
    CreatedAtUtc,
    UpdatedAtUtc,
    Priority
)
VALUES
(
    '11111111-1111-1111-1111-111111111111',
    'SmokeNoOp',
    'Queued',
    0,
    3,
    '{
  "jobName": "RuntimeSmoke",
  "sourceType": "LocalStorage",
  "targetType": "LocalStorage",
  "manifestType": "Csv",
  "mappingProfilePath": "smoke.json",
  "dryRun": true
}',
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    100
);

SELECT TOP 5
    WorkItemId,
    Status,
    CreatedAtUtc
FROM migration.WorkItems
ORDER BY WorkItemId DESC;
