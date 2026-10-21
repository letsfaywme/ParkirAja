[CmdletBinding()]
param(
    [string]$PostgresBin = 'C:\Program Files\PostgreSQL\16\bin'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$fixtureSecrets = @()

# All credentials and PostgreSQL settings belong to child processes, never this session.
function Invoke-FixtureProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$Arguments = @(),
        [hashtable]$Environment = @{},
        [AllowNull()][string]$InputText = $null,
        [switch]$NoRedirect
    )
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $FilePath
    $info.WorkingDirectory = $PSScriptRoot
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = -not $NoRedirect
    $info.RedirectStandardError = -not $NoRedirect
    $info.RedirectStandardInput = -not $NoRedirect
    # Windows command-line quoting, including quotes and trailing backslashes.
    $info.Arguments = (($Arguments | ForEach-Object {
        '"' + ([regex]::Replace([regex]::Replace($_, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1')) + '"'
    }) -join ' ')
    foreach ($key in @($info.EnvironmentVariables.Keys)) {
        if ($key -like 'PG*' -or $key -like 'PARKING_*') {
            $info.EnvironmentVariables.Remove($key)
        }
    }
    foreach ($key in $Environment.Keys) { $info.EnvironmentVariables[$key] = $Environment[$key] }
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (-not $process.Start()) { throw "Gagal menjalankan $FilePath." }
        if (-not $NoRedirect) {
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if ($null -ne $InputText) { $process.StandardInput.Write($InputText) }
            $process.StandardInput.Close()
        }
        $process.WaitForExit()
        if (-not $NoRedirect) {
            $outputText = $stdout.GetAwaiter().GetResult()
            $errorText = $stderr.GetAwaiter().GetResult()
            foreach ($secret in $fixtureSecrets) {
                $outputText = $outputText.Replace($secret, '[REDACTED]')
                $errorText = $errorText.Replace($secret, '[REDACTED]')
            }
            if ($outputText) { Write-Host $outputText.TrimEnd() }
            if ($errorText) { Write-Host $errorText.TrimEnd() }
        }
        if ($process.ExitCode -ne 0) {
            throw "Proses $([IO.Path]::GetFileName($FilePath)) gagal (exit $($process.ExitCode))."
        }
    } finally {
        $process.Dispose()
    }
}

function New-FixturePassword {
    $bytes = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

$harness = Join-Path $PSScriptRoot 'ParkingSystem.Checks/ParkingSystem.Checks.csproj'
$initializer = Join-Path $PSScriptRoot 'Initialize-Database.ps1'
$forms = Join-Path $PSScriptRoot 'Check-Forms.ps1'
foreach ($file in @($harness, $initializer, $forms)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Prasyarat pemeriksaan belum tersedia: $file. Pemeriksaan tidak dijalankan."
    }
}
foreach ($binary in @('initdb.exe', 'pg_ctl.exe', 'psql.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PostgresBin $binary) -PathType Leaf)) {
        throw "PostgreSQL 16 diperlukan: $(Join-Path $PostgresBin $binary). Gunakan -PostgresBin untuk lokasi instalasi lain."
    }
}
$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$powershell = Join-Path $PSHOME 'powershell.exe'
if (-not (Test-Path -LiteralPath $powershell -PathType Leaf)) {
    $powershell = (Get-Command powershell.exe -ErrorAction Stop).Source
}
$initdb = Join-Path $PostgresBin 'initdb.exe'
$pgctl = Join-Path $PostgresBin 'pg_ctl.exe'
$psql = Join-Path $PostgresBin 'psql.exe'
# Reject another major version before creating anything.
$version = & $initdb --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '\b16\.') {
    throw "Binary PostgreSQL 16 diperlukan di $PostgresBin."
}

$temporary = Join-Path ([IO.Path]::GetTempPath()) ('ParkingSystem-Business-' + [Guid]::NewGuid().ToString('N'))
$data = Join-Path $temporary 'data'
$log = Join-Path $temporary 'postgres.log'
$passwordFile = Join-Path $temporary 'initdb-password'
$created = $false
$startAttempted = $false
try {
    New-Item -ItemType Directory -Path $temporary | Out-Null
    $created = $true
    # Password file and cluster are private to current Windows identity.
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.SetOwner($identity)
    $rule = New-Object Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($rule)
    Set-Acl -LiteralPath $temporary -AclObject $acl

    $bootstrapPassword = New-FixturePassword
    $ownerPassword = New-FixturePassword
    $runtimePassword = New-FixturePassword
    [IO.File]::WriteAllText($passwordFile, $bootstrapPassword + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $fixtureSecrets = @($bootstrapPassword, $ownerPassword, $runtimePassword)
    Invoke-FixtureProcess $initdb @('-D', $data, '--username=fixture_bootstrap', '--auth-host=scram-sha-256', '--auth-local=scram-sha-256', '--encoding=UTF8', '--locale=C', "--pwfile=$passwordFile")
    Remove-Item -LiteralPath $passwordFile -Force

    $listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        $port = $listener.LocalEndpoint.Port
    } finally { $listener.Stop() }
    # Port reservation must be released for PostgreSQL. A competing bind fails, never falls back to another cluster.
    $startAttempted = $true
    try {
        # Server inherits pg_ctl handles on Windows; no redirected pipes may remain open for server lifetime.
        Invoke-FixtureProcess $pgctl @('-D', $data, '-l', $log, '-w', '-t', '30', '-o', "-h 127.0.0.1 -p $port", 'start') -NoRedirect
    } catch {
        if (Test-Path -LiteralPath $log -PathType Leaf) {
            $startupLog = [IO.File]::ReadAllText($log)
            foreach ($secret in $fixtureSecrets) { $startupLog = $startupLog.Replace($secret, '[REDACTED]') }
            Write-Host "PostgreSQL startup log:`n$startupLog"
        }
        throw
    }
    $bootstrapEnvironment = @{ PGPASSWORD = $bootstrapPassword; PGCONNECT_TIMEOUT = '10' }
    $sql = @"
CREATE ROLE parking_owner LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '$ownerPassword';
CREATE ROLE parking_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '$runtimePassword';
CREATE DATABASE parking_checks OWNER parking_owner;
REVOKE ALL ON DATABASE parking_checks FROM PUBLIC;
GRANT CONNECT ON DATABASE parking_checks TO parking_runtime;
"@
    Invoke-FixtureProcess $psql @('-X', '--no-password', '--set', 'ON_ERROR_STOP=1', '--host', '127.0.0.1', '--port', "$port", '--username', 'fixture_bootstrap', '--dbname', 'postgres') $bootstrapEnvironment $sql
    $ownerLibpq = "host=127.0.0.1 port=$port dbname=parking_checks user=parking_owner connect_timeout=10"
    Invoke-FixtureProcess $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $initializer, '-ConnectionString', $ownerLibpq, '-RuntimeRole', 'parking_runtime', '-PsqlPath', $psql) @{ PGPASSWORD = $ownerPassword; PGCONNECT_TIMEOUT = '10' }
    # Second application must validate existing schema without resetting fixture data.
    Invoke-FixtureProcess $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $initializer, '-ConnectionString', $ownerLibpq, '-RuntimeRole', 'parking_runtime', '-PsqlPath', $psql) @{ PGPASSWORD = $ownerPassword; PGCONNECT_TIMEOUT = '10' }

    $runtimeConnection = "Host=127.0.0.1;Port=$port;Database=parking_checks;Username=parking_runtime;Password=$runtimePassword;Timeout=10;Include Error Detail=false"
    $ownerConnection = "Host=127.0.0.1;Port=$port;Database=parking_checks;Username=parking_owner;Password=$ownerPassword;Timeout=10;Include Error Detail=false"
    $testPassword = New-FixturePassword
    $fixtureSecrets += $testPassword
    # Owner connection is fixture-only: harness seeds and checks SQL invariants. UI receives runtime only.
    Invoke-FixtureProcess $dotnet @('run', '--project', $harness) @{ PARKING_DB_CONNECTION = $runtimeConnection; PARKING_CHECKS_OWNER_CONNECTION = $ownerConnection; PARKING_CHECKS_POSTGRES_BIN = $PostgresBin; PARKING_CHECKS_DATA_DIRECTORY = $data; PARKING_CHECKS_PASSWORD = $testPassword }
    Invoke-FixtureProcess $powershell @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $forms) @{ PARKING_DB_CONNECTION = $runtimeConnection; PARKING_CHECKS_PASSWORD = $testPassword } -NoRedirect
    Write-Host 'PASS: pemeriksaan bisnis dan WinForms selesai.'
} finally {
    if ($created) {
        $safeToRemove = $true
        if ($startAttempted -and (Test-Path -LiteralPath (Join-Path $data 'postmaster.pid'))) {
            try {
                Invoke-FixtureProcess $pgctl @('-D', $data, '-w', '-t', '30', '-m', 'immediate', 'stop')
            } catch {
                $safeToRemove = $false
                Write-Warning "Cluster fixture gagal dihentikan; data tidak dihapus: $temporary"
                throw
            }
        }
        if ($safeToRemove) { Remove-Item -LiteralPath $temporary -Recurse -Force }
    }
}
