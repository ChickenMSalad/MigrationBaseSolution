Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        $scriptDirectory = (Get-Location).Path
    }
    else {
        $scriptDirectory = Split-Path -Parent $scriptPath
    }
}

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

$misplacedSettingsPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\Settings'
if (Test-Path $misplacedSettingsPath) {
    Write-Host "Removing misplaced P5.1.10 settings folder: $misplacedSettingsPath"
    Remove-Item -LiteralPath $misplacedSettingsPath -Recurse -Force
}
else {
    Write-Host 'No misplaced P5.1.10 settings folder found.'
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\IAzureAppSettingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingRequirement.cs',
    'config\azure-runtime\app-settings\app-settings.registry.sample.json'
)

$missing = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        [void]$missing.Add($relativePath)
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'P5.1.10 cleanup completed, but expected files are still missing:'
    foreach ($item in $missing) { Write-Host " - $item" }
    throw 'P5.1.10 expected files missing after cleanup.'
}

Write-Host 'P5.1.10 app settings registry cleanup complete.'
