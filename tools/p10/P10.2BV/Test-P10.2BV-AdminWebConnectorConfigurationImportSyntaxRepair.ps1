Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script path.'
        }

        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        if ([System.IO.Directory]::Exists($candidate)) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web.'
        }

        $current = $parent.FullName
    }
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not [System.IO.File]::Exists($Path)) {
        throw ('Expected file missing: {0}' -f $Path)
    }
}

$repoRoot = Get-RepoRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$pagePath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'features', 'connectors', 'configuration', 'pages', 'ConnectorConfiguration.tsx')
$apiPath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'features', 'connectors', 'configuration', 'api', 'connectorConfigurationApi.ts')
$typePath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'features', 'connectors', 'configuration', 'types', 'connectorConfiguration.ts')
$cardPath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'components', 'Card.tsx')
$loadingPath = [System.IO.Path]::Combine($adminWebRoot, 'src', 'components', 'LoadingError.tsx')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BV-AdminWebConnectorConfigurationImportSyntaxRepair.Report.md')

Assert-FileExists -Path $pagePath
Assert-FileExists -Path $apiPath
Assert-FileExists -Path $typePath
Assert-FileExists -Path $cardPath
Assert-FileExists -Path $loadingPath
Assert-FileExists -Path $reportPath

$content = [System.IO.File]::ReadAllText($pagePath)

$requiredSources = @(
    '../api/connectorConfigurationApi',
    '../types/connectorConfiguration',
    '../../../../components/Card',
    '../../../../components/LoadingError'
)

foreach ($source in $requiredSources) {
    if (-not $content.Contains(('from "{0}"' -f $source))) {
        throw ('Expected import source missing from ConnectorConfiguration.tsx: {0}' -f $source)
    }
}

$badTokens = @(
    '../api/connectorConfigurationApi.tsx',
    '../types/connectorConfiguration.tsx',
    '../../../../components/Card.tsx',
    '../../../../components/LoadingError.tsx',
    '"";',
    ''''';'
)

foreach ($badToken in $badTokens) {
    if ($content.Contains($badToken)) {
        throw ('Unexpected malformed import token found in ConnectorConfiguration.tsx: {0}' -f $badToken)
    }
}

Write-Host 'P10.2BV Admin Web Connector Configuration import syntax repair validation passed.'
