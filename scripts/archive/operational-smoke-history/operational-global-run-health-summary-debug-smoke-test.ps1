param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "Requesting global operational run health summary with detailed error handling..."
Write-Host "GET $BaseUrl/api/operational/runs/health-summary"

try {
    $response = Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/operational/runs/health-summary" `
        -ContentType "application/json"

    $response | ConvertTo-Json -Depth 20
    Write-Host ""
    Write-Host "Operational global run health summary debug smoke passed."
}
catch [System.Net.WebException] {
    Write-Host ""
    Write-Host "Request failed."

    if ($_.Exception.Response) {
        Write-Host "HTTP Status: $([int]$_.Exception.Response.StatusCode) $($_.Exception.Response.StatusDescription)"

        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()

            if ($body) {
                Write-Host ""
                Write-Host "Response body:"
                Write-Host $body
            }
        }
        catch {
            Write-Host "Could not read error response body."
        }
    }

    throw
}
