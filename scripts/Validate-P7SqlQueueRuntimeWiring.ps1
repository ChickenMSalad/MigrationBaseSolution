Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRootSafe {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) {
        return $PSScriptRoot
    }

    $invocation = $MyInvocation
    if ($invocation -and $invocation.MyCommand -and $invocation.MyCommand.Path) {
        return Split-Path -Parent $invocation.MyCommand.Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $true
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()

    if ($normalized.Contains('\bin\')) {
        return $true
    }

    if ($normalized.Contains('\obj\')) {
        return $true
    }

    return $false
}

function Get-XmlDocumentSafe {
    param([string] $Path)

    $content = Get-Content -LiteralPath $Path -Raw
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.LoadXml($content)
    return $xml
}

$scriptRoot = Get-ScriptRootSafe
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')

$requiredFiles = @(
    'database\sql\p7\003_operational_queue_runtime_wiring.sql',
    'src\Migration.Infrastructure\Runtime\SqlServer\SqlOperationalQueueRuntimeModels.cs',
    'src\Migration.Infrastructure\Runtime\SqlServer\SqlOperationalQueueRuntime.cs',
    'src\Migration.Infrastructure\Runtime\SqlServer\SqlOperationalRunStore.cs'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing required P7.2 file: $relativePath"
    }
}

$p71RequiredFiles = @(
    'database\sql\p7\001_operational_runtime_store.sql',
    'database\sql\p7\002_operational_queue_procedures.sql',
    'src\Migration.Infrastructure\Runtime\SqlServer\ISqlOperationalConnectionFactory.cs',
    'src\Migration.Infrastructure\Runtime\SqlServer\SqlOperationalStoreModels.cs',
    'src\Migration.Infrastructure\Runtime\SqlServer\SqlOperationalQueueStore.cs'
)

foreach ($relativePath in $p71RequiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "P7.2 requires P7.1 file before integration: $relativePath"
    }
}

$sqlPath = Join-Path $repoRoot 'database\sql\p7\003_operational_queue_runtime_wiring.sql'
$sqlText = Get-Content -LiteralPath $sqlPath -Raw
$requiredSqlTokens = @(
    'usp_EnqueueManifestWorkItems',
    'usp_StartMigrationRun',
    'usp_CompleteMigrationRunIfDrained',
    'usp_GetRunOperationalSummary',
    'usp_GetRunnableMigrationRuns'
)

foreach ($token in $requiredSqlTokens) {
    if ($sqlText.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "P7.2 SQL script is missing expected token: $token"
    }
}

$csFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\Migration.Infrastructure\Runtime\SqlServer') -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
foreach ($file in @($csFiles)) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    if ($text.IndexOf('PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "C# runtime source unexpectedly contains PackageReference text: $($file.FullName)"
    }
}

$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Filter '*.csproj' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
foreach ($projectFile in @($projectFiles)) {
    $xml = Get-XmlDocumentSafe -Path $projectFile.FullName
    $packageReferences = @($xml.SelectNodes('//*[local-name()="PackageReference"]'))
    foreach ($packageReference in $packageReferences) {
        $versionAttribute = $packageReference.Attributes.GetNamedItem('Version')
        if ($versionAttribute -ne $null) {
            throw "Inline PackageReference Version attribute found in $($projectFile.FullName). Use Directory.Packages.props instead."
        }
    }
}

Write-Host 'P7.2 SQL queue runtime wiring validation passed.'
