[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Path,

    [Parameter(Mandatory = $false)]
    [string[]] $ExcludePathFragment = @(),

    [Parameter(Mandatory = $false)]
    [switch] $PassThru
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-QualityTargetScript {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $InputPath,

        [Parameter(Mandatory = $false)]
        [string[]] $ExcludedFragments = @()
    )

    $resolved = @()

    foreach ($candidate in @($InputPath)) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (-not (Test-Path -LiteralPath $candidate)) {
            throw ('Script quality target path does not exist: {0}' -f $candidate)
        }

        $item = Get-Item -LiteralPath $candidate
        if ($item.PSIsContainer) {
            $resolved += @(Get-ChildItem -LiteralPath $item.FullName -Recurse -File -Filter '*.ps1')
        }
        elseif ($item.Name -like '*.ps1') {
            $resolved += @($item)
        }
    }

    $filtered = @()

    foreach ($script in @($resolved | Sort-Object FullName -Unique)) {
        $fullName = $script.FullName
        $isExcluded = $false

        foreach ($fragment in @($ExcludedFragments)) {
            if (-not [string]::IsNullOrWhiteSpace($fragment) -and
                $fullName.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $isExcluded = $true
                break
            }
        }

        if (-not $isExcluded) {
            $filtered += @($script)
        }
    }

    return @($filtered)
}

function New-QualityIssue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Rule,

        [Parameter(Mandatory = $true)]
        [string] $Message
    )

    return [pscustomobject]@{
        Path = $Path
        Rule = $Rule
        Message = $Message
    }
}

$scripts = Resolve-QualityTargetScript `
    -InputPath @($Path) `
    -ExcludedFragments @($ExcludePathFragment)

$issues = @()

$badInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
$badXmlPropertyPatterns = @(
    '.' + 'PackageReference',
    '.' + 'ItemGroup',
    '.' + 'None',
    '.' + 'Content',
    '.' + 'Reference',
    '.' + 'ProjectReference'
)
$inlinePackageReferenceVersionPattern = 'PackageReference Version='
$binObjPattern = '\\' + 'bin' + '\\|' + '\\' + 'obj' + '\\'
$secretEchoPatterns = @(
    'Write-Host' + ' ' + '$Arguments',
    'Write-Output' + ' ' + '$Arguments',
    'throw' + ' ' + '$Arguments',
    'Exception' + ' ' + '$Arguments'
)
$allowedScopedVariablePattern = '^\$(script|global|local|private|using|env):$'

foreach ($script in @($scripts)) {
    $scriptText = Get-Content -LiteralPath $script.FullName -Raw

    if ($scriptText.IndexOf($badInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidFragileInvocationRoot' -Message 'Avoid fragile invocation-root usage. Use PSScriptRoot with a PSCommandPath fallback.')
    }

    $colonMatches = [System.Text.RegularExpressions.Regex]::Matches($scriptText, '\$[A-Za-z_][A-Za-z0-9_]*:')
    foreach ($match in @($colonMatches)) {
        $value = $match.Value
        if ($value -notmatch $allowedScopedVariablePattern) {
            $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidFragileColonInterpolation' -Message ('Potential fragile colon interpolation token: {0}' -f $value))
        }
    }

    foreach ($xmlPattern in @($badXmlPropertyPatterns)) {
        if ($scriptText.IndexOf($xmlPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidStrictModeUnsafeXmlPropertyAccess' -Message ('Potential direct XML property access token: {0}. Prefer PSObject.Properties checks.' -f $xmlPattern))
        }
    }

    if ($scriptText.IndexOf($inlinePackageReferenceVersionPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidInlinePackageReferenceVersion' -Message 'Do not add inline PackageReference Version attributes. Use central package management.')
    }

    if ($scriptText -match $binObjPattern) {
        $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidBinObjRegexPathChecks' -Message 'Avoid brittle bin/obj regex path checks. Prefer path-segment filtering.')
    }

    foreach ($secretPattern in @($secretEchoPatterns)) {
        if ($scriptText.IndexOf($secretPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $issues += @(New-QualityIssue -Path $script.FullName -Rule 'AvoidSecretEcho' -Message 'Avoid printing process arguments or exception arguments that may contain secrets.')
        }
    }
}

$result = [pscustomobject]@{
    CheckedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    ScriptCount = @($scripts).Count
    IssueCount = @($issues).Count
    Issues = @($issues)
}

if ($PassThru) {
    return $result
}

if ($result.IssueCount -gt 0) {
    $lines = @('Runtime PowerShell script quality validation failed.')
    foreach ($issue in @($result.Issues)) {
        $lines += ('{0}: {1} - {2}' -f $issue.Path, $issue.Rule, $issue.Message)
    }
    throw ($lines -join [Environment]::NewLine)
}

Write-Host ('Runtime PowerShell script quality validation passed. Scripts checked: {0}' -f $result.ScriptCount)
