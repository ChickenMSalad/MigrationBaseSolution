[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$payloadRoot = Join-Path $repoRoot 'payload'
Write-Host "[P4.19] Repo root: $repoRoot"

function Copy-PayloadFile {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    $targetDirectory = Split-Path -Parent $target

    if (-not (Test-Path -LiteralPath $source)) {
        throw "Payload source not found: $source"
    }

    if (-not $Apply) {
        Write-Host "[P4.19] WOULD copy $RelativePath"
        return
    }

    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($target, 'Copy payload file')) {
        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Host "[P4.19] Copied $RelativePath"
    }
}

function Add-LineIfMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Line,
        [Parameter(Mandatory = $true)][string]$BeforePattern
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Line)) {
        Write-Host "[P4.19] Already present: $Line"
        return
    }

    if (-not $Apply) {
        Write-Host ("[P4.19] WOULD add line to {0}: {1}" -f $Path, $Line)
        return
    }

    $updated = [regex]::Replace($content, $BeforePattern, [System.Text.RegularExpressions.MatchEvaluator]{
        param($match)
        return $Line + [Environment]::NewLine + $match.Value
    }, 1)

    if ($updated -eq $content) {
        $updated = $content.TrimEnd() + [Environment]::NewLine + $Line + [Environment]::NewLine
    }

    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Host "[P4.19] Updated $Path"
}

$files = @(
    'src/Core/Migration.Admin.Api/Endpoints/Operational/Credentials/OperationalConnectorCredentialVaultEndpointExtensions.cs',
    'src/Core/Migration.Admin.Api/Registration/AdminApiConnectorCredentialVaultRegistrationExtensions.cs',
    'src/Core/Migration.Application/Operational/Credentials/ConnectorCredentialReference.cs',
    'src/Core/Migration.Application/Operational/Credentials/IConnectorCredentialReferenceStore.cs',
    'src/Core/Migration.Infrastructure.Sql/Credentials/SqlConnectorCredentialReferenceStore.cs',
    'apps/migration-admin-ui/src/features/credentials/credentialVaultTypes.ts',
    'apps/migration-admin-ui/src/features/credentials/credentialVaultApi.ts',
    'apps/migration-admin-ui/src/features/credentials/CredentialVaultWorkspace.tsx',
    'docs/operations/P4.19-connector-credential-vault-secret-references.md'
)

foreach ($file in $files) {
    Copy-PayloadFile -RelativePath $file
}

$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
Add-LineIfMissing -Path $programPath -Line 'using Migration.Admin.Api.Endpoints.Operational.Credentials;' -BeforePattern 'using '
Add-LineIfMissing -Path $programPath -Line 'using Migration.Admin.Api.Registration;' -BeforePattern 'using '

if (-not (Test-Path -LiteralPath $programPath)) {
    throw "Program.cs not found: $programPath"
}

$programText = Get-Content -LiteralPath $programPath -Raw
if (-not $programText.Contains('AddAdminApiConnectorCredentialVault')) {
    if (-not $Apply) {
        Write-Host '[P4.19] WOULD add AddAdminApiConnectorCredentialVault service registration'
    }
    else {
        $programText = [regex]::Replace($programText, '(var\s+app\s*=\s*builder\.Build\s*\(\s*\)\s*;)', 'builder.Services.AddAdminApiConnectorCredentialVault();' + [Environment]::NewLine + '$1', 1)
        Set-Content -LiteralPath $programPath -Value $programText -Encoding UTF8
        Write-Host '[P4.19] Added AddAdminApiConnectorCredentialVault service registration'
    }
}

$programText = Get-Content -LiteralPath $programPath -Raw
if (-not $programText.Contains('MapOperationalConnectorCredentialVaultEndpoints')) {
    if (-not $Apply) {
        Write-Host '[P4.19] WOULD add MapOperationalConnectorCredentialVaultEndpoints endpoint registration'
    }
    else {
        $programText = [regex]::Replace($programText, '(app\.Run\s*\(\s*\)\s*;)', 'app.MapOperationalConnectorCredentialVaultEndpoints();' + [Environment]::NewLine + '$1', 1)
        Set-Content -LiteralPath $programPath -Value $programText -Encoding UTF8
        Write-Host '[P4.19] Added MapOperationalConnectorCredentialVaultEndpoints endpoint registration'
    }
}

$appPath = Join-Path $repoRoot 'apps/migration-admin-ui/src/App.tsx'
if (Test-Path -LiteralPath $appPath) {
    Add-LineIfMissing -Path $appPath -Line "import { CredentialVaultWorkspace } from './features/credentials/CredentialVaultWorkspace';" -BeforePattern 'import '

    $appText = Get-Content -LiteralPath $appPath -Raw
    if (-not $appText.Contains('<CredentialVaultWorkspace />')) {
        if (-not $Apply) {
            Write-Host '[P4.19] WOULD add CredentialVaultWorkspace to App.tsx'
        }
        else {
            $appText = $appText -replace '(</main>)', ("  <CredentialVaultWorkspace />" + [Environment]::NewLine + '$1')
            Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8
            Write-Host '[P4.19] Added CredentialVaultWorkspace to App.tsx'
        }
    }
}

Write-Host '[P4.19] Complete.'
