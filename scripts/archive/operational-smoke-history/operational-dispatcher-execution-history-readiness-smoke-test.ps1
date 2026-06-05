param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting dispatcher execution history readiness..."
Write-Host "GET $BaseUrl/api/operational/dispatcher/executions/readiness"

$response = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/readiness" `
    -ContentType "application/json"

Write-Host "Ready: $($response.ready)"
Write-Host "ServiceRegistered: $($response.serviceRegistered)"
Write-Host "TableExists: $($response.tableExists)"
Write-Host "RequiredColumnsExist: $($response.requiredColumnsExist)"
Write-Host "SchemaName: $($response.schemaName)"

if ($response.messages) {
    foreach ($message in $response.messages) {
        Write-Host "- $message"
    }
}

$response | ConvertTo-Json -Depth 10
