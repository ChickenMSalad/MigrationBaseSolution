DECLARE @RunId uniqueidentifier = '11111111-1111-1111-1111-111111111111';

IF NOT EXISTS
(
    SELECT 1
    FROM migration.ManifestRows
    WHERE RunId = @RunId
      AND SourceExternalId = N'runtime-smoke-001'
)
BEGIN
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
        N'runtime-smoke-001',
        N'/runtime/smoke/001',
        N'runtime-smoke-hash-001',
        N'SmokeNoOp',
        N'Pending',
        N'{"kind":"runtime-smoke","operation":"SmokeNoOp"}',
        N'{}',
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END

SELECT *
FROM migration.ManifestRows
WHERE RunId = @RunId;