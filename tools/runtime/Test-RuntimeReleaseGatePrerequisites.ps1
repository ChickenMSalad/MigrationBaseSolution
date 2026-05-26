[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [Parameter(Mandatory = $false)]
    [string] $ConfigurationPath = (Join-Path $RepoRoot 'config-samples\runtime-release-gates.sample.json')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Add-GateIssue {
    param(
        [Parameter(Mandatory = $true)] [string] $Severity,
        [Parameter(Mandatory = $true)] [string] $Code,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    $script:gateIssues += [pscustomobject]@{
        Severity = $Severity
        Code = $Code
        Message = $Message
    }
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Configuration file not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Configuration file is empty: $Path"
    }

    return $content | ConvertFrom-Json
}

$script:gateIssues = @()
$config = Read-JsonFile -Path $ConfigurationPath

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    Add-GateIssue -Severity 'Error' -Code 'repo.root.missing' -Message "Repo root not found: $RepoRoot"
}

$gitDir = Join-Path $RepoRoot '.git'
if (-not (Test-Path -LiteralPath $gitDir)) {
    Add-GateIssue -Severity 'Warning' -Code 'repo.git.missing' -Message 'Repo root does not contain a .git directory.'
}

if ($config.PSObject.Properties.Name -contains 'requiredScripts') {
    foreach ($scriptPath in @($config.requiredScripts)) {
        $fullPath = Join-Path $RepoRoot $scriptPath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            Add-GateIssue -Severity 'Error' -Code 'required.script.missing' -Message "Required validator script is missing: $scriptPath"
        }
    }
}

if ($config.PSObject.Properties.Name -contains 'recommendedInputs') {
    foreach ($inputPath in @($config.recommendedInputs)) {
        $fullInputPath = Join-Path $RepoRoot $inputPath
        if (-not (Test-Path -LiteralPath $fullInputPath)) {
            Add-GateIssue -Severity 'Warning' -Code 'recommended.input.missing' -Message "Recommended release-gate input is missing: $inputPath"
        }
    }
}

[pscustomobject]@{
    RepoRoot = $RepoRoot
    ConfigurationPath = $ConfigurationPath
    CheckedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    Issues = @($script:gateIssues)
}
