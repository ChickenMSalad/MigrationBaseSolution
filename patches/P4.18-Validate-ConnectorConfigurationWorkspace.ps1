[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.18] $Message"
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Expected file not found: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    Assert-FileExists -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$endpointPath = Join-Path $repoRoot "src\Core\Migration.Admin.Api\Endpoints\Operational\Connectors\OperationalConnectorConfigurationEndpointExtensions.cs"
$programPath = Join-Path $repoRoot "src\Core\Migration.Admin.Api\Program.cs"
$appPath = Join-Path $repoRoot "apps\migration-admin-ui\src\App.tsx"
$workspacePath = Join-Path $repoRoot "apps\migration-admin-ui\src\features\connectors\ConnectorConfigurationWorkspace.tsx"
$apiPath = Join-Path $repoRoot "apps\migration-admin-ui\src\features\connectors\connectorConfigurationApi.ts"
$typesPath = Join-Path $repoRoot "apps\migration-admin-ui\src\features\connectors\connectorConfigurationTypes.ts"
$docPath = Join-Path $repoRoot "docs\operations\P4.18-connector-configuration-workspace.md"

Assert-FileContains -Path $endpointPath -Text "MapOperationalConnectorConfigurationEndpoints"
Assert-FileContains -Path $programPath -Text "MapOperationalConnectorConfigurationEndpoints"
Assert-FileContains -Path $appPath -Text "ConnectorConfigurationWorkspace"
Assert-FileContains -Path $workspacePath -Text "Connector Configuration"
Assert-FileContains -Path $apiPath -Text "fetchConnectorConfigurationSummary"
Assert-FileContains -Path $typesPath -Text "ConnectorConfigurationSummary"
Assert-FileContains -Path $docPath -Text "P4.18 Connector Configuration Workspace"

Write-Step "Validation passed."
