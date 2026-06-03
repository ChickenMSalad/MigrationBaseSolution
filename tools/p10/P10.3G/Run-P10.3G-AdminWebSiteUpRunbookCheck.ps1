param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3G'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'site-up-runbook-check.summary.md'
$detailsPath = Join-Path $artifactRoot 'site-up-runbook-check.details.csv'

$results = New-Object 'System.Collections.Generic.List[object]'

$targets = @(
    @{ Name = 'AdminWebRoot'; Url = $AdminWebBaseUrl },
    @{ Name = 'AdminApiConnectors'; Url = ($AdminApiBaseUrl.TrimEnd('/') + '/api/connectors') },
    @{ Name = 'AdminApiProjects'; Url = ($AdminApiBaseUrl.TrimEnd('/') + '/api/projects') }
)

foreach ($target in $targets) {
    $status = 'Unknown'
    $code = ''
    $message = ''
    try {
        $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
        if ($null -ne $curl) {
            $output = & curl.exe -k -s -o NUL -w '%{http_code}' --max-time $TimeoutSeconds $target.Url
            $exitCode = $LASTEXITCODE
            $code = $output
            if ($exitCode -eq 0 -and $output -match '^[0-9][0-9][0-9]$') {
                if ($output -ge '200' -and $output -lt '500') {
                    $status = 'Success'
                }
                else {
                    $status = 'NonSuccess'
                }
            }
            else {
                $status = 'RequestFailed'
                $message = ('curl exit code {0}' -f $exitCode)
            }
        }
        else {
            $response = Invoke-WebRequest -Uri $target.Url -UseBasicParsing -TimeoutSec $TimeoutSeconds
            $code = [string]$response.StatusCode
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                $status = 'Success'
            }
            else {
                $status = 'NonSuccess'
            }
        }
    }
    catch {
        $status = 'RequestFailed'
        $message = $_.Exception.Message
    }

    $row = New-Object PSObject -Property ([ordered]@{
        Name = $target.Name
        Url = $target.Url
        Status = $status
        StatusCode = $code
        Message = $message
    })
    [void]$results.Add($row)
    Write-Host ('{0} => {1} {2}' -f $target.Name, $status, $code)
}

$results.ToArray() | Export-Csv -LiteralPath $detailsPath -NoTypeInformation

$successCount = 0
foreach ($result in $results) {
    if ($result.Status -eq 'Success') { $successCount++ }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3G - Admin Web Site-Up Runbook Check')
[void]$summary.Add('')
[void]$summary.Add(('- Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('- Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('- Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('- Total checks: `{0}`' -f $results.Count))
[void]$summary.Add(('- Successful checks: `{0}`' -f $successCount))
[void]$summary.Add(('- Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($successCount -ne $results.Count) {
    throw ('Site-up runbook check completed with {0} non-success check(s).' -f ($results.Count - $successCount))
}
