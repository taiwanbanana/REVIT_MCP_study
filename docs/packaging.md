# Packaging RevitMCP

Use `scripts/package-release.ps1` to build a folder-based release under `dist/RevitMCP`.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-release.ps1 -RevitVersions 2024 -Clean
```

The generated package includes:

- Revit add-in files under `addins/<version>/`
- MCP Server runtime JavaScript files under `MCP-Server/build/`
- `install.ps1`
- `uninstall.ps1`
- `start-mcp-server.bat`
- `README.md`

## Runtime Classification

Include these in the distributable package:

- `MCP/bin/Release.R*/RevitMCP.dll`
- `MCP/bin/Release.R*/Newtonsoft.Json.dll`
- `MCP/RevitMCP.addin`
- `MCP-Server/build/**/*.js`
- `MCP-Server/package.json`
- `MCP-Server/package-lock.json`
- Generated `install.ps1`, `uninstall.ps1`, and `start-mcp-server.bat`

Exclude these from the distributable package:

- AI agent instructions: `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.agents/`, `.codex/`
- Knowledge and review content: `domain/`, `knowledge/`, `log/`
- Project docs and teaching material: `docs/`, `slides/`, root-level guide markdown
- Development scratch/output: `scratch/`, `output/`, root `*.rfa` test files
- Build internals: `MCP/obj/`, `MCP/bin/`, `MCP-Server/build/**/*.map`, `MCP-Server/build/**/*.d.ts`
- Dependency folders: `node_modules/`, `MCP-Server/node_modules/`

Recommended Git flow:

```powershell
git switch -c package-installer
```

Keep generated `dist/` output out of Git. Commit only packaging scripts and documentation.

After the folder package is stable, the next step is wrapping `dist/RevitMCP` with an installer tool such as Inno Setup or NSIS.
