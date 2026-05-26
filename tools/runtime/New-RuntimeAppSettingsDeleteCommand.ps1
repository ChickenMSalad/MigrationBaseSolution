[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Path,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string] $InputPath)

    if (-not (Test-Path -LiteralPath $InputPath)) {
        throw "Appsettings JSON file was not found: $InputPath"
    }

    return (Get-Content -LiteralPath $InputPath -Raw) | ConvertFrom-Json
}

$stalePrefixes = @(
    'MIGRATION_ServiceBusDispatcher__',
    'MIGRATION_ServiceBusExecutor__',
    'MIGRATION_SqlWorkItemDispatcher__',
    'MIGRATION_SqlServiceBusExecutor__',
    'MIGRATION_SqlOperationalWorkItemQueue__',
    'OperationalStore__Sql__'
)

$staleExact = @(
    'MIGRATION_ConnectionStrings__MigrationOperationStore'
)

$items = Read-JsonFile -InputPath $Path
$namesToDelete = @()
foreach ($item in @($items)) {
    if ($null -eq $item -or -not $item.PSObject.Properties['name']) {
        continue
    }

    $name = [string]$item.name
    if ([string]::IsNullOrWhiteSpace($name)) {
        continue
    }

    if ($staleExact -contains $name) {
        $namesToDelete += $name
        continue
    }

    foreach ($prefix in $stalePrefixes) {
        if ($name.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $namesToDelete += $name
            break
        }
    }
}

$namesToDelete = @($namesToDelete | Sort-Object -Unique)
if ($namesToDelete.Count -eq 0) {
    $text = '# No stale runtime appsettings were found for deletion.'
}
else {
    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add('# Review before running. Do not delete settings until the app has passed runtime smoke using canonical keys.')
    [void]$lines.Add('az webapp config appsettings delete `')
    [void]$lines.Add('  --resource-group $resourceGroup `')
    if ($Role -eq 'Dispatcher') {
        [void]$lines.Add('  --name $dispatcherApp `')
    }
    else {
        [void]$lines.Add('  --name $executorApp `')
    }
    [void]$lines.Add('  --setting-names `')

    for ($i = 0; $i -lt $namesToDelete.Count; $i++) {
        $suffix = if ($i -lt ($namesToDelete.Count - 1)) { ' `' } else { '' }
        [void]$lines.Add(('    "{0}"{1}' -f $namesToDelete[$i], $suffix))
    }
    $text = ($lines -join [Environment]::NewLine)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $text
}
else {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $text -Encoding UTF8
    Write-Host "Wrote command to $OutputPath"
}
