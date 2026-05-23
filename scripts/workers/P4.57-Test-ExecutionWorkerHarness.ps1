[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$workerPath = Join-Path $repoRoot "scripts/workers/P4.57-Invoke-LocalExecutionWorker.ps1"

Assert-Contains -Path $workerPath -Text "/api/operational/execution-work-items/lease"
Assert-Contains -Path $workerPath -Text "/api/operational/execution-work-items/complete"
Assert-Contains -Path $workerPath -Text "/api/operational/execution-work-items/fail"
Assert-Contains -Path $workerPath -Text "FailLeasedItems"

Write-Host "[P4.57] Worker harness validation passed."
