Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRootSafe {
    if ($null -ne $PSScriptRoot -and -not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    if ($null -ne $MyInvocation -and
        $null -ne $MyInvocation.MyCommand -and
        $null -ne $MyInvocation.MyCommand.Path -and
        -not [string]::IsNullOrWhiteSpace($MyInvocation.MyCommand.Path)) {
        return Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param(
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()

    if ($normalized.Contains('\bin\')) {
        return $true
    }

    if ($normalized.Contains('\obj\')) {
        return $true
    }

    return $false
}

function Assert-FileExists {
    param(
        [string]$Root,
        [string]$RelativePath
    )

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required file: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$Root,
        [string]$RelativePath,
        [string]$ExpectedText
    )

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing required file: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($ExpectedText)) {
        throw "File '$RelativePath' does not contain expected text: $ExpectedText"
    }
}

function Test-NoInlinePackageVersions {
    param(
        [string]$RootPath
    )

    $projectFiles = @(Get-ChildItem -LiteralPath $RootPath -Filter '*.csproj' -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) })

    foreach ($projectFile in $projectFiles) {
        if ($null -eq $projectFile -or [string]::IsNullOrWhiteSpace($projectFile.FullName)) {
            continue
        }

        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

        if ($null -eq $projectXml -or
            $null -eq $projectXml.PSObject.Properties['Project'] -or
            $null -eq $projectXml.Project) {
            continue
        }

        if ($null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) {
            continue
        }

        $itemGroups = @($projectXml.Project.ItemGroup)

        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup -or
                $null -eq $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            $packageReferences = @($itemGroup.PackageReference)

            foreach ($packageReference in $packageReferences) {
                if ($null -eq $packageReference) {
                    continue
                }

                $hasVersionAttribute = $false

                if ($null -ne $packageReference.PSObject.Properties['Version']) {
                    $hasVersionAttribute = $true
                }

                if ($null -ne $packageReference.PSObject.Properties['Attributes'] -and
                    $null -ne $packageReference.Attributes) {
                    foreach ($attribute in @($packageReference.Attributes)) {
                        if ($null -ne $attribute -and
                            $null -ne $attribute.PSObject.Properties['Name'] -and
                            $attribute.Name -eq 'Version') {
                            $hasVersionAttribute = $true
                        }
                    }
                }

                if ($hasVersionAttribute) {
                    throw "Inline PackageReference Version attribute found in $($projectFile.FullName). Use Directory.Packages.props instead."
                }
            }
        }
    }
}

$scriptRoot = Get-ScriptRootSafe
$repoRoot = Split-Path -Parent $scriptRoot

$requiredFiles = @(
    'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerOptions.cs',
    'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerHostedService.cs',
    'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerServiceCollectionExtensions.cs',
    'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerSampleExecutor.cs',
    'docs\P7.3-HostedWorkerRuntime-Notes.md'
)

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists -Root $repoRoot -RelativePath $relativePath
}

Assert-FileContains -Root $repoRoot -RelativePath 'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerHostedService.cs' -ExpectedText 'BackgroundService'
Assert-FileContains -Root $repoRoot -RelativePath 'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerHostedService.cs' -ExpectedText 'RunContinuousAsync'
Assert-FileContains -Root $repoRoot -RelativePath 'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerServiceCollectionExtensions.cs' -ExpectedText 'AddSqlOperationalWorkerRuntime'
Assert-FileContains -Root $repoRoot -RelativePath 'src\Migration.Infrastructure\Runtime\Hosted\SqlOperationalWorkerOptions.cs' -ExpectedText 'MigrationRuntime:SqlOperationalWorker'

Test-NoInlinePackageVersions -RootPath $repoRoot

Write-Host 'P7.3 hosted worker runtime validation passed.'
