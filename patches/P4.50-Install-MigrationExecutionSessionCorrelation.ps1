[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.50] {0}" -f $Message)
}

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-LineOnce {
    param([string]$Path, [string]$Line, [string]$Anchor)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Line)) {
        Write-Step ("Already present: {0}" -f $Line)
        return
    }

    if (-not $content.Contains($Anchor)) {
        throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add line {0}" -f $Line)
        return
    }

    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line {0}" -f $Line)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "database/sql/operational/003_create_migration_execution_sessions.sql"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionSessionRecord.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/CreateExecutionSessionRequest.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionSessionStore.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionSessionStore.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/IOperationalEventStore.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/OperationalEventRecord.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Events/SqlOperationalEventStore.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionSessionEndpointExtensions.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Events/OperationalEventEndpointExtensions.cs"
Copy-PayloadFile "docs/operations/P4.50-migration-execution-session-correlation.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce `
    -Path $programPath `
    -Line "using Migration.Admin.Api.Operational.Execution;" `
    -Anchor "using Migration.Admin.Api.Operational.Events;"

Add-LineOnce `
    -Path $programPath `
    -Line "builder.Services.AddScoped<IExecutionSessionStore, SqlExecutionSessionStore>();" `
    -Anchor "builder.Services.AddScoped<IOperationalEventStore, SqlOperationalEventStore>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce `
    -Path $compositionPath `
    -Line "using Migration.Admin.Api.Endpoints.Operational.Execution;" `
    -Anchor "using Migration.Admin.Api.Endpoints.Operational.Events;"

Add-LineOnce `
    -Path $compositionPath `
    -Line "        endpoints.MapExecutionSessionEndpoints();" `
    -Anchor "        endpoints.MapOperationalEventEndpoints();"

Write-Step "Complete."
