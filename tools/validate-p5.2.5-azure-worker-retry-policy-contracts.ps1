Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) { return (Split-Path -Parent $invocationPath) }
    return (Get-Location).Path
}

function Get-RepoRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Get-XmlChildElements {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) { return @() }
    $property = $Node.PSObject.Properties[$Name]
    if ($null -eq $property) { return @() }
    if ($null -eq $property.Value) { return @() }
    return @($property.Value)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$repoRoot = Get-RepoRoot

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\AzureWorkerRetryPolicyDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\AzureWorkerRetryFailureDispositions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\AzureWorkerRetryPolicyValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\AzureWorkerRetryPolicyValidator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\IAzureWorkerRetryPolicyRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Retry\AzureWorkerRetryPolicyRegistry.cs',
    'config\azure-runtime\workers\retry-policies.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$projectFiles = @(Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersions = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = Get-XmlChildElements -Node $projectXml.Project -Name 'ItemGroup'
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = Get-XmlChildElements -Node $itemGroup -Name 'PackageReference'
        foreach ($packageRef in $packageRefs) {
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) {
                $inlineVersions.Add($projectFile.FullName)
            }
        }
    }
}

if (@($inlineVersions).Count -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + ((@($inlineVersions) | Sort-Object -Unique) -join "`n - "))
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -Path $coreProject

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.5 Azure worker retry policy contract validation passed.'
