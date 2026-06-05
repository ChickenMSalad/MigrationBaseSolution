Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        $candidate = Resolve-Path -Path (Join-Path $PSScriptRoot '..\..\..')
        return $candidate.Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepositoryRoot
$requiredFiles = @(
    [pscustomobject]@{ Path = 'src\Admin\Migration.Admin.Web\package.json'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'src\Admin\Migration.Admin.Web\vite.config.ts'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'tools\p10\P10.2CE\Run-P10.2CE-AdminApi.ps1'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'tools\p10\P10.2CE\Run-P10.2CE-AdminWeb.ps1'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'tools\p10\P10.2CE\Run-P10.2CE-LocalStackSmoke.ps1'; Kind = 'Leaf' },
    [pscustomobject]@{ Path = 'docs\P10\P10.2CE-AdminWebLocalStackOrchestration.Report.md'; Kind = 'Leaf' }
)

foreach ($item in $requiredFiles) {
    $path = Join-Path $repoRoot $item.Path
    if ($item.Kind -eq 'Leaf') {
        if (-not (Test-Path -Path $path -PathType Leaf)) {
            throw ('Required file was not found: {0}' -f $path)
        }
    }
}

$runScripts = @(
    'tools\p10\P10.2CE\Run-P10.2CE-AdminApi.ps1',
    'tools\p10\P10.2CE\Run-P10.2CE-AdminWeb.ps1',
    'tools\p10\P10.2CE\Run-P10.2CE-LocalStackSmoke.ps1'
)

foreach ($relativeScript in $runScripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $content = Get-Content -Path $scriptPath -Raw
    if ($content -match '\.tsx[''\"]') {
        throw ('Extension-bearing TSX import-like text found in script: {0}' -f $scriptPath)
    }
}

Write-Host 'P10.2CE Admin Web local stack orchestration validation passed.'
