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

    $missingDocs = $expectedDocs | Where-Object { !(Test-Path $_) }
    $missingTools = $expectedTools | Where-Object { !(Test-Path $_) }

    $cleanupTargets = @()

    if (Test-Path ".\tools\dropins") {
        $cleanupTargets += Get-ChildItem ".\tools\dropins" -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "^p2-set" } |
            ForEach-Object { $_.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "") }

        $cleanupTargets += Get-ChildItem ".\tools\dropins" -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "^apply-p2-set.*\.ps1$|fix|corrective" } |
            ForEach-Object { $_.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "") }
    }

    $cleanupTargets = @($cleanupTargets) | Sort-Object -Unique

    $report = New-Object System.Collections.Generic.List[string]
    $report.Add("# Post-P2 Docs and Tools Inventory")
    $report.Add("")
    $report.Add("Generated: $(Get-Date -Format o)")
    $report.Add("")
    $report.Add("## Missing expected docs")
    if ($missingDocs.Count -eq 0) {
        $report.Add("- None")
    }
    else {
        foreach ($item in $missingDocs) {
            $report.Add("- $item")
        }
    }

    $report.Add("")
    $report.Add("## Missing expected tools")
    if ($missingTools.Count -eq 0) {
        $report.Add("- None")
    }
    else {
        foreach ($item in $missingTools) {
            $report.Add("- $item")
        }
    }

    $report.Add("")
    $report.Add("## Candidate cleanup targets")
    if ($cleanupTargets.Count -eq 0) {
        $report.Add("- None")
    }
    else {
        foreach ($item in $cleanupTargets) {
            $report.Add("- $item")
        }
    }

    $report.Add("")
    $report.Add("## Suggested git cleanup commands")
    $report.Add("")
    $report.Add("Review before running:")
    $report.Add("")
    $report.Add("```powershell")

    foreach ($target in $cleanupTargets) {
        $safe = $target.Replace('\', '/')
        $report.Add("git rm -r -- `"$safe`"")
    }

    $report.Add("```")

    $reportPath = Join-Path $reportDir "P2_DOCS_TOOLS_INVENTORY_REPORT.md"
    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    if ($WriteCleanupCommands) {
        $cmdPath = Join-Path $reportDir "p2-cleanup-git-rm-commands.ps1"
        $cmd = New-Object System.Collections.Generic.List[string]
        $cmd.Add('$ErrorActionPreference = "Stop"')
        $cmd.Add("# Review this generated file before running.")
        $cmd.Add("# It removes post-P2 drop-in payloads and temporary apply/fix scripts only.")
        $cmd.Add("")

        foreach ($target in $cleanupTargets) {
            $safe = $target.Replace('\', '/')
            $cmd.Add("git rm -r -- `"$safe`"")
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
