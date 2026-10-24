<#
.SYNOPSIS
    Applies every Database/Migrations/*.sql (in name order) to one database, after a COPY_ONLY backup.

.DESCRIPTION
    By default the target is read from tabseetCourse/Web.config -> <connectionStrings> "TabseetConnection",
    so whatever database the site is pointed at is the one that gets migrated (no password is stored here).
    Each script is idempotent and runs with sqlcmd -b: the first failing script stops the run.

.EXAMPLE
    pwsh -File Database\Migrations\Apply-Migrations.ps1                 # target = Web.config connection (server)
    pwsh -File Database\Migrations\Apply-Migrations.ps1 -WhatIf         # show target + scripts, change nothing
    pwsh -File Database\Migrations\Apply-Migrations.ps1 -Server . -Database TabseetDB_Local -NoBackup
#>
[CmdletBinding()]
param(
    [string]$Server,
    [string]$Database,
    [string]$User,
    [string]$Password,
    [switch]$NoBackup,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw "sqlcmd.exe was not found on PATH." }

# ---- resolve target -------------------------------------------------------
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $Server) {
    $webConfig = Join-Path $root 'tabseetCourse\Web.config'
    [xml]$cfg = Get-Content $webConfig -Raw
    $csNode = $cfg.configuration.connectionStrings
    if ($csNode.configSource) {
        $csFile = Join-Path (Split-Path -Parent $webConfig) $csNode.configSource
        if (-not (Test-Path $csFile)) { throw "Web.config points to '$($csNode.configSource)' but $csFile does not exist (copy ConnectionStrings.config.example and fill it in)." }
        [xml]$extCfg = Get-Content $csFile -Raw
        $csNode = $extCfg.connectionStrings
    }
    $cs = ($csNode.add | Where-Object { $_.name -eq 'TabseetConnection' }).connectionString
    if (-not $cs) { throw "TabseetConnection was not found in $webConfig" }
    $kv = @{}
    foreach ($part in $cs.Split(';')) {
        if ($part -match '^\s*([^=]+?)\s*=\s*(.*?)\s*$') { $kv[$Matches[1].ToLower()] = $Matches[2] }
    }
    $Server   = $kv['data source'];     if (-not $Server)   { $Server   = $kv['server'] }
    $Database = $kv['initial catalog']; if (-not $Database) { $Database = $kv['database'] }
    $User     = $kv['user id'];         if (-not $User)     { $User     = $kv['uid'] }
    $Password = $kv['password'];        if (-not $Password) { $Password = $kv['pwd'] }
}
if (-not $Server -or -not $Database) { throw "Server and Database could not be resolved." }

$auth = @()
if ($User) { $auth = @('-U', $User, '-P', $Password) }          # otherwise integrated security
$conn = @('-S', $Server, '-d', $Database) + $auth + @('-C', '-l', '30')

$scripts = Get-ChildItem (Join-Path $PSScriptRoot '*.sql') | Sort-Object Name
Write-Host "Target  : $Server / $Database $(if ($User) { "(login $User)" } else { '(Windows auth)' })"
Write-Host "Scripts : $($scripts.Count)"
$scripts | ForEach-Object { Write-Host "   - $($_.Name)" }

# ---- sanity: can we reach it? -------------------------------------------
$who = & sqlcmd @conn -h -1 -W -Q "SET NOCOUNT ON; SELECT DB_NAME() + ' @ ' + @@SERVERNAME;" 2>&1
if ($LASTEXITCODE -ne 0) { throw "Cannot connect: $who" }
Write-Host "Connected: $($who | Select-Object -First 1)"

if ($WhatIf) { Write-Host "`n-WhatIf: nothing was changed."; return }

# ---- backup ---------------------------------------------------------------
if (-not $NoBackup) {
    $dir = (& sqlcmd @conn -h -1 -W -Q "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(260));" 2>&1 | Select-Object -First 1).Trim()
    if (-not $dir -or $dir -eq 'NULL') { throw "Could not read the server's default backup folder (use -NoBackup to skip the backup)." }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $bak = Join-Path $dir "${Database}_before_migrations_$stamp.bak"
    Write-Host "`nBackup  : $bak"
    $o = & sqlcmd @conn -b -Q "BACKUP DATABASE [$Database] TO DISK = N'$bak' WITH COPY_ONLY, INIT, STATS = 100;" 2>&1
    $o | ForEach-Object { Write-Host "   $_" }
    if ($LASTEXITCODE -ne 0) { throw "Backup failed - no migration was applied." }
}

# ---- apply ----------------------------------------------------------------
foreach ($f in $scripts) {
    Write-Host "`n--- $($f.Name)"
    $o = & sqlcmd @conn -b -i $f.FullName 2>&1
    $o | Where-Object { "$_".Trim() -ne '' } | ForEach-Object { Write-Host "   $_" }
    if ($LASTEXITCODE -ne 0) { throw "$($f.Name) failed (exit $LASTEXITCODE). Later scripts were not run." }
}

# ---- verify ---------------------------------------------------------------
Write-Host "`n--- verification"
$check = @"
SET NOCOUNT ON;
SELECT 'table  ' + n + ' : ' + CASE WHEN OBJECT_ID('dbo.' + n) IS NULL THEN 'MISSING' ELSE 'ok' END
FROM (VALUES ('TeacherLectureStatus'),('TeacherLectureEvents'),('AdminNotifications')) t(n)
UNION ALL
SELECT 'column std_lecture.' + n + ' : ' + CASE WHEN COL_LENGTH('dbo.std_lecture', n) IS NULL THEN 'MISSING' ELSE 'ok' END
FROM (VALUES ('StartedAt'),('LastHeartbeatAt'),('WatchedSeconds'),('RequiredSeconds'),('CompletedAt'),('Source'),('MarkedBy'),('MarkedAt')) c(n)
UNION ALL
SELECT 'column Std_Reg_Category.' + n + ' : ' + CASE WHEN COL_LENGTH('dbo.Std_Reg_Category', n) IS NULL THEN 'MISSING' ELSE 'ok' END
FROM (VALUES ('RequestedAt'),('ApprovedAt'),('ApprovedBy')) c(n)
UNION ALL
SELECT 'index  ' + n + ' : ' + CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = n) THEN 'ok' ELSE 'MISSING' END
FROM (VALUES ('UX_std_lecture_Std_Lecture'),('IX_Std_Reg_Category_Std_Category')) i(n);
"@
$v = & sqlcmd @conn -h -1 -W -Q $check 2>&1
$v | Where-Object { "$_".Trim() -ne '' } | ForEach-Object { Write-Host "   $_" }
if ($v -match 'MISSING') { throw "Verification found missing objects." }
Write-Host "`nDone: all migrations are applied on $Server / $Database."
