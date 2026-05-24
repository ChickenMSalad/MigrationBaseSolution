Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$project = '.\src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'

Write-Host 'Recommended safe local smoke user-secrets commands:'
Write-Host ''
Write-Host "dotnet user-secrets set \"SqlOperationalWorker:Enabled\" \"true\" --project $project"
Write-Host "dotnet user-secrets set \"SqlOperationalWorker:CompleteNoOpWorkItems\" \"true\" --project $project"
Write-Host "dotnet user-secrets set \"SqlOperationalMigrationJobExecutor:Enabled\" \"false\" --project $project"
Write-Host ''
Write-Host 'Run worker host:'
Write-Host "dotnet run --project $project --environment Development"
