[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p7\P7.11A-Workspace-Artifact-Hygiene.md',
    'config-samples\runtime-workspace-artifact-hygiene.sample.json',
    'tools\runtime\New-RuntimeWorkspaceArtifactInventory.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.11A file is missing: {0}' -f $relativePath)
    }
}

$scriptPath = Join-Path $repoRoot 'tools\runtime\New-RuntimeWorkspaceArtifactInventory.ps1'
$parseErrors = $null
[System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
    $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
    throw ('PowerShell parser errors in runtime inventory script: {0}' -f $message)
}

$scriptText = Get-Content -LiteralPath $scriptPath -Raw
$forbiddenInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
if ($scriptText.IndexOf($forbiddenInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime inventory script uses forbidden invocation-root pattern.'
}

if ($scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime inventory script contains StrictMode-unsafe XML property examples.'
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-workspace-artifact-hygiene.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('artifactRoots', 'publishRoots', 'evidenceRoots', 'excludePathFragments')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample config is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.11A workspace artifact hygiene validation passed.'
