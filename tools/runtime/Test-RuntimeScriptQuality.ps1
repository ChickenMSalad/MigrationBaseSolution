[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string[]]$RelativePaths = @('tools', 'deploy'),
    [switch]$FailOnIssue,
    [string]$OutputJsonPath
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

function Test-IsBuildOutputPath {
    param([string]$Path)

    $segments = @($Path -split '[\\/]')
    return (($segments -contains 'bin') -or ($segments -contains 'obj'))
}

function New-Issue {
    param(
        [string]$RuleId,
        [string]$Path,
        [int]$LineNumber,
        [string]$Message,
        [string]$Line
    )

    [pscustomobject]@{
        RuleId = $RuleId
        Path = $Path
        LineNumber = $LineNumber
        Message = $Message
        Line = $Line
    }
}

$root = Resolve-RepoRoot -RequestedRoot $RepoRoot
$issues = New-Object System.Collections.Generic.List[object]
$scopedVariablePattern = '\$(script|global|local|private|using|env):'
$fragileColonPattern = '\$[A-Za-z_][A-Za-z0-9_]*:'
$legacyInvocationPattern = '\$MyInvocation\.' + 'ScriptName'
$buildOutputBackslashPattern = '\\' + '(bin|obj)' + '\\'
$buildOutputPipePattern = 'bin' + '\\\\|' + '\\\\' + 'obj'
$secretWord = 'Pass' + 'word'
$writeHostWord = 'Write' + '-Host'
$throwWord = 'thr' + 'ow'
$secretExposurePattern = ('{0}.*{1}|{1}.*{0}|{2}.*{0}' -f $secretWord, $writeHostWord, $throwWord)

foreach ($relativePath in $RelativePaths) {
    $scanRoot = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $scanRoot)) {
        continue
    }

    $scripts = @(Get-ChildItem -LiteralPath $scanRoot -Recurse -File -Filter '*.ps1' | Where-Object { -not (Test-IsBuildOutputPath -Path $_.FullName) })

    foreach ($scriptFile in $scripts) {
        $relativeScriptPath = $scriptFile.FullName.Substring($root.Length).TrimStart('\', '/')
        $lines = @(Get-Content -LiteralPath $scriptFile.FullName)

        for ($index = 0; $index -lt $lines.Count; $index++) {
            $line = [string]$lines[$index]
            $lineNumber = $index + 1

            $colonMatches = [System.Text.RegularExpressions.Regex]::Matches($line, $fragileColonPattern)
            foreach ($match in $colonMatches) {
                $value = $match.Value
                if ($value -match $scopedVariablePattern) {
                    continue
                }

                $issues.Add((New-Issue -RuleId 'PS001' -Path $relativeScriptPath -LineNumber $lineNumber -Message 'Potential fragile PowerShell colon interpolation. Use ${name}: or format strings.' -Line $line)) | Out-Null
            }

            if ($line -match $legacyInvocationPattern) {
                $issues.Add((New-Issue -RuleId 'PS002' -Path $relativeScriptPath -LineNumber $lineNumber -Message 'Avoid legacy invocation variables; use PSScriptRoot with a fallback.' -Line $line)) | Out-Null
            }

            if (($line -match $buildOutputBackslashPattern) -or ($line -match $buildOutputPipePattern)) {
                $issues.Add((New-Issue -RuleId 'PS003' -Path $relativeScriptPath -LineNumber $lineNumber -Message 'Avoid brittle build-output regex path checks; use path segments.' -Line $line)) | Out-Null
            }

            if ($line -match $secretExposurePattern) {
                $issues.Add((New-Issue -RuleId 'PS004' -Path $relativeScriptPath -LineNumber $lineNumber -Message 'Potential secret exposure in console output or exception text.' -Line $line)) | Out-Null
            }
        }
    }
}

$result = @($issues.ToArray())

if (-not [string]::IsNullOrWhiteSpace($OutputJsonPath)) {
    $json = $result | ConvertTo-Json -Depth 5
    if ([string]::IsNullOrWhiteSpace($json)) {
        $json = '[]'
    }
    Set-Content -LiteralPath $OutputJsonPath -Value $json -Encoding UTF8
}

if ($FailOnIssue -and $result.Count -gt 0) {
    $summary = ($result | Select-Object -First 10 | ForEach-Object { '{0}:{1} {2} {3}' -f $_.Path, $_.LineNumber, $_.RuleId, $_.Message }) -join [Environment]::NewLine
    throw ("Runtime script quality gate failed with {0} issue(s). First issues:{1}{2}" -f $result.Count, [Environment]::NewLine, $summary)
}

$result
