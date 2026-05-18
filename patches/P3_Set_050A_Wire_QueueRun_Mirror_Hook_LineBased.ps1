$repoRoot = (Resolve-Path ".").Path
$runEndpointPath = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Runs\RunEndpointExtensions.cs"

if (-not (Test-Path $runEndpointPath)) {
    throw "Could not find $runEndpointPath"
}

$content = Get-Content $runEndpointPath -Raw

if ($content -notmatch "using Migration\.Admin\.Api\.OperationalStore;") {
    if ($content -match "using Microsoft\.AspNetCore\.Mvc;") {
        $content = $content -replace "using Microsoft\.AspNetCore\.Mvc;", "using Microsoft.AspNetCore.Mvc;`r`nusing Migration.Admin.Api.OperationalStore;"
    }
    else {
        $content = "using Migration.Admin.Api.OperationalStore;`r`n" + $content
    }
}

if ($content -notmatch "IAdminOperationalRunMirrorService operationalRunMirror") {
    $content = [regex]::Replace(
        $content,
        "IMigrationRunQueue\s+queue\s*,",
        "IMigrationRunQueue queue, IAdminOperationalRunMirrorService operationalRunMirror,",
        1)

    if ($content -notmatch "IAdminOperationalRunMirrorService operationalRunMirror") {
        throw "Could not inject IAdminOperationalRunMirrorService parameter. Open RunEndpointExtensions.cs and add it next to IMigrationRunQueue queue."
    }
}

if ($content -notmatch "operationalRunMirror\.MirrorRunAsync\(project, run") {
    $enqueuePattern = "(?<indent>\s*)await\s+queue\.EnqueueAsync\(run,\s*cancellationToken\)\.ConfigureAwait\(false\);"

    $match = [regex]::Match($content, $enqueuePattern)

    if (-not $match.Success) {
        throw "Could not find await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false); in RunEndpointExtensions.cs."
    }

    $indent = $match.Groups["indent"].Value
    $replacement = $match.Value + "`r`n" + $indent + "await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false);"

    $content = [regex]::Replace(
        $content,
        $enqueuePattern,
        [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $replacement },
        1)
}

Set-Content -Path $runEndpointPath -Value $content -NoNewline

Write-Host "QueueRun operational mirror hook applied using line-based insertion."
