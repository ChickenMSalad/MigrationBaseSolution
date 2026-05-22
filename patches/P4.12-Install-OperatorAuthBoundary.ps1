[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.12] $Message" -ForegroundColor Cyan
}

function Copy-PayloadFile {
    param(
        [string]$SourceRelativePath,
        [string]$TargetRelativePath
    )

    $sourcePath = Join-Path $repoRoot (Join-Path 'payload' $SourceRelativePath)
    $targetPath = Join-Path $repoRoot $TargetRelativePath
    $targetDirectory = Split-Path -Parent $targetPath

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Payload file not found: $sourcePath"
    }

    if (-not $Apply) {
        Write-Step "WOULD copy $sourcePath -> $targetPath"
        return
    }

    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($targetPath, 'Copy payload file')) {
        Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
        Write-Step "Copied $TargetRelativePath"
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Write-Step "Repo root: $repoRoot"

$adminProgramPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
if (-not (Test-Path -LiteralPath $adminProgramPath)) {
    throw "Admin API Program.cs not found: $adminProgramPath"
}

Copy-PayloadFile 'src/Core/Migration.Admin.Api/Endpoints/Security/AdminSecurityStatusEndpointExtensions.cs' 'src/Core/Migration.Admin.Api/Endpoints/Security/AdminSecurityStatusEndpointExtensions.cs'
Copy-PayloadFile 'apps/migration-admin-ui/src/auth/authConfig.ts' 'apps/migration-admin-ui/src/auth/authConfig.ts'
Copy-PayloadFile 'apps/migration-admin-ui/src/auth/authSession.ts' 'apps/migration-admin-ui/src/auth/authSession.ts'
Copy-PayloadFile 'apps/migration-admin-ui/src/components/OperatorAuthBoundaryCard.tsx' 'apps/migration-admin-ui/src/components/OperatorAuthBoundaryCard.tsx'
Copy-PayloadFile 'apps/migration-admin-ui/.env.example' 'apps/migration-admin-ui/.env.example'
Copy-PayloadFile 'config-samples/appsettings.AdminApi.Auth.sample.json' 'config-samples/appsettings.AdminApi.Auth.sample.json'
Copy-PayloadFile 'docs/security/P4.12-operator-auth-boundary.md' 'docs/security/P4.12-operator-auth-boundary.md'

$programText = Get-Content -LiteralPath $adminProgramPath -Raw
$registrationText = 'app.MapAdminSecurityStatusEndpoints();'

if ($programText.Contains($registrationText)) {
    Write-Step 'Program.cs already maps Admin security status endpoints'
} elseif (-not $Apply) {
    Write-Step "WOULD add $registrationText to Program.cs"
} else {
    $anchor = 'app.UseMigrationAdminApiAuthenticationState();'
    if (-not $programText.Contains($anchor)) {
        throw "Program.cs anchor not found: $anchor"
    }

    $updatedProgramText = $programText.Replace($anchor, $anchor + [Environment]::NewLine + $registrationText)

    if ($PSCmdlet.ShouldProcess($adminProgramPath, 'Register Admin security status endpoints')) {
        Set-Content -LiteralPath $adminProgramPath -Value $updatedProgramText -Encoding UTF8
        Write-Step 'Updated Program.cs endpoint registration'
    }
}

Write-Step 'Complete. Next: ./patches/P4.12-Validate-OperatorAuthBoundary.ps1; dotnet restore; dotnet build; npm run build'
