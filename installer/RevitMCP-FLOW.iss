#define AppName "RevitMCP-FLOW"
#define AppVersion "1.0.0"
#define AppPublisher "taiwanbanana"
#define DistDir "..\dist\RevitMCP-FLOW"

[Setup]
AppId={{090A4C8C-61DC-426D-87DF-E4BAE0F80EC1}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppSupportURL=https://github.com/taiwanbanana/REVIT_MCP_study_private
DefaultDirName={localappdata}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=output
OutputBaseFilename={#AppName}-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "revit2022"; Description: "Revit 2022"; GroupDescription: "Install add-in for:"; Flags: unchecked
Name: "revit2023"; Description: "Revit 2023"; GroupDescription: "Install add-in for:"; Flags: unchecked
Name: "revit2024"; Description: "Revit 2024"; GroupDescription: "Install add-in for:"
Name: "revit2025"; Description: "Revit 2025"; GroupDescription: "Install add-in for:"; Flags: unchecked
Name: "revit2026"; Description: "Revit 2026"; GroupDescription: "Install add-in for:"; Flags: unchecked
Name: "claudeconfig"; Description: "Claude Desktop"; GroupDescription: "Configure MCP client:"
Name: "geminiconfig"; Description: "Gemini CLI"; GroupDescription: "Configure MCP client:"; Flags: unchecked

[Files]
; MCP Server runtime
Source: "{#DistDir}\MCP-Server\*"; DestDir: "{app}\MCP-Server"; Flags: recursesubdirs createallsubdirs
Source: "{#DistDir}\start-mcp-server.bat"; DestDir: "{app}"

; AI config helper (runs post-install)
Source: "configure-ai.ps1"; DestDir: "{app}"

; Revit add-ins per version
Source: "{#DistDir}\addins\2022\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2022"; Flags: recursesubdirs createallsubdirs; Tasks: revit2022
Source: "{#DistDir}\addins\2023\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: recursesubdirs createallsubdirs; Tasks: revit2023
Source: "{#DistDir}\addins\2024\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: recursesubdirs createallsubdirs; Tasks: revit2024
Source: "{#DistDir}\addins\2025\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; Flags: recursesubdirs createallsubdirs; Tasks: revit2025
Source: "{#DistDir}\addins\2026\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2026"; Flags: recursesubdirs createallsubdirs; Tasks: revit2026

[Run]
; Install npm dependencies
Filename: "cmd.exe"; Parameters: "/c npm install --omit=dev"; WorkingDir: "{app}\MCP-Server"; \
  StatusMsg: "Installing MCP Server dependencies..."; Flags: runhidden waituntilterminated

; Configure Claude Desktop
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -File ""{app}\configure-ai.ps1"" -InstallPath ""{app}"" -Target Claude"; \
  StatusMsg: "Configuring Claude Desktop..."; Flags: runhidden waituntilterminated; Tasks: claudeconfig

; Configure Gemini CLI
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -File ""{app}\configure-ai.ps1"" -InstallPath ""{app}"" -Target Gemini"; \
  StatusMsg: "Configuring Gemini CLI..."; Flags: runhidden waituntilterminated; Tasks: geminiconfig

[UninstallDelete]
Type: filesandordirs; Name: "{app}\MCP-Server\node_modules"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if not Exec('cmd.exe', '/c node --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Node.js is not installed or not in PATH.' + #13#10 +
           'Please install Node.js from https://nodejs.org before continuing.',
           mbError, MB_OK);
    Result := False;
  end;
end;
