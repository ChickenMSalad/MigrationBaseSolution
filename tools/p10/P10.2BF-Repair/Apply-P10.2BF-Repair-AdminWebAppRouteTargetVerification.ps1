Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))

$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$originalReport = [System.IO.Path]::Combine($docsDir, 'P10.2BF-AdminWebAppRouteTargetVerification.Report.md')
$repairReport = [System.IO.Path]::Combine($docsDir, 'P10.2BF-Repair-AdminWebAppRouteTargetVerification.md')

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2BF Repair - Admin Web App Route Target Verification')
[void]$lines.Add('')
[void]$lines.Add('Repair scope: test/validation repair only.')
[void]$lines.Add('')
[void]$lines.Add('- No Admin Web source files were moved.')
[void]$lines.Add('- No App.tsx rewrite was performed.')
[void]$lines.Add('- Historical tool folders are not scanned by this repair.')
[void]$lines.Add(('Original BF report present: {0}' -f (Test-Path -Path $originalReport -PathType Leaf)))

Set-Content -Path $repairReport -Value $lines -Encoding UTF8
Write-Host ('Wrote repair report: {0}' -f $repairReport)
Write-Host 'P10.2BF Repair applied.'
