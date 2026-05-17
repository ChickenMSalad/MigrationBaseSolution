$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p2-set052-final-readiness-report"

Write-Host "Applying P2 Set 052 from $repoRoot"

$files = @(
    "src\Migration.ControlPlane\Operations\P2ReadinessReportContracts.cs",
    "src\Migration.ControlPlane\Operations\IP2ReadinessReportService.cs",
    "src\Migration.ControlPlane\Operations\P2ReadinessReportService.cs",
    "src\Migration.ControlPlane\Operations\P2ReadinessReportRegistrationExtensions.cs",
    "src\Migration.Admin.Api\Endpoints\P2ReadinessReportEndpointExtensions.cs",
    "src\Admin\Migration.Admin.Web\src\api\p2ReadinessReport.ts",
    "tools\test\smoke-p2-readiness-report.ps1",
    "tools\test\smoke-p2-readiness-report.cmd",
    "docs\cloud-roadmap-cleanup\P2_SET_052_FINAL_READINESS_REPORT.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddP2ReadinessReport") {
    $program = $program.Replace(
        "builder.Services.AddQueueExecutionGovernance();",
        "builder.Services.AddQueueExecutionGovernance();`r`nbuilder.Services.AddP2ReadinessReport();")
}

if ($program -notmatch "MapP2ReadinessReportEndpoints") {
    $program = $program.Replace(
        "api.MapQueueExecutionGovernanceEndpoints();",
        "api.MapQueueExecutionGovernanceEndpoints();`r`napi.MapP2ReadinessReportEndpoints();")
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P2 Set 052 applied."
