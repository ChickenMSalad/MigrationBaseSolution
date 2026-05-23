[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.85] {0}" -f $Message) }

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

function Add-UiManualAdmissionSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay admission manual controls UI support"
        return
    }

    if (-not $content.Contains("forceAdmitExecutionReplay")) {
        $anchor = "import { evaluateExecutionReplayAdmission, fetchExecutionReplayAdmissionHistory } from './executionReplayAdmissionApi';"
        if (-not $content.Contains($anchor)) {
            throw ("UI import anchor not found: {0}" -f $anchor)
        }

        $content = $content.Replace(
            $anchor,
            "import { forceAdmitExecutionReplay, forceDeferExecutionReplay } from './executionReplayAdmissionManualApi';`r`n" + $anchor)
    }

    if (-not $content.Contains("const [manualAdmissionOperator, setManualAdmissionOperator]")) {
        $anchor = "const [replayAdmissionTake, setReplayAdmissionTake]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) {
            throw ("UI state anchor not found: {0}" -f $anchor)
        }

        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert(
            $lineEnd + 1,
            "`r`n  const [manualAdmissionOperator, setManualAdmissionOperator] = useState('operator');`r`n  const [manualAdmissionReason, setManualAdmissionReason] = useState('');")
    }

    if (-not $content.Contains("async function forceAdmitSelectedReplay()")) {
        $marker = "async function evaluateReplayAdmissionQueue()"
        $block = @'
  async function forceAdmitSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await forceAdmitExecutionReplay({
        executionSessionId: selectedSession.executionSessionId,
        operator: manualAdmissionOperator,
        reason: manualAdmissionReason,
      });

      setManualAdmissionReason('');
      setStatusMessage(`Replay admission decision recorded: ${result.decision}.`);
      await loadSessions();
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to force-admit replay session.');
    }
  }

  async function forceDeferSelectedReplay() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await forceDeferExecutionReplay({
        executionSessionId: selectedSession.executionSessionId,
        operator: manualAdmissionOperator,
        reason: manualAdmissionReason,
      });

      setManualAdmissionReason('');
      setStatusMessage(`Replay admission decision recorded: ${result.decision}.`);
      await loadSessions();
      await loadSessionDetails(selectedSession);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to force-defer replay session.');
    }
  }

'@
        if (-not $content.Contains($marker)) {
            throw ("UI function marker not found: {0}" -f $marker)
        }

        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Force admit replay")) {
        $anchor = '<button type="button" onClick={evaluateReplayAdmissionQueue}>Evaluate admission</button>'
        if (-not $content.Contains($anchor)) {
            throw ("UI button anchor not found: {0}" -f $anchor)
        }

        $insert = @'
            <label>
              Manual admission operator
              <input value={manualAdmissionOperator} onChange={(event) => setManualAdmissionOperator(event.target.value)} placeholder="Operator" />
            </label>
            <label>
              Manual admission reason
              <input value={manualAdmissionReason} onChange={(event) => setManualAdmissionReason(event.target.value)} placeholder="Required reason" />
            </label>
            <button type="button" onClick={forceAdmitSelectedReplay} disabled={!selectedSession || !manualAdmissionOperator.trim() || !manualAdmissionReason.trim()}>Force admit replay</button>
            <button type="button" onClick={forceDeferSelectedReplay} disabled={!selectedSession || !manualAdmissionOperator.trim() || !manualAdmissionReason.trim()}>Force defer replay</button>
'@
        $content = $content.Replace($anchor, $anchor + [Environment]::NewLine + $insert)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay admission manual controls UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "docs/ui/P4.85-replay-admission-manual-controls-ui.md"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiManualAdmissionSupport -Path $workspacePath

Write-Step "Complete."
