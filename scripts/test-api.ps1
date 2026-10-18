#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Gets a real tr-web API at http://localhost:10213 for tests, agents and the bench.

.DESCRIPTION
    The one script every pipeline agent, test suite (including the xUnit fixture) and the bench run
    use to get a real tr-web API. It reuses a fresh API when one is already running; otherwise it
    builds tr-web from a checkout and starts it on a throwaway data folder (TEENSYROM_DATA_DIR).

    Usage:  pwsh scripts/test-api.ps1 <ensure|restart|stop|status> [-WebRepo <path>] [-WithFrontend]

      ensure    Reuse the running API if /api/version answers, the recorded pid is the process
                listening on 10213 and the tr-web fingerprint is unchanged; otherwise restart.
                A stale build or the operator's own tr-web fails one of those checks and is replaced.
      restart   Stop whatever listens on 10213, build tr-web (Debug) and start a fresh API.
      stop      Kill the listener and the recorded pid, then clear the state.
      status    Print one line: not running / running and fresh / running but stale (and why).

    Exit code 0 on success. On failure: exit 1 with a single "test-api: <reason>" line on stderr.

    Which tr-web: -WebRepo, else $env:TEENSYROM_WEB_DIR, else the sibling "teensyrom-web" folder next
    to this repo's root (the rad-orc workspace layout), else C:\dev\src\TeensyROM-Web. The tests must
    run against the tr-web that carries the port-10213 change; the script fails fast if the chosen
    checkout's appsettings.json does not listen on 10213.

    An agent that changes tr-web code must run "restart" before further tests. The bench plan uses
    this same script; it passes -WithFrontend to also serve the production Angular build from wwwroot
    (runs tr-web's src/scripts/copy-frontend.ps1, so tr-web needs "pnpm install" done first).

    State, logs and data live in $env:TEMP\teensyrom-cli-test-api. The version string is identical
    across builds, so staleness is judged by a fingerprint: tr-web HEAD plus a hash of "git status
    --porcelain" and "git diff HEAD", excluding the api-spec file every build rewrites. Untracked
    files are listed by status but their contents are not hashed.

    Startup discovery on an empty data folder may find attached TeensyROM units and reset one it
    finds busy; this is accepted.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Action,

    [string]$WebRepo,

    [switch]$WithFrontend
)

$ErrorActionPreference = 'Stop'

$Port = 10213
$VersionUrl = "http://localhost:$Port/api/version"
$RootUrl = "http://localhost:$Port/"
$StartTimeoutSeconds = 180
$StateDir = Join-Path $env:TEMP 'teensyrom-cli-test-api'
$StatePath = Join-Path $StateDir 'state.json'
$ApiLog = Join-Path $StateDir 'api.log'
$ApiErrLog = Join-Path $StateDir 'api.err.log'
$BuildLog = Join-Path $StateDir 'build.log'
$FrontendLog = Join-Path $StateDir 'frontend.log'
$ApiProjectRelPath = 'src/apps/api/src/TeensyRom.Api'
$SpecExclude = ':!src/apps/api/src/TeensyRom.Api/api-spec'
$RepoRoot = Split-Path $PSScriptRoot -Parent

function Fail([string]$Message) {
    [Console]::Error.WriteLine("test-api: $Message")
    exit 1
}

function Resolve-ExistingDir([string]$Path, [string]$Source) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        Fail "tr-web checkout from $Source not found: $Path"
    }
    (Resolve-Path -LiteralPath $Path).ProviderPath.TrimEnd('\')
}

# Returns the tr-web checkout to use, or $null when the default locations hold none.
function Resolve-WebRepo {
    if ($WebRepo) { return Resolve-ExistingDir $WebRepo '-WebRepo' }
    if ($env:TEENSYROM_WEB_DIR) { return Resolve-ExistingDir $env:TEENSYROM_WEB_DIR 'TEENSYROM_WEB_DIR' }
    $sibling = Join-Path (Split-Path $RepoRoot -Parent) 'teensyrom-web'
    if (Test-Path -LiteralPath $sibling -PathType Container) { return Resolve-ExistingDir $sibling 'sibling folder' }
    $fallback = 'C:\dev\src\TeensyROM-Web'
    if (Test-Path -LiteralPath $fallback -PathType Container) { return Resolve-ExistingDir $fallback 'default location' }
    $null
}

# Resolves the checkout for commands that build, and fails fast when it is not on the 10213 port.
function Resolve-BuildableWebRepo {
    $repo = Resolve-WebRepo
    if (-not $repo) {
        Fail "no tr-web checkout found - pass -WebRepo, set TEENSYROM_WEB_DIR, or place it at $(Join-Path (Split-Path $RepoRoot -Parent) 'teensyrom-web')"
    }
    $settingsPath = Join-Path $repo "$ApiProjectRelPath/appsettings.json"
    if (-not (Test-Path -LiteralPath $settingsPath)) { Fail "$repo is not a tr-web checkout (missing $settingsPath)" }
    $url = (Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).Kestrel.Endpoints.Http.Url
    if (-not $url -or -not $url.EndsWith(":$Port")) {
        Fail "$repo listens on $url, not $Port - point -WebRepo or TEENSYROM_WEB_DIR at the tr-web checkout with the port change"
    }
    $repo
}

function Get-Fingerprint([string]$Repo) {
    function Invoke-RepoGit([string[]]$GitArgs) {
        $out = & git -C $Repo @GitArgs 2>$null
        if ($LASTEXITCODE -ne 0) { Fail "git $($GitArgs -join ' ') failed in $Repo" }
        $out
    }
    $head = (Invoke-RepoGit @('rev-parse', 'HEAD') | Select-Object -First 1).Trim()
    $status = Invoke-RepoGit @('status', '--porcelain', '--', '.', $SpecExclude)
    $diff = Invoke-RepoGit @('diff', 'HEAD', '--', '.', $SpecExclude)
    $text = (@($status) + '--' + @($diff)) -join "`n"
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))).ToLowerInvariant()
    "${head}:$hash"
}

function Get-PortListeners {
    @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique)
}

# Returns the version reported by /api/version when the API answers, otherwise $null.
function Get-ApiVersion {
    try {
        $body = (Invoke-WebRequest -Uri $VersionUrl -TimeoutSec 3).Content.Trim()
        $reported = try { ($body | ConvertFrom-Json).version } catch { $null }
        if ($reported) { "$reported" } else { $body }
    }
    catch { $null }
}

function Read-State {
    if (-not (Test-Path -LiteralPath $StatePath)) { return $null }
    try { Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json } catch { $null }
}

function Remove-State {
    Remove-Item -LiteralPath $StatePath -Force -ErrorAction SilentlyContinue
}

# Guards against a recycled pid: the recorded process must be a dotnet host that predates the state.
function Test-RecordedProcess($State) {
    if (-not $State -or -not $State.pid) { return $false }
    $proc = Get-Process -Id ([int]$State.pid) -ErrorAction SilentlyContinue
    if (-not $proc -or $proc.ProcessName -ne 'dotnet') { return $false }
    $proc.StartTime.ToUniversalTime() -le ([datetime]$State.startedAt).ToUniversalTime()
}

function Get-Health([string]$Repo) {
    $listeners = Get-PortListeners
    if (-not $listeners) {
        return [pscustomobject]@{ Running = $false; Reasons = @(); Version = $null }
    }
    $version = Get-ApiVersion
    $state = Read-State
    $reasons = @()
    if ($null -eq $version) { $reasons += "$VersionUrl does not answer" }
    if (-not $state) {
        $reasons += 'not started by test-api.ps1 (no recorded state)'
    }
    else {
        if ([int]$state.pid -notin $listeners) {
            $reasons += "port $Port is held by pid $($listeners -join ',') instead of the recorded pid $($state.pid)"
        }
        if ($Repo) {
            if ($state.webRepo -ne $Repo) { $reasons += "started from $($state.webRepo), not $Repo" }
            elseif ($state.fingerprint -ne (Get-Fingerprint $Repo)) { $reasons += 'tr-web changed since the API was built' }
        }
        if ($WithFrontend -and -not $state.withFrontend) { $reasons += 'started without the frontend' }
    }
    [pscustomobject]@{ Running = $true; Reasons = $reasons; Version = $version }
}

function Stop-Api {
    $targets = [Collections.Generic.HashSet[int]]::new()
    foreach ($listener in Get-PortListeners) { [void]$targets.Add($listener) }
    $state = Read-State
    if (Test-RecordedProcess $state) { [void]$targets.Add([int]$state.pid) }

    foreach ($target in $targets) {
        Stop-Process -Id $target -Force -ErrorAction SilentlyContinue
        Wait-Process -Id $target -Timeout 15 -ErrorAction SilentlyContinue
    }

    $deadline = (Get-Date).AddSeconds(30)
    while ($true) {
        $held = Get-PortListeners
        if (-not $held) { break }
        if ((Get-Date) -gt $deadline) { Fail "could not free port $Port (still held by pid $($held -join ','))" }
        Start-Sleep -Milliseconds 250
    }
    Remove-State
}

function Build-Frontend([string]$Repo, [string]$ApiProject) {
    $copyScript = Join-Path $Repo 'src/scripts/copy-frontend.ps1'
    if (-not (Test-Path -LiteralPath $copyScript)) { Fail "frontend copy script not found: $copyScript" }
    New-Item -ItemType Directory -Path (Join-Path $ApiProject 'wwwroot') -Force | Out-Null
    Write-Host 'test-api: building the frontend (production Angular build)...'
    & pwsh -NoProfile -File $copyScript *> $FrontendLog
    if ($LASTEXITCODE -ne 0) { Fail "frontend build failed (exit $LASTEXITCODE) - run pnpm install in $Repo\src if dependencies are missing; see $FrontendLog" }
}

function Build-Api([string]$Repo, [string]$ApiProject) {
    Write-Host 'test-api: building tr-web (Debug)...'
    Push-Location $Repo
    try {
        & dotnet build $ApiProject -c Debug --nologo -v:minimal *> $BuildLog
        $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
    if ($exitCode -ne 0) {
        $firstError = Select-String -LiteralPath $BuildLog -Pattern ': error ' -List | Select-Object -First 1
        $detail = if ($firstError) { ": $($firstError.Line.Trim())" } else { '' }
        Fail "dotnet build failed (exit $exitCode)$detail - see $BuildLog"
    }
}

function Find-ApiDll([string]$ApiProject) {
    $binDir = Join-Path $ApiProject 'bin/Debug/net9.0'
    $dll = Get-ChildItem -LiteralPath $binDir -Recurse -Filter 'TeensyRom.Api.dll' -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.DirectoryName 'TeensyRom.Api.runtimeconfig.json') } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $dll) { Fail "built TeensyRom.Api.dll not found under $binDir" }
    $dll.FullName
}

function Restart-Api {
    $repo = Resolve-BuildableWebRepo
    $apiProject = Join-Path $repo $ApiProjectRelPath
    New-Item -ItemType Directory -Path $StateDir -Force | Out-Null

    Stop-Api
    $fingerprint = Get-Fingerprint $repo
    if ($WithFrontend) { Build-Frontend $repo $apiProject }
    Build-Api $repo $apiProject

    $dll = Find-ApiDll $apiProject
    $workingDir = if ($WithFrontend) { $apiProject } else { Split-Path $dll -Parent }
    $dataDir = Join-Path $StateDir ('data-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

    $previousDataDir = $env:TEENSYROM_DATA_DIR
    $env:TEENSYROM_DATA_DIR = $dataDir
    try {
        $proc = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @("`"$dll`"") `
            -WorkingDirectory $workingDir -RedirectStandardOutput $ApiLog -RedirectStandardError $ApiErrLog `
            -WindowStyle Hidden -PassThru
    }
    finally { $env:TEENSYROM_DATA_DIR = $previousDataDir }

    # A refused loopback connection costs ~2 s on Windows, so only probe HTTP once the port is listening.
    $deadline = (Get-Date).AddSeconds($StartTimeoutSeconds)
    $version = $null
    while ($null -eq $version) {
        if ($proc.HasExited) { Fail "API exited during startup (exit $($proc.ExitCode)) - see $ApiLog and $ApiErrLog" }
        if ($proc.Id -in (Get-PortListeners)) {
            $version = Get-ApiVersion
            if ($null -ne $version) { break }
        }
        if ((Get-Date) -gt $deadline) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Fail "API did not answer $VersionUrl within $StartTimeoutSeconds s - see $ApiLog and $ApiErrLog"
        }
        Start-Sleep -Milliseconds 500
    }

    [ordered]@{
        pid          = $proc.Id
        webRepo      = $repo
        fingerprint  = $fingerprint
        dataDir      = $dataDir
        startedAt    = [datetime]::UtcNow.ToString('o')
        log          = $ApiLog
        withFrontend = [bool]$WithFrontend
    } | ConvertTo-Json | Set-Content -LiteralPath $StatePath -Encoding utf8

    Get-ChildItem -LiteralPath $StateDir -Directory -Filter 'data-*' |
        Where-Object { $_.FullName -ne $dataDir } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    if ($WithFrontend) {
        $isHtml = $false
        try { $isHtml = "$((Invoke-WebRequest -Uri $RootUrl -TimeoutSec 10).Headers['Content-Type'])" -match 'text/html' } catch { }
        if (-not $isHtml) { Write-Warning "GET $RootUrl did not return HTML - the frontend may not be served from wwwroot" }
    }
    Write-Host "test-api: API started (pid $($proc.Id), version $version) at http://localhost:$Port"
}

function Invoke-Ensure {
    $repo = Resolve-BuildableWebRepo
    $health = Get-Health $repo
    if ($health.Running -and $health.Reasons.Count -eq 0) {
        $state = Read-State
        Write-Host "test-api: reusing API (pid $($state.pid), version $($health.Version)) at http://localhost:$Port"
        return
    }
    if ($health.Running) { Write-Host "test-api: replacing running API - $($health.Reasons -join '; ')" }
    Restart-Api
}

function Invoke-Status {
    $health = Get-Health (Resolve-WebRepo)
    if (-not $health.Running) { Write-Host 'not running'; return }
    if ($health.Reasons.Count -gt 0) { Write-Host "running but stale: $($health.Reasons -join '; ')"; return }
    $state = Read-State
    Write-Host "running and fresh (pid $($state.pid), version $($health.Version))"
}

try {
    switch ($Action) {
        'ensure' { Invoke-Ensure }
        'restart' { Restart-Api }
        'stop' { Stop-Api; Write-Host "test-api: stopped; port $Port is free" }
        'status' { Invoke-Status }
        default { Fail 'usage: test-api.ps1 <ensure|restart|stop|status> [-WebRepo <path>] [-WithFrontend]' }
    }
}
catch {
    Fail ($_.Exception.Message -replace '\s*[\r\n]+\s*', ' ')
}
