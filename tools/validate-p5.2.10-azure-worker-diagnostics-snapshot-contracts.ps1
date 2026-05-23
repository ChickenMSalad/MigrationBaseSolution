Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected file: ${RelativePath}"
    }
}

function Test-PathIsGenerated {
    param([Parameter(Mandatory=$true)][string]$Path)
    return ($Path -match '[\\/]bin[\\/]' -or $Path -match '[\\/]obj[\\/]')
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Diagnostics\AzureWorkerDiagnosticSnapshot.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Diagnostics\AzureWorkerDiagnosticSnapshotStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Diagnostics\AzureWorkerDiagnosticSignal.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Diagnostics\AzureWorkerDiagnosticSnapshotOptions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Diagnostics\IAzureWorkerDiagnosticSnapshotProvider.cs',
    'config\azure-runtime\workers\diagnostics.snapshot.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists -RelativePath $file }

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    -not (Test-PathIsGenerated -Path $_.FullName)
})

$inlineVersionViolations = @()
foreach ($project in $projectFiles) {
    $content = Get-Content -LiteralPath $project.FullName -Raw

    $hasInlineVersionAttribute = [regex]::IsMatch($content, '<PackageReference\b[^>]*\bVersion\s*=')
    $hasInlineVersionElement = [regex]::IsMatch($content, '<PackageReference\b[\s\S]*?<Version>')

    if ($hasInlineVersionAttribute -or $hasInlineVersionElement) {
        $inlineVersionViolations += $project.FullName
    }
}

if (@($inlineVersionViolations).Count -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + ((@($inlineVersionViolations) | Sort-Object -Unique) -join "`n - "))
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.2.10 Azure worker diagnostics snapshot contract validation passed.'
