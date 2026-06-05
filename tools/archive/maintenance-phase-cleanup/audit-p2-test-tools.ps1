$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $coreValidators = @(
        "validate-p2-completion.ps1",
        "validate-full-p2-stack.ps1"
    )

    $checkpointValidators = @(
        "validate-operational-diagnostics-stack.ps1",
        "validate-auth-operations-stack.ps1",
        "validate-queue-execution-stack.ps1",
        "validate-audit-persistence-stack.ps1",
        "validate-telemetry-stack.ps1"
    )

    $allScripts = @()
    if (Test-Path ".\tools\test") {
        $allScripts = Get-ChildItem ".\tools\test" -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in @(".ps1", ".cmd") } |
            Sort-Object Name
    }

    $coreFound = @()
    $checkpointFound = @()
    $smokeFound = @()
    $cmdWrappers = @()
    $reviewCandidates = @()

    foreach ($script in $allScripts) {
        if ($script.Extension -eq ".cmd") {
            $cmdWrappers += $script.Name
            continue
        }

        if ($coreValidators -contains $script.Name) {
            $coreFound += $script.Name
            continue
        }

        if ($checkpointValidators -contains $script.Name) {
            $checkpointFound += $script.Name
            continue
        }

        if ($script.Name -like "smoke-*.ps1") {
            $smokeFound += $script.Name
            continue
        }

        $reviewCandidates += $script.Name
    }

    $missingCore = $coreValidators | Where-Object { $coreFound -notcontains $_ }
    $missingCheckpoint = $checkpointValidators | Where-Object { $checkpointFound -notcontains $_ }

    $reportPath = Join-Path $reportDir "P2_TEST_TOOL_INVENTORY_REPORT.md"

    $report = @()
    $report += "# P2 Test Tool Inventory Report"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Summary"
    $report += ""
    $report += "| Category | Count |"
    $report += "|---|---:|"
    $report += "| Core validators | $($coreFound.Count) |"
    $report += "| Checkpoint validators | $($checkpointFound.Count) |"
    $report += "| Endpoint smoke tests | $($smokeFound.Count) |"
    $report += "| CMD wrappers | $($cmdWrappers.Count) |"
    $report += "| Review candidates | $($reviewCandidates.Count) |"
    $report += ""
    $report += "## Missing core validators"
    if ($missingCore.Count -eq 0) { $report += "- None" } else { foreach ($x in $missingCore) { $report += "- $x" } }
    $report += ""
    $report += "## Missing checkpoint validators"
    if ($missingCheckpoint.Count -eq 0) { $report += "- None" } else { foreach ($x in $missingCheckpoint) { $report += "- $x" } }
    $report += ""
    $report += "## Core validators"
    if ($coreFound.Count -eq 0) { $report += "- None" } else { foreach ($x in $coreFound) { $report += "- $x" } }
    $report += ""
    $report += "## Checkpoint validators"
    if ($checkpointFound.Count -eq 0) { $report += "- None" } else { foreach ($x in $checkpointFound) { $report += "- $x" } }
    $report += ""
    $report += "## Endpoint smoke tests"
    if ($smokeFound.Count -eq 0) { $report += "- None" } else { foreach ($x in $smokeFound) { $report += "- $x" } }
    $report += ""
    $report += "## CMD wrappers"
    if ($cmdWrappers.Count -eq 0) { $report += "- None" } else { foreach ($x in $cmdWrappers) { $report += "- $x" } }
    $report += ""
    $report += "## Review candidates"
    if ($reviewCandidates.Count -eq 0) { $report += "- None" } else { foreach ($x in $reviewCandidates) { $report += "- $x" } }
    $report += ""
    $report += "## Recommendation"
    $report += ""
    $report += "- Keep all core validators."
    $report += "- Keep checkpoint validators."
    $report += "- Keep smoke tests while endpoints remain active."
    $report += "- Review CMD wrappers later; they are convenience wrappers, not required by CI."
    $report += "- Do not delete scripts from this report without running full P2 validation afterward."

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host "P2 test tool inventory complete."
    Write-Host "Report: $reportPath"
    Write-Host "Core validators       : $($coreFound.Count)"
    Write-Host "Checkpoint validators : $($checkpointFound.Count)"
    Write-Host "Smoke tests           : $($smokeFound.Count)"
    Write-Host "CMD wrappers          : $($cmdWrappers.Count)"
    Write-Host "Review candidates     : $($reviewCandidates.Count)"
}
finally {
    Pop-Location
}
