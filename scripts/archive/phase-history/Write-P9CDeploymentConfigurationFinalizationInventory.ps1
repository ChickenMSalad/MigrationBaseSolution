Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    if ($normalized.Contains('\.git\')) { return $true }
    if ($normalized.Contains('\bin\')) { return $true }
    if ($normalized.Contains('\obj\')) { return $true }
    if ($normalized.Contains('\.vs\')) { return $true }
    if ($normalized.Contains('\node_modules\')) { return $true }
    if ($normalized.Contains('\tools\dropins\')) { return $true }
    if ($normalized.Contains('\packages\')) { return $true }
    return $false
}

function Add-Line {
    param([string]$Text)
    $script:Lines.Add($Text) | Out-Null
}

function Add-FileSummary {
    param([string]$RelativePath, [string[]]$Patterns)
    Add-Line ""
    Add-Line "## $RelativePath"
    Add-Line ""
    $path = Join-Path $script:Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line 'Missing.'
        return
    }
    Add-Line 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line ("- Contains: ``{0}``" -f $pattern)
        }
        else {
            Add-Line ("- Missing: ``{0}``" -f $pattern)
        }
    }
}

function Add-BoundedSearchSection {
    param([string]$Title, [string]$SearchRoot, [string]$Pattern, [int]$MaxMatches)
    Add-Line ""
    Add-Line "## $Title"
    Add-Line ""
    $rootPath = Join-Path $script:Root $SearchRoot
    if (-not (Test-Path -LiteralPath $rootPath)) {
        Add-Line "Missing search root: $SearchRoot"
        return
    }
    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $rootPath -File -Include '*.cs','*.json','*.props','*.csproj','*.md' -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
        Select-Object -First 250
    foreach ($file in $files) {
        if ($matches.Count -ge $MaxMatches) { break }
        $relative = $file.FullName.Substring($script:Root.Length + 1)
        $lineNumber = 0
        foreach ($line in (Get-Content -LiteralPath $file.FullName)) {
            $lineNumber++
            if ($line -like "*$Pattern*") {
                $matches.Add(("- {0}:{1}: {2}" -f $relative, $lineNumber, $line.Trim())) | Out-Null
                if ($matches.Count -ge $MaxMatches) { break }
            }
        }
    }
    if ($matches.Count -eq 0) { Add-Line 'No matches found.' }
    else {
        foreach ($match in $matches) { Add-Line $match }
        if ($matches.Count -ge $MaxMatches) { Add-Line "- Search stopped after $MaxMatches matches." }
    }
}

$script:Root = Get-RepositoryRoot
$script:Lines = New-Object System.Collections.Generic.List[string]

Add-Line '# P9C Deployment Configuration Finalization Inventory'
Add-Line ''
Add-Line ("GeneratedUtc: {0}" -f [DateTimeOffset]::UtcNow.ToString('o'))
Add-Line ''
Add-Line 'This inventory is bounded. It avoids .git, bin, obj, .vs, node_modules, packages, and tools/dropins to prevent long-running scans.'

Add-FileSummary -RelativePath 'docs\p9\P9C-Deployment-Configuration-Finalization.md' -Patterns @('ConnectionStrings:MigrationOperationalStore','MIGRATION_OpenTelemetry__EnableTracing','Do not configure a production RunId override')
Add-FileSummary -RelativePath 'config\templates\p9c-deployment-configuration.template.json' -Patterns @('MigrationOperationalStore','SqlOperationalWorker','ServiceBusDispatcher','ServiceBusExecutor','OpenTelemetry')
Add-FileSummary -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('AddOperationalOpenTelemetry','AddEnvironmentVariables(prefix: "MIGRATION_")')
Add-FileSummary -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @('AddOperationalOpenTelemetry')
Add-FileSummary -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('AddOperationalOpenTelemetry')

Add-BoundedSearchSection -Title 'Connection string references' -SearchRoot 'src' -Pattern 'MigrationOperationalStore' -MaxMatches 40
Add-BoundedSearchSection -Title 'MIGRATION_ configuration provider references' -SearchRoot 'src' -Pattern 'MIGRATION_' -MaxMatches 40
Add-BoundedSearchSection -Title 'OpenTelemetry configuration references' -SearchRoot 'src' -Pattern 'OpenTelemetry' -MaxMatches 40
Add-BoundedSearchSection -Title 'Service Bus configuration references' -SearchRoot 'src' -Pattern 'ServiceBus' -MaxMatches 60

$outDir = Join-Path $script:Root 'docs\p9'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P9C-Deployment-Configuration-Finalization-Inventory.generated.md'
Set-Content -LiteralPath $out -Value $script:Lines -Encoding UTF8
Write-Host "Wrote $out"
