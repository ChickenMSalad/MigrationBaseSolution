[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.18] $Message"
}

function Copy-PayloadFile {
    param(
        [Parameter(Mandatory=$true)][string]$RelativePath
    )

    $source = Join-Path $repoRoot ("payload\" + $RelativePath)
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw "Payload file not found: $source"
    }

    $targetDirectory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        } else {
            Write-Step "WOULD create directory $targetDirectory"
        }
    }

    if ($Apply) {
        if ($PSCmdlet.ShouldProcess($target, "Copy payload file")) {
            Copy-Item -LiteralPath $source -Destination $target -Force
            Write-Step ("Copied {0}" -f $RelativePath)
        }
    } else {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
    }
}

function Add-TextIfMissing {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$RequiredText,
        [Parameter(Mandatory=$true)][string]$InsertAfterText,
        [Parameter(Mandatory=$true)][string]$TextToInsert
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($RequiredText)) {
        Write-Step ("Already updated: {0}" -f $Path)
        return
    }

    if (-not $content.Contains($InsertAfterText)) {
        throw ("Insert marker not found in {0}: {1}" -f $Path, $InsertAfterText)
    }

    $updated = $content.Replace($InsertAfterText, $InsertAfterText + [Environment]::NewLine + $TextToInsert)

    if ($Apply) {
        if ($PSCmdlet.ShouldProcess($Path, "Patch text")) {
            Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
            Write-Step ("Updated {0}" -f $Path)
        }
    } else {
        Write-Step ("WOULD update {0}" -f $Path)
    }
}

function Add-TextBeforeIfMissing {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$RequiredText,
        [Parameter(Mandatory=$true)][string]$InsertBeforeText,
        [Parameter(Mandatory=$true)][string]$TextToInsert
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($RequiredText)) {
        Write-Step ("Already updated: {0}" -f $Path)
        return
    }

    if (-not $content.Contains($InsertBeforeText)) {
        throw ("Insert marker not found in {0}: {1}" -f $Path, $InsertBeforeText)
    }

    $updated = $content.Replace($InsertBeforeText, $TextToInsert + [Environment]::NewLine + $InsertBeforeText)

    if ($Apply) {
        if ($PSCmdlet.ShouldProcess($Path, "Patch text")) {
            Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
            Write-Step ("Updated {0}" -f $Path)
        }
    } else {
        Write-Step ("WOULD update {0}" -f $Path)
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Write-Step ("Repo root: {0}" -f $repoRoot)

$adminApiProject = Join-Path $repoRoot "src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj"
$programPath = Join-Path $repoRoot "src\Core\Migration.Admin.Api\Program.cs"
$appPath = Join-Path $repoRoot "apps\migration-admin-ui\src\App.tsx"

if (-not (Test-Path -LiteralPath $adminApiProject)) {
    throw "Admin API project not found at expected normalized path: $adminApiProject"
}
if (-not (Test-Path -LiteralPath $programPath)) {
    throw "Program.cs not found: $programPath"
}
if (-not (Test-Path -LiteralPath $appPath)) {
    throw "UI App.tsx not found: $appPath"
}

Copy-PayloadFile "src\Core\Migration.Admin.Api\Endpoints\Operational\Connectors\OperationalConnectorConfigurationEndpointExtensions.cs"
Copy-PayloadFile "apps\migration-admin-ui\src\features\connectors\ConnectorConfigurationWorkspace.tsx"
Copy-PayloadFile "apps\migration-admin-ui\src\features\connectors\connectorConfigurationApi.ts"
Copy-PayloadFile "apps\migration-admin-ui\src\features\connectors\connectorConfigurationTypes.ts"
Copy-PayloadFile "docs\operations\P4.18-connector-configuration-workspace.md"

Add-TextIfMissing `
    -Path $programPath `
    -RequiredText "MapOperationalConnectorConfigurationEndpoints" `
    -InsertAfterText "app.MapSqlOperationalBackboneEndpoints();" `
    -TextToInsert "app.MapOperationalConnectorConfigurationEndpoints();"

Add-TextBeforeIfMissing `
    -Path $appPath `
    -RequiredText "ConnectorConfigurationWorkspace" `
    -InsertBeforeText "import './styles.css';" `
    -TextToInsert "import { ConnectorConfigurationWorkspace } from './features/connectors/ConnectorConfigurationWorkspace';"

Add-TextBeforeIfMissing `
    -Path $appPath `
    -RequiredText "<ConnectorConfigurationWorkspace />" `
    -InsertBeforeText "</main>" `
    -TextToInsert "        <ConnectorConfigurationWorkspace />"

Write-Step "Complete. Next: ./patches/P4.18-Validate-ConnectorConfigurationWorkspace.ps1; dotnet restore; dotnet build; npm run build"
