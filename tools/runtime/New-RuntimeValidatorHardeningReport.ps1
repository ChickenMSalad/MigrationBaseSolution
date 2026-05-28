[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Path,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath,

    [Parameter(Mandatory = $false)]
    [string[]] $ExcludePathFragment = @()
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve runtime script root.'
}

$qualityScript = Join-Path $scriptRoot 'Test-RuntimePowerShellScriptQuality.ps1'
if (-not (Test-Path -LiteralPath $qualityScript)) {
    throw ('Required quality script is missing: {0}' -f $qualityScript)
}

$result = & $qualityScript -Path $Path -ExcludePathFragment $ExcludePathFragment -PassThru

$outputFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Runtime Validator Hardening Report') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('- Generated UTC: {0}' -f $result.CheckedAtUtc)) | Out-Null
$lines.Add(('- Scripts checked: {0}' -f $result.ScriptCount)) | Out-Null
$lines.Add(('- Issues found: {0}' -f $result.IssueCount)) | Out-Null
$lines.Add('') | Out-Null

if ($result.IssueCount -eq 0) {
    $lines.Add('No script quality issues were detected in the requested scope.') | Out-Null
}
else {
    $lines.Add('## Issues') | Out-Null
    $lines.Add('') | Out-Null
    foreach ($issue in $result.Issues) {
        $lines.Add(('- `{0}` — **{1}** — {2}' -f $issue.Path, $issue.Rule, $issue.Message)) | Out-Null
    }
}

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime validator hardening report written to {0}' -f $outputFullPath)
