[CmdletBinding()]
param(
    [string]$AdminApiProject = "src/Core/Migration.Admin.Api/Migration.Admin.Api.csproj",
    [string]$AdminApiUrl = "https://localhost:55436",
    [int]$UiPort = 5174,
    [switch]$SkipRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.30-DEV] {0}" -f $Message)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$adminProjectPath = Join-Path $repoRoot $AdminApiProject
$uiPath = Join-Path $repoRoot "apps/migration-admin-ui"
$envPath = Join-Path $uiPath ".env.local"

if (-not (Test-Path -LiteralPath $adminProjectPath)) {
    throw ("Admin API project not found: {0}" -f $adminProjectPath)
}

if (-not (Test-Path -LiteralPath $uiPath)) {
    throw ("UI path not found: {0}" -f $uiPath)
}

if (-not (Test-Path -LiteralPath $envPath)) {
    Set-Content -LiteralPath $envPath -Value ("VITE_ADMIN_API_BASE_URL={0}" -f $AdminApiUrl) -Encoding UTF8
    Write-Step ("Created {0}" -f $envPath)
}

if (-not $SkipRestore) {
    Write-Step "Restoring .NET solution"
    Push-Location $repoRoot
    try {
        dotnet restore
    }
    finally {
        Pop-Location
    }

    Write-Step "Installing UI packages"
    Push-Location $uiPath
    try {
        npm install
    }
    finally {
        Pop-Location
    }
}

Write-Step "Starting Admin API in a new PowerShell window"
$apiCommand = "cd `"$repoRoot`"; dotnet run --project `"$adminProjectPath`" --urls `"$AdminApiUrl`""
Start-Process powershell -ArgumentList @("-NoExit", "-Command", $apiCommand) | Out-Null

Write-Step "Starting migration-admin-ui in a new PowerShell window"
$uiCommand = "cd `"$uiPath`"; `$env:VITE_ADMIN_API_BASE_URL=`"$AdminApiUrl`"; npm run dev -- --host localhost --port $UiPort"
Start-Process powershell -ArgumentList @("-NoExit", "-Command", $uiCommand) | Out-Null

Write-Step ("Admin API: {0}" -f $AdminApiUrl)
Write-Step ("UI: http://localhost:{0}" -f $UiPort)
