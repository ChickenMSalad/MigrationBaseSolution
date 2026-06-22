Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot
$filesRoot = Join-Path $repoRoot 'files'
if (-not (Test-Path -LiteralPath $filesRoot)) {
    $filesRoot = Join-Path $scriptRoot '..\files'
}
$filesRoot = [System.IO.Path]::GetFullPath($filesRoot)

function Copy-DropInFile {
    param(
        [Parameter(Mandatory=$true)][string]$RelativePath
    )

    $sourcePath = Join-Path $filesRoot $RelativePath
    $targetPath = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw ('Missing drop-in source file: ' + $sourcePath)
    }

    $targetDirectory = Split-Path -Parent $targetPath
    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if (Test-Path -LiteralPath $targetPath) {
        $backupPath = $targetPath + '.p7-azureblob-intermediate-tag-reader-fix.bak'
        Copy-Item -LiteralPath $targetPath -Destination $backupPath -Force
    }

    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    Write-Host ('Applied ' + $RelativePath)
}

Copy-DropInFile 'src\Core\Migration.Domain\Models\AssetBinary.cs'
Copy-DropInFile 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\Migration.Connectors.Sources.AzureBlob.csproj'
Copy-DropInFile 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'
Copy-DropInFile 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
Copy-DropInFile 'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'

Write-Host 'P7 AzureBlob intermediate tag reader fix applied.'
