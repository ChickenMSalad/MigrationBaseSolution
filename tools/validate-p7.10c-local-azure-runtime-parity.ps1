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
if ([string]::IsNullOrWhiteSpace($toolRoot)) { throw 'Unable to resolve validator root.' }

$repoRoot = Split-Path -Parent $toolRoot
$requiredFiles = @(
    'docs\p7\P7.10C-Local-Azure-Runtime-Parity.md',
    'config-samples\runtime-local-azure-parity.sample.json',
    'tools\runtime\Export-RuntimeSqlObjectSnapshot.ps1',
    'tools\runtime\Compare-RuntimeSqlObjectSnapshot.ps1',
    'tools\runtime\New-RuntimeLocalAzureParityReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10C file is missing: {0}' -f $relativePath)
    }
}

$scriptRelativePaths = @(
    'tools\runtime\Export-RuntimeSqlObjectSnapshot.ps1',
    'tools\runtime\Compare-RuntimeSqlObjectSnapshot.ps1',
    'tools\runtime\New-RuntimeLocalAzureParityReport.ps1'
)

foreach ($relativeScript in $scriptRelativePaths) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    $fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples\runtime-local-azure-parity.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('local', 'azure', 'requiredObjects', 'requiredForeignKeys')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Parity sample config missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.10C local/Azure runtime parity validation passed.'
