param(
    [string]$SecretsProject = "src/Migration.Admin.Api/Migration.Admin.Api.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "Disabling operational run mirror in user secrets..."
Write-Host "Project: $SecretsProject"

dotnet user-secrets `
    --project $SecretsProject `
    set "OperationalRunMirror:Enabled" "false"

Write-Host "Operational run mirror disabled."
