[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.73] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)
    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source)) { throw ("Payload file not found: {0}" -f $source) }
    if (-not $Apply) { Write-Step ("WOULD copy {0}" -f $RelativePath); return }
    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-LineOnce {
    param([string]$Path, [string]$Line, [string]$Anchor)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Line)) { Write-Step ("Already present: {0}" -f $Line); return }
    if (-not $content.Contains($Anchor)) { throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor) }
    if (-not $Apply) { Write-Step ("WOULD add line {0}" -f $Line); return }
    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line {0}" -f $Line)
}

function Add-UiReplayPolicySupport {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $Apply) { Write-Step "WOULD add replay policy UI support"; return }

    if (-not $content.Contains("evaluateExecutionReplayPolicy")) {
        $anchor = "import { prepareExecutionReplayManifest } from './executionReplayPreparationApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, $anchor + "`r`nimport { evaluateExecutionReplayPolicy } from './executionReplayPolicyApi';`r`nimport type { ExecutionReplayPolicyEvaluationResult } from './executionReplayPolicyTypes';")
    }

    if (-not $content.Contains("const [replayPolicy, setReplayPolicy]")) {
        $anchor = "const [replayPreparation, setReplayPreparation]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayPolicy, setReplayPolicy] = useState<ExecutionReplayPolicyEvaluationResult | null>(null);")
    }

    if (-not $content.Contains("async function evaluateSelectedReplayPolicy()")) {
        $marker = "async function prepareSelectedReplayManifest()"
        $block = @'
  async function evaluateSelectedReplayPolicy() {
    if (!selectedSession) {
      return;
    }

    try {
      const result = await evaluateExecutionReplayPolicy(selectedSession.executionSessionId, replayScope);
      setReplayPolicy(result);
      setStatusMessage(`Replay policy decision: ${result.decision}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to evaluate replay policy.');
    }
  }

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $block + $marker)
    }

    if (-not $content.Contains("Evaluate policy")) {
        $anchor = '<button type="button" onClick={prepareSelectedReplayManifest}>Prepare replay manifest</button>'
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, '<button type="button" onClick={evaluateSelectedReplayPolicy}>Evaluate policy</button>' + "`r`n            " + $anchor)
    }

    if (-not $content.Contains("<h3>Replay policy</h3>")) {
        $marker = "{replayPreparation ? ("
        $insert = @'
          {replayPolicy ? (
            <div className="table-shell">
              <h3>Replay policy</h3>
              <div className="metric-grid">
                <article><span>Decision</span><strong>{replayPolicy.decision}</strong></article>
                <article><span>Policy score</span><strong>{replayPolicy.policyScore}</strong></article>
                <article><span>Prepared items</span><strong>{replayPolicy.metrics.preparedItemCount}</strong></article>
                <article><span>Dead-letter %</span><strong>{replayPolicy.metrics.deadLetteredPercent}</strong></article>
              </div>
              <table>
                <thead><tr><th>Severity</th><th>Code</th><th>Message</th></tr></thead>
                <tbody>
                  {replayPolicy.violations.length === 0 ? (
                    <tr><td colSpan={3}>No replay policy violations.</td></tr>
                  ) : (
                    replayPolicy.violations.map((violation) => (
                      <tr key={`${violation.severity}-${violation.code}`}>
                        <td>{violation.severity}</td>
                        <td><code>{violation.code}</code></td>
                        <td>{violation.message}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay policy UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayPolicyModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayPolicyService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPolicyService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayPolicyEndpointExtensions.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPolicyTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayPolicyApi.ts"
Copy-PayloadFile "docs/operations/P4.73-replay-policy-enforcement.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce -Path $programPath -Line "builder.Services.AddScoped<IExecutionReplayPolicyService, SqlExecutionReplayPolicyService>();" -Anchor "builder.Services.AddScoped<IExecutionReplayApprovalService, SqlExecutionReplayApprovalService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce -Path $compositionPath -Line "        endpoints.MapExecutionReplayPolicyEndpoints();" -Anchor "        endpoints.MapExecutionReplayApprovalEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplayPolicySupport -Path $workspacePath

Write-Step "Complete."
