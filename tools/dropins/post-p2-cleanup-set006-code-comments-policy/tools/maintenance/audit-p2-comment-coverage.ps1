$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $priorityPatterns = @(
        "*Governance*.cs",
        "*SafetyGate*.cs",
        "*OperationalMode*.cs",
        "*Readiness*.cs",
        "*CredentialAccess*.cs",
        "*AuthPolicy*.cs",
        "*AuditPersistence*.cs",
        "*Telemetry*.cs",
        "*Idempotency*.cs",
        "*Lease*.cs"
    )

    $scanRoots = @(
        "src\Migration.ControlPlane\Auth",
        "src\Migration.ControlPlane\Audit",
        "src\Migration.ControlPlane\Operations",
        "src\Migration.ControlPlane\Queues",
        "src\Migration.ControlPlane\Telemetry",
        "src\Migration.Admin.Api\Endpoints"
    )

    $files = @()
    foreach ($rootPath in $scanRoots) {
        if (Test-Path $rootPath) {
            $files += Get-ChildItem $rootPath -File -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
        }
    }

    $priorityFiles = @()
    foreach ($file in $files) {
        foreach ($pattern in $priorityPatterns) {
            if ($file.Name -like $pattern) {
                $priorityFiles += $file
                break
            }
        }
    }

    $priorityFiles = $priorityFiles | Sort-Object FullName -Unique

    $withXmlSummary = @()
    $withoutXmlSummary = @()
    $withTodo = @()
    $veryShortFiles = @()

    foreach ($file in $priorityFiles) {
        $relative = $file.FullName.Substring($repoRoot.Path.Length + 1)
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue

        if ($content -match "///\s*<summary>") {
            $withXmlSummary += $relative
        }
        else {
            $withoutXmlSummary += $relative
        }

        if ($content -match "\bTODO\b|\bHACK\b|\bFIXME\b") {
            $withTodo += $relative
        }

        $lines = if ([string]::IsNullOrWhiteSpace($content)) { 0 } else { ($content -split "`r?`n").Count }
        if ($lines -lt 25) {
            $veryShortFiles += "$relative ($lines lines)"
        }
    }

    $reportPath = Join-Path $reportDir "P2_COMMENT_COVERAGE_REPORT.md"

    $report = @()
    $report += "# P2 Comment Coverage Report"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Summary"
    $report += ""
    $report += "| Category | Count |"
    $report += "|---|---:|"
    $report += "| Priority files scanned | $($priorityFiles.Count) |"
    $report += "| With XML summary | $($withXmlSummary.Count) |"
    $report += "| Without XML summary | $($withoutXmlSummary.Count) |"
    $report += "| With TODO/HACK/FIXME | $($withTodo.Count) |"
    $report += "| Very short priority files | $($veryShortFiles.Count) |"
    $report += ""
    $report += "## Files with XML summaries"
    if ($withXmlSummary.Count -eq 0) { $report += "- None" } else { foreach ($x in $withXmlSummary) { $report += "- $x" } }
    $report += ""
    $report += "## Files without XML summaries"
    if ($withoutXmlSummary.Count -eq 0) { $report += "- None" } else { foreach ($x in $withoutXmlSummary) { $report += "- $x" } }
    $report += ""
    $report += "## Files with TODO/HACK/FIXME"
    if ($withTodo.Count -eq 0) { $report += "- None" } else { foreach ($x in $withTodo) { $report += "- $x" } }
    $report += ""
    $report += "## Very short priority files"
    if ($veryShortFiles.Count -eq 0) { $report += "- None" } else { foreach ($x in $veryShortFiles) { $report += "- $x" } }
    $report += ""
    $report += "## Recommendation"
    $report += ""
    $report += "- Do not comment every file."
    $report += "- Add XML summaries to public governance/safety contracts first."
    $report += "- Add comments where future maintainers need to understand why execution is disabled or gated."
    $report += "- Leave simple DTO records alone unless they represent important operational boundaries."
    $report += "- Treat this report as a targeting guide, not a build rule."

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host "P2 comment coverage audit complete."
    Write-Host "Report: $reportPath"
    Write-Host "Priority files scanned : $($priorityFiles.Count)"
    Write-Host "With XML summary       : $($withXmlSummary.Count)"
    Write-Host "Without XML summary    : $($withoutXmlSummary.Count)"
    Write-Host "TODO/HACK/FIXME files  : $($withTodo.Count)"
}
finally {
    Pop-Location
}
