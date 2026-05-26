DECLARE @RunId uniqueidentifier = NEWID();

INSERT INTO migration.Runs
(
    RunId,
    Status,
    CreatedAtUtc,
    UpdatedAtUtc
)
VALUES
(
    @RunId,
    N'Pending',
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
);

INSERT INTO migration.ManifestRows
(
    RunId,
    SourceRowNumber,
    SourceExternalId,
    SourcePath,
    ContentHash,
    Operation,
    ManifestStatus,
    PayloadJson,
    ValidationJson,
    CreatedAtUtc,
    UpdatedAtUtc
)
VALUES
(
    @RunId,
    1,
    'runtime-smoke-001',
    '/runtime/smoke/001',
    'runtime-smoke-hash-001',
    'SmokeNoOp',
    'Pending',
    N'{"kind":"runtime-smoke","operation":"SmokeNoOp"}',
    N'{}',
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
);

SELECT @RunId AS RuntimeRunId;