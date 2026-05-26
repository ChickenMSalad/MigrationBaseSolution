[CmdletBinding()]
param(
    [string]$RepoRoot,
    [Parameter(Mandatory=$true)]
    [string]$SetName,
    [switch]$FailOnIssue
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param([string]$RequestedRoot)

    if (-not [string]::IsNullOrWhiteSpace($RequestedRoot)) {
        return (Resolve-Path -LiteralPath $RequestedRoot).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
    }

    return (Resolve-Path -LiteralPath (Get-Location)).Path
}

function New-Issue {
    param([string]$RuleId, [string]$Message, [string]$Path)

    [pscustomobject]@{
        RuleId = $RuleId
        Message = $Message
        Path = $Path
    }
}

$root = Resolve-RepoRoot -RequestedRoot $RepoRoot
$issues = New-Object System.Collections.Generic.List[object]
$normalizedSetName = $SetName.ToUpperInvariant()
$setFileToken = $normalizedSetName.Replace('.', '.')

$docFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs\p7') -File -ErrorAction SilentlyContinue | Where-Object { $_.Name.ToUpperInvariant().Contains($normalizedSetName) })
if ($docFiles.Count -eq 0) {
    $issues.Add((New-Issue -RuleId 'SET001' -Message ('Missing docs/p7 document for {0}.' -f $SetName) -Path 'docs/p7')) | Out-Null
}

$validatorName = ('validate-{0}*.ps1' -f $SetName.ToLowerInvariant())
$validatorFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'tools') -File -Filter $validatorName -ErrorAction SilentlyContinue)
if ($validatorFiles.Count -eq 0) {
    $issues.Add((New-Issue -RuleId 'SET002' -Message ('Missing tools validator for {0}.' -f $SetName) -Path 'tools')) | Out-Null
}

foreach ($docFile in $docFiles) {
    $docText = Get-Content -LiteralPath $docFile.FullName -Raw
    foreach ($requiredTerm in @('Purpose', 'Usage')) {
        if ($docText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $issues.Add((New-Issue -RuleId 'SET003' -Message ('Documentation is missing section or term: {0}' -f $requiredTerm) -Path $docFile.FullName.Substring($root.Length).TrimStart('\', '/'))) | Out-Null
        }
    }
}

$result = @($issues.ToArray())

if ($FailOnIssue -and $result.Count -gt 0) {
    $summary = ($result | ForEach-Object { '{0} {1}: {2}' -f $_.RuleId, $_.Path, $_.Message }) -join [Environment]::NewLine
    throw ("Runtime cleanup set validation failed with {0} issue(s).{1}{2}" -f $result.Count, [Environment]::NewLine, $summary)
}

$result
