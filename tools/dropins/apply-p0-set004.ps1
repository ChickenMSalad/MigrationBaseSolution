$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$preflightPath = Join-Path $repoRoot "src/Admin/Migration.Admin.Web/src/pages/Preflight.tsx"

if (-not (Test-Path $preflightPath)) {
    throw "Preflight.tsx not found at $preflightPath"
}

$text = Get-Content $preflightPath -Raw

if ($text -notmatch 'from "\.\./api/preflight"') {
    $text = $text -replace 'import \{ api \} from "\.\./api/client";', 'import { api } from "../api/client"; import { runProjectPreflight } from "../api/preflight";'
}

$text = $text -replace 'api\.runPreflight\(', 'runProjectPreflight('

Set-Content -Path $preflightPath -Value $text -NoNewline

Write-Host "P0 Set 004 applied: Preflight.tsx now uses runProjectPreflight()."
