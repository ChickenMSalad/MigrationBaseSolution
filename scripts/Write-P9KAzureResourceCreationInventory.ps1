Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    return (Get-Location).Path
}

function Add-Line {
    param([System.Collections.Generic.List[string]]$Lines, [string]$Text)
    $Lines.Add($Text) | Out-Null
}

function Add-FileSummary {
    param(
        [string]$RootPath,
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    Add-Line $Lines ""
    Add-Line $Lines "## $RelativePath"
    Add-Line $Lines ""

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line $Lines "Missing."
        return
    }

    Add-Line $Lines "Present."
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line $Lines ("- Contains: {0}" -f $pattern)
        }
        else {
            Add-Line $Lines ("- Missing: {0}" -f $pattern)
        }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'P9K-Azure-Resource-Creation-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]

Add-Line $lines '# P9K Azure Resource Creation Inventory'
Add-Line $lines ''
Add-Line $lines ("GeneratedUtc: {0}" -f ([DateTimeOffset]::UtcNow.ToString('o')))
Add-Line $lines ''
Add-Line $lines 'This inventory verifies repository-side Azure resource creation planning before actual resource provisioning.'

Add-FileSummary -RootPath $root -Lines $lines -RelativePath 'docs\p9\P9K-Azure-Resource-Creation-Plan.md' -Patterns @(
    'Required Azure resources',
    'Provision first. Deploy disabled second. Enable last.',
    'Do not configure a production RunId override.',
    'Success criteria'
)

Add-FileSummary -RootPath $root -Lines $lines -RelativePath 'docs\p9\P9K-Azure-Cli-Resource-Creation-Runbook.md' -Patterns @(
    'az group create',
    'az servicebus namespace create',
    'az servicebus queue create',
    'az monitor app-insights component create'
)

Add-FileSummary -RootPath $root -Lines $lines -RelativePath 'config\templates\p9k-azure-resource-creation.template.json' -Patterns @(
    'resourceGroupName',
    'MigrationOperationalStore',
    'serviceBus',
    'applicationInsightsName',
    'productionRunIdOverrideAllowed'
)

Add-Line $lines ''
Add-Line $lines '## Next human actions'
Add-Line $lines ''
Add-Line $lines '- Choose Azure subscription, resource group, and region.'
Add-Line $lines '- Review docs/p9/P9K-Azure-Cli-Resource-Creation-Runbook.md.'
Add-Line $lines '- Provision Azure resources manually with reviewed Azure CLI commands.'
Add-Line $lines '- Keep workers disabled until SQL and Service Bus validation pass.'

Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
