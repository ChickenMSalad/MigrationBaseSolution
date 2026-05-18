param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$SecretsProject = "src/Migration.Admin.Api/Migration.Admin.Api.csproj",
    [switch]$DisableAfterVerification
)

$ErrorActionPreference = "Stop"

Write-Host "=== P3 Operational Mirror Controlled Enablement Smoke ==="
Write-Host ""

Write-Host "Step 1: Enabling operational run mirror in user secrets..."
./scripts/enable-operational-run-mirror.ps1 `
    -SecretsProject $SecretsProject

Write-Host ""
Write-Host "IMPORTANT:"
Write-Host "Restart Migration.Admin.Api now so the updated user-secret value is loaded."
Write-Host "After restart, press Enter to continue."
Read-Host

Write-Host ""
Write-Host "Step 2: Verifying operational mirror full status..."
./scripts/operational-run-mirror-full-status.ps1 `
    -BaseUrl $BaseUrl

Write-Host ""
Write-Host "Step 3: Verifying enablement guard..."
$guardUrl = "$BaseUrl/api/operational/mirror/enablement-guard"
$guard = Invoke-RestMethod `
    -Method Get `
    -Uri $guardUrl `
    -ContentType "application/json"

Write-Host "CanMirror: $($guard.canMirror)"
Write-Host "MirrorEnabled: $($guard.mirrorEnabled)"
Write-Host "ReadinessPassed: $($guard.readinessPassed)"
Write-Host "SqlSchemaPassed: $($guard.sqlSchemaPassed)"

if (-not $guard.canMirror) {
    Write-Host ""
    Write-Host "Mirror guard did not pass. Stop here."
    $guard | ConvertTo-Json -Depth 10
    exit 1
}

Write-Host ""
Write-Host "Mirror guard passed."
Write-Host ""
Write-Host "Step 4:"
Write-Host "Submit one normal existing project run using your existing workflow:"
Write-Host "POST /api/projects/{projectId}/runs"
Write-Host ""
Write-Host "After the existing run endpoint returns Accepted, press Enter to verify SQL mirror writes."
Read-Host

Write-Host ""
Write-Host "Step 5: Verifying operational mirror writes..."
./scripts/operational-mirror-write-verification-smoke-test.ps1 `
    -BaseUrl $BaseUrl

if ($DisableAfterVerification) {
    Write-Host ""
    Write-Host "Step 6: DisableAfterVerification specified. Disabling mirror..."
    ./scripts/disable-operational-run-mirror.ps1 `
        -SecretsProject $SecretsProject

    Write-Host ""
    Write-Host "Restart Migration.Admin.Api to load the disabled mirror setting."
}

Write-Host ""
Write-Host "Controlled enablement smoke script completed."
