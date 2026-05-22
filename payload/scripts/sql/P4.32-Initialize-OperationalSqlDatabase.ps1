[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "MigrationBaseSolution_Operational",
    [string]$ScriptsPath = "database/sql/operational",
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.32-SQL] {0}" -f $Message)
}

function Invoke-SqlCommand {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $CommandText
        $null = $command.ExecuteNonQuery()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScalar {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $CommandText
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$resolvedScriptsPath = Join-Path $repoRoot $ScriptsPath

if (-not (Test-Path -LiteralPath $resolvedScriptsPath)) {
    throw ("Operational SQL script folder not found: {0}" -f $resolvedScriptsPath)
}

$masterConnectionString = "Server=$ServerInstance;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
$databaseConnectionString = "Server=$ServerInstance;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;"

$createDatabaseSql = @"
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    CREATE DATABASE [$DatabaseName];
END
"@

if (-not $Apply) {
    Write-Step ("WOULD ensure database exists: {0} on {1}" -f $DatabaseName, $ServerInstance)
}
else {
    Write-Step ("Ensuring database exists: {0} on {1}" -f $DatabaseName, $ServerInstance)
    Invoke-SqlCommand -ConnectionString $masterConnectionString -CommandText $createDatabaseSql
}

$sqlFiles = @(Get-ChildItem -LiteralPath $resolvedScriptsPath -Filter "*.sql" -File | Sort-Object Name)

if ($sqlFiles.Count -eq 0) {
    throw ("No SQL files found in {0}" -f $resolvedScriptsPath)
}

foreach ($file in $sqlFiles) {
    if (-not $Apply) {
        Write-Step ("WOULD apply {0}" -f $file.FullName)
        continue
    }

    Write-Step ("Applying {0}" -f $file.Name)
    $sql = Get-Content -LiteralPath $file.FullName -Raw

    $batches = [regex]::Split($sql, "(?im)^\s*GO\s*;?\s*$") |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($batch in $batches) {
        Invoke-SqlCommand -ConnectionString $databaseConnectionString -CommandText $batch
    }
}

if ($Apply) {
    Write-Step "Operational SQL database bootstrap completed."
}
else {
    Write-Step "Preview completed. Rerun with -Apply to create/apply."
}
