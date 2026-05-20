$repoRoot = (Resolve-Path ".").Path
$servicePath = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore\Runs\Health\OperationalGlobalRunHealthSummaryService.cs"

if (-not (Test-Path $servicePath)) {
    throw "Could not find $servicePath"
}

$content = Get-Content $servicePath -Raw
$original = $content

# SQL COUNT_BIG returns Int64, but SUM(CASE ... THEN 1 ELSE 0 END) commonly returns Int32.
# The original ReadInt used GetInt64 unconditionally, which can throw InvalidCastException and surface as HTTP 500.
$content = $content -replace 'TotalWorkItemCount = COUNT_BIG\(1\),', 'TotalWorkItemCount = CAST(COUNT_BIG(1) AS BIGINT),'
$content = $content -replace 'OutstandingWorkItemCount = SUM\(CASE WHEN Status IN \(N''Created'', N''Pending'', N''Queued''\) THEN 1 ELSE 0 END\),', 'OutstandingWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status IN (N''Created'', N''Pending'', N''Queued'') THEN 1 ELSE 0 END), 0) AS BIGINT),'
$content = $content -replace 'LockedWorkItemCount = SUM\(CASE WHEN Status = N''Locked'' THEN 1 ELSE 0 END\),', 'LockedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N''Locked'' THEN 1 ELSE 0 END), 0) AS BIGINT),'
$content = $content -replace 'CompletedWorkItemCount = SUM\(CASE WHEN Status = N''Completed'' THEN 1 ELSE 0 END\),', 'CompletedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N''Completed'' THEN 1 ELSE 0 END), 0) AS BIGINT),'
$content = $content -replace 'FailedWorkItemCount = SUM\(CASE WHEN Status = N''Failed'' THEN 1 ELSE 0 END\)', 'FailedWorkItemCount = CAST(COALESCE(SUM(CASE WHEN Status = N''Failed'' THEN 1 ELSE 0 END), 0) AS BIGINT)'

$oldReadInt = @'
    private static int ReadInt(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal)
            ? 0
            : Convert.ToInt32(reader.GetInt64(ordinal));
    }
'@

$newReadInt = @'
    private static int ReadInt(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }
'@

if ($content.Contains($oldReadInt)) {
    $content = $content.Replace($oldReadInt, $newReadInt)
}
elseif ($content -notmatch 'return Convert\.ToInt32\(reader\.GetValue\(ordinal\)\);') {
    throw "Could not find expected ReadInt implementation to replace."
}

$content = $content -replace 'activeRunCount,', 'Math.Max(activeRunCount, 0),'

if ($content -eq $original) {
    Write-Host "No changes were needed; run health summary service already appears repaired."
}
else {
    Set-Content -Path $servicePath -Value $content -NoNewline
    Write-Host "Repaired operational global run health summary aggregate type handling."
}

Write-Host ""
Write-Host "Next:"
Write-Host "  dotnet build"
Write-Host "  Restart Admin API"
Write-Host "  ./scripts/operational-global-run-health-summary-smoke-test.ps1 -BaseUrl `"https://localhost:55436`""
