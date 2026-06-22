Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptRoot

function Assert-FileContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ('Missing expected file: ' + $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

function Assert-FileNotContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ('Missing expected file: ' + $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Found rejected text in ' + $Path + ': ' + $Text)
    }
}

$azureSource = Join-Path $RepoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'
$binaryValidation = Join-Path $RepoRoot 'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'

Assert-FileContains -Path $azureSource -Text 'Binary = string.IsNullOrWhiteSpace(sourceLocation)'
Assert-FileContains -Path $azureSource -Text 'SourceUri = sourceLocation'
Assert-FileContains -Path $azureSource -Text 'ResolveSourceLocation'
Assert-FileContains -Path $azureSource -Text 'blobUri'
Assert-FileContains -Path $azureSource -Text 'TryReadLength'
Assert-FileContains -Path $binaryValidation -Text 'binary.Length.HasValue && binary.Length.Value <= 0'
Assert-FileNotContains -Path $binaryValidation -Text 'if (binary.Length is <= 0)'

Write-Host 'P7 AzureBlob binary payload fix validation passed.'
