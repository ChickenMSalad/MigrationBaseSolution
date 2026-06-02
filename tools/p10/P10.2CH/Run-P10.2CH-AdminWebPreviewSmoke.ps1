Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')
    return $candidate.ProviderPath
}

function Invoke-HttpSmoke {
    param(
        [Parameter(Mandatory=$true)][string]$Url,
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$LogPath
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 15
        Add-Content -LiteralPath $LogPath -Value ('PASS {0}: {1} {2}' -f $Label, [int]$response.StatusCode, $Url)
        return $true
    }
    catch {
        Add-Content -LiteralPath $LogPath -Value ('FAIL {0}: {1}' -f $Label, $_.Exception.Message)
        return $false
    }
}

$repoRoot = Resolve-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CH'
$stdoutLog = Join-Path $artifactRoot 'preview-smoke.stdout.log'
$stderrLog = Join-Path $artifactRoot 'preview-smoke.stderr.log'
$summaryPath = Join-Path $artifactRoot 'preview-smoke.summary.md'
$smokeLog = Join-Path $artifactRoot 'preview-smoke.routes.log'
$port = 5174
$hostName = '127.0.0.1'
$baseUrl = ('http://{0}:{1}' -f $hostName, $port)

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

Set-Content -LiteralPath $stdoutLog -Value '' -Encoding UTF8
Set-Content -LiteralPath $stderrLog -Value '' -Encoding UTF8
Set-Content -LiteralPath $smokeLog -Value '' -Encoding UTF8

$npmCommand = (Get-Command npm.cmd -ErrorAction SilentlyContinue)
if ($null -eq $npmCommand) {
    $npmCommand = Get-Command npm -ErrorAction Stop
}

Push-Location $adminWebRoot
try {
    Add-Content -LiteralPath $smokeLog -Value 'Running npm run build before preview smoke.'
    $buildProcess = Start-Process -FilePath $npmCommand.Source -ArgumentList @('run','build') -WorkingDirectory $adminWebRoot -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
    if ($buildProcess.ExitCode -ne 0) {
        throw ('npm run build failed with exit code {0}. Review {1} and {2}.' -f $buildProcess.ExitCode, $stdoutLog, $stderrLog)
    }

    Add-Content -LiteralPath $smokeLog -Value ('Starting Vite preview on {0}.' -f $baseUrl)
    $previewArgs = @('run','preview','--','--host',$hostName,'--port',([string]$port),'--strictPort')
    $previewProcess = Start-Process -FilePath $npmCommand.Source -ArgumentList $previewArgs -WorkingDirectory $adminWebRoot -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog

    $started = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        try {
            $probe = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 3
            if ([int]$probe.StatusCode -ge 200 -and [int]$probe.StatusCode -lt 500) {
                $started = $true
                break
            }
        }
        catch {
            # Keep waiting for preview startup.
        }
    }

    if (-not $started) {
        throw ('Vite preview did not become reachable at {0}.' -f $baseUrl)
    }

    $routes = @(
        [pscustomobject]@{ Label = 'root'; Path = '/' },
        [pscustomobject]@{ Label = 'runtime dashboard'; Path = '/operations/runtime-dashboard' },
        [pscustomobject]@{ Label = 'connector configuration'; Path = '/connectors/configuration' },
        [pscustomobject]@{ Label = 'operational events'; Path = '/operations/operational-events' }
    )

    $allPassed = $true
    foreach ($route in $routes) {
        $url = $baseUrl.TrimEnd('/') + $route.Path
        $passed = Invoke-HttpSmoke -Url $url -Label $route.Label -LogPath $smokeLog
        if (-not $passed) {
            $allPassed = $false
        }
    }

    $summary = New-Object 'System.Collections.Generic.List[string]'
    [void]$summary.Add('# P10.2CH - Admin Web Preview Smoke')
    [void]$summary.Add('')
    [void]$summary.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
    [void]$summary.Add(('Base URL: `{0}`' -f $baseUrl))
    [void]$summary.Add(('Route log: `{0}`' -f $smokeLog))
    [void]$summary.Add(('stdout log: `{0}`' -f $stdoutLog))
    [void]$summary.Add(('stderr log: `{0}`' -f $stderrLog))
    [void]$summary.Add('')
    if ($allPassed) {
        [void]$summary.Add('Result: preview smoke passed.')
    }
    else {
        [void]$summary.Add('Result: preview smoke failed. Review the route log.')
    }
    Set-Content -LiteralPath $summaryPath -Value $summary -Encoding UTF8

    if (-not $allPassed) {
        throw ('One or more preview routes failed. Review {0}.' -f $smokeLog)
    }

    Write-Host ('Preview smoke passed. Summary: {0}' -f $summaryPath)
}
finally {
    Pop-Location
    if ($null -ne $previewProcess) {
        try {
            if (-not $previewProcess.HasExited) {
                Stop-Process -Id $previewProcess.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            # Best-effort cleanup only.
        }
    }
}
