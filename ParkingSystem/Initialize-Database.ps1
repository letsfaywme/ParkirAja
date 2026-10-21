<#
.SYNOPSIS
Apply parking schema version 1 explicitly; never run by WinForms startup.
.DESCRIPTION
ConnectionString is an owner/migration libpq connection string or PostgreSQL URI,
not an Npgsql connection string. No database/host/user/password defaults are supplied.
Prefer pgpass/PGPASSFILE over a password in command-line arguments. RuntimeRole must
already exist, be separate from the migration owner, and have no elevated or
inherited ownership privileges. This script never creates roles or credentials.

Schema creation and validation run in one transaction with ON_ERROR_STOP=1.
Reapplying version 1 checks columns, identity/generated/defaults, constraints,
foreign keys and indexes; incompatible schemas abort without data replacement.
Runtime gets schema USAGE, SELECT/INSERT/UPDATE on business tables, SELECT on
schema_version and USAGE/SELECT on sequences. No DELETE/TRUNCATE/schema CREATE.
Admin creation is a separate explicit application --create-admin operation.

Smoke checks, on an isolated disposable PostgreSQL database only:
1. Create separate migration owner and unprivileged runtime roles explicitly.
2. Run this script twice using the same parameters; both should succeed with no data reset.
3. Set schema_version.version to a different value only after dropping its CHECK,
   or alter one column/constraint in that fixture; rerun must fail without repairing it.
4. As runtime, SELECT/INSERT/UPDATE should work; DELETE/TRUNCATE/CREATE TABLE should fail.
.EXAMPLE
.\Initialize-Database.ps1 -ConnectionString 'host=127.0.0.1 port=5432 dbname=parking_fixture user=fixture_owner' -RuntimeRole 'fixture_runtime'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ConnectionString,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $RuntimeRole,

    [string] $PsqlPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'ConnectionString wajib berisi koneksi libpq eksplisit.'
}
if ([string]::IsNullOrWhiteSpace($RuntimeRole) -or $RuntimeRole.IndexOf([char]0) -ge 0) {
    throw 'RuntimeRole wajib berisi nama role PostgreSQL yang sudah dibuat.'
}
if ([string]::IsNullOrWhiteSpace($PsqlPath)) {
    $psqlCommand = Get-Command 'psql.exe' -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $psqlCommand) {
        $PsqlPath = $psqlCommand.Source
    } else {
        $PsqlPath = Join-Path $env:ProgramFiles 'PostgreSQL/16/bin/psql.exe'
    }
}
if (-not (Test-Path -LiteralPath $PsqlPath -PathType Leaf)) {
    throw 'psql.exe tidak ditemukan. Berikan -PsqlPath, misalnya C:\Program Files\PostgreSQL\16\bin\psql.exe.'
}
$schemaPath = Join-Path $PSScriptRoot 'ParkingSystem/Database/schema.sql'
if (-not (Test-Path -LiteralPath $schemaPath -PathType Leaf)) {
    throw 'Database/schema.sql tidak ditemukan di proyek ParkingSystem.'
}

$previousClientEncoding = [Environment]::GetEnvironmentVariable('PGCLIENTENCODING', 'Process')
try {
    [Environment]::SetEnvironmentVariable('PGCLIENTENCODING', 'UTF8', 'Process')
    & $PsqlPath '--no-psqlrc' '--no-password' '--dbname' $ConnectionString '--set' 'ON_ERROR_STOP=1' '--set' "runtime_role=$RuntimeRole" '--file' $schemaPath
    if ($LASTEXITCODE -ne 0) {
        throw "Inisialisasi database gagal (psql exit code $LASTEXITCODE). Schema/data tidak diganti; periksa koneksi, role, dan kompatibilitas versi."
    }
    Write-Host 'Schema parking versi 1 siap; hak runtime diterapkan. Buat admin melalui aplikasi --create-admin.'
} finally {
    [Environment]::SetEnvironmentVariable('PGCLIENTENCODING', $previousClientEncoding, 'Process')
}
