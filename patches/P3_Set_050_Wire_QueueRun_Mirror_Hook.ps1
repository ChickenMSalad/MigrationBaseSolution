$repoRoot = (Resolve-Path ".").Path
$runEndpointPath = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Runs\RunEndpointExtensions.cs"

if (-not (Test-Path $runEndpointPath)) {
    throw "Could not find $runEndpointPath"
}

$content = Get-Content $runEndpointPath -Raw

if ($content -notmatch "using Migration\.Admin\.Api\.OperationalStore;") {
    $content = $content.Replace(
        "using Microsoft.AspNetCore.Mvc;",
        "using Microsoft.AspNetCore.Mvc; using Migration.Admin.Api.OperationalStore;")
}

$queueRunParameterPattern = "RunPreflightGateService preflightGate, IMigrationRunQueue queue, \[FromServices\] ArtifactPathResolver artifactPathResolver"
$queueRunParameterReplacement = "RunPreflightGateService preflightGate, IMigrationRunQueue queue, IAdminOperationalRunMirrorService operationalRunMirror, [FromServices] ArtifactPathResolver artifactPathResolver"

if ($content -notmatch "MapPost\(`"/projects/\{projectId\}/runs`".*?IAdminOperationalRunMirrorService operationalRunMirror") {
    $content = [regex]::Replace(
        $content,
        $queueRunParameterPattern,
        $queueRunParameterReplacement,
        1)
}

$queueRunEnqueueNeedle = "await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false); await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false); return Results.Accepted($`"/api/runs/{run.RunId}`", run);"
$queueRunEnqueueReplacement = "await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false); await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false); await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false); return Results.Accepted($`"/api/runs/{run.RunId}`", run);"

if ($content -notmatch "MapPost\(`"/projects/\{projectId\}/runs`".*?operationalRunMirror\.MirrorRunAsync\(project, run") {
    if ($content.Contains($queueRunEnqueueNeedle)) {
        $content = $content.Replace(
            $queueRunEnqueueNeedle,
            $queueRunEnqueueReplacement)
    }
    else {
        throw "Could not locate QueueRun enqueue/return sequence. Open RunEndpointExtensions.cs and apply the README manual patch."
    }
}

Set-Content -Path $runEndpointPath -Value $content -NoNewline

Write-Host "QueueRun operational mirror hook applied to $runEndpointPath"
