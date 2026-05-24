Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Assert-PathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$RootPath,
        [string]$RelativePath,
        [string]$Text
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.1C-KeyVault-SecretProvider-Baseline.md'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Azure.template.json'
Assert-PathExists -RootPath $root -RelativePath 'scripts\Write-P81CCloudSecretInventory.ps1'

Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1C-KeyVault-SecretProvider-Baseline.md' -Text 'MIGRATION_ConnectionStrings__MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1C-KeyVault-SecretProvider-Baseline.md' -Text 'Do not configure `SqlOperationalQueueExecutor:RunId` in production'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Azure.template.json' -Text 'SqlOperationalQueueExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Azure.template.json' -Text 'RunUntilIdleAndStop'

$programFile = Join-Path $root 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
if (Test-Path -LiteralPath $programFile) {
    $program = Get-Content -LiteralPath $programFile -Raw
    if ($program -notlike '*AddEnvironmentVariables(prefix: "MIGRATION_")*') {
        throw 'SQL operational worker Program.cs does not contain AddEnvironmentVariables(prefix: "MIGRATION_").'
    }
}
else {
    throw 'SQL operational worker Program.cs was not found.'
}

Write-Host 'P8.1C Key Vault / secret provider baseline validation passed.'
