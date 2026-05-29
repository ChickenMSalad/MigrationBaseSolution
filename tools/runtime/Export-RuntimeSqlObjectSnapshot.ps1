[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath,

    [Parameter(Mandatory = $false)]
    [string] $EnvironmentName = 'unknown'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = [System.IO.Path]::Combine($repoRoot, $OutputPath)
}
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$query = @"
SET NOCOUNT ON;

SELECT
    'TABLE' AS ItemType,
    s.name AS SchemaName,
    t.name AS ObjectName,
    NULL AS ColumnName,
    NULL AS DataType,
    NULL AS IsNullable,
    NULL AS IsIdentity,
    NULL AS RelatedObject
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = 'migration'

UNION ALL

SELECT
    'COLUMN' AS ItemType,
    s.name AS SchemaName,
    t.name AS ObjectName,
    c.name AS ColumnName,
    ty.name AS DataType,
    CONVERT(nvarchar(5), c.is_nullable) AS IsNullable,
    CONVERT(nvarchar(5), c.is_identity) AS IsIdentity,
    NULL AS RelatedObject
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = 'migration'

UNION ALL

SELECT
    'FOREIGN_KEY' AS ItemType,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SchemaName,
    OBJECT_NAME(fk.parent_object_id) AS ObjectName,
    fk.name AS ColumnName,
    NULL AS DataType,
    NULL AS IsNullable,
    NULL AS IsIdentity,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) + '.' + OBJECT_NAME(fk.referenced_object_id) AS RelatedObject
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = 'migration'
ORDER BY ItemType, SchemaName, ObjectName, ColumnName;
"@

$tempPath = [System.IO.Path]::GetTempFileName()
try {
    $serverName = $SqlServer
    if ($serverName -notmatch '\.database\.windows\.net$' -and $serverName -notmatch '[\\,]') {
        $serverName = ('{0}.database.windows.net' -f $serverName)
    }

    & sqlcmd -S $serverName -d $Database -U $SqlAdmin -P $SqlPasswordPlain -Q $query -s '|' -W -h -1 -o $tempPath
    if ($LASTEXITCODE -ne 0) {
        throw 'sqlcmd failed while exporting runtime SQL object snapshot.'
    }

    $items = @()
    foreach ($line in Get-Content -LiteralPath $tempPath) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -match '^\-+$') { continue }
        $parts = $line -split '\|', 8
        if (@($parts).Count -lt 8) { continue }
        $items += [pscustomobject]@{
            itemType = $parts[0].Trim()
            schemaName = $parts[1].Trim()
            objectName = $parts[2].Trim()
            columnName = $parts[3].Trim()
            dataType = $parts[4].Trim()
            isNullable = $parts[5].Trim()
            isIdentity = $parts[6].Trim()
            relatedObject = $parts[7].Trim()
        }
    }

    $snapshot = [pscustomobject]@{
        environmentName = $EnvironmentName
        generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        database = $Database
        objects = @($items)
    }

    $json = ConvertTo-Json -InputObject $snapshot -Depth 10
    Set-Content -LiteralPath $outputFullPath -Value $json -Encoding UTF8
    Write-Host ('Runtime SQL object snapshot written to {0}' -f $outputFullPath)
}
finally {
    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}
