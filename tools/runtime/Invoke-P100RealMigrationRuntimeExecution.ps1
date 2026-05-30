[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $true)]
    [Guid] $RunId,

    [Parameter(Mandatory = $false)]
    [string] $JobDefinitionPath,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($JobDefinitionPath)) {
    $JobDefinitionPath = Join-Path $repoRoot 'profiles\jobs\p10-localstorage-real-migration.job.json'
}
elseif (-not [System.IO.Path]::IsPathRooted($JobDefinitionPath)) {
    $JobDefinitionPath = Join-Path $repoRoot $JobDefinitionPath
}

if (-not (Test-Path -LiteralPath $JobDefinitionPath)) {
    throw ('Job definition file was not found: {0}' -f $JobDefinitionPath)
}

$jobPayload = Get-Content -LiteralPath $JobDefinitionPath -Raw
if ([string]::IsNullOrWhiteSpace($jobPayload)) {
    throw 'Job definition file is empty.'
}

try {
    $job = ConvertFrom-Json -InputObject $jobPayload
}
catch {
    throw ('Job definition JSON is invalid: {0}' -f $_.Exception.Message)
}

foreach ($requiredProperty in @('jobName', 'sourceType', 'targetType', 'manifestType', 'mappingProfilePath')) {
    if ($null -eq $job.PSObject.Properties[$requiredProperty]) {
        throw ('Job definition is missing required property: {0}' -f $requiredProperty)
    }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot 'artifacts\p10-real-migration'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot ('enqueue-p10-real-migration-{0}.sql' -f $RunId)
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$escapedPayload = $jobPayload.Replace("'", "''")
$runKey = ('p10-real-migration-{0}' -f $RunId)

$sql = @"
SET NOCOUNT ON;

DECLARE @RunId uniqueidentifier = '$RunId';
DECLARE @PayloadJson nvarchar(max) = N'$escapedPayload';

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    THROW 51000, 'Required table migration.Runs is missing.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51001, 'Required table migration.WorkItems is missing.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.Runs WHERE RunId = @RunId)
BEGIN
    INSERT INTO migration.Runs
    (
        RunId,
        RunKey,
        RunName,
        SourceSystem,
        TargetSystem,
        Status,
        EnvironmentName,
        IsDryRun,
        RequestedAtUtc,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        @RunId,
        N'$runKey',
        N'P10 Real Migration Runtime Execution',
        N'LocalStorage',
        N'LocalStorage',
        N'Queued',
        N'Azure',
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;

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
    @RunId,
    N'MigrationJobDefinition',
    N'Queued',
    0,
    3,
    @PayloadJson,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    100
);

SELECT TOP (10)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY WorkItemId DESC;
"@

Set-Content -LiteralPath $OutputPath -Value $sql -Encoding UTF8

$sqlServerName = $SqlServer
if ($sqlServerName -notlike '*.database.windows.net') {
    $sqlServerName = ('{0}.database.windows.net' -f $sqlServerName)
}

& sqlcmd -S $sqlServerName -d $Database -U $SqlAdmin -P $SqlPasswordPlain -i $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw ('sqlcmd failed while enqueueing P10 real migration runtime work. Exit code: {0}' -f $LASTEXITCODE)
}

Write-Host ('P10 real migration work item enqueued for RunId {0}.' -f $RunId)
Write-Host ('SQL script written to {0}.' -f $OutputPath)
