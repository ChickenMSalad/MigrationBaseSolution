Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return $normalized.Contains('\bin\') -or $normalized.Contains('\obj\')
}

$repositoryRoot = Get-RepositoryRoot
$outputDirectory = Join-Path $repositoryRoot 'artifacts\p7'
$outputPath = Join-Path $outputDirectory 'P7-Runtime-Inventory.txt'

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('P7 Runtime Inventory')
$lines.Add(('Generated: {0:O}' -f (Get-Date)))
$lines.Add('')
$lines.Add('Core runtime projects:')

$coreProjects = @(
    'src\Core\Migration.Infrastructure\Migration.Infrastructure.csproj',
    'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj',
    'src\Core\Migration.GenericRuntime\Migration.GenericRuntime.csproj',
    'src\Core\Migration.Orchestration\Migration.Orchestration.csproj',
    'src\Core\Migration.Application\Migration.Application.csproj',
    'src\Core\Migration.Domain\Migration.Domain.csproj',
    'src\Core\Migration.Shared\Migration.Shared.csproj'
)

foreach ($relativePath in $coreProjects) {
    $exists = Test-Path -LiteralPath (Join-Path $repositoryRoot $relativePath)
    $mark = ' '
    if ($exists) {
        $mark = 'x'
    }
    $lines.Add(('- [{0}] {1}' -f $mark, $relativePath))
}

$lines.Add('')
$lines.Add('Worker/host projects:')
$workerProjects = @(
    'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj',
    'src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj',
    'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj',
    'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj',
    'src\Hosts\Migration.Hosts.GenericMigration.Console\Migration.Hosts.GenericMigration.Console.csproj',
    'src\Hosts\Migration.Runner.Cli\Migration.Runner.Cli.csproj'
)

foreach ($relativePath in $workerProjects) {
    $exists = Test-Path -LiteralPath (Join-Path $repositoryRoot $relativePath)
    $mark = ' '
    if ($exists) {
        $mark = 'x'
    }
    $lines.Add(('- [{0}] {1}' -f $mark, $relativePath))
}

$lines.Add('')
$lines.Add('P7 SQL scripts:')
$sqlScripts = Get-ChildItem -Path (Join-Path $repositoryRoot 'database\sql\p7') -Filter '*.sql' -File -ErrorAction SilentlyContinue |
    Sort-Object Name
foreach ($script in @($sqlScripts)) {
    $lines.Add(('- {0}' -f $script.FullName.Substring($repositoryRoot.Length + 1)))
}

$lines.Add('')
$lines.Add('Potential runtime keywords:')
$keywords = @('QueueExecutor', 'GenericRuntime', 'Orchestration', 'WorkItem', 'Manifest', 'Retry', 'DeadLetter')
foreach ($keyword in $keywords) {
    $matches = Get-ChildItem -Path (Join-Path $repositoryRoot 'src') -File -Recurse -Include *.cs |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
        Select-String -Pattern $keyword -SimpleMatch -ErrorAction SilentlyContinue
    $count = @($matches).Count
    $lines.Add(('- {0}: {1} matches' -f $keyword, $count))
}

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "P7 runtime inventory written to: $outputPath"
