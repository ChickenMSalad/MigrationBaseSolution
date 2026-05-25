Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    return (Get-Location).Path
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p9\P9J-Azure-Resource-Provisioning-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]

function Add-Line {
    param([string]$Text)
    $lines.Add($Text) | Out-Null
}

function Add-FileSummary {
    param([string]$RelativePath, [string[]]$Patterns)
    Add-Line ""
    Add-Line "## $RelativePath"
    Add-Line ""
    $path = Join-Path $root $RelativePath
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

Add-Line '# P9J Azure Resource Provisioning Inventory'
Add-Line ""
Add-Line ("GeneratedUtc: {0}" -f ([DateTimeOffset]::UtcNow.ToString('o')))
Add-Line ""
Add-Line 'This inventory verifies repository-side Azure resource provisioning planning before actual cloud resource creation.'

Add-FileSummary 'docs\p9\P9J-Azure-Resource-Provisioning-Plan.md' @(
    'Required Azure resources',
    'Provision first. Deploy disabled second. Enable last.',
    'Do not configure a production RunId override.',
    'MIGRATION_ConnectionStrings__MigrationOperationalStore',
    'Success criteria'
)

Add-FileSummary 'config\templates\p9j-azure-resource-provisioning.template.json' @(
    'resourceGroupName',
    'MigrationOperationalStore',
    'serviceBus',
    'applicationInsightsName',
    'productionRunIdOverrideAllowed'
)

Add-Line ""
Add-Line '## Next human actions'
Add-Line ""
Add-Line '- Choose Azure subscription, resource group, and region.'
Add-Line '- Provision Azure SQL and Service Bus.'
Add-Line '- Configure Application Insights or Azure Monitor connection string.'
Add-Line '- Deploy apps disabled first.'
Add-Line '- Enable workers only after SQL and Service Bus validation.'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
