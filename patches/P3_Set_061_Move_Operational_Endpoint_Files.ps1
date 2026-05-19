$repoRoot = (Resolve-Path ".").Path
$endpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints"

if (-not (Test-Path $endpointRoot)) {
    throw "Could not find $endpointRoot"
}

$moves = @(
    @{ File = "OperationalDispatchEndpointExtensions.cs"; Folder = "Operational\Dispatch" },

    @{ File = "OperationalHealthEndpointExtensions.cs"; Folder = "Operational\Diagnostics" },
    @{ File = "OperationalMirrorDiagnosticsEndpointExtensions.cs"; Folder = "Operational\Diagnostics" },
    @{ File = "OperationalSqlSchemaDiagnosticsEndpointExtensions.cs"; Folder = "Operational\Diagnostics" },
    @{ File = "OperationalMetricsEndpointExtensions.cs"; Folder = "Operational\Diagnostics" },

    @{ File = "OperationalMirrorReadEndpointExtensions.cs"; Folder = "Operational\Runs" },
    @{ File = "OperationalRunStatusProjectionEndpointExtensions.cs"; Folder = "Operational\Runs" },
    @{ File = "OperationalRunControlEndpointExtensions.cs"; Folder = "Operational\Runs" },
    @{ File = "OperationalRunStatusReconciliationEndpointExtensions.cs"; Folder = "Operational\Runs" },

    @{ File = "OperationalWorkItemLeaseEndpointExtensions.cs"; Folder = "Operational\WorkItems" },
    @{ File = "OperationalWorkItemLeaseExpirationEndpointExtensions.cs"; Folder = "Operational\WorkItems" }
)

$moveCount = 0

foreach ($move in $moves) {
    $source = Join-Path $endpointRoot $move.File
    $destinationFolder = Join-Path $endpointRoot $move.Folder
    $destination = Join-Path $destinationFolder $move.File

    if (Test-Path $source) {
        New-Item -ItemType Directory -Force -Path $destinationFolder | Out-Null

        if (Test-Path $destination) {
            Remove-Item $destination -Force
        }

        Move-Item -Path $source -Destination $destination
        Write-Host "Moved $($move.File) -> Endpoints\$($move.Folder)"
        $moveCount++
    }
    elseif (Test-Path $destination) {
        Write-Host "Already moved: Endpoints\$($move.Folder)\$($move.File)"
    }
    else {
        Write-Host "Not found, skipped: $($move.File)"
    }
}

Write-Host "Operational endpoint file move complete. Files moved: $moveCount"
