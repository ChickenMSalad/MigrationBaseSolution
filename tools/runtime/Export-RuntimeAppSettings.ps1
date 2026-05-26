#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string] $DispatcherApp,

    [Parameter(Mandatory = $true)]
    [string] $ExecutorApp,

    [string] $OutputDirectory = "."
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Force -Path $Path | Out-Null
    }
}

function Export-AppSettings {
    param(
        [Parameter(Mandatory = $true)][string] $AppName,
        [Parameter(Mandatory = $true)][string] $Path
    )

    $json = az webapp config appsettings list `
        --resource-group $ResourceGroup `
        --name $AppName `
        -o json

    if ([string]::IsNullOrWhiteSpace($json)) {
        throw "No app settings were returned for app '$AppName'."
    }

    $json | Set-Content -LiteralPath $Path -Encoding UTF8
    Write-Host "Exported $AppName app settings to $Path"
}

Ensure-Directory -Path $OutputDirectory
Export-AppSettings -AppName $DispatcherApp -Path (Join-Path $OutputDirectory 'dispatcher-appsettings.json')
Export-AppSettings -AppName $ExecutorApp -Path (Join-Path $OutputDirectory 'executor-appsettings.json')
