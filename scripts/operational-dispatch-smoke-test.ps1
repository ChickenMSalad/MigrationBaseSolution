param(
    [string]$BaseUrl = "https://localhost:7001",
    [int]$ManifestRecordCount = 3,
    [switch]$SkipDispatch
)

$ErrorActionPreference = "Stop"

$sampleUrl = "$BaseUrl/api/operational/runs/dispatch/sample?count=$ManifestRecordCount"
$dispatchUrl = "$BaseUrl/api/operational/runs/dispatch"

Write-Host "Requesting sample operational dispatch payload..."
Write-Host "GET $sampleUrl"

$sampleRequest = Invoke-RestMethod `
    -Method Get `
    -Uri $sampleUrl `
    -ContentType "application/json"

Write-Host "Sample request returned."
Write-Host "SourceSystem: $($sampleRequest.sourceSystem)"
Write-Host "TargetSystem: $($sampleRequest.targetSystem)"
Write-Host "ManifestRecords: $($sampleRequest.manifestRecords.Count)"

if ($SkipDispatch) {
    Write-Host "SkipDispatch was specified. Not posting dispatch request."
    $sampleRequest | ConvertTo-Json -Depth 10
    exit 0
}

Write-Host "Posting operational dispatch request..."
Write-Host "POST $dispatchUrl"

$response = Invoke-RestMethod `
    -Method Post `
    -Uri $dispatchUrl `
    -ContentType "application/json" `
    -Body ($sampleRequest | ConvertTo-Json -Depth 10)

Write-Host "Operational dispatch response returned."
Write-Host "RunId: $($response.runId)"
Write-Host "ManifestRecordCount: $($response.manifestRecordCount)"
Write-Host "PublishedQueueMessageCount: $($response.publishedQueueMessageCount)"

$response | ConvertTo-Json -Depth 10
