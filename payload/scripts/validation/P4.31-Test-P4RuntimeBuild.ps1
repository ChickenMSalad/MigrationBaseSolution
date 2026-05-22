[CmdletBinding()]
param(
    [switch]$SkipUiBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.31-BUILD] {0}" -f $Message)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$uiPath = Join-Path $repoRoot "apps/migration-admin-ui"

Push-Location $repoRoot
try {
    Write-Step "dotnet restore"
    dotnet restore

    Write-Step "dotnet build"
    dotnet build --no-restore
}
finally {
    Pop-Location
}

if (-not $SkipUiBuild) {
    Push-Location $uiPath
    try {
        Write-Step "npm run build"
        npm run build
    }
    finally {
        Pop-Location
    }
}

Write-Step "Build validation passed."
