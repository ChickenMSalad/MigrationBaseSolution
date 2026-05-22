[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory = $true)][string] $Message)
    Write-Host "[P4.3] $Message"
}

function Copy-PayloadFile {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw ("Payload file not found: {0}" -f $Source)
    }

    $destinationDirectory = Split-Path -Parent $Destination

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
            Write-Step ("Created {0}" -f $destinationDirectory)
        }
        else {
            Write-Step ("WOULD create {0}" -f $destinationDirectory)
        }
    }

    if ($Apply) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Step ("Copied {0}" -f $Destination)
    }
    else {
        Write-Step ("WOULD copy {0} -> {1}" -f $Source, $Destination)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Write-Step ("Repo root: {0}" -f $repoRoot)

$manifestProjectDirectory = Join-Path $repoRoot "src\Manifests\Migration.Manifest.Sql"
$manifestProjectPath = Join-Path $manifestProjectDirectory "Migration.Manifest.Sql.csproj"
$payloadProviderPath = Join-Path $repoRoot "payload\src\Manifests\Migration.Manifest.Sql\SqlManifestProvider.cs"
$targetProviderPath = Join-Path $manifestProjectDirectory "SqlManifestProvider.cs"

if (-not (Test-Path -LiteralPath $manifestProjectPath)) {
    throw ("Expected project not found: {0}" -f $manifestProjectPath)
}

Copy-PayloadFile -Source $payloadProviderPath -Destination $targetProviderPath

Write-Step "Complete. Next: ./patches/P4.3-Validate-SqlManifestProvider.ps1; dotnet restore; dotnet build"
