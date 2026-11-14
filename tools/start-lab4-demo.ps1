param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^equipment_borrowing_demo_[a-z0-9_]+$')]
    [ValidateLength(1, 63)]
    [string]$Database,
    [switch]$CreateDatabase
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$previousConnection = $env:EQUIPMENT_BORROWING_CONNECTION_STRING
$previousPassword = $env:PGPASSWORD
try {
    $savedConnection = [Environment]::GetEnvironmentVariable('EQUIPMENT_BORROWING_CONNECTION_STRING', 'User')
    if ([string]::IsNullOrWhiteSpace($savedConnection)) { throw 'Saved user database configuration is missing.' }
    $demoConnection = New-Object System.Data.Common.DbConnectionStringBuilder
    $demoConnection.set_ConnectionString($savedConnection)
    $demoConnection['Database'] = $Database
    $env:EQUIPMENT_BORROWING_CONNECTION_STRING = $demoConnection.get_ConnectionString()
    if ($CreateDatabase) {
        $psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
        if (!(Test-Path -LiteralPath $psql)) { throw 'PostgreSQL 18 psql was not found at its standard installation path.' }
        $env:PGPASSWORD = [string]$demoConnection['Password']
        $serverPort = if ($demoConnection.ContainsKey('Port')) { [string]$demoConnection['Port'] } else { '5432' }
        & $psql -X -w -h ([string]$demoConnection['Host']) -p $serverPort -U ([string]$demoConnection['Username']) -d postgres -v ON_ERROR_STOP=1 -c ('CREATE DATABASE "' + $Database + '"')
        if ($LASTEXITCODE -ne 0) { throw 'Database creation failed. Existing databases are never replaced.' }
    }
    Push-Location $repoRoot
    try {
        & dotnet ef database update --project src/EquipmentBorrowing.Infrastructure --startup-project src/EquipmentBorrowing.Infrastructure --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Migration failed; Desktop was not launched.' }
        $desktopPath = Join-Path $repoRoot 'src/EquipmentBorrowing.Desktop/bin/Release/net10.0/EquipmentBorrowing.Desktop.exe'
        $demoApp = Start-Process -FilePath $desktopPath -WorkingDirectory $repoRoot -PassThru
        Write-Output ('Demo database: ' + $Database)
        Write-Output ('Desktop process: ' + $demoApp.Id)
        Write-Output 'Saved user configuration is unchanged. Reuse this command without -CreateDatabase for restart checks.'
    }
    finally { Pop-Location }
}
finally {
    $env:EQUIPMENT_BORROWING_CONNECTION_STRING = $previousConnection
    $env:PGPASSWORD = $previousPassword
}
