param(
    [string]$AdminApiHealthUrl = 'https://localhost:55436/health',
    [string]$AdminWebUrl = 'http://127.0.0.1:5173'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Invoke-SmokeRequest {
    param(
        [string]$Name,
        [string]$Url
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        Write-Host ('{0}: HTTP {1} {2}' -f $Name, [int]$response.StatusCode, $Url)
        return $true
    }
    catch {
        Write-Host ('{0}: FAILED {1}' -f $Name, $Url)
        Write-Host $_.Exception.Message
        return $false
    }
}

$apiOk = Invoke-SmokeRequest -Name 'Admin API health' -Url $AdminApiHealthUrl
$webOk = Invoke-SmokeRequest -Name 'Admin Web' -Url $AdminWebUrl

if (-not $apiOk -or -not $webOk) {
    throw 'Local stack smoke failed. Review process logs under artifacts\p10\P10.2CE.'
}

Write-Host 'P10.2CE local stack smoke passed.'
