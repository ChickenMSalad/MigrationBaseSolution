IF OBJECT_ID(N'dbo.MigrationOperationalEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationOperationalEvents
    (
        OperationalEventId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationOperationalEvents PRIMARY KEY,
        EventType NVARCHAR(128) NOT NULL,
        Severity NVARCHAR(32) NOT NULL,
        Category NVARCHAR(64) NOT NULL,
        Source NVARCHAR(128) NOT NULL,
        Message NVARCHAR(2048) NOT NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationOperationalEvents_CreatedUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_MigrationOperationalEvents_CreatedUtc
        ON dbo.MigrationOperationalEvents (CreatedUtc DESC);

    CREATE INDEX IX_MigrationOperationalEvents_EventType_CreatedUtc
        ON dbo.MigrationOperationalEvents (EventType, CreatedUtc DESC);
END
