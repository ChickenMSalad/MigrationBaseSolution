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
    'docs\p7\P7.9I-Runtime-Deployment-Evidence-Handoff.md',
    'config-samples\runtime-deployment-evidence.template.json',
    'tools\runtime\New-RuntimeDeploymentEvidenceBundle.ps1',
    'tools\runtime\Test-RuntimeDeploymentEvidenceBundle.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9I file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\New-RuntimeDeploymentEvidenceBundle.ps1',
    'tools\runtime\Test-RuntimeDeploymentEvidenceBundle.ps1',
    'tools\validate-p7.9i-runtime-deployment-evidence-handoff.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref] $parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$scriptsToQualityCheck = @(
    'tools\runtime\New-RuntimeDeploymentEvidenceBundle.ps1',
    'tools\runtime\Test-RuntimeDeploymentEvidenceBundle.ps1'
)

$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
$unsafeXmlTokens = @('.PackageReference', '.None', '.Content', '.ItemGroup')

foreach ($relativeScript in $scriptsToQualityCheck) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw

    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }

    $colonMatches = [regex]::Matches($scriptText, '\$[A-Za-z_][A-Za-z0-9_]*:')
    foreach ($match in $colonMatches) {
        if ($match.Value -notmatch '^\$(script|global|local|private|using|env):$') {
            throw ('Potential fragile colon interpolation in {0}: {1}' -f $relativeScript, $match.Value)
        }
    }

    foreach ($unsafeXmlToken in $unsafeXmlTokens) {
        if ($scriptText.IndexOf($unsafeXmlToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
        }
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-deployment-evidence.template.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('environmentName', 'repoCommit', 'deploymentUtc', 'evidenceFiles')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Evidence template is missing property: {0}' -f $propertyName)
    }
}

foreach ($evidenceName in @('runtimeSnapshot', 'parityReport', 'sqlRunParentFkValidation', 'sqlBaselineReconciliationValidation', 'serviceBusQueueState', 'workItemState')) {
    if ($null -eq $config.evidenceFiles.PSObject.Properties[$evidenceName]) {
        throw ('Evidence template is missing evidenceFiles entry: {0}' -f $evidenceName)
    }
}

Write-Host 'P7.9I runtime deployment evidence handoff validation passed.'
