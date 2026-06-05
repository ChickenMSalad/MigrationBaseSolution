$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $auditScripts = @(
        ".\tools\maintenance\audit-p2-docs-tools.ps1",
        ".\tools\maintenance\audit-p2-test-tools.ps1",
        ".\tools\maintenance\audit-p2-docs.ps1",
        ".\tools\maintenance\audit-p2-source-structure.ps1",
        ".\tools\maintenance\audit-p2-comment-coverage.ps1"
    )

    $requiredValidators = @(
        ".\tools\test\validate-p2-completion.ps1",
        ".\tools\test\validate-full-p2-stack.ps1",
        ".\tools\test\validate-operational-diagnostics-stack.ps1",
        ".\tools\test\validate-auth-operations-stack.ps1",
        ".\tools\test\validate-queue-execution-stack.ps1",
        ".\tools\test\validate-audit-persistence-stack.ps1",
        ".\tools\test\validate-telemetry-stack.ps1"
    )

    $requiredDocs = @(
        ".\docs\cloud-roadmap-cleanup\P2_COMPLETION_CHECKPOINT.md",
        ".\docs\cloud-roadmap-cleanup\P3_RECOMMENDED_PLAN.md",
        ".\docs\p3-planning\P3_SQL_OPERATIONAL_MODEL.md",
        ".\docs\p3-planning\P3_SQL_SCHEMA_STARTING_POINT.md",
        ".\docs\p3-planning\P3_EXECUTION_BOUNDARIES.md"
    )

    $auditResults = @()

    foreach ($script in $auditScripts) {
        if (Test-Path $script) {
            Write-Host "Running $script"
            & powershell -ExecutionPolicy Bypass -File $script
            $auditResults += "$script : Passed"
            Write-Host ""
        }
        else {
            $auditResults += "$script : Missing"
        }
    }

    $missingValidators = @()
    foreach ($item in $requiredValidators) {
        if (!(Test-Path $item)) {
            $missingValidators += $item
        }
    }

    $missingDocs = @()
    foreach ($item in $requiredDocs) {
        if (!(Test-Path $item)) {
            $missingDocs += $item
        }
    }

    $dropinArtifactCount = 0
    if (Test-Path ".\tools\dropins") {
        $dropinArtifactCount = @(
            Get-ChildItem ".\tools\dropins" -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like "p2-set*" }
        ).Count
    }

    $reportPath = Join-Path $reportDir "POST_P2_CLEANUP_CHECKPOINT_REPORT.md"

    $report = @()
    $report += "# Post-P2 Cleanup Checkpoint Report"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Audit script results"
    $report += ""

    foreach ($result in $auditResults) {
        $report += "- $result"
    }

    $report += ""
    $report += "## Required validators"

    if ($missingValidators.Count -eq 0) {
        $report += "- All required validators are present."
    }
    else {
        foreach ($item in $missingValidators) {
            $report += "- Missing: $item"
        }
    }

    $report += ""
    $report += "## Required docs"

    if ($missingDocs.Count -eq 0) {
        $report += "- All required docs are present."
    }
    else {
        foreach ($item in $missingDocs) {
            $report += "- Missing: $item"
        }
    }

    $report += ""
    $report += "## Drop-in payload status"
    $report += "- Remaining tools/dropins/p2-set* directories: $dropinArtifactCount"
    $report += ""
    $report += "## Cleanup status"

    if ($missingValidators.Count -eq 0 -and $missingDocs.Count -eq 0) {
        $report += "- Post-P2 cleanup baseline is ready."
    }
    else {
        $report += "- Post-P2 cleanup baseline has missing required files. Resolve before P3."
    }

    $report += ""
    $report += "## Recommended final validation"
    $report += "- dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
    $report += "- powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host "Post-P2 cleanup checkpoint complete."
    Write-Host "Report: $reportPath"
    Write-Host "Missing validators: $($missingValidators.Count)"
    Write-Host "Missing docs: $($missingDocs.Count)"
    Write-Host "Drop-in p2-set dirs: $dropinArtifactCount"
}
finally {
    Pop-Location
}
