$repoRoot = (Resolve-Path ".").Path
$filePath = Join-Path $repoRoot "src\Migration.Application\DependencyInjection\OperationalStoreApplicationRegistrationExtensions.cs"

if (Test-Path $filePath) {
    Remove-Item $filePath
    Write-Host "Removed $filePath"
}
else {
    Write-Host "File was already removed: $filePath"
}
