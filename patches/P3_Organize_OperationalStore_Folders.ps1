$repoRoot = (Resolve-Path ".").Path
$operationalStoreRoot = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore"

if (-not (Test-Path $operationalStoreRoot)) {
    throw "Could not find $operationalStoreRoot"
}

function Move-OperationalStoreFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName,

        [Parameter(Mandatory = $true)]
        [string]$TargetFolder
    )

    $source = Join-Path $operationalStoreRoot $FileName
    $destinationFolder = Join-Path $operationalStoreRoot $TargetFolder
    $destination = Join-Path $destinationFolder $FileName

    if (Test-Path $source) {
        New-Item -ItemType Directory -Force -Path $destinationFolder | Out-Null

        if (Test-Path $destination) {
            Remove-Item $destination -Force
        }

        Move-Item -Path $source -Destination $destination
        Write-Host "Moved $FileName -> OperationalStore\$TargetFolder"
        return
    }

    if (Test-Path $destination) {
        Write-Host "Already moved: OperationalStore\$TargetFolder\$FileName"
        return
    }

    Write-Host "Skipped missing: $FileName"
}

Write-Host "Organizing Migration.Admin.Api OperationalStore files..."
Write-Host ""

# Core mirror/runtime
$coreMirrorFiles = @(
    "AdminOperationalRunMirrorService.cs",
    "IAdminOperationalRunMirrorService.cs",
    "OperationalRunMirrorOptions.cs",
    "OperationalRunMirrorOptionsValidator.cs",
    "OperationalMirrorInvocationState.cs",
    "OperationalMirrorInvocationSnapshot.cs"
)

foreach ($file in $coreMirrorFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Mirror"
}

# Readiness / guard / status
$diagnosticFiles = @(
    "IOperationalMirrorReadinessEvaluator.cs",
    "OperationalMirrorReadinessEvaluator.cs",
    "OperationalMirrorReadinessResponse.cs",
    "OperationalMirrorStatusResponse.cs",
    "IOperationalMirrorEnablementGuard.cs",
    "OperationalMirrorEnablementGuard.cs",
    "OperationalMirrorEnablementGuardResponse.cs",
    "IOperationalMirrorWriteVerificationService.cs",
    "OperationalMirrorWriteVerificationService.cs",
    "OperationalMirrorWriteVerificationResponse.cs",
    "IOperationalSqlSchemaSmokeTestService.cs",
    "OperationalSqlSchemaSmokeTestService.cs",
    "OperationalSqlSchemaSmokeTestResponse.cs"
)

foreach ($file in $diagnosticFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Diagnostics"
}

# Operational read APIs / DTOs
$runReadFiles = @(
    "IOperationalMirrorReadService.cs",
    "OperationalMirrorReadService.cs",
    "OperationalRunSummaryResponse.cs",
    "OperationalRunDetailResponse.cs",
    "OperationalManifestRecordResponse.cs",
    "OperationalWorkItemResponse.cs",
    "OperationalFailureResponse.cs",
    "OperationalCheckpointResponse.cs"
)

foreach ($file in $runReadFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Runs\Read"
}

# Run status projections
$projectionFiles = @(
    "IOperationalRunStatusProjectionService.cs",
    "OperationalRunStatusProjectionService.cs",
    "OperationalRunStatusProjectionResponse.cs"
)

foreach ($file in $projectionFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Runs\Projection"
}

# Run controls
$controlFiles = @(
    "IOperationalRunControlService.cs",
    "OperationalRunControlService.cs",
    "OperationalRunControlActionRequest.cs",
    "OperationalRunControlStateResponse.cs"
)

foreach ($file in $controlFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Runs\Control"
}

# Run status reconciliation
$reconciliationFiles = @(
    "IOperationalRunStatusReconciliationService.cs",
    "OperationalRunStatusReconciliationService.cs",
    "OperationalRunStatusReconciliationResponse.cs"
)

foreach ($file in $reconciliationFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Runs\Reconciliation"
}

# Run finalization
$finalizationFiles = @(
    "IOperationalRunCompletionFinalizationService.cs",
    "OperationalRunCompletionFinalizationService.cs",
    "OperationalRunCompletionReadinessResponse.cs",
    "IOperationalRunFailureFinalizationService.cs",
    "OperationalRunFailureFinalizationService.cs",
    "OperationalRunFailureReadinessResponse.cs",
    "IOperationalRunAutoFinalizationService.cs",
    "OperationalRunAutoFinalizationService.cs",
    "OperationalRunAutoFinalizationHostedService.cs",
    "OperationalRunAutoFinalizationOptions.cs",
    "OperationalRunAutoFinalizationStatusResponse.cs"
)

foreach ($file in $finalizationFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Runs\Finalization"
}

# Work item leasing / recovery / lease expiration
$workItemFiles = @(
    "IOperationalWorkItemLeaseService.cs",
    "OperationalWorkItemLeaseService.cs",
    "OperationalWorkItemLeaseRequest.cs",
    "OperationalWorkItemLeaseResponse.cs",
    "OperationalWorkItemLeaseItem.cs",
    "OperationalWorkItemHeartbeatRequest.cs",
    "OperationalWorkItemCompleteRequest.cs",
    "OperationalWorkItemFailRequest.cs",
    "OperationalWorkItemStateTransitionResponse.cs",
    "IOperationalWorkItemRecoveryService.cs",
    "OperationalWorkItemRecoveryService.cs",
    "OperationalWorkItemReleaseRequest.cs",
    "OperationalWorkItemResetRequest.cs",
    "IOperationalLeaseExpirationService.cs",
    "OperationalLeaseExpirationService.cs",
    "OperationalLeaseExpirationOptions.cs",
    "OperationalExpiredLeaseItem.cs",
    "OperationalExpiredLeaseListResponse.cs",
    "OperationalReclaimExpiredLeasesRequest.cs",
    "OperationalReclaimExpiredLeasesResponse.cs"
)

foreach ($file in $workItemFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "WorkItems"
}

# Metrics
$metricFiles = @(
    "IOperationalMetricsService.cs",
    "OperationalMetricsService.cs",
    "OperationalWorkItemMetricsResponse.cs",
    "OperationalLeaseMetricsResponse.cs",
    "OperationalLeaseWorkerMetric.cs",
    "OperationalRunMetricsResponse.cs",
    "OperationalRunStatusMetric.cs",
    "OperationalDiagnosticsSummaryResponse.cs"
)

foreach ($file in $metricFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Metrics"
}

# Dispatcher
$dispatcherFiles = @(
    "IOperationalDispatcherService.cs",
    "OperationalDispatcherService.cs",
    "OperationalDispatcherOptions.cs",
    "OperationalDispatcherStatusResponse.cs",
    "OperationalDispatcherRunOnceResponse.cs",
    "OperationalDispatcherHostedService.cs",
    "IOperationalDispatcherDiagnosticsService.cs",
    "OperationalDispatcherDiagnosticsService.cs",
    "OperationalDispatcherDiagnosticsResponse.cs",
    "OperationalDispatcherEligibleWorkItemPreview.cs",
    "IDispatcherExecutionHistoryService.cs",
    "DispatcherExecutionHistoryService.cs",
    "DispatcherExecutionRecord.cs"
)

foreach ($file in $dispatcherFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Dispatcher"
}

# Retention
$retentionFiles = @(
    "IOperationalRetentionService.cs",
    "OperationalRetentionService.cs",
    "OperationalRetentionOptions.cs",
    "OperationalRetentionStatusResponse.cs",
    "OperationalRetentionActionResponse.cs"
)

foreach ($file in $retentionFiles) {
    Move-OperationalStoreFile -FileName $file -TargetFolder "Retention"
}

# Move loose .sql files into Sql/Scripts.
Write-Host ""
Write-Host "Moving loose SQL files under OperationalStore\Sql\Scripts..."

$sqlTarget = Join-Path $operationalStoreRoot "Sql\Scripts"
New-Item -ItemType Directory -Force -Path $sqlTarget | Out-Null

Get-ChildItem -Path $operationalStoreRoot -File -Filter "*.sql" | ForEach-Object {
    $destination = Join-Path $sqlTarget $_.Name

    if (Test-Path $destination) {
        Remove-Item $destination -Force
    }

    Move-Item -Path $_.FullName -Destination $destination
    Write-Host "Moved $($_.Name) -> OperationalStore\Sql\Scripts"
}

Write-Host ""
Write-Host "OperationalStore organization complete."
Write-Host ""
Write-Host "Current top-level files remaining under OperationalStore:"
Get-ChildItem -Path $operationalStoreRoot -File |
    Sort-Object Name |
    ForEach-Object {
        Write-Host " - $($_.Name)"
    }

Write-Host ""
Write-Host "Current OperationalStore folders:"
Get-ChildItem -Path $operationalStoreRoot -Directory |
    Sort-Object Name |
    ForEach-Object {
        Write-Host " - $($_.Name)"
    }
