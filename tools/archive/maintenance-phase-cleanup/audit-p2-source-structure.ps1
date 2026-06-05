$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot

try {
    $reportDir = ".\docs\post-p2-cleanup"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $areas = @(
        @{ Name = "ControlPlane Auth"; Path = "src\Migration.ControlPlane\Auth" },
        @{ Name = "ControlPlane Audit"; Path = "src\Migration.ControlPlane\Audit" },
        @{ Name = "ControlPlane Operations"; Path = "src\Migration.ControlPlane\Operations" },
        @{ Name = "ControlPlane Queues"; Path = "src\Migration.ControlPlane\Queues" },
        @{ Name = "ControlPlane Telemetry"; Path = "src\Migration.ControlPlane\Telemetry" },
        @{ Name = "Admin API Endpoints"; Path = "src\Migration.Admin.Api\Endpoints" },
        @{ Name = "Admin Web API Clients"; Path = "src\Admin\Migration.Admin.Web\src\api" }
    )

    $areaSummaries = @()
    $contractFiles = @()
    $serviceFiles = @()
    $registrationFiles = @()
    $endpointFiles = @()
    $frontendApiFiles = @()
    $commentReviewCandidates = @()
    $largeFiles = @()

    foreach ($area in $areas) {
        $path = $area.Path
        $files = @()

        if (Test-Path $path) {
            $files = Get-ChildItem $path -File -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -in @(".cs", ".ts") } |
                Sort-Object FullName
        }

        $areaSummaries += [pscustomobject]@{
            Name = $area.Name
            Path = $path
            Count = $files.Count
        }

        foreach ($file in $files) {
            $relative = $file.FullName.Substring($repoRoot.Path.Length + 1)
            $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
            $lineCount = if ([string]::IsNullOrEmpty($content)) { 0 } else { ($content -split "`r?`n").Count }

            if ($file.Name -like "*Contracts.cs" -or $file.Name -like "I*.cs") {
                $contractFiles += $relative
            }

            if ($file.Name -like "*Service.cs" -or $file.Name -like "*Provider.cs" -or $file.Name -like "*Writer.cs") {
                $serviceFiles += $relative
            }

            if ($file.Name -like "*RegistrationExtensions.cs") {
                $registrationFiles += $relative
            }

            if ($file.Name -like "*EndpointExtensions.cs") {
                $endpointFiles += $relative
            }

            if ($file.Extension -eq ".ts") {
                $frontendApiFiles += $relative
            }

            if ($lineCount -gt 180) {
                $largeFiles += "$relative ($lineCount lines)"
            }

            $isGovernanceOrSafety =
                $relative -like "*Operations*" -or
                $relative -like "*Auth*" -or
                $relative -like "*Queues*" -or
                $relative -like "*Credential*" -or
                $relative -like "*Readiness*" -or
                $relative -like "*Governance*" -or
                $relative -like "*Safety*"

            $hasXmlComment = $content -match "///\s*<summary>"

            if ($isGovernanceOrSafety -and !$hasXmlComment -and $file.Extension -eq ".cs") {
                $commentReviewCandidates += $relative
            }
        }
    }

    $reportPath = Join-Path $reportDir "P2_SOURCE_STRUCTURE_INVENTORY_REPORT.md"

    $report = @()
    $report += "# P2 Source Structure Inventory Report"
    $report += ""
    $report += "Generated: $(Get-Date -Format o)"
    $report += ""
    $report += "## Area summary"
    $report += ""
    $report += "| Area | Path | Files |"
    $report += "|---|---|---:|"

    foreach ($summary in $areaSummaries) {
        $report += "| $($summary.Name) | `$($summary.Path)` | $($summary.Count) |"
    }

    function Add-ListSection {
        param(
            [string]$Title,
            [object[]]$Items
        )

        $script:report += ""
        $script:report += "## $Title"

        if ($Items.Count -eq 0) {
            $script:report += "- None"
        }
        else {
            foreach ($item in $Items) {
                $script:report += "- $item"
            }
        }
    }

    Add-ListSection "Contracts and interfaces" $contractFiles
    Add-ListSection "Services / providers / writers" $serviceFiles
    Add-ListSection "Registration extension files" $registrationFiles
    Add-ListSection "Endpoint extension files" $endpointFiles
    Add-ListSection "Frontend API client files" $frontendApiFiles
    Add-ListSection "Large files over 180 lines" $largeFiles
    Add-ListSection "Comment-review candidates" $commentReviewCandidates

    $report += ""
    $report += "## Recommendation"
    $report += ""
    $report += "- Add comments only where they explain safety, governance, lifecycle, or architectural intent."
    $report += "- Do not add obvious comments that repeat method names."
    $report += "- Registration extension files may be consolidated later to reduce `Program.cs` noise."
    $report += "- Endpoint files should remain split by feature area unless they are truly tiny and redundant."
    $report += "- Do not reorganize source folders before a clean validation run."

    Set-Content -Path $reportPath -Value $report -Encoding UTF8

    Write-Host "P2 source structure inventory complete."
    Write-Host "Report: $reportPath"
    Write-Host "Contracts/interfaces : $($contractFiles.Count)"
    Write-Host "Services/providers   : $($serviceFiles.Count)"
    Write-Host "Registrations        : $($registrationFiles.Count)"
    Write-Host "Endpoints            : $($endpointFiles.Count)"
    Write-Host "Frontend API clients : $($frontendApiFiles.Count)"
    Write-Host "Comment candidates   : $($commentReviewCandidates.Count)"
}
finally {
    Pop-Location
}
