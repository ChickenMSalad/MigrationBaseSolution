$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $docRoots = @(
        ".\docs\cloud-roadmap-cleanup",
        ".\docs\post-p2-cleanup"
    )

    $allDocs = @()
    foreach ($root in $docRoots) {
        if (Test-Path $root) {
            $allDocs += Get-ChildItem $root -File -Filter "*.md" -ErrorAction SilentlyContinue |
                Sort-Object FullName
        }
    }

    $finalReference = @()
    $checkpoints = @()
    $setHistory = @()
    $cleanupDocs = @()
    $reviewCandidates = @()

    foreach ($doc in $allDocs) {
        $relative = $doc.FullName.Substring($repoRoot.Path.Length + 1)

        if ($doc.Name -in @(
            "P2_COMPLETION_CHECKPOINT.md",
            "P3_RECOMMENDED_PLAN.md",
            "P2_OPERATIONAL_DIAGNOSTICS_CHECKPOINT.md",
            "P2_AUTH_OPERATIONS_CHECKPOINT.md",
            "P2_QUEUE_EXECUTION_STACK_CHECKPOINT.md",
            "P2_AUDIT_PERSISTENCE_CHECKPOINT.md",
            "P2_TELEMETRY_CHECKPOINT.md"
        )) {
            $finalReference += $relative
            continue
        }

        if ($doc.Name -like "*CHECKPOINT*.md") {
            $checkpoints += $relative
            continue
        }

        if ($doc.Name -like "P2_SET_*.md") {
            $setHistory += $relative
            continue
        }

        if ($relative -like "docs\post-p2-cleanup\*") {
            $cleanupDocs += $relative
            continue
        }

        $reviewCandidates += $relative
    }

    $reportPath = Join-Path $reportDir "P2_DOC_INVENTORY_REPORT.md"

    $report = @()
    $report += "# P2 Documentation Inventory Report"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Summary"
    $report += ""
    $report += "| Category | Count |"
    $report += "|---|---:|"
    $report += "| Final reference docs | $($finalReference.Count) |"
    $report += "| Checkpoint docs | $($checkpoints.Count) |"
    $report += "| Set-history docs | $($setHistory.Count) |"
    $report += "| Post-P2 cleanup docs | $($cleanupDocs.Count) |"
    $report += "| Review candidates | $($reviewCandidates.Count) |"
    $report += ""
    $report += "## Final reference docs"
    if ($finalReference.Count -eq 0) { $report += "- None" } else { foreach ($x in $finalReference) { $report += "- $x" } }
    $report += ""
    $report += "## Checkpoint docs"
    if ($checkpoints.Count -eq 0) { $report += "- None" } else { foreach ($x in $checkpoints) { $report += "- $x" } }
    $report += ""
    $report += "## Set-history docs"
    if ($setHistory.Count -eq 0) { $report += "- None" } else { foreach ($x in $setHistory) { $report += "- $x" } }
    $report += ""
    $report += "## Post-P2 cleanup docs"
    if ($cleanupDocs.Count -eq 0) { $report += "- None" } else { foreach ($x in $cleanupDocs) { $report += "- $x" } }
    $report += ""
    $report += "## Review candidates"
    if ($reviewCandidates.Count -eq 0) { $report += "- None" } else { foreach ($x in $reviewCandidates) { $report += "- $x" } }
    $report += ""
    $report += "## Recommendation"
    $report += ""
    $report += "- Keep final reference docs."
    $report += "- Keep checkpoint docs at least until P3 starts."
    $report += "- Keep set-history docs if you want historical traceability."
    $report += "- Later, set-history docs can be archived into a single compressed documentation archive or summarized into a phase history document."
    $report += "- Do not remove docs before the final handoff document is rewritten correctly."

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host "P2 documentation inventory complete."
    Write-Host "Report: $reportPath"
    Write-Host "Final reference docs : $($finalReference.Count)"
    Write-Host "Checkpoint docs      : $($checkpoints.Count)"
    Write-Host "Set-history docs     : $($setHistory.Count)"
    Write-Host "Cleanup docs         : $($cleanupDocs.Count)"
    Write-Host "Review candidates    : $($reviewCandidates.Count)"
}
finally {
    Pop-Location
}
