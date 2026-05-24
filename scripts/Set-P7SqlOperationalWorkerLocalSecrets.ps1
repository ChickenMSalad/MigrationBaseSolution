Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [bool]$EnableWorker = $true,

    [bool]$CompleteNoOpWorkItems = $true,

    [bool]$EnableMigrationJobExecutor = $false,

    [int]$BatchSize = 10,

    [int]$LeaseSeconds = 300,

    [string]$WorkerId = 'local-dev-sql-operational-worker-01'
)

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

$root = Get-RepositoryRoot
$project = Join-Path $root 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'

if (-not (Test-Path -LiteralPath $project)) {
    throw "SQL operational worker host project was not found: $project"
}

function Set-SecretValue {
    param([string]$Name, [string]$Value)

    & dotnet user-secrets set $Name $Value --project $project | Write-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet user-secrets failed for '$Name'."
    }
}

Set-SecretValue -Name 'ConnectionStrings:MigrationOperationalStore' -Value $ConnectionString
Set-SecretValue -Name 'SqlOperationalWorker:Enabled' -Value ([string]$EnableWorker).ToLowerInvariant()
Set-SecretValue -Name 'SqlOperationalWorker:WorkerId' -Value $WorkerId
Set-SecretValue -Name 'SqlOperationalWorker:BatchSize' -Value ([string]$BatchSize)
Set-SecretValue -Name 'SqlOperationalWorker:LeaseSeconds' -Value ([string]$LeaseSeconds)
Set-SecretValue -Name 'SqlOperationalWorker:CompleteNoOpWorkItems' -Value ([string]$CompleteNoOpWorkItems).ToLowerInvariant()
Set-SecretValue -Name 'SqlOperationalMigrationJobExecutor:Enabled' -Value ([string]$EnableMigrationJobExecutor).ToLowerInvariant()

Write-Host 'P7 SQL operational worker local user secrets updated.'
