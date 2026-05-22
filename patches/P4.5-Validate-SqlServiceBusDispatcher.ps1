[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.5-VALIDATE] $Message" -ForegroundColor Cyan
}

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Required file not found: {0}" -f $Path)
    }
}

function Assert-TextExists {
    param([string]$Path, [string]$Text)
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = Get-Location
while (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'MigrationBaseSolution.sln'))) {
    $parent = Split-Path -Parent $repoRoot
    if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $repoRoot) {
        throw 'Could not locate MigrationBaseSolution.sln.'
    }
    $repoRoot = $parent
}

Set-Location -LiteralPath $repoRoot

$projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj'
$programPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs'
$optionsPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Options\SqlServiceBusDispatcherOptions.cs'
$dispatcherPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs'
$configPath = Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusDispatcher.sample.json'
$packagesPath = Join-Path $repoRoot 'Directory.Packages.props'
$slnPath = Join-Path $repoRoot 'MigrationBaseSolution.sln'

Assert-FileExists -Path $projectPath
Assert-FileExists -Path $programPath
Assert-FileExists -Path $optionsPath
Assert-FileExists -Path $dispatcherPath
Assert-FileExists -Path $configPath

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$packageReferences = @()
foreach ($itemGroup in @($project.Project.ItemGroup)) {
    if ($itemGroup.PSObject.Properties.Name -contains 'PackageReference') {
        $packageReferences += @($itemGroup.PackageReference)
    }
}

foreach ($reference in $packageReferences) {
    if ($reference.PSObject.Properties.Name -contains 'Version') {
        throw ("Inline package version found in {0}" -f $projectPath)
    }
}

Assert-TextExists -Path $packagesPath -Text 'Azure.Messaging.ServiceBus'
Assert-TextExists -Path $programPath -Text 'SqlServiceBusDispatcherOptions'
Assert-TextExists -Path $dispatcherPath -Text 'MigrationWorkItems'
Assert-TextExists -Path $slnPath -Text 'Migration.Workers.ServiceBusDispatcher.csproj'

Write-Step 'P4.5 SQL Service Bus dispatcher validation passed.'
