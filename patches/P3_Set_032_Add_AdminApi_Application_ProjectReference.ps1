$repoRoot = (Resolve-Path ".").Path
$projectPath = Join-Path $repoRoot "src\Migration.Admin.Api\Migration.Admin.Api.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Could not find $projectPath"
}

[xml]$project = Get-Content $projectPath

$projectReferenceInclude = "..\Migration.Application\Migration.Application.csproj"

$existing = @($project.Project.ItemGroup.ProjectReference | Where-Object {
    $_.Include -eq $projectReferenceInclude
})

if ($existing.Count -gt 0) {
    Write-Host "Migration.Application project reference already exists."
    exit 0
}

$itemGroup = $project.Project.ItemGroup | Where-Object {
    $_.ProjectReference
} | Select-Object -First 1

if (-not $itemGroup) {
    $itemGroup = $project.CreateElement("ItemGroup")
    [void]$project.Project.AppendChild($itemGroup)
}

$projectReference = $project.CreateElement("ProjectReference")
$includeAttribute = $project.CreateAttribute("Include")
$includeAttribute.Value = $projectReferenceInclude
[void]$projectReference.Attributes.Append($includeAttribute)
[void]$itemGroup.AppendChild($projectReference)

$project.Save($projectPath)

Write-Host "Added Migration.Application project reference to $projectPath"
