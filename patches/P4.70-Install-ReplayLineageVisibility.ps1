[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.70] {0}" -f $Message) }

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

function Add-UiReplayLineageSupport {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $Apply) {
        Write-Step "WOULD add replay lineage UI support"
        return
    }

    if (-not $content.Contains("fetchExecutionReplayLineage")) {
        $anchor = "import { materializeExecutionReplay } from './executionReplayMaterializationApi';"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace(
            $anchor,
            $anchor + "`r`nimport { fetchExecutionReplayLineage } from './executionReplayLineageApi';`r`nimport type { ExecutionReplayLineageResult } from './executionReplayLineageTypes';")
    }

    if (-not $content.Contains("const [replayLineage, setReplayLineage]")) {
        $anchor = "const [replayMaterialization, setReplayMaterialization]"
        $idx = $content.IndexOf($anchor)
        if ($idx -lt 0) { throw ("UI anchor not found: {0}" -f $anchor) }
        $lineEnd = $content.IndexOf(";", $idx)
        $content = $content.Insert($lineEnd + 1, "`r`n  const [replayLineage, setReplayLineage] = useState<ExecutionReplayLineageResult | null>(null);")
    }

    if (-not $content.Contains("fetchExecutionReplayLineage(session.executionSessionId)")) {
        $anchor = "fetchExecutionWorkItemQueueSummary(session.executionSessionId),"
        if (-not $content.Contains($anchor)) { throw ("UI anchor not found: {0}" -f $anchor) }
        $content = $content.Replace($anchor, $anchor + "`r`n        fetchExecutionReplayLineage(session.executionSessionId),")

        $anchor2 = "queueSummaryResponse] = await Promise.all"
        if ($content.Contains($anchor2)) {
            $content = $content.Replace("queueSummaryResponse] = await Promise.all", "queueSummaryResponse, lineageResponse] = await Promise.all")
        } else {
            $content = $content.Replace("queueSummaryResponse] = await Promise.all", "queueSummaryResponse, lineageResponse] = await Promise.all")
        }

        $anchor3 = "setQueueSummary(queueSummaryResponse);"
        if (-not $content.Contains($anchor3)) { throw ("UI anchor not found: {0}" -f $anchor3) }
        $content = $content.Replace($anchor3, $anchor3 + "`r`n      setReplayLineage(lineageResponse);")
    }

    if (-not $content.Contains("<h3>Replay lineage</h3>")) {
        $marker = "{replayMaterialization ? ("
        $insert = @'
          {replayLineage ? (
            <div className="table-shell">
              <h3>Replay lineage</h3>
              <div className="metric-grid">
                <article><span>Root session</span><strong>{replayLineage.rootExecutionSessionId}</strong></article>
                <article><span>Source session</span><strong>{replayLineage.sourceExecutionSessionId ?? '—'}</strong></article>
                <article><span>Replay depth</span><strong>{replayLineage.replayDepth}</strong></article>
                <article><span>Children</span><strong>{replayLineage.children.length}</strong></article>
              </div>
              <table>
                <thead><tr><th>Type</th><th>Name</th><th>Status</th><th>Scope</th><th>Session</th></tr></thead>
                <tbody>
                  {replayLineage.ancestors.map((node) => (
                    <tr key={`ancestor-${node.executionSessionId}`}>
                      <td>ancestor</td>
                      <td>{node.name}</td>
                      <td>{node.status}</td>
                      <td>{node.replayScope ?? '—'}</td>
                      <td><code>{node.executionSessionId}</code></td>
                    </tr>
                  ))}
                  {replayLineage.children.map((node) => (
                    <tr key={`child-${node.executionSessionId}`}>
                      <td>child</td>
                      <td>{node.name}</td>
                      <td>{node.status}</td>
                      <td>{node.replayScope ?? '—'}</td>
                      <td><code>{node.executionSessionId}</code></td>
                    </tr>
                  ))}
                  {replayLineage.ancestors.length === 0 && replayLineage.children.length === 0 ? (
                    <tr><td colSpan={5}>No replay ancestors or children found.</td></tr>
                  ) : null}
                </tbody>
              </table>
            </div>
          ) : null}

'@
        if (-not $content.Contains($marker)) { throw ("UI marker not found: {0}" -f $marker) }
        $content = $content.Replace($marker, $insert + $marker)
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Added replay lineage UI support"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayLineageModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayLineageService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayLineageService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayLineageEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayLineageTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayLineageApi.ts"
Copy-PayloadFile "docs/operations/P4.70-replay-lineage-visibility.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineOnce -Path $programPath -Line "builder.Services.AddScoped<IExecutionReplayLineageService, SqlExecutionReplayLineageService>();" -Anchor "builder.Services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();"

$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
Add-LineOnce -Path $compositionPath -Line "        endpoints.MapExecutionReplayLineageEndpoints();" -Anchor "        endpoints.MapExecutionReplayMaterializationEndpoints();"

$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
Add-UiReplayLineageSupport -Path $workspacePath

Write-Step "Complete."
