#Requires -Version 5.1
<#
Builds a distributable RevitMCP folder under dist/RevitMCP.

The package is intentionally a folder-based installer first:
- Revit add-in files are built per Revit version.
- MCP Server build output and package metadata are included.
- install.ps1 / uninstall.ps1 / start-mcp-server.bat are generated into the package.

Example:
  powershell -ExecutionPolicy Bypass -File scripts/package-release.ps1 -RevitVersions 2024 -Clean
#>

param(
    [string[]]$RevitVersions = @("2024"),
    [string]$OutputDir = "",
    [switch]$Clean,
    [switch]$SkipBuild,
    [switch]$SkipServerBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    Write-Host "    $FilePath $($Arguments -join ' ')" -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Copy-RequiredFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path $Source)) {
        throw "Required file not found: $Source"
    }

    $destinationDir = Split-Path -Parent $Destination
    if (-not (Test-Path $destinationDir)) {
        New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDir)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
}

$projectRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
$mcpDir = Join-Path $projectRoot "MCP"
$serverDir = Join-Path $projectRoot "MCP-Server"

if (-not (Test-Path (Join-Path $mcpDir "RevitMCP.csproj"))) {
    throw "Cannot find MCP/RevitMCP.csproj. Run this script from the project checkout."
}
if (-not (Test-Path (Join-Path $serverDir "package.json"))) {
    throw "Cannot find MCP-Server/package.json. Run this script from the project checkout."
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $projectRoot "dist\RevitMCP"
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$distRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot "dist"))

if (-not $OutputDir.StartsWith($distRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDir must stay under the project dist folder: $distRoot"
}

$configByVersion = @{
    "2022" = "Release.R22"
    "2023" = "Release.R23"
    "2024" = "Release.R24"
    "2025" = "Release.R25"
    "2026" = "Release.R26"
}

foreach ($version in $RevitVersions) {
    if (-not $configByVersion.ContainsKey($version)) {
        throw "Unsupported Revit version '$version'. Use one or more of: $($configByVersion.Keys -join ', ')"
    }
}

Write-Host ""
Write-Host "RevitMCP package build" -ForegroundColor White
Write-Host "Project: $projectRoot"
Write-Host "Output:  $OutputDir"
Write-Host "Versions: $($RevitVersions -join ', ')"

if ($Clean -and (Test-Path $OutputDir)) {
    Write-Step "Cleaning output"
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

if (-not $SkipServerBuild) {
    Write-Step "Building MCP Server"
    Invoke-Checked -FilePath "npm.cmd" -Arguments @("install") -WorkingDirectory $serverDir
    Invoke-Checked -FilePath "npm.cmd" -Arguments @("run", "build") -WorkingDirectory $serverDir
}

Write-Step "Copying MCP Server runtime"
$serverOut = Join-Path $OutputDir "MCP-Server"
New-Item -ItemType Directory -Path $serverOut -Force | Out-Null
Copy-RequiredFile -Source (Join-Path $serverDir "package.json") -Destination (Join-Path $serverOut "package.json")
if (Test-Path (Join-Path $serverDir "package-lock.json")) {
    Copy-RequiredFile -Source (Join-Path $serverDir "package-lock.json") -Destination (Join-Path $serverOut "package-lock.json")
}
if (-not (Test-Path (Join-Path $serverDir "build\index.js"))) {
    throw "MCP Server build output not found. Run without -SkipServerBuild or build MCP-Server first."
}
Copy-Item -LiteralPath (Join-Path $serverDir "build") -Destination $serverOut -Recurse -Force

Write-Step "Building and copying Revit add-ins"
$addinSource = Join-Path $mcpDir "RevitMCP.addin"
foreach ($version in $RevitVersions) {
    $config = $configByVersion[$version]
    if (-not $SkipBuild) {
        Invoke-Checked -FilePath "dotnet" -Arguments @("build", "-c", $config, "RevitMCP.csproj") -WorkingDirectory $mcpDir
    }

    $buildDir = Join-Path $mcpDir "bin\$config"
    $dllSource = Join-Path $buildDir "RevitMCP.dll"
    $jsonSource = Join-Path $buildDir "Newtonsoft.Json.dll"
    $versionOut = Join-Path $OutputDir "addins\$version"
    $dllOutDir = Join-Path $versionOut "RevitMCP"

    Copy-RequiredFile -Source $addinSource -Destination (Join-Path $versionOut "RevitMCP.addin")
    Copy-RequiredFile -Source $dllSource -Destination (Join-Path $dllOutDir "RevitMCP.dll")
    if (Test-Path $jsonSource) {
        Copy-RequiredFile -Source $jsonSource -Destination (Join-Path $dllOutDir "Newtonsoft.Json.dll")
    }
}

Write-Step "Writing installer files"

$installScript = @'
#Requires -Version 5.1
param(
    [string[]]$RevitVersions = @(),
    [switch]$SkipNpmInstall,
    [switch]$SkipAIConfig
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$packageRoot = $PSScriptRoot
$addinsRoot = Join-Path $packageRoot "addins"
$serverRoot = Join-Path $packageRoot "MCP-Server"

if (-not (Test-Path $addinsRoot)) { throw "Missing addins folder: $addinsRoot" }
if (-not (Test-Path (Join-Path $serverRoot "build\index.js"))) { throw "Missing MCP server build output." }

if ($RevitVersions.Count -eq 0) {
    $RevitVersions = Get-ChildItem -LiteralPath $addinsRoot -Directory | Select-Object -ExpandProperty Name
}

foreach ($version in $RevitVersions) {
    $source = Join-Path $addinsRoot $version
    if (-not (Test-Path $source)) {
        Write-Warning "Skipping Revit $version; package does not include add-in files for that version."
        continue
    }

    $target = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$version"
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source "RevitMCP.addin") -Destination $target -Force
    Copy-Item -LiteralPath (Join-Path $source "RevitMCP") -Destination $target -Recurse -Force
    Write-Host "Installed RevitMCP add-in for Revit $version -> $target" -ForegroundColor Green
}

if (-not $SkipNpmInstall) {
    Write-Host "Installing MCP Server runtime dependencies..." -ForegroundColor Cyan
    Push-Location $serverRoot
    try {
        & npm.cmd install --omit=dev
        if ($LASTEXITCODE -ne 0) { throw "npm install failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipAIConfig) {
    $indexJs = Join-Path $serverRoot "build\index.js"
    $entry = [PSCustomObject]@{
        command = "node"
        args = @($indexJs)
    }

    $configs = @(
        @{ Name = "Claude Desktop"; Path = Join-Path $env:APPDATA "Claude\claude_desktop_config.json" },
        @{ Name = "Gemini CLI"; Path = Join-Path $env:USERPROFILE ".gemini\settings.json" }
    )

    foreach ($config in $configs) {
        $path = $config.Path
        $dir = Split-Path -Parent $path
        New-Item -ItemType Directory -Path $dir -Force | Out-Null

        $json = $null
        if (Test-Path $path) {
            try {
                $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
            }
            catch {
                Copy-Item -LiteralPath $path -Destination "$path.bak" -Force
            }
        }
        if ($null -eq $json) {
            $json = [PSCustomObject]@{ mcpServers = [PSCustomObject]@{} }
        }
        if (-not ($json.PSObject.Properties.Match("mcpServers").Count -gt 0)) {
            $json | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{})
        }
        if ($json.mcpServers.PSObject.Properties.Match("revit-mcp").Count -gt 0) {
            $json.mcpServers."revit-mcp" = $entry
        }
        else {
            $json.mcpServers | Add-Member -MemberType NoteProperty -Name "revit-mcp" -Value $entry
        }
        $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $path -Encoding UTF8
        Write-Host "Configured $($config.Name): $path" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Install complete. Restart Revit, then use start-mcp-server.bat or your MCP client config." -ForegroundColor Green
'@

$uninstallScript = @'
#Requires -Version 5.1
param(
    [string[]]$RevitVersions = @("2022", "2023", "2024", "2025", "2026")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

foreach ($version in $RevitVersions) {
    $target = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$version"
    $addin = Join-Path $target "RevitMCP.addin"
    $folder = Join-Path $target "RevitMCP"

    if (Test-Path $addin) {
        Remove-Item -LiteralPath $addin -Force
        Write-Host "Removed $addin"
    }
    if (Test-Path $folder) {
        Remove-Item -LiteralPath $folder -Recurse -Force
        Write-Host "Removed $folder"
    }
}

Write-Host "Uninstall complete. Restart Revit if it is currently open."
'@

$startBat = @'
@echo off
setlocal
cd /d "%~dp0MCP-Server"
node build\index.js
'@

$readme = @'
# RevitMCP Distribution

This folder contains a packaged RevitMCP release.

## Install

Run PowerShell from this folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

To install only one Revit version:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -RevitVersions 2024
```

## Start MCP Server

Use:

```cmd
start-mcp-server.bat
```

The installer can also update Claude Desktop and Gemini CLI MCP config entries to point at this package.

## Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## Layout

- `addins/<version>/RevitMCP.addin`
- `addins/<version>/RevitMCP/RevitMCP.dll`
- `MCP-Server/build/index.js`
- `install.ps1`
- `uninstall.ps1`
- `start-mcp-server.bat`
'@

Set-Content -LiteralPath (Join-Path $OutputDir "install.ps1") -Value $installScript -Encoding UTF8
Set-Content -LiteralPath (Join-Path $OutputDir "uninstall.ps1") -Value $uninstallScript -Encoding UTF8
Set-Content -LiteralPath (Join-Path $OutputDir "start-mcp-server.bat") -Value $startBat -Encoding ASCII
Set-Content -LiteralPath (Join-Path $OutputDir "README.md") -Value $readme -Encoding UTF8

Write-Step "Package complete"
Get-ChildItem -LiteralPath $OutputDir | Select-Object Mode, Length, LastWriteTime, Name | Format-Table -AutoSize
Write-Host ""
Write-Host "Output folder: $OutputDir" -ForegroundColor Green
