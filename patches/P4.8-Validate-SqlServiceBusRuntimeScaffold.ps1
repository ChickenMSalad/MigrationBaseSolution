[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Assert-File {
    param([Parameter(Mandatory = $true)][string] $RelativePath)

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw ("Expected file not found: {0}" -f $RelativePath)
    }

    Write-Host ("[P4.8] OK file: {0}" -f $RelativePath)
}

function Assert-Text {
    param(
        [Parameter(Mandatory = $true)][string] $RelativePath,
        [Parameter(Mandatory = $true)][string] $ExpectedText
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw ("Expected file not found: {0}" -f $RelativePath)
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($content -notlike ("*{0}*" -f $ExpectedText)) {
        throw ("Expected text not found in {0}: {1}" -f $RelativePath, $ExpectedText)
    }

    Write-Host ("[P4.8] OK text: {0}" -f $ExpectedText)
}

Assert-File "deploy/azure/sql-servicebus-runtime/main.bicep"
Assert-File "deploy/azure/sql-servicebus-runtime/main.parameters.example.json"
Assert-File "deploy/azure/sql-servicebus-runtime/deploy-sql-servicebus-runtime.ps1"
Assert-File "deploy/azure/sql-servicebus-runtime/README.md"
Assert-File "config-samples/appsettings.P4.8.AzureRuntime.sample.json"
Assert-File "docs/azure/P4.8_SQL_SERVICEBUS_RUNTIME_SCAFFOLD.md"

Assert-Text "deploy/azure/sql-servicebus-runtime/main.bicep" "Microsoft.Sql/servers"
Assert-Text "deploy/azure/sql-servicebus-runtime/main.bicep" "Microsoft.ServiceBus/namespaces"
Assert-Text "deploy/azure/sql-servicebus-runtime/main.bicep" "migration-work-items"
Assert-Text "deploy/azure/sql-servicebus-runtime/main.bicep" "Microsoft.App/managedEnvironments"
Assert-Text "config-samples/appsettings.P4.8.AzureRuntime.sample.json" "MigrationOperationalStore"

Write-Host "[P4.8] Validation passed."
