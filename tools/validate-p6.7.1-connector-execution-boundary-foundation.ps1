Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

function Assert-NoInlinePackageVersions {
    $projectFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
        Where-Object { $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' }

    $violations = New-Object System.Collections.Generic.List[string]

    foreach ($project in $projectFiles) {
        [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw
        if ($null -eq $xml.Project) { continue }

        $itemGroups = @()
        if ($xml.Project.PSObject.Properties.Name -contains 'ItemGroup') {
            $itemGroups = @($xml.Project.ItemGroup)
        }

        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup) { continue }
            if (-not ($itemGroup.PSObject.Properties.Name -contains 'PackageReference')) { continue }

            foreach ($packageReference in @($itemGroup.PackageReference)) {
                if ($null -eq $packageReference) { continue }

                $hasVersionProperty = $packageReference.PSObject.Properties.Name -contains 'Version'
                $hasVersionAttribute = $false

                if ($null -ne $packageReference.Attributes) {
                    $attribute = $packageReference.Attributes.GetNamedItem('Version')
                    $hasVersionAttribute = $null -ne $attribute
                }

                if ($hasVersionProperty -or $hasVersionAttribute) {
                    $violations.Add($project.FullName)
                    break
                }
            }
        }
    }

    if ($violations.Count -gt 0) {
        $message = "Inline PackageReference Version attributes detected:`n - " + (($violations | Sort-Object -Unique) -join "`n - ")
        throw $message
    }
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionMode.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionDirection.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionRequest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\IAzureConnectorExecutor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\NoOpAzureConnectorExecutor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ConnectorExecution\AzureConnectorExecutionOptions.cs',
    'config\azure-runtime\connector-execution\connector-execution.sample.json'
)

foreach ($file in $expectedFiles) {
    Assert-FileExists -RelativePath $file
}

Assert-NoInlinePackageVersions

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing MigrationBase.Core project: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.7.1 connector execution boundary foundation validation passed.'
