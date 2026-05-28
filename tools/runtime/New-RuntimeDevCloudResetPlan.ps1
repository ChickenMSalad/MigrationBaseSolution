[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $DispatcherApp,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ExecutorApp,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ServiceBusNamespace,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ServiceBusQueue,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = '.\artifacts\runtime-reset\runtime-dev-cloud-reset-plan.md'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$outputFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')

$lines = @()
$lines += '# Runtime Dev Cloud Reset Plan'
$lines += ''
$lines += ('Generated UTC: {0}' -f $generatedUtc)
$lines += ''
$lines += '## Target'
$lines += ''
$lines += ('- Resource group: `{0}`' -f $ResourceGroup)
$lines += ('- Dispatcher app: `{0}`' -f $DispatcherApp)
$lines += ('- Executor app: `{0}`' -f $ExecutorApp)
$lines += ('- SQL server: `{0}`' -f $SqlServer)
$lines += ('- Database: `{0}`' -f $Database)
$lines += ('- Service Bus namespace: `{0}`' -f $ServiceBusNamespace)
$lines += ('- Service Bus queue: `{0}`' -f $ServiceBusQueue)
$lines += ''
$lines += '## Required evidence before reset'
$lines += ''
$lines += '```powershell'
$lines += 'New-Item -ItemType Directory -Force -Path .\artifacts\runtime-reset | Out-Null'
$lines += ('az webapp config appsettings list --resource-group {0} --name {1} -o json > .\artifacts\runtime-reset\dispatcher-appsettings.before.json' -f $ResourceGroup, $DispatcherApp)
$lines += ('az webapp config appsettings list --resource-group {0} --name {1} -o json > .\artifacts\runtime-reset\executor-appsettings.before.json' -f $ResourceGroup, $ExecutorApp)
$lines += '```'
$lines += ''
$lines += '## Stop workers before destructive SQL cleanup'
$lines += ''
$lines += '```powershell'
$lines += ('az webapp stop --resource-group {0} --name {1}' -f $ResourceGroup, $DispatcherApp)
$lines += ('az webapp stop --resource-group {0} --name {1}' -f $ResourceGroup, $ExecutorApp)
$lines += '```'
$lines += ''
$lines += '## SQL readiness diagnostics'
$lines += ''
$lines += 'Run `tools\runtime\Invoke-RuntimeDevResetReadiness.ps1` and review the output before any destructive cleanup.'
$lines += ''
$lines += '## Destructive cleanup gate'
$lines += ''
$lines += 'Only after reviewing diagnostics, edit `database\sql\p7\017_runtime_dev_reset_cleanup_template.sql` and set `@AllowDestructiveReset = 1` intentionally.'
$lines += ''
$lines += '## Service Bus dead letters'
$lines += ''
$lines += 'Do not assume Azure CLI can purge dead letters in this environment. If dead-letter cleanup is required, use a reviewed Service Bus management script or portal operation and record evidence.'
$lines += ''
$lines += '## Restart after cleanup'
$lines += ''
$lines += '```powershell'
$lines += ('az webapp start --resource-group {0} --name {1}' -f $ResourceGroup, $ExecutorApp)
$lines += ('az webapp start --resource-group {0} --name {1}' -f $ResourceGroup, $DispatcherApp)
$lines += '```'

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Runtime dev cloud reset plan written to {0}' -f $outputFullPath)
