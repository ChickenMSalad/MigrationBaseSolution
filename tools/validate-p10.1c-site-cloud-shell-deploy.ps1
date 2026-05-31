[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs/p10/P10.1C-Site-Cloud-Shell-Deploy.md',
    'config-samples/p10-admin-api-cloud-shell-deploy.sample.json',
    'tools/runtime/New-P101AdminApiCloudDeployCommands.ps1',
    'tools/runtime/Test-P101AdminApiCloudShell.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.1C file is missing: {0}' -f $relativePath)
    }
}

$scriptFiles = @(
    'tools/runtime/New-P101AdminApiCloudDeployCommands.ps1',
    'tools/runtime/Test-P101AdminApiCloudShell.ps1'
)

foreach ($relativeScript in $scriptFiles) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $text = Get-Content -LiteralPath $scriptPath -Raw
    $fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($text.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }

    if ($text.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $text.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples/p10-admin-api-cloud-shell-deploy.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('resourceGroup', 'adminApiApp', 'projectPath', 'publishPath', 'zipPath', 'healthUrls')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample config missing property: {0}' -f $propertyName)
    }
}

$docPath = [System.IO.Path]::Combine($repoRoot, 'docs/p10/P10.1C-Site-Cloud-Shell-Deploy.md')
$docText = Get-Content -LiteralPath $docPath -Raw
foreach ($term in @('Migration.Admin.Api.csproj', 'Migration.Admin.Web', 'Swagger')) {
    if ($docText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('P10.1C documentation missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.1C site cloud shell deploy validation passed.'
