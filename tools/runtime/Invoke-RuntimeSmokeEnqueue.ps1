[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $true)]
    [Guid] $RunId,

    [Parameter(Mandatory = $false)]
    [string] $PayloadPath,

    [Parameter(Mandatory = $false)]
    [string] $SeedSqlPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = Join-Path $current.Path 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $candidate) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate repo root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter(Mandatory = $true)][string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $($LASTEXITCODE): $FilePath"
    }
}

$repoRoot = Get-RepoRoot

if ([string]::IsNullOrWhiteSpace($PayloadPath)) {
    $PayloadPath = Join-Path $repoRoot 'config-samples\runtime-smoke-job-definition.sample.json'
}

if ([string]::IsNullOrWhiteSpace($SeedSqlPath)) {
    $SeedSqlPath = Join-Path $repoRoot 'database\sql\p7\010_runtime_smoke_job_seed.sql'
}

if (-not (Test-Path -LiteralPath $PayloadPath)) {
    throw "Smoke payload file was not found: $PayloadPath"
}

if (-not (Test-Path -LiteralPath $SeedSqlPath)) {
    throw "Smoke seed SQL template was not found: $SeedSqlPath"
}

$payloadText = Get-Content -LiteralPath $PayloadPath -Raw
try {
    $null = $payloadText | ConvertFrom-Json
}
catch {
    throw "Smoke payload file is not valid JSON: $PayloadPath"
}

$payloadSql = $payloadText.Replace("'", "''")
$runIdSql = $RunId.ToString()
$tempSql = Join-Path ([System.IO.Path]::GetTempPath()) ("runtime-smoke-enqueue-" + [Guid]::NewGuid().ToString('N') + ".sql")

$sql = @"
DECLARE @RunId uniqueidentifier = '$runIdSql';
DECLARE @PayloadJson nvarchar(max) = N'$payloadSql';

IF @RunId IS NULL
BEGIN
    THROW 51000, 'RunId is required.', 1;
END;

IF @PayloadJson IS NULL OR ISJSON(@PayloadJson) <> 1
BEGIN
    THROW 51001, 'Payload JSON is required and must be valid JSON.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51002, 'Required table migration.WorkItems does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.MigrationRuns WHERE RunId = @RunId)
   AND NOT EXISTS (SELECT 1 FROM migration.Runs WHERE RunId = @RunId)
BEGIN
    THROW 51003, 'RunId does not exist in migration.MigrationRuns or migration.Runs. Seed/create the run before enqueueing smoke work.', 1;
END;

INSERT INTO migration.WorkItems
(
    RunId,
    WorkType,
    Status,
    Priority,
    AttemptCount,
    MaxAttempts,
    PayloadJson,
    CreatedAtUtc,
    UpdatedAtUtc
)
VALUES
(
    @RunId,
    N'MigrationJobDefinition',
    N'Queued',
    100,
    0,
    3,
    @PayloadJson,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
);

SELECT TOP (10)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CompletedAtUtc,
    LastErrorMessage,
    CreatedAtUtc
FROM migration.WorkItems
ORDER BY WorkItemId DESC;
"@

Set-Content -LiteralPath $tempSql -Value $sql -Encoding UTF8
$serverName = if ($SqlServer.EndsWith('.database.windows.net')) { $SqlServer } else { "$SqlServer.database.windows.net" }

try {
    Invoke-CheckedProcess -FilePath 'sqlcmd' -Arguments @(
        '-S', $serverName,
        '-d', $Database,
        '-U', $SqlAdmin,
        '-P', $SqlPasswordPlain,
        '-i', $tempSql
    )
}
finally {
    if (Test-Path -LiteralPath $tempSql) {
        Remove-Item -LiteralPath $tempSql -Force -ErrorAction SilentlyContinue
    }
}
