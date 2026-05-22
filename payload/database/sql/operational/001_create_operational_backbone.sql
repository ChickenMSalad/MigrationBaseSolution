SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.MigrationProjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationProjects
    (
        ProjectId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationProjects PRIMARY KEY,
        ProjectKey nvarchar(200) NOT NULL,
        DisplayName nvarchar(300) NOT NULL,
        Status nvarchar(50) NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT UQ_MigrationProjects_ProjectKey UNIQUE (ProjectKey)
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationRuns
    (
        RunId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationRuns PRIMARY KEY,
        ProjectId uniqueidentifier NOT NULL,
        RunKey nvarchar(200) NOT NULL,
        Status nvarchar(50) NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        StartedAtUtc datetimeoffset NULL,
        CompletedAtUtc datetimeoffset NULL,
        CONSTRAINT FK_MigrationRuns_MigrationProjects FOREIGN KEY (ProjectId) REFERENCES dbo.MigrationProjects(ProjectId),
        CONSTRAINT UQ_MigrationRuns_RunKey UNIQUE (RunKey)
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationManifestRows', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationManifestRows
    (
        ManifestRowId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationManifestRows PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        RowNumber bigint NOT NULL,
        SourceIdentifier nvarchar(600) NOT NULL,
        SourceUri nvarchar(2000) NULL,
        PayloadJson nvarchar(max) NOT NULL,
        Status nvarchar(50) NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationManifestRows_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId)
    );

    CREATE INDEX IX_MigrationManifestRows_RunId_RowNumber ON dbo.MigrationManifestRows(RunId, RowNumber);
    CREATE INDEX IX_MigrationManifestRows_RunId_Status ON dbo.MigrationManifestRows(RunId, Status);
    CREATE INDEX IX_MigrationManifestRows_RunId_SourceIdentifier ON dbo.MigrationManifestRows(RunId, SourceIdentifier);
END;
GO

IF OBJECT_ID(N'dbo.MigrationWorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationWorkItems
    (
        WorkItemId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationWorkItems PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        ManifestRowId uniqueidentifier NULL,
        WorkItemType nvarchar(100) NOT NULL,
        Status nvarchar(50) NOT NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_MigrationWorkItems_AttemptCount DEFAULT (0),
        AvailableAtUtc datetimeoffset NULL,
        LeasedUntilUtc datetimeoffset NULL,
        LeaseOwner nvarchar(300) NULL,
        PayloadJson nvarchar(max) NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationWorkItems_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId),
        CONSTRAINT FK_MigrationWorkItems_MigrationManifestRows FOREIGN KEY (ManifestRowId) REFERENCES dbo.MigrationManifestRows(ManifestRowId)
    );

    CREATE INDEX IX_MigrationWorkItems_RunId_Status_Available ON dbo.MigrationWorkItems(RunId, Status, AvailableAtUtc, LeasedUntilUtc);
    CREATE INDEX IX_MigrationWorkItems_RunId_ManifestRowId ON dbo.MigrationWorkItems(RunId, ManifestRowId);
END;
GO

IF OBJECT_ID(N'dbo.MigrationFailures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationFailures
    (
        FailureId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationFailures PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        WorkItemId uniqueidentifier NULL,
        ManifestRowId uniqueidentifier NULL,
        FailureType nvarchar(100) NOT NULL,
        FailureCode nvarchar(150) NOT NULL,
        Message nvarchar(max) NOT NULL,
        DetailsJson nvarchar(max) NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationFailures_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId),
        CONSTRAINT FK_MigrationFailures_MigrationWorkItems FOREIGN KEY (WorkItemId) REFERENCES dbo.MigrationWorkItems(WorkItemId),
        CONSTRAINT FK_MigrationFailures_MigrationManifestRows FOREIGN KEY (ManifestRowId) REFERENCES dbo.MigrationManifestRows(ManifestRowId)
    );

    CREATE INDEX IX_MigrationFailures_RunId_CreatedAtUtc ON dbo.MigrationFailures(RunId, CreatedAtUtc);
    CREATE INDEX IX_MigrationFailures_RunId_FailureCode ON dbo.MigrationFailures(RunId, FailureCode);
END;
GO

IF OBJECT_ID(N'dbo.MigrationRunCheckpoints', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationRunCheckpoints
    (
        CheckpointId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationRunCheckpoints PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        CheckpointName nvarchar(200) NOT NULL,
        CheckpointValue nvarchar(1000) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationRunCheckpoints_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId)
    );

    CREATE INDEX IX_MigrationRunCheckpoints_RunId_CheckpointName ON dbo.MigrationRunCheckpoints(RunId, CheckpointName, CreatedAtUtc DESC);
END;
GO

IF OBJECT_ID(N'dbo.MigrationAssetMappings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationAssetMappings
    (
        AssetMappingId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationAssetMappings PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        SourceSystem nvarchar(100) NOT NULL,
        SourceIdentifier nvarchar(600) NOT NULL,
        TargetSystem nvarchar(100) NOT NULL,
        TargetIdentifier nvarchar(600) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationAssetMappings_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId),
        CONSTRAINT UQ_MigrationAssetMappings_Source UNIQUE (RunId, SourceSystem, SourceIdentifier, TargetSystem)
    );

    CREATE INDEX IX_MigrationAssetMappings_RunId_TargetIdentifier ON dbo.MigrationAssetMappings(RunId, TargetSystem, TargetIdentifier);
END;
GO

IF OBJECT_ID(N'dbo.MigrationConnectorRegistrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationConnectorRegistrations
    (
        ConnectorRegistrationId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationConnectorRegistrations PRIMARY KEY,
        ProjectId uniqueidentifier NOT NULL,
        ConnectorKey nvarchar(200) NOT NULL,
        ConnectorRole nvarchar(50) NOT NULL,
        ConnectorType nvarchar(100) NOT NULL,
        ConfigurationJson nvarchar(max) NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationConnectorRegistrations_MigrationProjects FOREIGN KEY (ProjectId) REFERENCES dbo.MigrationProjects(ProjectId),
        CONSTRAINT UQ_MigrationConnectorRegistrations_ProjectConnector UNIQUE (ProjectId, ConnectorKey)
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationMappingProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationMappingProfiles
    (
        MappingProfileId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationMappingProfiles PRIMARY KEY,
        ProjectId uniqueidentifier NOT NULL,
        MappingKey nvarchar(200) NOT NULL,
        Version int NOT NULL,
        MappingJson nvarchar(max) NOT NULL,
        IsActive bit NOT NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        UpdatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationMappingProfiles_MigrationProjects FOREIGN KEY (ProjectId) REFERENCES dbo.MigrationProjects(ProjectId),
        CONSTRAINT UQ_MigrationMappingProfiles_ProjectMappingVersion UNIQUE (ProjectId, MappingKey, Version)
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationRunEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationRunEvents
    (
        RunEventId uniqueidentifier NOT NULL CONSTRAINT PK_MigrationRunEvents PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        EventType nvarchar(150) NOT NULL,
        Severity nvarchar(50) NOT NULL,
        Message nvarchar(max) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        CreatedAtUtc datetimeoffset NOT NULL,
        CONSTRAINT FK_MigrationRunEvents_MigrationRuns FOREIGN KEY (RunId) REFERENCES dbo.MigrationRuns(RunId)
    );

    CREATE INDEX IX_MigrationRunEvents_RunId_CreatedAtUtc ON dbo.MigrationRunEvents(RunId, CreatedAtUtc);
END;
GO
