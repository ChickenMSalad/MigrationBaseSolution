$repoRoot = (Resolve-Path ".").Path
$runEndpointPath = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Runs\RunEndpointExtensions.cs"

if (-not (Test-Path $runEndpointPath)) {
    throw "Could not find $runEndpointPath"
}

$content = Get-Content $runEndpointPath -Raw

if ($content -notmatch "IAdminOperationalRunMirrorService operationalRunMirror") {
    throw "IAdminOperationalRunMirrorService parameter is not present. Apply Set 050A first."
}

$queueRunNeedle = @"
                await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);
                await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);

                return Results.Accepted($"/api/runs/{run.RunId}", run);
"@

$queueRunReplacement = @"
                await store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);
                await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);
                await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false);

                return Results.Accepted($"/api/runs/{run.RunId}", run);
"@

if ($content.Contains($queueRunReplacement)) {
    Write-Host "QueueRun mirror call already exists."
}
elseif ($content.Contains($queueRunNeedle)) {
    $content = $content.Replace($queueRunNeedle, $queueRunReplacement)
    Set-Content -Path $runEndpointPath -Value $content -NoNewline
    Write-Host "Inserted QueueRun mirror call."
}
else {
    throw "Could not find exact QueueRun block. Manually insert MirrorRunAsync after the QueueRun queue.EnqueueAsync line shown in README."
}
