[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    return Join-Path $RepoRoot $RelativePath
}

function Add-TextIfMissing {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Text,

        [Parameter(Mandatory = $true)]
        [scriptblock] $Updater
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -ge 0) {
        return $false
    }

    $updated = & $Updater $content
    if ([string]::IsNullOrWhiteSpace($updated)) {
        throw ('Updater returned empty content for {0}' -f $Path)
    }

    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    return $true
}

$programPath = Resolve-RepoPath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs'
$projectPath = Resolve-RepoPath 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj'
$mappingPath = Resolve-RepoPath 'src\Workers\Migration.Workers.ServiceBusExecutor\runtime-smoke.mapping.json'

foreach ($requiredPath in @($programPath, $projectPath, $mappingPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw ('Required file is missing: {0}' -f $requiredPath)
    }
}

$usingLine = 'using Migration.Workers.ServiceBusExecutor.Smoke;'
[void](Add-TextIfMissing -Path $programPath -Text $usingLine -Updater {
    param($content)
    return $usingLine + [Environment]::NewLine + $content
})

$registrationLine = 'builder.Services.AddRuntimeSmokeExecutionProviders();'
[void](Add-TextIfMissing -Path $programPath -Text $registrationLine -Updater {
    param($content)

    $anchor = 'builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);'
    if ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Could not find expected Program.cs anchor: {0}' -f $anchor)
    }

    return $content.Replace(
        $anchor,
        $anchor + [Environment]::NewLine + $registrationLine)
})

[xml] $projectXml = Get-Content -LiteralPath $projectPath -Raw
$project = $projectXml.Project
if ($null -eq $project) {
    throw 'Executor project file is missing Project root element.'
}

$itemGroups = @($project.ItemGroup)
$existingMappingItems = @()
foreach ($itemGroup in $itemGroups) {
    if ($null -eq $itemGroup) { continue }

    $noneItems = @()
    $noneProperty = $itemGroup.PSObject.Properties['None']
    if ($null -ne $noneProperty -and $null -ne $noneProperty.Value) {
        $noneItems = @($noneProperty.Value)
    }

    foreach ($noneItem in $noneItems) {
        if ($null -eq $noneItem) { continue }
        $updateAttribute = $noneItem.GetAttribute('Update')
        $includeAttribute = $noneItem.GetAttribute('Include')
        if ($updateAttribute -eq 'runtime-smoke.mapping.json' -or $includeAttribute -eq 'runtime-smoke.mapping.json') {
            $existingMappingItems += $noneItem
        }
    }

    $contentItems = @()
    $contentProperty = $itemGroup.PSObject.Properties['Content']
    if ($null -ne $contentProperty -and $null -ne $contentProperty.Value) {
        $contentItems = @($contentProperty.Value)
    }

    foreach ($contentItem in $contentItems) {
        if ($null -eq $contentItem) { continue }
        $updateAttribute = $contentItem.GetAttribute('Update')
        $includeAttribute = $contentItem.GetAttribute('Include')
        if ($updateAttribute -eq 'runtime-smoke.mapping.json' -or $includeAttribute -eq 'runtime-smoke.mapping.json') {
            $existingMappingItems += $contentItem
        }
    }
}

if (@($existingMappingItems).Count -eq 0) {
    $itemGroup = $projectXml.CreateElement('ItemGroup')
    $none = $projectXml.CreateElement('None')
    $none.SetAttribute('Update', 'runtime-smoke.mapping.json')

    $copyOutput = $projectXml.CreateElement('CopyToOutputDirectory')
    $copyOutput.InnerText = 'PreserveNewest'
    [void] $none.AppendChild($copyOutput)

    $copyPublish = $projectXml.CreateElement('CopyToPublishDirectory')
    $copyPublish.InnerText = 'PreserveNewest'
    [void] $none.AppendChild($copyPublish)

    [void] $itemGroup.AppendChild($none)
    [void] $project.AppendChild($itemGroup)
    $projectXml.Save($projectPath)
}

Write-Host 'Runtime smoke provider registration applied.'
