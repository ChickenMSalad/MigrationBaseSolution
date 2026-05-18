$projectPath = "src/Migration.Infrastructure/Migration.Infrastructure.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Could not find $projectPath"
}

[xml]$project = Get-Content $projectPath

$namespaceManager = New-Object System.Xml.XmlNamespaceManager($project.NameTable)

$existing = $project.Project.ItemGroup.PackageReference | Where-Object {
    $_.Include -eq "Microsoft.Extensions.Diagnostics.HealthChecks"
}

if ($existing) {
    Write-Host "Microsoft.Extensions.Diagnostics.HealthChecks package reference already exists."
    exit 0
}

$itemGroup = $project.Project.ItemGroup | Where-Object {
    $_.PackageReference
} | Select-Object -First 1

if (-not $itemGroup) {
    $itemGroup = $project.CreateElement("ItemGroup")
    [void]$project.Project.AppendChild($itemGroup)
}

$packageReference = $project.CreateElement("PackageReference")
$includeAttribute = $project.CreateAttribute("Include")
$includeAttribute.Value = "Microsoft.Extensions.Diagnostics.HealthChecks"
[void]$packageReference.Attributes.Append($includeAttribute)
[void]$itemGroup.AppendChild($packageReference)

$project.Save($projectPath)

Write-Host "Added Microsoft.Extensions.Diagnostics.HealthChecks package reference to $projectPath"
