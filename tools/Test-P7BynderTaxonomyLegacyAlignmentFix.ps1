Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Read-TextFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw "Missing expected file: $Path" }
    return [System.IO.File]::ReadAllText($Path)
}

$endpointPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
$builderPath = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Taxonomy\BynderTaxonomyWorkbookBuilder.cs'
$registrationPath = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Registration\ServiceCollectionExtensions.cs'
$servicePath = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Services\BynderMetadataPropertiesService.cs'

$endpoint = Read-TextFile -Path $endpointPath
$builder = Read-TextFile -Path $builderPath
$registration = Read-TextFile -Path $registrationPath
$service = Read-TextFile -Path $servicePath

$failures = New-Object System.Collections.Generic.List[string]

if ($endpoint -notmatch 'BynderTaxonomyWorkbookBuilder') { $failures.Add('Admin endpoint does not resolve BynderTaxonomyWorkbookBuilder.') }
if ($endpoint -match 'BynderRestClient|RestRequest|api/v4/metaproperties|/v6/authentication/oauth2/token|client_secret') { $failures.Add('Admin endpoint still contains direct Bynder vendor API/auth implementation.') }
if ($endpoint -match 'ReadBynderTaxonomyAsync|BynderMetapropertyObjects|BynderMetapropertyOptions') { $failures.Add('Admin endpoint still contains direct Bynder taxonomy parsing helpers.') }
if ($endpoint -notmatch 'BuildBynderMetadataPropertiesWorkbookAsync') { $failures.Add('Admin endpoint is missing connector-owned metadata workbook path.') }
if ($endpoint -notmatch 'BuildBynderBlankMetadataTemplateAsync') { $failures.Add('Admin endpoint is missing connector-owned blank template path.') }

if ($builder -notmatch 'MetapropertyOptionBuilderFactoryApi') { $failures.Add('Connector workbook builder does not reuse MetapropertyOptionBuilderFactoryApi.') }
if ($builder -notmatch 'ExcelWriter\.WriteDataTables') { $failures.Add('Connector workbook builder does not reuse ExcelWriter.WriteDataTables.') }
if ($builder -notmatch 'global::Bynder\.Sdk\.Settings\.Configuration') { $failures.Add('Connector workbook builder does not use Bynder SDK Configuration.') }

if ($registration -notmatch 'TryAddSingleton<BynderTaxonomyWorkbookBuilder>') { $failures.Add('Bynder connector DI registration is missing BynderTaxonomyWorkbookBuilder.') }
if ($service -match 'var rowOptionArray = new List<object>\s*var rowOptionArray') { $failures.Add('Duplicate rowOptionArray typo still exists.') }

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "P7 Bynder taxonomy legacy alignment validation failed with $($failures.Count) failure(s)."
}

Write-Host 'PASS: P7 Bynder taxonomy legacy alignment fix is present.'
