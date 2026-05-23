Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    if ($PSCommandPath -and -not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        return Split-Path -Parent $PSCommandPath
    }

    return (Get-Location).Path
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    return Join-Path -Path $Root -ChildPath $RelativePath
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $fullPath = Join-RepoPath -Root $Root -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return $RelativePath
    }

    return $null
}

function Get-SourceProjectFiles {
    param([Parameter(Mandatory = $true)][string]$Root)

    $projectFiles = Get-ChildItem -LiteralPath $Root -Recurse -Filter '*.csproj' -File
    $filtered = @()

    foreach ($projectFile in @($projectFiles)) {
        $fullName = $projectFile.FullName
        if ($fullName -match '\\bin\\' -or $fullName -match '\\obj\\') {
            continue
        }

        $filtered += $projectFile
    }

    return @($filtered)
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory = $true)][string]$Root)

    $violations = @()
    foreach ($projectFile in @(Get-SourceProjectFiles -Root $Root)) {
        $matches = @(Select-String -LiteralPath $projectFile.FullName -Pattern '<PackageReference\s+[^>]*\bVersion\s*=' -SimpleMatch:$false)
        foreach ($match in @($matches)) {
            $violations += ('{0}:{1}' -f $projectFile.FullName, $match.LineNumber)
        }
    }

    return @($violations)
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$ExpectedText
    )

    $fullPath = Join-RepoPath -Root $Root -RelativePath $RelativePath
    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Expected text not found in ${RelativePath}: ${ExpectedText}"
    }
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Resolve-Path (Join-Path -Path $scriptDirectory -ChildPath '..')
$repoRootPath = $repoRoot.Path

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Validation\AzureDeploymentValidationScriptDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Validation\AzureDeploymentValidationScriptRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Validation\IAzureDeploymentValidationScriptRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Validation\AzureDeploymentValidationScriptRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Validation\AzureDeploymentValidationScriptResult.cs',
    'config\azure-runtime\deployment\validation-scripts.sample.json',
    'tools\validate-p5.3.4-azure-deployment-validation-script-contracts.ps1'
)

$missing = @()
foreach ($relativePath in @($expectedFiles)) {
    $missingFile = Assert-FileExists -Root $repoRootPath -RelativePath $relativePath
    if ($null -ne $missingFile) {
        $missing += $missingFile
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.3.4 files:'
    foreach ($file in @($missing)) {
        Write-Host (' - {0}' -f $file)
    }

    exit 1
}

Assert-FileContains -Root $repoRootPath -RelativePath 'config\azure-runtime\deployment\validation-scripts.sample.json' -ExpectedText 'sql-connectivity'
Assert-FileContains -Root $repoRootPath -RelativePath 'config\azure-runtime\deployment\validation-scripts.sample.json' -ExpectedText 'worker-host-health'

$inlineVersionViolations = @(Assert-NoInlinePackageVersions -Root $repoRootPath)
if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:'
    foreach ($violation in @($inlineVersionViolations)) {
        Write-Host (' - {0}' -f $violation)
    }

    exit 1
}

$projectPath = Join-RepoPath -Root $repoRootPath -RelativePath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) {
        throw 'MigrationBase.Core restore failed.'
    }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'MigrationBase.Core build failed.'
    }
}
else {
    Write-Host 'MigrationBase.Core project file not found; source file presence validated only.'
}

Write-Host 'P5.3.4 Azure deployment validation script contract validation passed.'
