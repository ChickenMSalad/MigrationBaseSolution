Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$true)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )
    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Heartbeat\AzureWorkerHeartbeatOptions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Heartbeat\AzureWorkerHeartbeatState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Heartbeat\AzureWorkerHeartbeatDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Heartbeat\AzureWorkerHeartbeatEvaluation.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Heartbeat\IAzureWorkerHeartbeatStore.cs',
    'config\azure-runtime\workers\heartbeat.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists -RelativePath $file }

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -RelativePath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

[xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
$badRefs = New-Object System.Collections.Generic.List[string]
$projectNode = $projectXml.Project
if ($null -ne $projectNode -and $projectNode.PSObject.Properties['ItemGroup']) {
    foreach ($itemGroup in @($projectNode.ItemGroup)) {
        if ($null -eq $itemGroup -or -not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $include = Get-XmlAttributeValue -Node $packageRef -Name 'Include'
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) {
                $badRefs.Add("${include} has inline Version ${version}") | Out-Null
            }
        }
    }
}

if (@($badRefs).Count -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + (($badRefs | Sort-Object) -join "`n - "))
}

Push-Location $repoRoot
try {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore .\src\Core\MigrationBase.Core\MigrationBase.Core.csproj
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build .\src\Core\MigrationBase.Core\MigrationBase.Core.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
finally {
    Pop-Location
}

Write-Host 'P5.2.2 Azure worker heartbeat contract validation passed.'
