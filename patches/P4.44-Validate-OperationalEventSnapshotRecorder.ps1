[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Events/OperationalEventSnapshotRecorderService.cs"
$optionsPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Events/OperationalEventSnapshotRecorderOptions.cs"

Assert-Contains -Path $servicePath -Text "ScheduledOperationalMetricsSnapshot"
Assert-Contains -Path $optionsPath -Text "OperationalEventSnapshots"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddHostedService<OperationalEventSnapshotRecorderService>();"
Assert-OccursOnce -Path $programPath -Text "builder.Services.Configure<OperationalEventSnapshotRecorderOptions>(builder.Configuration.GetSection(OperationalEventSnapshotRecorderOptions.SectionName));"

Write-Host "[P4.44] Validation passed."
