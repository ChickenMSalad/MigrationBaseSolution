IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_WorkItems_ClaimQueue'
      AND object_id = OBJECT_ID('migration.WorkItems')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_WorkItems_ClaimQueue
    ON migration.WorkItems
    (
        Status,
        AvailableAtUtc,
        Priority
    )
    INCLUDE
    (
        WorkItemId,
        RunId,
        WorkType,
        AttemptCount,
        LeaseExpiresAtUtc
    );
END
GO