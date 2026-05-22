# Packaging RevitMCP

Use `scripts/package-release.ps1` to build a folder-based release under `dist/RevitMCP`.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-release.ps1 -RevitVersions 2024 -Clean
```

The generated package includes:

- Revit add-in files under `addins/<version>/`
- MCP Server runtime files under `MCP-Server/`
- `install.ps1`
- `uninstall.ps1`
- `start-mcp-server.bat`
- `README.md`

Recommended Git flow:

```powershell
git switch -c package-installer
```

Keep generated `dist/` output out of Git. Commit only packaging scripts and documentation.

After the folder package is stable, the next step is wrapping `dist/RevitMCP` with an installer tool such as Inno Setup or NSIS.
