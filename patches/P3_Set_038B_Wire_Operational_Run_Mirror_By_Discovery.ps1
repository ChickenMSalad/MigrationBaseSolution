$repoRoot = (Resolve-Path ".").Path

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$adminApiRoot = Join-Path $repoRoot "src\Migration.Admin.Api"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

if (-not (Test-Path $adminApiRoot)) {
    throw "Could not find $adminApiRoot"
}

$candidates = @(Get-ChildItem -Path $adminApiRoot -Recurse -Filter "*.cs" | Where-Object {
    $content = Get-Content $_.FullName -Raw
    ($content -match "IMigrationRunQueue" -and $content -match "EnqueueAsync") -or
    ($content -match "projects/\{projectId\}/runs") -or
    ($content -match "MapPost" -and $content -match "EnqueueAsync\(run")
})

if ($candidates.Count -eq 0) {
    throw "Could not discover the existing project run endpoint file. Run ./patches/P3_Set_038B_Find_Run_Endpoint_Candidates.ps1 and share the output."
}

if ($candidates.Count -gt 1) {
    Write-Host "Multiple candidate files found:"
    $candidates | ForEach-Object { Write-Host $_.FullName }
    throw "Multiple candidates found. Run ./patches/P3_Set_038B_Find_Run_Endpoint_Candidates.ps1 and choose the correct endpoint file manually."
}

$runEndpointPath = $candidates[0].FullName

Write-Host "Discovered run endpoint file:"
Write-Host $runEndpointPath

$program = Get-Content $programPath -Raw
$runEndpoint = Get-Content $runEndpointPath -Raw

if ($program -notmatch "AddMigrationAdminApiOperationalRunMirror\(") {
    $program = $program.Replace(
        "builder.Services.AddOperationalStore(builder.Configuration);",
        "builder.Services.AddOperationalStore(builder.Configuration);`r`nbuilder.Services.AddMigrationAdminApiOperationalRunMirror(builder.Configuration);")
}

if ($runEndpoint -notmatch "Migration\.Admin\.Api\.OperationalStore") {
    if ($runEndpoint -match "using Microsoft\.AspNetCore\.Mvc;") {
        $runEndpoint = $runEndpoint.Replace(
            "using Microsoft.AspNetCore.Mvc;",
            "using Microsoft.AspNetCore.Mvc;`r`nusing Migration.Admin.Api.OperationalStore;")
    }
    else {
        $runEndpoint = "using Migration.Admin.Api.OperationalStore;`r`n" + $runEndpoint
    }
}

if ($runEndpoint -notmatch "IAdminOperationalRunMirrorService operationalRunMirror") {
    $patterns = @(
        "IMigrationRunQueue queue, [FromServices] ArtifactPathResolver artifactPathResolver",
        "IMigrationRunQueue queue,[FromServices] ArtifactPathResolver artifactPathResolver",
        "[FromServices] IMigrationRunQueue queue, [FromServices] ArtifactPathResolver artifactPathResolver",
        "[FromServices] IMigrationRunQueue queue,[FromServices] ArtifactPathResolver artifactPathResolver"
    )

    $replaced = $false

    foreach ($pattern in $patterns) {
        if ($runEndpoint.Contains($pattern)) {
            $replacement = $pattern.Replace(
                "IMigrationRunQueue queue",
                "IMigrationRunQueue queue, IAdminOperationalRunMirrorService operationalRunMirror")
            $runEndpoint = $runEndpoint.Replace($pattern, $replacement)
            $replaced = $true
            break
        }
    }

    if (-not $replaced) {
        throw "Could not inject IAdminOperationalRunMirrorService into the endpoint parameters. Apply manually using README instructions. File: $runEndpointPath"
    }
}

if ($runEndpoint -notmatch "operationalRunMirror\.MirrorRunAsync\(project, run") {
    $needle = "await queue.EnqueueAsync(run, cancellationToken).ConfigureAwait(false);"

    if ($runEndpoint.Contains($needle)) {
        $runEndpoint = $runEndpoint.Replace(
            $needle,
            "$needle`r`n            await operationalRunMirror.MirrorRunAsync(project, run, cancellationToken).ConfigureAwait(false);")
    }
    else {
        throw "Could not find queue enqueue line. Apply manually after the queue.EnqueueAsync(run, cancellationToken) call. File: $runEndpointPath"
    }
}

Set-Content -Path $programPath -Value $program -NoNewline
Set-Content -Path $runEndpointPath -Value $runEndpoint -NoNewline

Write-Host "Operational run mirror wiring applied to:"
Write-Host $runEndpointPath
