[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.83] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-LineOnce {
    param([string]$Path, [string]$Line, [string]$Anchor)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Line)) {
        Write-Step ("Already present: {0}" -f $Line)
        return
    }

    if (-not $content.Contains($Anchor)) {
        throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add line {0}" -f $Line)
        return
    }

    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line: {0}" -f $Line)
}

function Add-UiAdmissionBackgroundStatusSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay admission background status UI support"
        return
    }

    if (-not $content.Contains("fetchExecutionReplayAdmissionBackgroundStatus")) {
        $anchor = "import { evaluateExecutionReplayAdmission, fetchExecutionReplayAdmissionHistory } from './executionReplayAdmissionApi';"
        if (-not $content.Contains($anchor)) {
            throw ("UI import anchor not found: {0}" -f $anchor)
        }

        $content = $content.Replace(
            $anchor,
            "import { fetchExecutionReplayAdmissionBackgroundStatus } from './executionReplayAdmissionBackgroundApi';`r`nimport type { ExecutionReplayAdmissionBackgroundStatus } from './executionReplayAdmissionBackgroundTypes';`r`n" + $anchor)
    }

    if (-not $content.Contains("const [replayAdmissionBackgroundStatus, setReplayAdmissionBackgroundStatus]")) {
        $anchor = "const [replayAdmission, setReplayAdmission]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) {
            throw ("UI state anchor not found: {0}" -f $anchor)
        }

        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayAdmissionBackgroundStatus, setReplayAdmissionBackgroundStatus] = useState<ExecutionReplayAdmissionBackgroundStatus | null>(null);")
    }

    if (-not $content.Contains("fetchExecutionReplayAdmissionBackgroundStatus()")) {
        $anchor = "await loadSessions();"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) {
            throw ("UI initial load anchor not found: {0}" -f $anchor)
        }

        $insert = @'
      try {
        const backgroundStatus = await fetchExecutionReplayAdmissionBackgroundStatus();
        setReplayAdmissionBackgroundStatus(backgroundStatus);
      } catch {
        setReplayAdmissionBackgroundStatus(null);
      }

'@
        $content = $content.Insert($idx + $anchor.Length + 2, $insert)
    }

    if (-not $content.Contains("<h3>Replay admission automation</h3>")) {
        $marker = "{replayAdmission ? ("
        $insert = @'
          {replayAdmissionBackgroundStatus ? (
            <div className="table-shell">
              <h3>Replay admission automation</h3>
              <div className="metric-grid">
                <article><span>Background</span><strong>{replayAdmissionBackgroundStatus.enabled ? 'Enabled' : 'Disabled'}</strong></article>
                <article><span>Admission</span><strong>{replayAdmissionBackgroundStatus.admissionEnabled ? 'Enabled' : 'Disabled'}</strong></article>
                <article><span>Interval</span><strong>{replayAdmissionBackgroundStatus.intervalSeconds}s</strong></article>
                <article><span>Max concurrent</span><strong>{replayAdmissionBackgroundStatus.maxConcurrentReplays}</strong></article>
              </div>
              <p>
                UTC window: {replayAdmissionBackgroundStatus.allowedStartHourUtc}:00–{replayAdmissionBackgroundStatus.allowedEndHourUtc}:00.
                Take: {replayAdmissionBackgroundStatus.take}.
              </p>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) {
            throw ("UI panel marker not found: {0}" -f $marker)
        }

        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay admission background status UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionBackgroundStatusModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAdmissionStatusEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionBackgroundTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionBackgroundApi.ts"
Copy-PayloadFile "docs/operations/P4.83-replay-admission-background-status.md"

$endpointCompositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/ExecutionReplayEndpointCompositionExtensions.cs"
Add-LineOnce `
    -Path $endpointCompositionPath `
    -Line "        endpoints.MapExecutionReplayAdmissionStatusEndpoints();" `
    -Anchor "        endpoints.MapExecutionReplayAdmissionEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiAdmissionBackgroundStatusSupport -Path $workspacePath

Write-Step "Complete."
