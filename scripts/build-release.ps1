#Requires -Version 5.1
<#
.SYNOPSIS
    RevitMCP-FLOW 一鍵發布腳本
.DESCRIPTION
    1. Build all Revit versions (R22-R26)
    2. Bundle MCP Server: esbuild ESM → CJS → pkg EXE
    3. Copy DLLs to dist/
    4. Recompile Inno Setup → RevitMCP-FLOW-Setup.exe
#>
param(
    [switch]$SkipDotnet,
    [switch]$SkipPkg,
    [switch]$SkipInno
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "  ✅ $msg" -ForegroundColor Green }
function Err($msg)  { Write-Host "  ❌ $msg" -ForegroundColor Red; exit 1 }

# ── Step 1: Build all Revit DLLs ──────────────────────────────────────
if (-not $SkipDotnet) {
    Step "Building Revit add-in (R22–R26)"
    $configs = @("Release.R22","Release.R23","Release.R24","Release.R25","Release.R26")
    foreach ($cfg in $configs) {
        & dotnet build -c $cfg "$root\MCP\RevitMCP.csproj" | Out-Null
        if ($LASTEXITCODE -eq 0) { Ok "$cfg" } else { Err "$cfg build failed" }
    }
}

# ── Step 2: Bundle MCP Server → EXE ───────────────────────────────────
if (-not $SkipPkg) {
    Step "Bundling MCP Server (esbuild → pkg EXE)"

    $bundleDir = "$root\MCP-Server\bundle"
    New-Item -ItemType Directory -Force $bundleDir | Out-Null

    # esbuild: ESM → CJS
    & esbuild "$root\MCP-Server\build\index.js" `
        --bundle --platform=node --format=cjs `
        --outfile="$bundleDir\server.cjs"
    if ($LASTEXITCODE -ne 0) { Err "esbuild failed" }
    Ok "esbuild → server.cjs ($([math]::Round((Get-Item "$bundleDir\server.cjs").Length/1KB)) KB)"

    # pkg: CJS → EXE
    $exeOut = "$root\installer\output\RevitMCP-FLOW-server.exe"
    New-Item -ItemType Directory -Force "$root\installer\output" | Out-Null
    & pkg "$bundleDir\server.cjs" --target node22-win-x64 --output $exeOut
    if ($LASTEXITCODE -ne 0) { Err "pkg failed" }
    Ok "pkg → RevitMCP-FLOW-server.exe ($([math]::Round((Get-Item $exeOut).Length/1MB,1)) MB)"
}

# ── Step 3: Copy DLLs to dist ─────────────────────────────────────────
Step "Updating dist/ DLLs"
$map = @{ "2022"="R22"; "2023"="R23"; "2024"="R24"; "2025"="R25"; "2026"="R26" }
foreach ($ver in $map.Keys) {
    $src = "$root\MCP\bin\Release.$($map[$ver])\RevitMCP.dll"
    $dst = "$root\dist\RevitMCP-FLOW\addins\$ver\RevitMCP\RevitMCP.dll"
    Copy-Item $src $dst -Force
    Ok "Revit $ver DLL updated"
}

# ── Step 4: Compile Inno Setup ────────────────────────────────────────
if (-not $SkipInno) {
    Step "Compiling Inno Setup installer"
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $iscc)) { $iscc = "C:\Program Files\Inno Setup 6\ISCC.exe" }
    if (-not (Test-Path $iscc)) { Err "Inno Setup not found" }

    & $iscc "$root\installer\RevitMCP-FLOW.iss" 2>&1 | Select-String "Successful|error" | Write-Host
    if ($LASTEXITCODE -ne 0) { Err "Inno Setup compile failed" }

    $setup = "$root\installer\output\RevitMCP-FLOW-Setup.exe"
    Ok "RevitMCP-FLOW-Setup.exe ($([math]::Round((Get-Item $setup).Length/1MB,1)) MB)"
}

Write-Host "`n🎉 Release build complete!" -ForegroundColor Green
Write-Host "   Installer: $root\installer\output\RevitMCP-FLOW-Setup.exe" -ForegroundColor Yellow
