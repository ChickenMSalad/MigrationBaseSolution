[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string] $DispatcherApp,

    [Parameter(Mandatory = $true)]
    [string] $ExecutorApp,

    [Parameter(Mandatory = $true)]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $true)]
    [Guid] $RunId,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusNamespace,

    [Parameter(Mandatory = $true)]
    [string] $ServiceBusQueue,

    [Parameter(Mandatory = $false)]
    [string] $PayloadPath,

    [Parameter(Mandatory = $false)]
    [int] $DeploymentTimeoutSeconds = 900,

    [Parameter(Mandatory = $false)]
    [int] $RestartTimeoutSeconds = 180,

    [Parameter(Mandatory = $false)]
    [switch] $SkipPublish,

    [Parameter(Mandatory = $false)]
    [switch] $SkipDeploy
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$azCommand = Get-Command az -ErrorAction Stop
$azPath = $azCommand.Source

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = Join-Path $current.Path 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $candidate) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate repo root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $FilePath,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [Parameter(Mandatory = $false)]
        [int] $TimeoutSeconds = 0,

        [Parameter(Mandatory = $false)]
        [string] $OperationName = 'command'
    )

    if ($TimeoutSeconds -le 0) {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw ("{0} failed with exit code {1}." -f $OperationName, $LASTEXITCODE)
        }

        return
    }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    # Windows PowerShell 5.1 / older .NET Framework does not expose
    # ProcessStartInfo.ArgumentList. Build a safely quoted argument string instead.
    $quotedArguments = New-Object System.Collections.Generic.List[string]
    foreach ($argument in $Arguments) {
        if ($null -eq $argument) {
            continue
        }

        $argumentText = [string] $argument
        if ($argumentText.Length -eq 0) {
            [void] $quotedArguments.Add('""')
            continue
        }

        if ($argumentText -match '[\s"]') {
            $escapedArgument = $argumentText.Replace('\\', '\\').Replace('"', '\"')
            [void] $quotedArguments.Add(('"{0}"' -f $escapedArgument))
        }
        else {
            [void] $quotedArguments.Add($argumentText)
        }
    }

    $startInfo.Arguments = [string]::Join(' ', $quotedArguments)

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo

    $stdoutBuilder = New-Object System.Text.StringBuilder
    $stderrBuilder = New-Object System.Text.StringBuilder

    $outputHandler = [System.Diagnostics.DataReceivedEventHandler] {
        param($sender, $eventArgs)
        if ($null -ne $eventArgs.Data) {
            [void] $stdoutBuilder.AppendLine($eventArgs.Data)
            Write-Host $eventArgs.Data
        }
    }

    $errorHandler = [System.Diagnostics.DataReceivedEventHandler] {
        param($sender, $eventArgs)
        if ($null -ne $eventArgs.Data) {
            [void] $stderrBuilder.AppendLine($eventArgs.Data)
            Write-Warning $eventArgs.Data
        }
    }

    $process.add_OutputDataReceived($outputHandler)
    $process.add_ErrorDataReceived($errorHandler)

    try {
        [void] $process.Start()
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()

        $timeoutMilliseconds = $TimeoutSeconds * 1000
        if (-not $process.WaitForExit($timeoutMilliseconds)) {
            try {
                $process.Kill()
            }
            catch {
                Write-Warning ("Timed out and could not kill process for {0}: {1}" -f $OperationName, $_.Exception.Message)
            }

            throw ("{0} timed out after {1} seconds. Check Azure deployment status manually before retrying." -f $OperationName, $TimeoutSeconds)
        }

        # Allow async output handlers to flush.
        $process.WaitForExit()

        if ($process.ExitCode -ne 0) {
            $stderr = $stderrBuilder.ToString().Trim()
            if ([string]::IsNullOrWhiteSpace($stderr)) {
                throw ("{0} failed with exit code {1}." -f $OperationName, $process.ExitCode)
            }

            throw ("{0} failed with exit code {1}: {2}" -f $OperationName, $process.ExitCode, $stderr)
        }
    }
    finally {
        $process.remove_OutputDataReceived($outputHandler)
        $process.remove_ErrorDataReceived($errorHandler)
        $process.Dispose()
    }
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("{0} was not found: {1}" -f $Description, $Path)
    }
}

$repoRoot = Get-RepoRoot
$publishScript = Join-Path $repoRoot 'tools\runtime\Publish-RuntimeWorker.ps1'
$enqueueScript = Join-Path $repoRoot 'tools\runtime\Invoke-RuntimeSmokeEnqueue.ps1'
$stateScript = Join-Path $repoRoot 'tools\runtime\Test-RuntimeSmokeState.ps1'

foreach ($requiredPath in @($publishScript, $enqueueScript, $stateScript)) {
    Assert-FileExists -Path $requiredPath -Description 'Required runtime script'
}

if (-not $SkipPublish) {
    Write-Host 'Publishing dispatcher...'
    & $publishScript -Role Dispatcher
    if ($LASTEXITCODE -ne 0) {
        throw 'Dispatcher publish failed.'
    }

    Write-Host 'Publishing executor...'
    & $publishScript -Role Executor
    if ($LASTEXITCODE -ne 0) {
        throw 'Executor publish failed.'
    }
}
else {
    Write-Host 'Skipping publish because -SkipPublish was specified.'
}

$dispatcherZip = Join-Path $repoRoot 'artifacts\publish\sb-dispatcher.zip'
$executorZip = Join-Path $repoRoot 'artifacts\publish\sb-executor.zip'

Assert-FileExists -Path $dispatcherZip -Description 'Dispatcher ZIP'
Assert-FileExists -Path $executorZip -Description 'Executor ZIP'

if (-not $SkipDeploy) {
    Write-Host 'Deploying dispatcher...'
    Invoke-CheckedProcess -FilePath $azPath -Arguments @(
        'webapp', 'deploy',
        '--resource-group', $ResourceGroup,
        '--name', $DispatcherApp,
        '--src-path', $dispatcherZip,
        '--type', 'zip',
        '--clean', 'true',
        '--restart', 'true'
    ) -TimeoutSeconds $DeploymentTimeoutSeconds -OperationName 'Dispatcher deployment'

    Write-Host 'Deploying executor...'
    Invoke-CheckedProcess -FilePath $azPath -Arguments @(
        'webapp', 'deploy',
        '--resource-group', $ResourceGroup,
        '--name', $ExecutorApp,
        '--src-path', $executorZip,
        '--type', 'zip',
        '--clean', 'true',
        '--restart', 'true'
    ) -TimeoutSeconds $DeploymentTimeoutSeconds -OperationName 'Executor deployment'
}
else {
    Write-Host 'Skipping deploy because -SkipDeploy was specified.'
}

Write-Host 'Restarting executor before enqueue...'
Invoke-CheckedProcess -FilePath $azPath -Arguments @(
    'webapp', 'restart',
    '--resource-group', $ResourceGroup,
    '--name', $ExecutorApp
) -TimeoutSeconds $RestartTimeoutSeconds -OperationName 'Executor restart'

$enqueueArgs = @(
    '-SqlServer', $SqlServer,
    '-Database', $Database,
    '-SqlAdmin', $SqlAdmin,
    '-SqlPasswordPlain', $SqlPasswordPlain,
    '-RunId', $RunId.ToString()
)

if (-not [string]::IsNullOrWhiteSpace($PayloadPath)) {
    $enqueueArgs += @('-PayloadPath', $PayloadPath)
}

Write-Host 'Enqueueing smoke work item...'
& $enqueueScript @enqueueArgs
if ($LASTEXITCODE -ne 0) {
    throw 'Smoke enqueue failed.'
}

Write-Host 'Restarting dispatcher to dispatch smoke item...'
Invoke-CheckedProcess -FilePath $azPath -Arguments @(
    'webapp', 'restart',
    '--resource-group', $ResourceGroup,
    '--name', $DispatcherApp
) -TimeoutSeconds $RestartTimeoutSeconds -OperationName 'Dispatcher restart'

Write-Host 'Smoke state after dispatch:'
& $stateScript `
    -SqlServer $SqlServer `
    -Database $Database `
    -SqlAdmin $SqlAdmin `
    -SqlPasswordPlain $SqlPasswordPlain `
    -ResourceGroup $ResourceGroup `
    -ServiceBusNamespace $ServiceBusNamespace `
    -ServiceBusQueue $ServiceBusQueue
if ($LASTEXITCODE -ne 0) {
    throw 'Smoke state verification failed.'
}

Write-Host 'Deployment smoke command completed. Review executor logs and SQL final status before handoff.'
