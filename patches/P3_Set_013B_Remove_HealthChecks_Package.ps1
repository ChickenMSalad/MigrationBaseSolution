$repoRoot = (Resolve-Path ".").Path
$projectPath = Join-Path $repoRoot "src\Migration.Infrastructure\Migration.Infrastructure.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Could not find $projectPath"
}

[xml]$project = Get-Content $projectPath

$packageReferences = @($project.Project.ItemGroup.PackageReference | Where-Object {
    $_.Include -eq "Microsoft.Extensions.Diagnostics.HealthChecks"
})

foreach ($packageReference in $packageReferences) {
    [void]$packageReference.ParentNode.RemoveChild($packageReference)
}

$project.Save($projectPath)

Write-Host "Removed Microsoft.Extensions.Diagnostics.HealthChecks package reference from $projectPath if it existed."
