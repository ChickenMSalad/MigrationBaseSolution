Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$logPath = [System.IO.Path]::Combine($docsRoot, 'P10.2BT-AdminWebNpmBuild.log')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$previousLocation = Get-Location
try {
    Set-Location -Path $adminWebRoot
    $output = & npm run build 2>&1
    $exitCode = $LASTEXITCODE
    Set-Content -Path $logPath -Value @($output) -Encoding UTF8
    if ($exitCode -ne 0) {
        throw ('Admin Web npm build failed with exit code {0}. See {1}' -f $exitCode, $logPath)
    }
    Write-Host ('Admin Web npm build passed. Log: {0}' -f $logPath)
}
finally {
    Set-Location -Path $previousLocation
}
