Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-UsingIfMissing {
    param([string]$Path, [string]$UsingText)

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($UsingText)) { return }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $lines.AddRange([string[]]($content -split "`r?`n"))

    $insertIndex = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].StartsWith('using ', [System.StringComparison]::Ordinal)) {
            $insertIndex = $i + 1
        }
    }

    $lines.Insert($insertIndex, $UsingText)
    Set-Content -LiteralPath $Path -Value $lines -Encoding UTF8
}

function Ensure-ProjectReference {
    param([string]$ProjectPath, [string]$ReferenceInclude)

    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    $existing = @()
    if ($null -ne $xml.Project -and $null -ne $xml.Project.PSObject.Properties['ItemGroup']) {
        foreach ($itemGroup in @($xml.Project.ItemGroup)) {
            if ($null -eq $itemGroup -or $null -eq $itemGroup.PSObject.Properties['ProjectReference']) { continue }
            foreach ($reference in @($itemGroup.ProjectReference)) {
                if ($null -ne $reference -and $null -ne $reference.Include) { $existing += [string]$reference.Include }
            }
        }
    }

    if ($existing -contains $ReferenceInclude) { return }

    $content = Get-Content -LiteralPath $ProjectPath -Raw
    $itemGroup = @"
  <ItemGroup>
    <ProjectReference Include="$ReferenceInclude" />
  </ItemGroup>
"@
    $content = $content -replace '</Project>', ($itemGroup + '</Project>')
    Set-Content -LiteralPath $ProjectPath -Value $content -Encoding UTF8
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

$programPath = Join-Path $root 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs'
$projectPath = Join-Path $root 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj'

if (-not (Test-Path -LiteralPath $programPath)) { throw "Required file missing: src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs" }
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Required file missing: src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj" }

Add-UsingIfMissing -Path $programPath -UsingText 'using Migration.Workers.QueueExecutor.Registration;'

$program = Get-Content -LiteralPath $programPath -Raw
$old = 'builder.Services.AddSingleton<IServiceBusWorkItemExecutor, PlaceholderServiceBusWorkItemExecutor>();'
$new = @'
builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);
builder.Services.AddSingleton<IServiceBusWorkItemExecutor, SqlOperationalServiceBusWorkItemExecutor>();
'@

if ($program.Contains($old)) {
    $program = $program.Replace($old, $new.TrimEnd())
    Set-Content -LiteralPath $programPath -Value $program -Encoding UTF8
}
elseif (-not $program.Contains('SqlOperationalServiceBusWorkItemExecutor')) {
    throw "Expected placeholder registration was not found and SqlOperationalServiceBusWorkItemExecutor is not already registered. Review Program.cs manually."
}

Ensure-ProjectReference -ProjectPath $projectPath -ReferenceInclude '..\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'

Write-Host 'P8.2C Service Bus real execution adapter wiring repaired.'
