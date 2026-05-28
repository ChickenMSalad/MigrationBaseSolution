[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ConfigurationPath,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = 'artifacts\clean-dev-runtime-rebuild\clean-dev-runtime-rebuild-plan.ps1'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path (Get-Location).Path $Path)
}

function Quote-Single {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Value
    )

    return "'" + ($Value -replace "'", "''") + "'"
}

$configFullPath = Resolve-FullPath -Path $ConfigurationPath
if (-not (Test-Path -LiteralPath $configFullPath)) {
    throw ('Configuration file not found: {0}' -f $ConfigurationPath)
}

$config = Get-Content -LiteralPath $configFullPath -Raw | ConvertFrom-Json
$requiredProperties = @(
    'resourceGroup',
    'dispatcherApp',
    'executorApp',
    'sqlServer',
    'database',
    'serviceBusNamespace',
    'serviceBusQueue',
    'dispatcherZipPath',
    'executorZipPath',
    'canonicalSqlScripts',
    'evidenceOutputPath'
)

foreach ($propertyName in $requiredProperties) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Missing required rebuild configuration property: {0}' -f $propertyName)
    }
}

$outputFullPath = Resolve-FullPath -Path $OutputPath
$outputDirectory = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Generated clean dev runtime rebuild plan.')
$lines.Add('# Review before running. This plan intentionally does not contain SQL passwords.')
$lines.Add('Set-StrictMode -Version 2.0')
$lines.Add('$ErrorActionPreference = ''Stop''')
$lines.Add('')
$lines.Add('$resourceGroup = ' + (Quote-Single ([string]$config.resourceGroup)))
$lines.Add('$dispatcherApp = ' + (Quote-Single ([string]$config.dispatcherApp)))
$lines.Add('$executorApp = ' + (Quote-Single ([string]$config.executorApp)))
$lines.Add('$sqlServer = ' + (Quote-Single ([string]$config.sqlServer)))
$lines.Add('$database = ' + (Quote-Single ([string]$config.database)))
$lines.Add('$serviceBusNamespace = ' + (Quote-Single ([string]$config.serviceBusNamespace)))
$lines.Add('$serviceBusQueue = ' + (Quote-Single ([string]$config.serviceBusQueue)))
$lines.Add('$evidencePath = ' + (Quote-Single ([string]$config.evidenceOutputPath)))
$lines.Add('')
$lines.Add('New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null')
$lines.Add('')
$lines.Add('az webapp config appsettings list --resource-group $resourceGroup --name $dispatcherApp -o json > (Join-Path $evidencePath ''dispatcher-appsettings.before.json'')')
$lines.Add('az webapp config appsettings list --resource-group $resourceGroup --name $executorApp -o json > (Join-Path $evidencePath ''executor-appsettings.before.json'')')
$lines.Add('az servicebus queue show --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name $serviceBusQueue -o json > (Join-Path $evidencePath ''servicebus-queue.before.json'')')
$lines.Add('')
$lines.Add('# Apply SQL scripts manually with sqlcmd after setting $sqlAdmin and $sqlPasswordPlain.')
foreach ($script in @($config.canonicalSqlScripts)) {
    $lines.Add('# sqlcmd -S "$sqlServer.database.windows.net" -d $database -U $sqlAdmin -P $sqlPasswordPlain -i ' + (Quote-Single ([string]$script)))
}
$lines.Add('')
$lines.Add('az webapp deploy --resource-group $resourceGroup --name $dispatcherApp --src-path ' + (Quote-Single ([string]$config.dispatcherZipPath)) + ' --type zip --clean true --restart true')
$lines.Add('az webapp deploy --resource-group $resourceGroup --name $executorApp --src-path ' + (Quote-Single ([string]$config.executorZipPath)) + ' --type zip --clean true --restart true')
$lines.Add('')
$lines.Add('az webapp restart --resource-group $resourceGroup --name $dispatcherApp')
$lines.Add('az webapp restart --resource-group $resourceGroup --name $executorApp')
$lines.Add('')
$lines.Add('az servicebus queue show --resource-group $resourceGroup --namespace-name $serviceBusNamespace --name $serviceBusQueue -o json > (Join-Path $evidencePath ''servicebus-queue.after.json'')')
$lines.Add('Write-Host ''Clean dev runtime rebuild command plan complete.''')

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Wrote clean dev runtime rebuild plan: {0}' -f $outputFullPath)
