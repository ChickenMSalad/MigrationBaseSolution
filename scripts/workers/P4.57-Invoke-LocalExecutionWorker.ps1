[CmdletBinding()]
param(
    [string]$BaseUrl = "https://localhost:55436",
    [Parameter(Mandatory = $true)]
    [Guid]$ExecutionSessionId,
    [string]$WorkerId = "local-dev-worker",
    [int]$BatchSize = 1,
    [int]$LeaseSeconds = 300,
    [int]$PollSeconds = 5,
    [int]$MaxIterations = 1,
    [switch]$FailLeasedItems,
    [switch]$AllowUntrustedCertificate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.57-WORKER] {0}" -f $Message)
}

function Enable-UntrustedCertificateSupport {
    if (-not ("TrustAllCertsPolicyP457" -as [type])) {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;

public sealed class TrustAllCertsPolicyP457 : ICertificatePolicy
{
    public bool CheckValidationResult(
        ServicePoint srvPoint,
        X509Certificate certificate,
        WebRequest request,
        int certificateProblem)
    {
        return true;
    }
}
"@
    }

    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicyP457
}

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 8
    return Invoke-RestMethod -Uri $Url -Method Post -Body $json -ContentType "application/json"
}

function Send-Heartbeat {
    param(
        [string]$Status,
        [int]$ActiveLeaseCount,
        [string]$Message
    )

    Invoke-JsonPost `
        -Url ($trimmedBaseUrl + "/api/operational/execution-workers/heartbeat") `
        -Body @{
            workerId = $WorkerId
            executionSessionId = $ExecutionSessionId
            status = $Status
            activeLeaseCount = $ActiveLeaseCount
            message = $Message
        } | Out-Null
}

if ($AllowUntrustedCertificate) {
    Enable-UntrustedCertificateSupport
}

$trimmedBaseUrl = $BaseUrl.TrimEnd("/")
$iteration = 0

Send-Heartbeat -Status "starting" -ActiveLeaseCount 0 -Message "Local worker harness starting."

while ($iteration -lt $MaxIterations) {
    $iteration++

    Write-Step ("Iteration {0} leasing up to {1} work item(s)" -f $iteration, $BatchSize)

    Send-Heartbeat -Status "leasing" -ActiveLeaseCount 0 -Message "Leasing work items."

    $leaseResponse = Invoke-JsonPost `
        -Url ($trimmedBaseUrl + "/api/operational/execution-work-items/lease") `
        -Body @{
            executionSessionId = $ExecutionSessionId
            workerId = $WorkerId
            take = $BatchSize
            leaseSeconds = $LeaseSeconds
        }

    $items = @($leaseResponse.items)
    Send-Heartbeat -Status "processing" -ActiveLeaseCount $items.Count -Message ("Processing {0} item(s)." -f $items.Count)

    if ($items.Count -eq 0) {
        Write-Step "No work items leased."
    }
    else {
        foreach ($item in $items) {
            Write-Step ("Processing {0} {1}" -f $item.executionWorkItemId, $item.workItemName)

            if ($FailLeasedItems) {
                Invoke-JsonPost `
                    -Url ($trimmedBaseUrl + "/api/operational/execution-work-items/fail") `
                    -Body @{
                        executionWorkItemId = $item.executionWorkItemId
                        leaseId = $item.leaseId
                        workerId = $WorkerId
                        errorMessage = "Simulated local worker failure."
                    } | Out-Null

                Write-Step ("Failed {0}" -f $item.executionWorkItemId)
            }
            else {
                Invoke-JsonPost `
                    -Url ($trimmedBaseUrl + "/api/operational/execution-work-items/complete") `
                    -Body @{
                        executionWorkItemId = $item.executionWorkItemId
                        leaseId = $item.leaseId
                        workerId = $WorkerId
                    } | Out-Null

                Write-Step ("Completed {0}" -f $item.executionWorkItemId)
            }
        }
    }

    Send-Heartbeat -Status "idle" -ActiveLeaseCount 0 -Message "Iteration completed."

    if ($iteration -lt $MaxIterations) {
        Start-Sleep -Seconds $PollSeconds
    }
}

Send-Heartbeat -Status "stopped" -ActiveLeaseCount 0 -Message "Local worker harness completed."
Write-Step "Local worker harness completed."
