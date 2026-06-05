param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxGetProbes = 100
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    $left = $BaseUrl.TrimEnd('/')
    $right = $Path.TrimStart('/')
    return ('{0}/{1}' -f $left, $right)
}

function Add-Detail {
    param(
        [System.Collections.Generic.List[object]]$Details,
        [string]$Method,
        [string]$Path,
        [string]$Source,
        [string]$Result,
        [string]$Classification,
        [string]$StatusCode,
        [string]$Error
    )

    [void]$Details.Add([pscustomobject]@{
        Method = $Method
        Path = $Path
        Source = $Source
        Result = $Result
        Classification = $Classification
        StatusCode = $StatusCode
        Error = $Error
    })
}

function Get-QuotedLiterals {
    param(
        [string]$Line
    )

    $values = New-Object 'System.Collections.Generic.List[string]'

    if ([string]::IsNullOrEmpty($Line)) {
        return $values
    }

    $quote = [char]0
    $buffer = New-Object 'System.Text.StringBuilder'
    $inQuote = $false
    $escaped = $false

    for ($i = 0; $i -lt $Line.Length; $i++) {
        $ch = $Line[$i]

        if ($inQuote) {
            if ($escaped) {
                [void]$buffer.Append($ch)
                $escaped = $false
                continue
            }

            if ($ch -eq '\') {
                $escaped = $true
                continue
            }

            if ($ch -eq $quote) {
                [void]$values.Add($buffer.ToString())
                [void]$buffer.Clear()
                $inQuote = $false
                $quote = [char]0
                continue
            }

            [void]$buffer.Append($ch)
            continue
        }

        if (($ch -eq "'") -or ($ch -eq '"')) {
            $inQuote = $true
            $quote = $ch
            [void]$buffer.Clear()
        }
    }

    return $values
}

function Get-MethodForLine {
    param(
        [string]$Line
    )

    if ($Line -match 'apiDelete|\.delete|DELETE') { return 'DELETE' }
    if ($Line -match 'apiPut|\.put|PUT') { return 'PUT' }
    if ($Line -match 'apiPost|\.post|POST') { return 'POST' }
    if ($Line -match 'apiPatch|\.patch|PATCH') { return 'PATCH' }
    return 'GET'
}

function Should-Skip-LiteralPath {
    param(
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) { return $true }
    if (-not $Path.StartsWith('/api/')) { return $true }
    if ($Path.Contains('${')) { return $true }
    if ($Path.Contains('{')) { return $true }
    if ($Path.Contains('}')) { return $true }
    if ($Path.EndsWith('=')) { return $true }
    if ($Path.EndsWith('?')) { return $true }
    return $false
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3D-Repair3'

if (-not (Test-Path $adminWebSrc)) {
    throw ('Admin Web source root was not found: {0}' -f $adminWebSrc)
}

if (-not (Test-Path $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'page-api-interaction-coverage.summary.md'
$detailsPath = Join-Path $artifactRoot 'page-api-interaction-coverage.details.csv'

$details = New-Object 'System.Collections.Generic.List[object]'
$seen = New-Object 'System.Collections.Generic.HashSet[string]'

$sourceFiles = Get-ChildItem -Path $adminWebSrc -Recurse -File -Include '*.ts','*.tsx' |
    Where-Object {
        $_.FullName -notmatch '\\node_modules\\' -and
        $_.FullName -notmatch '\\dist\\' -and
        $_.FullName -notmatch '\\reference\\'
    } |
    Sort-Object FullName

foreach ($file in $sourceFiles) {
    $relative = $file.FullName.Substring($adminWebSrc.Length).TrimStart('\')
    $lines = Get-Content -Path $file.FullName

    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notlike '*/api/*') { continue }

        $method = Get-MethodForLine -Line $line
        $literals = Get-QuotedLiterals -Line $line

        foreach ($literal in $literals) {
            if ([string]::IsNullOrWhiteSpace($literal)) { continue }

            if (-not $literal.StartsWith('/api/')) {
                if ($literal -like '*api/*') {
                    Add-Detail -Details $details -Method $method -Path $literal -Source $relative -Result 'Skipped' -Classification 'SkippedModuleImportOrRelativePath' -StatusCode '' -Error ''
                }
                continue
            }

            if (Should-Skip-LiteralPath -Path $literal) {
                Add-Detail -Details $details -Method $method -Path $literal -Source $relative -Result 'Skipped' -Classification 'SkippedDynamicOrTemplatePath' -StatusCode '' -Error ''
                continue
            }

            $key = ('{0} {1}' -f $method, $literal)
            if (-not $seen.Add($key)) { continue }

            if ($method -ne 'GET') {
                Add-Detail -Details $details -Method $method -Path $literal -Source $relative -Result 'Skipped' -Classification 'SkippedNonGet' -StatusCode '' -Error ''
                continue
            }

            if ($details.Count -ge 500) {
                Add-Detail -Details $details -Method $method -Path $literal -Source $relative -Result 'Skipped' -Classification 'SkippedSafetyLimit' -StatusCode '' -Error ''
                continue
            }
        }
    }
}

$getCandidates = @($details | Where-Object { $_.Method -eq 'GET' -and $_.Classification -ne 'SkippedModuleImportOrRelativePath' -and $_.Classification -ne 'SkippedDynamicOrTemplatePath' -and $_.Classification -ne 'SkippedNonGet' -and $_.Classification -ne 'SkippedSafetyLimit' })
$probeCount = 0

foreach ($candidate in $getCandidates) {
    if ($probeCount -ge $MaxGetProbes) {
        $candidate.Result = 'Skipped'
        $candidate.Classification = 'SkippedProbeLimit'
        continue
    }

    $probeCount++
    $url = Join-Url -BaseUrl $AdminApiBaseUrl -Path $candidate.Path

    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $candidate.Result = 'Success'
        $candidate.Classification = 'Success'
        $candidate.StatusCode = [string][int]$response.StatusCode
        $candidate.Error = ''
    }
    catch {
        $statusCode = ''
        $message = $_.Exception.Message

        if ($null -ne $_.Exception.Response) {
            try {
                $statusCode = [string][int]$_.Exception.Response.StatusCode
            }
            catch {
                $statusCode = ''
            }
        }

        $candidate.StatusCode = $statusCode
        $candidate.Error = $message

        if ($statusCode -eq '405') {
            $candidate.Result = 'VerbMismatch'
            $candidate.Classification = 'VerbMismatchEvidence'
        }
        elseif ($candidate.Path -like '*/probe*') {
            $candidate.Result = 'Skipped'
            $candidate.Classification = 'SkippedProbeActionEndpoint'
        }
        else {
            $candidate.Result = 'NonSuccess'
            $candidate.Classification = 'HttpFailure'
        }
    }
}

$details | Export-Csv -Path $detailsPath -NoTypeInformation

$total = $details.Count
$success = @($details | Where-Object { $_.Classification -eq 'Success' }).Count
$httpFailures = @($details | Where-Object { $_.Classification -eq 'HttpFailure' }).Count
$verbMismatch = @($details | Where-Object { $_.Classification -eq 'VerbMismatchEvidence' }).Count
$skippedModule = @($details | Where-Object { $_.Classification -eq 'SkippedModuleImportOrRelativePath' }).Count
$skippedDynamic = @($details | Where-Object { $_.Classification -eq 'SkippedDynamicOrTemplatePath' }).Count
$skippedNonGet = @($details | Where-Object { $_.Classification -eq 'SkippedNonGet' }).Count

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3D Repair3 - Admin Web Page API Interaction Coverage')
[void]$summary.Add('')
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('O'))))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
[void]$summary.Add(('Discovered endpoint records: `{0}`' -f $total))
[void]$summary.Add(('Successful GET probes: `{0}`' -f $success))
[void]$summary.Add(('Verb mismatch evidence: `{0}`' -f $verbMismatch))
[void]$summary.Add(('Real non-success GET probes: `{0}`' -f $httpFailures))
[void]$summary.Add(('Skipped module/relative imports: `{0}`' -f $skippedModule))
[void]$summary.Add(('Skipped dynamic/template paths: `{0}`' -f $skippedDynamic))
[void]$summary.Add(('Skipped non-GET interactions: `{0}`' -f $skippedNonGet))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))

Set-Content -Path $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($httpFailures -gt 0) {
    throw ('Page API interaction coverage completed with {0} real non-success GET probe(s). Review {1}' -f $httpFailures, $summaryPath)
}

Write-Host 'P10.3D Repair3 Admin Web page API interaction coverage passed.'
