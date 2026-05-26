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
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusNamespace,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusQueue,

    [Parameter(Mandatory = $false)]
    [int] $Top = 10
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

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

$serverName = if ($SqlServer.EndsWith('.database.windows.net')) { $SqlServer } else { "$SqlServer.database.windows.net" }

Write-Host 'Service Bus queue counts:'
Invoke-CheckedProcess -FilePath 'az' -Arguments @(
    'servicebus', 'queue', 'show',
    '--resource-group', $ResourceGroup,
    '--namespace-name', $ServiceBusNamespace,
    '--name', $ServiceBusQueue,
    '--query', '{MessageCount:messageCount,Active:countDetails.activeMessageCount,DeadLetter:countDetails.deadLetterMessageCount}',
    '-o', 'table'
)

Write-Host ''
Write-Host 'Latest runtime work items:'
$query = @"
SELECT TOP ($Top)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    DispatchedAtUtc,
    CompletedAtUtc,
    LastErrorCode,
    LastErrorMessage,
    CreatedAtUtc,
    UpdatedAtUtc
FROM migration.WorkItems
ORDER BY WorkItemId DESC;
"@

Invoke-CheckedProcess -FilePath 'sqlcmd' -Arguments @(
    '-S', $serverName,
    '-d', $Database,
    '-U', $SqlAdmin,
    '-P', $SqlPasswordPlain,
    '-Q', $query
)
