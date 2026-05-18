param(
    [string]$SecretsProject = "src/Migration.Admin.Api/Migration.Admin.Api.csproj"
)

$ErrorActionPreference = "Stop"

Write-Host "Enabling operational run mirror in user secrets..."
Write-Host "Project: $SecretsProject"

dotnet user-secrets `
    --project $SecretsProject `
    set "OperationalRunMirror:Enabled" "true"

Write-Host "Operational run mirror enabled."
