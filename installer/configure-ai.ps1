#Requires -Version 5.1
param(
    [string]$InstallPath,
    [ValidateSet("Claude", "Gemini")]
    [string]$Target
)

$ErrorActionPreference = "Stop"
$indexJs = Join-Path $InstallPath "MCP-Server\build\index.js"
$entry = [PSCustomObject]@{ command = "node"; args = @($indexJs) }

$configPath = switch ($Target) {
    "Claude" { Join-Path $env:APPDATA "Claude\claude_desktop_config.json" }
    "Gemini" { Join-Path $env:USERPROFILE ".gemini\settings.json" }
}

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
