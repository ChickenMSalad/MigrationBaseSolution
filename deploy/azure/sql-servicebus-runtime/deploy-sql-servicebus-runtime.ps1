[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string] $Location = "eastus",

    [Parameter(Mandatory = $false)]
    [string] $EnvironmentName = "dev",

    [Parameter(Mandatory = $false)]
    [string] $NamePrefix = "mbs",

    [Parameter(Mandatory = $true)]
    [string] $SqlAdministratorLogin,

    [Parameter(Mandatory = $true)]
    [securestring] $SqlAdministratorPassword,

    [Parameter(Mandatory = $false)]
    [string] $SqlDatabaseSkuName = "S0",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Basic", "Standard", "Premium")]
    [string] $ServiceBusSkuName = "Standard"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$templatePath = Join-Path $scriptRoot "main.bicep"

if (-not (Test-Path -LiteralPath $templatePath)) {
    throw ("Template not found: {0}" -f $templatePath)
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI 'az' was not found on PATH."
}

$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdministratorPassword))

try {
    $groupExists = az group exists --name $ResourceGroupName | ConvertFrom-Json

    if (-not $groupExists) {
        if ($PSCmdlet.ShouldProcess($ResourceGroupName, "Create resource group")) {
            az group create --name $ResourceGroupName --location $Location | Out-Host
        }
    }

    $deploymentName = "p4-8-sql-servicebus-runtime-{0}" -f (Get-Date -Format "yyyyMMddHHmmss")

    if ($PSCmdlet.ShouldProcess($ResourceGroupName, "Deploy P4.8 SQL Service Bus runtime scaffold")) {
        az deployment group create `
            --name $deploymentName `
            --resource-group $ResourceGroupName `
            --template-file $templatePath `
            --parameters `
                environmentName=$EnvironmentName `
                namePrefix=$NamePrefix `
                sqlAdministratorLogin=$SqlAdministratorLogin `
                sqlAdministratorPassword=$plainPassword `
                sqlDatabaseSkuName=$SqlDatabaseSkuName `
                serviceBusSkuName=$ServiceBusSkuName | Out-Host
    }
}
finally {
    if ($plainPassword) {
        $plainPassword = $null
    }
}
