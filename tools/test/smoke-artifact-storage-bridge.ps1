param(
    [string]$BaseUrl = "http://localhost:5173",
    [string]$WorkspaceId = "default",
    [string]$ArtifactKind = "manifest",
    [string]$ArtifactId = "smoke-test",
    [string]$FileName = "artifact-bridge-smoke.txt"
)

$ErrorActionPreference = "Stop"

Write-Host "Artifact Storage Bridge Smoke Test"
Write-Host "Base URL    : $BaseUrl"
Write-Host "Workspace   : $WorkspaceId"
Write-Host "Kind        : $ArtifactKind"
Write-Host "Artifact ID : $ArtifactId"
Write-Host "File        : $FileName"
Write-Host ""

$headers = @{
    "X-Workspace-Id" = $WorkspaceId
}

$content = "artifact storage bridge smoke test $(Get-Date -Format o)"
$uploadUri = "$BaseUrl/api/cloud/artifacts/$ArtifactKind/$ArtifactId/files/$FileName"
$indexUri = "$BaseUrl/api/cloud/artifacts/index"
$downloadPath = Join-Path $env:TEMP $FileName

Write-Host "Uploading artifact..."
$upload = Invoke-RestMethod `
    -Method Post `
    -Uri $uploadUri `
    -Headers $headers `
    -ContentType "text/plain" `
    -Body $content

if ($null -eq $upload.artifact -or $upload.artifact.fileName -ne $FileName) {
    throw "Upload response did not include expected artifact descriptor."
}

Write-Host "OK upload: $($upload.artifact.objectKey)"

Write-Host "Reading index..."
$index = Invoke-RestMethod `
    -Method Get `
    -Uri $indexUri `
    -Headers $headers

$match = $index.artifacts | Where-Object { $_.objectKey -eq $upload.artifact.objectKey } | Select-Object -First 1

if ($null -eq $match) {
    throw "Artifact manifest index does not contain uploaded object key: $($upload.artifact.objectKey)"
}

Write-Host "OK index contains uploaded artifact."

Write-Host "Downloading artifact..."
Invoke-WebRequest `
    -Uri $uploadUri `
    -Headers $headers `
    -OutFile $downloadPath | Out-Null

$downloaded = Get-Content $downloadPath -Raw

if ($downloaded -ne $content) {
    throw "Downloaded artifact content did not match uploaded content."
}

Write-Host "OK download content matched."

Write-Host "Deleting artifact..."
$delete = Invoke-RestMethod `
    -Method Delete `
    -Uri $uploadUri `
    -Headers $headers

if ($delete.deleted -ne $true) {
    throw "Delete response did not report deleted=true."
}

Write-Host "OK delete completed."

Write-Host ""
Write-Host "Artifact Storage Bridge smoke test completed successfully."
