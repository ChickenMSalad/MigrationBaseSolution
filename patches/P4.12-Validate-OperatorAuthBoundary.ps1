[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected file not found: $Path"
    }
}

function Assert-FileContains {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text not found in {0}: {1}' -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Write-Host "[P4.12] Repo root: $repoRoot" -ForegroundColor Cyan

Assert-FileContains (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs') 'app.MapAdminSecurityStatusEndpoints();'
Assert-FileContains (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Endpoints/Security/AdminSecurityStatusEndpointExtensions.cs') 'MapAdminSecurityStatusEndpoints'
Assert-FileContains (Join-Path $repoRoot 'apps/migration-admin-ui/src/auth/authConfig.ts') 'operatorAuthConfig'
Assert-FileContains (Join-Path $repoRoot 'apps/migration-admin-ui/src/auth/authSession.ts') 'readOperatorSession'
Assert-FileContains (Join-Path $repoRoot 'apps/migration-admin-ui/src/components/OperatorAuthBoundaryCard.tsx') 'OperatorAuthBoundaryCard'
Assert-FileContains (Join-Path $repoRoot 'apps/migration-admin-ui/.env.example') 'VITE_ENTRA_TENANT_ID'
Assert-FileContains (Join-Path $repoRoot 'config-samples/appsettings.AdminApi.Auth.sample.json') 'Migration.Operator'
Assert-FileContains (Join-Path $repoRoot 'docs/security/P4.12-operator-auth-boundary.md') 'Operator Authentication Boundary'

Write-Host '[P4.12] Validation passed.' -ForegroundColor Green
