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
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot 'artifacts\runtime-smoke'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot ('noop-smoke-state-{0}.txt' -f $RunId)
}

$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = Join-Path $repoRoot $outputFullPath
}

$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$query = @"
SELECT TOP (20)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = '$RunId'
ORDER BY WorkItemId DESC;
"@

$tempSql = [System.IO.Path]::GetTempFileName()
try {
    Set-Content -LiteralPath $tempSql -Value $query -Encoding UTF8

    $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
    $sqlcmdPath = $sqlcmd.Source
    $serverName = $SqlServer
    if ($serverName -notmatch '\.database\.windows\.net$') {
        $serverName = ('{0}.database.windows.net' -f $serverName)
    }

    $arguments = @(
        '-S', $serverName,
        '-d', $Database,
        '-U', $SqlAdmin,
        '-P', $SqlPasswordPlain,
        '-i', $tempSql,
        '-o', $outputFullPath
    )

    $process = Start-Process -FilePath $sqlcmdPath -ArgumentList $arguments -NoNewWindow -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw ('Runtime NoOp smoke state query failed with exit code {0}.' -f $process.ExitCode)
    }
}
finally {
    if (Test-Path -LiteralPath $tempSql) {
        Remove-Item -LiteralPath $tempSql -Force -ErrorAction SilentlyContinue
    }
}

Get-Content -LiteralPath $outputFullPath
Write-Host ('Runtime NoOp smoke state written to {0}' -f $outputFullPath)
