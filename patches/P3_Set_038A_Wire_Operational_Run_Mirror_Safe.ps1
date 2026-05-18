$repoRoot = (Resolve-Path ".").Path

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$runEndpointPath = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\RunEndpointExtensions.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

if (-not (Test-Path $runEndpointPath)) {
    throw "Could not find $runEndpointPath"
}

$program = Get-Content $programPath -Raw
$runEndpoint = Get-Content $runEndpointPath -Raw

if ($program -notmatch "AddMigrationAdminApiOperationalRunMirror\(") {
    $program = $program.Replace(
        "builder.Services.AddOperationalStore(builder.Configuration);",
        "builder.Services.AddOperationalStore(builder.Configuration);`r`nbuilder.Services.AddMigrationAdminApiOperationalRunMirror(builder.Configuration);")
}

if ($runEndpoint -notmatch "Migration\.Admin\.Api\.OperationalStore") {
    $runEndpoint = $runEndpoint.Replace(
        "using Microsoft.AspNetCore.Mvc;",
        "using Microsoft.AspNetCore.Mvc;`r`nusing Migration.Admin.Api.OperationalStore;")
}

if ($runEndpoint -notmatch "IAdminOperationalRunMirrorService operationalRunMirror") {
    $runEndpoint = $runEndpoint.Replace(
        "IMigrationRunQueue queue, [FromServices] ArtifactPathResolver artifactPathResolver",
        "IMigrationRunQueue queue, IAdminOperationalRunMirrorService operationalRunMirror, [FromServices] ArtifactPathResolver artifactPathResolver")
}

if ($runEndpoint -notmatch "operationalRunMirror\.MirrorRunAsync\(project, run") {
    $runEndpoint = $runEndpoint.Replace(
        "await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);",
        "await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);`r`n            await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false);")
}

Set-Content -Path $programPath -Value $program -NoNewline
Set-Content -Path $runEndpointPath -Value $runEndpoint -NoNewline

Write-Host "Operational run mirror wiring applied safely."
