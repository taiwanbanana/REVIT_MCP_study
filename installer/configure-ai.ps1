#Requires -Version 5.1
<#
.SYNOPSIS
    Configure MCP client to use RevitMCP-FLOW server.
.DESCRIPTION
    Writes RevitMCP-FLOW server entry to all detected AI client configs:
    - Claude Desktop  : %APPDATA%\Claude\claude_desktop_config.json
    - Claude Code CLI : %USERPROFILE%\.claude.json
    - Gemini CLI      : %USERPROFILE%\.gemini\settings.json
#>
param(
    [string]$InstallPath,
    [ValidateSet("Claude", "Gemini")]
    [string]$Target
)

$ErrorActionPreference = "Stop"
$exePath = Join-Path $InstallPath "RevitMCP-FLOW-server.exe"

function Write-ClaudeDesktopConfig($configPath, $entry) {
    $dir = Split-Path -Parent $configPath
    New-Item -ItemType Directory -Path $dir -Force | Out-Null

    $json = $null
    if (Test-Path $configPath) {
        try { $json = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json }
        catch { Copy-Item -LiteralPath $configPath -Destination "$configPath.bak" -Force }
    }
    if ($null -eq $json) {
        $json = [PSCustomObject]@{ mcpServers = [PSCustomObject]@{} }
    }
    if (-not ($json.PSObject.Properties.Match("mcpServers").Count -gt 0)) {
        $json | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{})
    }
    if ($json.mcpServers.PSObject.Properties.Match("RevitMCP-FLOW").Count -gt 0) {
        $json.mcpServers."RevitMCP-FLOW" = $entry
    } else {
        $json.mcpServers | Add-Member -MemberType NoteProperty -Name "RevitMCP-FLOW" -Value $entry
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configPath -Encoding UTF8
}

switch ($Target) {
    "Claude" {
        # Entry format for Claude Desktop (chat)
        $desktopEntry = [PSCustomObject]@{
            command = $exePath
            args    = @()
        }
        # Entry format for Claude Code CLI (~/.claude.json)
        $codeEntry = [PSCustomObject]@{
            type    = "stdio"
            command = $exePath
            args    = @()
            timeout = 15000
        }

        # 1. Claude Desktop (chat) — standard install location
        $desktopConfig = Join-Path $env:APPDATA "Claude\claude_desktop_config.json"
        Write-ClaudeDesktopConfig $desktopConfig $desktopEntry
        Write-Host "Configured Claude Desktop: $desktopConfig"

        # 2. Claude Code CLI — for developers / power users
        $codeConfig = Join-Path $env:USERPROFILE ".claude.json"
        if (Test-Path $codeConfig) {
            Write-ClaudeDesktopConfig $codeConfig $codeEntry
            Write-Host "Configured Claude Code CLI: $codeConfig"
        }
    }

    "Gemini" {
        $geminiEntry = [PSCustomObject]@{ command = $exePath; args = @() }
        $geminiConfig = Join-Path $env:USERPROFILE ".gemini\settings.json"
        Write-ClaudeDesktopConfig $geminiConfig $geminiEntry
        Write-Host "Configured Gemini CLI: $geminiConfig"
    }
}
