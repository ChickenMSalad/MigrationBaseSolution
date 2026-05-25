Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    return (Get-Location).Path
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Resource-Creation-Plan.md'
Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Cli-Resource-Creation-Runbook.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9k-azure-resource-creation.template.json'
Assert-PathExists -RootPath $root -RelativePath 'scripts\azure\P9K-AzureResourceCreationTemplate.ps1'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Resource-Creation-Plan.md' -Text 'Provision first. Deploy disabled second. Enable last.'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Resource-Creation-Plan.md' -Text 'Do not configure a production RunId override.'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Cli-Resource-Creation-Runbook.md' -Text 'az servicebus namespace create'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9K-Azure-Cli-Resource-Creation-Runbook.md' -Text 'az monitor app-insights component create'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9k-azure-resource-creation.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9k-azure-resource-creation.template.json' -Text 'productionRunIdOverrideAllowed'

Write-Host 'P9K Azure resource creation plan validation passed.'
