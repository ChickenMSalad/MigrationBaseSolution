Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-Line {
    param([string]$Text)
    $script:Lines.Add($Text) | Out-Null
}

function Add-FileSummary {
    param(
        [string]$Title,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    Add-Line ""
    Add-Line "## $RelativePath"
    Add-Line ""

    $path = Join-Path $script:Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line 'Missing.'
        return
    }

    Add-Line 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line ("- Contains: {0}" -f $pattern)
        }
        else {
            Add-Line ("- Missing: {0}" -f $pattern)
        }
    }
}

$script:Root = Get-RepositoryRoot
$script:Lines = New-Object System.Collections.Generic.List[string]

$outDir = Join-Path $script:Root 'docs\p9'
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}
$out = Join-Path $outDir 'P9D-Sql-Operational-Store-Cloud-Validation-Inventory.generated.md'

Add-Line '# P9D SQL Operational Store Cloud Validation Inventory'
Add-Line ''
Add-Line ("GeneratedUtc: {0}" -f ([DateTimeOffset]::UtcNow.ToString('o')))
Add-Line ''
Add-Line 'This inventory verifies the repository-side cloud SQL operational store validation surfaces before Azure SQL smoke execution.'

Add-FileSummary -Title 'Runbook' -RelativePath 'docs\p9\P9D-Sql-Operational-Store-Cloud-Validation.md' -Patterns @(
    'ConnectionStrings:MigrationOperationalStore',
    'MIGRATION_ConnectionStrings__MigrationOperationalStore',
    'RunId is uniqueidentifier / Guid',
    'WorkItemId is bigint / long'
)

Add-FileSummary -Title 'SQL inspection script' -RelativePath 'scripts\sql\P9D-InspectOperationalStore.sql' -Patterns @(
    'sys.tables',
    'sys.columns',
    'sys.foreign_keys',
    'sys.indexes',
    'ApproximateRows'
)

Add-FileSummary -Title 'Cloud settings template' -RelativePath 'config\templates\p9d-sql-operational-store-cloud-settings.template.json' -Patterns @(
    'MigrationOperationalStore',
    'OpenTelemetry',
    'SqlOperationalWorker',
    'SqlOperationalQueueExecutor'
)

Add-FileSummary -Title 'SQL worker host' -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddEnvironmentVariables(prefix: "MIGRATION_")'
)

Add-Line ''
Add-Line '## Recommended next checks'
Add-Line ''
Add-Line '- Run scripts/sql/P9D-InspectOperationalStore.sql against the target operational database.'
Add-Line '- Confirm WorkItemId columns are bigint / long and RunId remains uniqueidentifier / Guid.'
Add-Line '- Confirm production cloud settings do not require a RunId override.'

Set-Content -LiteralPath $out -Value $script:Lines -Encoding UTF8
Write-Host "Wrote $out"
