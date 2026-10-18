#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Refreshes the committed copy of tr-web's API spec and regenerates the CLI's API client from it.

.DESCRIPTION
    Usage:  pwsh scripts/regenerate-api-client.ps1 [-WebRepo <path>]

    Copies <tr-web>/src/apps/api/src/TeensyRom.Api/api-spec/TeensyRom.Api.json over
    src/TeensyRom.Cli/Api/TeensyRom.Api.json, restores the pinned NSwag tool and runs it on
    src/TeensyRom.Cli/Api/nswag.json, which rewrites Api/Generated/TeensyRomApiClient.g.cs.
    Needs the .NET SDK only - no Java or Node.

    Which tr-web: -WebRepo, else $env:TEENSYROM_WEB_DIR, else the sibling "teensyrom-web" folder next
    to this repo's root, else C:\dev\src\TeensyROM-Web (the same order scripts/test-api.ps1 uses).
    Check out the tr-web version the CLI should pin first; see src/TeensyRom.Cli/Api/README.md.
#>
[CmdletBinding()]
param(
    [string]$WebRepo
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path $PSScriptRoot -Parent
$ApiDir = Join-Path $RepoRoot 'src/TeensyRom.Cli/Api'
$SpecCopy = Join-Path $ApiDir 'TeensyRom.Api.json'
$NswagConfig = 'src/TeensyRom.Cli/Api/nswag.json'
$SpecRelPath = 'src/apps/api/src/TeensyRom.Api/api-spec/TeensyRom.Api.json'

function Fail([string]$Message) {
    [Console]::Error.WriteLine("regenerate-api-client: $Message")
    exit 1
}

function Resolve-ExistingDir([string]$Path, [string]$Source) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        Fail "tr-web checkout from $Source not found: $Path"
    }
    (Resolve-Path -LiteralPath $Path).ProviderPath.TrimEnd('\')
}

function Resolve-WebRepo {
    if ($WebRepo) { return Resolve-ExistingDir $WebRepo '-WebRepo' }
    if ($env:TEENSYROM_WEB_DIR) { return Resolve-ExistingDir $env:TEENSYROM_WEB_DIR 'TEENSYROM_WEB_DIR' }
    $sibling = Join-Path (Split-Path $RepoRoot -Parent) 'teensyrom-web'
    if (Test-Path -LiteralPath $sibling -PathType Container) { return Resolve-ExistingDir $sibling 'sibling folder' }
    $fallback = 'C:\dev\src\TeensyROM-Web'
    if (Test-Path -LiteralPath $fallback -PathType Container) { return Resolve-ExistingDir $fallback 'default location' }
    Fail "no tr-web checkout found - pass -WebRepo, set TEENSYROM_WEB_DIR, or place it at $sibling"
}

try {
    $repo = Resolve-WebRepo
    $source = Join-Path $repo $SpecRelPath
    if (-not (Test-Path -LiteralPath $source)) { Fail "spec not found: $source" }

    Copy-Item -LiteralPath $source -Destination $SpecCopy -Force
    $specVersion = (Get-Content -LiteralPath $SpecCopy -Raw | ConvertFrom-Json).info.version
    Write-Host "regenerate-api-client: spec copied from $repo (tr-web $specVersion)"

    $pin = $null
    if ((Get-Content -LiteralPath (Join-Path $ApiDir 'ApiPin.cs') -Raw) -match 'Version\s*=\s*"([^"]+)"') { $pin = $Matches[1] }
    if ($pin -ne $specVersion) {
        Write-Warning "ApiPin.Version is '$pin' but the spec is $specVersion - bump ApiPin.Version if the pin is moving."
    }

    Push-Location $RepoRoot
    try {
        & dotnet tool restore --verbosity quiet
        if ($LASTEXITCODE -ne 0) { Fail "dotnet tool restore failed (exit $LASTEXITCODE)" }

        & dotnet nswag run $NswagConfig
        if ($LASTEXITCODE -ne 0) { Fail "nswag run failed (exit $LASTEXITCODE)" }
    }
    finally { Pop-Location }

    Write-Host 'regenerate-api-client: Api/Generated/TeensyRomApiClient.g.cs regenerated'
}
catch {
    Fail ($_.Exception.Message -replace '\s*[\r\n]+\s*', ' ')
}
