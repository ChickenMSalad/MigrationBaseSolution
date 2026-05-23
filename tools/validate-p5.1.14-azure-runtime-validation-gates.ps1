Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot) { return $PSScriptRoot }
    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) { return (Split-Path -Parent $invocationPath) }
    return (Get-Location).Path
}

function Get-RepoRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Test-XmlPropertyExists {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return $false }
    return $null -ne $Node.PSObject.Properties[$Name]
}

function Get-XmlChildren {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if (-not (Test-XmlPropertyExists -Node $Node -Name $Name)) { return @() }
    $value = $Node.PSObject.Properties[$Name].Value
    if ($null -eq $value) { return @() }
    return @($value)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$repoRoot = Get-RepoRoot

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationGateDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationGateRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationGateResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationGateSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationGateStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\AzureRuntimeValidationSummary.cs',
    'config\azure-runtime\validation\runtime-validation-gates.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.1.14 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$projectFiles = @(Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' })

$inlineVersions = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = Get-XmlChildren -Node $projectXml.Project -Name 'ItemGroup'
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = Get-XmlChildren -Node $itemGroup -Name 'PackageReference'
        foreach ($packageRef in $packageRefs) {
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                $inlineVersions += $projectFile.FullName
            }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in (@($inlineVersions) | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    Write-Host 'MigrationBase.Core project not found; skipping project build.' -ForegroundColor Yellow
}

Write-Host 'P5.1.14 Azure runtime validation gates validation passed.' -ForegroundColor Green
