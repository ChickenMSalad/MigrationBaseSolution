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
    throw 'Unable to resolve validator script root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p7\P7.9C-Runtime-NoOp-Smoke-Provider.md',
    'src\Workers\Migration.Workers.ServiceBusExecutor\Smoke\RuntimeSmokeManifestProvider.cs',
    'src\Workers\Migration.Workers.ServiceBusExecutor\Smoke\RuntimeSmokeSourceConnector.cs',
    'src\Workers\Migration.Workers.ServiceBusExecutor\Smoke\RuntimeSmokeTargetConnector.cs',
    'src\Workers\Migration.Workers.ServiceBusExecutor\Smoke\RuntimeSmokeServiceCollectionExtensions.cs',
    'src\Workers\Migration.Workers.ServiceBusExecutor\runtime-smoke.mapping.json',
    'config-samples\runtime-smoke-job-definition.noop.sample.json',
    'tools\runtime\Apply-RuntimeSmokeProvider.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9C file is missing: {0}' -f $relativePath)
    }
}

$smokeRegistrationPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Smoke\RuntimeSmokeServiceCollectionExtensions.cs'
$smokeRegistrationText = Get-Content -LiteralPath $smokeRegistrationPath -Raw
foreach ($requiredTerm in @('RuntimeSmoke', 'IManifestProvider', 'IAssetSourceConnector', 'IAssetTargetConnector')) {
    if ($smokeRegistrationText.IndexOf($requiredTerm, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Smoke registration file is missing required term: {0}' -f $requiredTerm)
    }
}

$mappingPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\runtime-smoke.mapping.json'
$mapping = Get-Content -LiteralPath $mappingPath -Raw | ConvertFrom-Json
if ($mapping.profileName -ne 'RuntimeSmokeMapping') {
    throw 'Runtime smoke mapping profileName is incorrect.'
}
if ($mapping.sourceType -ne 'RuntimeSmoke') {
    throw 'Runtime smoke mapping sourceType is incorrect.'
}
if ($mapping.targetType -ne 'RuntimeSmoke') {
    throw 'Runtime smoke mapping targetType is incorrect.'
}

$payloadPath = Join-Path $repoRoot 'config-samples\runtime-smoke-job-definition.noop.sample.json'
$payload = Get-Content -LiteralPath $payloadPath -Raw | ConvertFrom-Json
if ($payload.manifestType -ne 'RuntimeSmoke' -or $payload.sourceType -ne 'RuntimeSmoke' -or $payload.targetType -ne 'RuntimeSmoke') {
    throw 'Runtime smoke sample payload must use RuntimeSmoke for manifest/source/target type.'
}
if ($payload.mappingProfilePath -ne 'runtime-smoke.mapping.json') {
    throw 'Runtime smoke sample payload must reference runtime-smoke.mapping.json.'
}

$programPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs'
if (Test-Path -LiteralPath $programPath) {
    $programText = Get-Content -LiteralPath $programPath -Raw
    if ($programText.IndexOf('AddRuntimeSmokeExecutionProviders', [System.StringComparison]::Ordinal) -lt 0) {
        Write-Warning 'Program.cs does not yet call AddRuntimeSmokeExecutionProviders. Run tools\runtime\Apply-RuntimeSmokeProvider.ps1.'
    }
}

$projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj'
if (Test-Path -LiteralPath $projectPath) {
    $projectText = Get-Content -LiteralPath $projectPath -Raw
    if ($projectText.IndexOf('runtime-smoke.mapping.json', [System.StringComparison]::Ordinal) -lt 0) {
        Write-Warning 'Executor csproj does not yet publish runtime-smoke.mapping.json. Run tools\runtime\Apply-RuntimeSmokeProvider.ps1.'
    }
    if ($projectText.IndexOf('PackageReference Include=', [System.StringComparison]::Ordinal) -ge 0 -and $projectText.IndexOf(' Version=', [System.StringComparison]::Ordinal) -ge 0) {
        throw 'Executor csproj appears to contain inline package versions.'
    }
}

$parserErrors = $null
foreach ($scriptRelativePath in @('tools\runtime\Apply-RuntimeSmokeProvider.ps1', 'tools\validate-p7.9c-runtime-noop-smoke-provider.ps1')) {
    $scriptPath = Join-Path $repoRoot $scriptRelativePath
    $tokens = $null
    $parserErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref] $tokens, [ref] $parserErrors) | Out-Null
    if ($null -ne $parserErrors -and @($parserErrors).Count -gt 0) {
        $message = (@($parserErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $scriptRelativePath, $message)
    }
}

Write-Host 'P7.9C runtime no-op smoke provider validation passed.'
