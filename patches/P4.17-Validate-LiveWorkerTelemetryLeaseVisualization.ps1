[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
function Assert-File([string]$RelativePath) { $p=Join-Path $repoRoot $RelativePath; if(-not(Test-Path -LiteralPath $p)){ throw "Expected file not found: $p" } }
function Assert-Contains([string]$RelativePath,[string]$Text){ $p=Join-Path $repoRoot $RelativePath; Assert-File $RelativePath; $c=Get-Content -LiteralPath $p -Raw; if($c -notmatch [regex]::Escape($Text)){ throw ("Expected text not found in {0}: {1}" -f $p,$Text) } }
Assert-File 'src/Core/Migration.Admin.Api/Endpoints/Operational/Workers/OperationalWorkerTelemetryEndpointExtensions.cs'
Assert-File 'apps/migration-admin-ui/src/features/workers/WorkerTelemetryWorkspace.tsx'
Assert-File 'apps/migration-admin-ui/src/features/workers/workerTelemetryApi.ts'
Assert-File 'apps/migration-admin-ui/src/features/workers/workerTelemetryTypes.ts'
Assert-Contains 'src/Core/Migration.Admin.Api/Program.cs' 'MapOperationalWorkerTelemetryEndpoints'
Assert-Contains 'apps/migration-admin-ui/src/features/workers/WorkerTelemetryWorkspace.tsx' 'WorkerTelemetryWorkspace'
Write-Host '[P4.17] Validation passed.'
