[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$payloadRoot = Join-Path $repoRoot 'payload'

function Write-Step([string]$Message) { Write-Host "[P4.17] $Message" }
function Ensure-Directory([string]$Path) { if (-not (Test-Path -LiteralPath $Path)) { New-Item -ItemType Directory -Path $Path | Out-Null } }
function Copy-File([string]$RelativePath) {
    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $source)) { throw "Payload file not found: $source" }
    if ($Apply) {
        Ensure-Directory $targetDir
        if ($PSCmdlet.ShouldProcess($target, 'Copy P4.17 file')) {
            Copy-Item -LiteralPath $source -Destination $target -Force
            Write-Step "Copied $RelativePath"
        }
    } else {
        Write-Step "WOULD copy $source -> $target"
    }
}

Write-Step "Repo root: $repoRoot"

$files = @(
 'src/Core/Migration.Admin.Api/Endpoints/Operational/Workers/OperationalWorkerTelemetryEndpointExtensions.cs',
 'apps/migration-admin-ui/src/features/workers/WorkerTelemetryWorkspace.tsx',
 'apps/migration-admin-ui/src/features/workers/workerTelemetryApi.ts',
 'apps/migration-admin-ui/src/features/workers/workerTelemetryTypes.ts',
 'docs/operations/P4.17-live-worker-telemetry-lease-visualization.md'
)
foreach ($f in $files) { Copy-File $f }

$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
if (-not (Test-Path -LiteralPath $programPath)) { throw "Program.cs not found: $programPath" }
$programText = Get-Content -LiteralPath $programPath -Raw
$usingText = 'using Migration.Admin.Api.Endpoints.Operational.Workers;'
$mapText = 'app.MapOperationalWorkerTelemetryEndpoints();'
if ($Apply) {
    if ($programText -notmatch [regex]::Escape($usingText)) { $programText = $usingText + [Environment]::NewLine + $programText }
    if ($programText -notmatch [regex]::Escape($mapText)) {
        $anchor = 'app.MapControllers();'
        if ($programText -match [regex]::Escape($anchor)) { $programText = $programText -replace [regex]::Escape($anchor), ($anchor + [Environment]::NewLine + $mapText) }
        else { $programText = $programText + [Environment]::NewLine + $mapText + [Environment]::NewLine }
    }
    Set-Content -LiteralPath $programPath -Value $programText -Encoding UTF8
    Write-Step 'Updated Program.cs worker telemetry endpoint registration'
} else {
    Write-Step 'WOULD add worker telemetry endpoint registration to Program.cs if missing'
}

$appPath = Join-Path $repoRoot 'apps/migration-admin-ui/src/App.tsx'
if (Test-Path -LiteralPath $appPath) {
    $appText = Get-Content -LiteralPath $appPath -Raw
    $importText = "import { WorkerTelemetryWorkspace } from './features/workers/WorkerTelemetryWorkspace';"
    if ($Apply) {
        if ($appText -notmatch [regex]::Escape($importText)) { $appText = $importText + [Environment]::NewLine + $appText }
        if ($appText -notmatch 'WorkerTelemetryWorkspace') { $appText = $appText + [Environment]::NewLine + '<WorkerTelemetryWorkspace />' + [Environment]::NewLine }
        elseif ($appText -match 'WorkerTelemetryWorkspace' -and $appText -notmatch '<WorkerTelemetryWorkspace') {
            $appText = $appText -replace '(</main>)', ("  <WorkerTelemetryWorkspace />" + [Environment]::NewLine + '$1')
        }
        Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8
        Write-Step 'Updated UI App.tsx worker telemetry registration when marker was available'
    } else { Write-Step 'WOULD add WorkerTelemetryWorkspace import/render to App.tsx if missing' }
} else { Write-Step 'UI App.tsx not found; copied feature files only' }

Write-Step 'Complete. Next: validate; dotnet restore; dotnet build; npm run build'
