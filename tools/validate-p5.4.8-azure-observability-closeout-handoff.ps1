Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) { return (Split-Path -Parent $invocationPath) }
    return (Get-Location).Path
}

function Get-RepositoryRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory=$true)][string]$Root)

    $projectFiles = @(Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.csproj' -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
        $projectNode = $projectXml.Project
        if ($null -eq $projectNode) { continue }
        if (-not $projectNode.PSObject.Properties.Match('ItemGroup').Count) { continue }

        foreach ($itemGroup in @($projectNode.ItemGroup)) {
            if ($null -eq $itemGroup) { continue }
            if (-not $itemGroup.PSObject.Properties.Match('PackageReference').Count) { continue }

            foreach ($packageReference in @($itemGroup.PackageReference)) {
                if ($null -eq $packageReference) { continue }
                $versionProperty = $packageReference.PSObject.Properties['Version']
                $versionAttribute = $packageReference.GetAttribute('Version')
                if ($null -ne $versionProperty -or -not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                    throw "Inline PackageReference Version detected in $($projectFile.FullName)."
                }
            }
        }
    }
}

$root = Get-RepositoryRoot

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureObservabilityCloseoutGate.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureObservabilityCloseoutGateSeverity.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureObservabilityHandoffDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureObservabilityCloseoutReport.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureObservabilityCloseoutGateResult.cs',
    'config/azure-runtime/observability/observability-closeout.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $root $relativePath)
}

Assert-NoInlinePackageVersions -Root $root

$coreProject = Join-Path $root 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
Assert-FileExists -Path $coreProject

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.4.8 Azure observability closeout handoff validation passed.'
