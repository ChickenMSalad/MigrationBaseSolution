[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory = $true)][string] $Message)
    Write-Host "[P4.3] $Message"
}

function Assert-PathExists {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected path not found: {0}" -f $Path)
    }

    Write-Step ("Found {0}" -f $Path)
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }

    Write-Step ("Verified text in {0}: {1}" -f $Path, $Text)
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory = $true)][string] $ProjectPath)

    [xml] $project = Get-Content -LiteralPath $ProjectPath -Raw

    $packageReferences = @()

    if ($project.PSObject.Properties.Name -contains "Project") {
        $projectNode = $project.Project

        if ($projectNode.PSObject.Properties.Name -contains "ItemGroup") {
            foreach ($itemGroup in @($projectNode.ItemGroup)) {
                if ($itemGroup.PSObject.Properties.Name -contains "PackageReference") {
                    $packageReferences += @($itemGroup.PackageReference)
                }
            }
        }
    }

    foreach ($reference in $packageReferences) {
        if ($null -ne $reference -and $reference.PSObject.Properties.Name -contains "Version") {
            throw ("Inline PackageReference Version found in {0}" -f $ProjectPath)
        }
    }

    Write-Step ("Verified no inline package versions in {0}" -f $ProjectPath)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\Manifests\Migration.Manifest.Sql\Migration.Manifest.Sql.csproj"
$providerPath = Join-Path $repoRoot "src\Manifests\Migration.Manifest.Sql\SqlManifestProvider.cs"

Assert-PathExists -Path $projectPath
Assert-PathExists -Path $providerPath
Assert-TextContains -Path $providerPath -Text "public sealed class SqlManifestProvider : IManifestProvider"
Assert-TextContains -Path $providerPath -Text "await connection.OpenAsync(cancellationToken)"
Assert-TextContains -Path $providerPath -Text "CommandTimeoutSeconds"
Assert-NoInlinePackageVersions -ProjectPath $projectPath

Write-Step "Validation complete."
