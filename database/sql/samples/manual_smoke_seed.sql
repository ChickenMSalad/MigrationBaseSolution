DECLARE @RunId uniqueidentifier =
(
    SELECT TOP 1 RunId
    FROM migration.MigrationRuns
    ORDER BY RequestedAtUtc DESC
);

INSERT INTO migration.MigrationManifestRows
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
    'smoke-source-001',
    '/smoke/source/001',
    'smoke-content-hash-001',
    'SmokeNoOp',
    'Pending',
    N'{"kind":"cloud-smoke","operation":"SmokeNoOp"}',
    N'{}',
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
);

SELECT
    RunId,
    SourceRowNumber,
    SourceExternalId,
    ManifestStatus
FROM migration.MigrationManifestRows
WHERE RunId = @RunId;