param(
    [switch]$WriteCleanupCommands
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $expectedDocs = @(
        "docs/cloud-roadmap-cleanup/P2_COMPLETION_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P3_RECOMMENDED_PLAN.md",
        "docs/cloud-roadmap-cleanup/P2_SET_053_COMPLETION_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P2_QUEUE_EXECUTION_STACK_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P2_AUDIT_PERSISTENCE_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P2_TELEMETRY_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P2_OPERATIONAL_DIAGNOSTICS_CHECKPOINT.md",
        "docs/cloud-roadmap-cleanup/P2_AUTH_OPERATIONS_CHECKPOINT.md"
    )

    $expectedTools = @(
        "tools/test/validate-p2-completion.ps1",
        "tools/test/validate-full-p2-stack.ps1",
        "tools/test/validate-operational-diagnostics-stack.ps1",
        "tools/test/validate-auth-operations-stack.ps1",
        "tools/test/validate-queue-execution-stack.ps1",
        "tools/test/validate-audit-persistence-stack.ps1",
        "tools/test/validate-telemetry-stack.ps1"
    )

    $missingDocs = @()
    foreach ($item in $expectedDocs) {
        if (!(Test-Path $item)) {
            $missingDocs += $item
        }
    }

    $missingTools = @()
    foreach ($item in $expectedTools) {
        if (!(Test-Path $item)) {
            $missingTools += $item
        }
    }

    $cleanupTargets = @()

    if (Test-Path ".\tools\dropins") {
        $dirs = Get-ChildItem ".\tools\dropins" -Directory -ErrorAction SilentlyContinue
        foreach ($dir in $dirs) {
            if ($dir.Name -like "p2-set*") {
                $cleanupTargets += $dir.FullName.Substring($repoRoot.Path.Length + 1)
            }
        }

        $files = Get-ChildItem ".\tools\dropins" -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            if ($file.Name -like "apply-p2-set*.ps1" -or
                $file.Name -like "*fix*.ps1" -or
                $file.Name -like "*corrective*.ps1") {
                $cleanupTargets += $file.FullName.Substring($repoRoot.Path.Length + 1)
            }
        }
    }

    $cleanupTargets = $cleanupTargets | Sort-Object -Unique

    $reportPath = Join-Path $reportDir "P2_DOCS_TOOLS_INVENTORY_REPORT.md"
    $report = @()

    $report += "# Post-P2 Docs and Tools Inventory"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Missing expected docs"

    if ($missingDocs.Count -eq 0) {
        $report += "- None"
    }
    else {
        foreach ($item in $missingDocs) {
            $report += "- $item"
        }
    }

    $report += ""
    $report += "## Missing expected tools"

    if ($missingTools.Count -eq 0) {
        $report += "- None"
    }
    else {
        foreach ($item in $missingTools) {
            $report += "- $item"
        }
    }

    $report += ""
    $report += "## Candidate cleanup targets"

    if ($cleanupTargets.Count -eq 0) {
        $report += "- None"
    }
    else {
        foreach ($item in $cleanupTargets) {
            $report += "- $item"
        }
    }

    $report += ""
    $report += "## Suggested git cleanup commands"
    $report += ""
    $report += "Review before running:"
    $report += ""
    $report += '```powershell'

    foreach ($target in $cleanupTargets) {
        $normalized = $target -replace "\\", "/"
        $report += "git rm -r -- '$normalized'"
    }

    $report += '```'

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    if ($WriteCleanupCommands) {
        $cmdPath = Join-Path $reportDir "p2-cleanup-git-rm-commands.ps1"
        $cmd = @()
        $cmd += '$ErrorActionPreference = "Stop"'
        $cmd += '# Review this generated file before running.'
        $cmd += '# It removes post-P2 drop-in payloads and temporary apply/fix scripts only.'
        $cmd += ""

        foreach ($target in $cleanupTargets) {
            $normalized = $target -replace "\\", "/"
            $cmd += "git rm -r -- '$normalized'"
        }

        Set-Content -Path $cmdPath -Value $cmd -Encoding UTF8
    }

    Write-Host "Post-P2 docs/tools inventory complete."
    Write-Host "Report: $reportPath"
    Write-Host "Missing docs : $($missingDocs.Count)"
    Write-Host "Missing tools: $($missingTools.Count)"
    Write-Host "Cleanup candidates: $($cleanupTargets.Count)"

    if ($WriteCleanupCommands) {
        Write-Host "Cleanup command file: .\docs\post-p2-cleanup\p2-cleanup-git-rm-commands.ps1"
    }
}
finally {
    Pop-Location
}
