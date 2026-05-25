#Requires -Version 5.1
param(
    [string]$InstallPath,
    [ValidateSet("Claude", "Gemini")]
    [string]$Target
)

$ErrorActionPreference = "Stop"
$exePath = Join-Path $InstallPath "RevitMCP-FLOW-server.exe"

# Claude Code 用 ~/.claude.json；Gemini CLI 用 ~/.gemini/settings.json
switch ($Target) {
    "Claude" {
        $configPath = Join-Path $env:USERPROFILE ".claude.json"

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

        $entry = [PSCustomObject]@{
            type    = "stdio"
            command = $exePath
            args    = @()
            timeout = 15000
        }

        if ($json.mcpServers.PSObject.Properties.Match("RevitMCP-FLOW").Count -gt 0) {
            $json.mcpServers."RevitMCP-FLOW" = $entry
        } else {
            $json.mcpServers | Add-Member -MemberType NoteProperty -Name "RevitMCP-FLOW" -Value $entry
        }

        $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configPath -Encoding UTF8
        Write-Host "Configured Claude Code: $configPath"
    }

    "Gemini" {
        $configPath = Join-Path $env:USERPROFILE ".gemini\settings.json"
        $dir = Split-Path -Parent $configPath
        New-Item -ItemType Directory -Path $dir -Force | Out-Null

        $entry = [PSCustomObject]@{ command = $exePath; args = @() }

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
        Write-Host "Configured Gemini CLI: $configPath"
    }
}
