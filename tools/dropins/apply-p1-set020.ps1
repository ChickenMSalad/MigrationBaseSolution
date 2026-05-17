$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\p1-set020-backend-auth-scaffold"

Write-Host "Applying P1 Set 020 from $repoRoot"

$files = @(
    "src\Migration.Admin.Api\Authentication\AdminApiAuthenticationOptions.cs",
    "src\Migration.Admin.Api\Authentication\AdminApiAuthenticationStateMiddleware.cs",
    "src\Migration.Admin.Api\Registration\AdminApiAuthenticationServiceCollectionExtensions.cs",
    "docs\azure\BACKEND_AUTH_SCAFFOLD.md",
    "docs\cloud-roadmap-cleanup\P1_SET_020_BACKEND_AUTH_SCAFFOLD.md"
)

foreach ($relative in $files) {
    $source = Join-Path $payloadRoot $relative
    $target = Join-Path $repoRoot $relative
    $targetDirectory = Split-Path $target -Parent

    if (!(Test-Path $source)) {
        throw "Drop-in package is missing expected file: $source"
    }

    if (!(Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item $source $target -Force
    Write-Host "Verified $relative"
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$program = Get-Content $programPath -Raw

if ($program -notmatch "AddMigrationAdminApiAuthentication") {
    if ($program -match "builder\.Services\.AddMigrationAdminApiRuntime\(builder\.Configuration\);") {
        $program = $program -replace "builder\.Services\.AddMigrationAdminApiRuntime\(builder\.Configuration\);", "builder.Services.AddMigrationAdminApiRuntime(builder.Configuration);`r`nbuilder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);"
        Write-Host "Patched Program.cs service registration."
    }
    else {
        throw "Could not find AddMigrationAdminApiRuntime registration anchor in Program.cs. No partial patch was written."
    }
}

if ($program -notmatch "UseMigrationAdminApiAuthenticationState") {
    if ($program -match "app\.UseSwaggerUI\(options =>\s*\{[\s\S]*?\}\);") {
        $program = [regex]::Replace(
            $program,
            "app\.UseSwaggerUI\(options =>\s*\{[\s\S]*?\}\);",
            { param($m) $m.Value + "`r`n`r`napp.UseMigrationAdminApiAuthenticationState();" },
            1)
        Write-Host "Patched Program.cs auth state middleware."
    }
    else {
        throw "Could not find SwaggerUI anchor in Program.cs. No partial patch was written."
    }
}

Set-Content -Path $programPath -Value $program -Encoding UTF8

Write-Host ""
Write-Host "P1 Set 020 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "  dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj"
