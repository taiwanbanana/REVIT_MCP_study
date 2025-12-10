# Revit MCP - AI-Powered Revit Control

<p align="center">
  <img src="https://img.shields.io/badge/Revit-2022-blue" alt="Revit 2022">
  <img src="https://img.shields.io/badge/Node.js-LTS-green" alt="Node.js">
  <img src="https://img.shields.io/badge/.NET-4.8-purple" alt=".NET 4.8">
  <img src="https://img.shields.io/badge/MCP-1.0-orange" alt="MCP Protocol">
</p>

透過 Model Context Protocol (MCP) 讓 AI 語言模型直接控制 Autodesk Revit，實現 AI 驅動的 BIM 工作流程。

## 🎯 功能特色

- **AI 直接控制 Revit** - 透過自然語言指令操作 Revit
- **支援多種 AI 平台** - Claude Desktop、Gemini CLI、VS Code Copilot、Google Antigravity
- **豐富的 Revit 工具** - 建立牆、樓板、門窗、查詢元素等
- **即時雙向通訊** - WebSocket 即時連線

## 📁 專案結構

```
REVIT-MCP/
├── MCP/                    # Revit Add-in (C#)
│   └── MCP/
│       ├── Application.cs           # 主程式進入點
│       ├── ConnectCommand.cs        # 連線命令
│       ├── RevitMCP.addin           # Add-in 配置
│       ├── Core/                    # 核心功能
│       │   ├── SocketService.cs     # WebSocket 服務
│       │   ├── CommandExecutor.cs   # 命令執行器
│       │   └── ExternalEventManager.cs
│       ├── Models/                  # 資料模型
│       └── Configuration/           # 設定管理
├── MCP-Server/             # MCP Server (Node.js/TypeScript)
│   ├── src/
│   │   ├── index.ts                 # MCP Server 主程式
│   │   ├── socket.ts                # Socket 客戶端
│   │   └── tools/
│   │       └── revit-tools.ts       # Revit 工具定義
│   ├── build/                       # 編譯輸出
│   ├── package.json
│   └── tsconfig.json
└── README.md
```

## 🔧 系統需求

| 項目 | 需求 |
|------|------|
| **作業系統** | Windows 10 或更新版本 |
| **Revit** | Autodesk Revit 2022 |
| **.NET** | .NET Framework 4.8 |
| **Node.js** | LTS 版本 (20.x 或更新) |

## 📦 安裝步驟

### 步驟 1：安裝 Revit Add-in

1. 編譯 `MCP/MCP` 專案（或下載預編譯版本）
   ```powershell
   cd MCP/MCP
   dotnet build -c Release
   ```

2. 複製檔案到 Revit Add-in 目錄：
   ```powershell
   # 複製 DLL 和 addin 檔案
   $source = "MCP\MCP\bin\Release"
   $target = "$env:APPDATA\Autodesk\Revit\Addins\2022"
   
   Copy-Item "$source\RevitMCP.dll" $target
   Copy-Item "$source\Newtonsoft.Json.dll" $target
   Copy-Item "MCP\MCP\RevitMCP.addin" $target
   ```

3. 重新啟動 Revit

### 步驟 2：安裝 MCP Server

1. 安裝相依套件
   ```bash
   cd MCP-Server
   npm install
   ```

2. 編譯 TypeScript
   ```bash
   npm run build
   ```

### 步驟 3：設定 AI 平台

請參考下方的 **[多方案 AI Agent 設定](#-多方案-ai-agent-設定)** 章節。

---

## 🚀 啟動方式

### 1️⃣ 啟動 Revit 並開啟 MCP 服務

1. 開啟 Revit 2022
2. 載入或建立專案
3. 在「MCP Tools」面板點擊「**MCP 服務 (開/關)**」按鈕
4. 確認看到「WebSocket 伺服器已啟動，監聽: localhost:8765」

### 2️⃣ 透過 AI 平台連線

依您選擇的 AI 平台，參考下方的設定說明。

---

## 🤖 多方案 AI Agent 設定

### 方案 1：Gemini CLI

Gemini CLI 是 Google 的命令列 AI 工具。

#### 安裝 Gemini CLI

```bash
npm install -g @anthropic-ai/gemini-cli
# 或
pip install gemini-cli
```

#### 設定 MCP

1. 建立設定檔 `~/.gemini/settings.json`：
   ```json
   {
     "mcpServers": {
       "revit-mcp": {
         "command": "node",
         "args": ["C:\\path\\to\\MCP-Server\\build\\index.js"],
         "env": {
           "REVIT_VERSION": "2022"
         }
       }
     }
   }
   ```

2. 或直接使用本專案提供的範本：
   ```powershell
   # 複製並修改路徑
   Copy-Item "MCP-Server\gemini_mcp_config.json" "$env:USERPROFILE\.gemini\settings.json"
   ```

3. 編輯檔案，將路徑改為您的實際路徑

#### 啟動步驟

```bash
# 1. 確認 Revit MCP 服務已啟動
# 2. 啟動 Gemini CLI
gemini

# 3. 開始對話，例如：
> 請幫我在 Revit 中建立一面 5 米長的牆
```

---

### 方案 2：VS Code (GitHub Copilot)

在 VS Code 中使用 GitHub Copilot Chat 搭配 MCP。

#### 設定步驟

1. 在專案根目錄建立 `.vscode/mcp.json`：
   ```json
   {
     "servers": {
       "revit-mcp": {
         "command": "node",
         "args": ["${workspaceFolder}/MCP-Server/build/index.js"],
         "env": {
           "REVIT_VERSION": "2022"
         }
       }
     }
   }
   ```

2. 或使用全域設定 `%APPDATA%\Code\User\settings.json`：
   ```json
   {
     "mcp.servers": {
       "revit-mcp": {
         "command": "node",
         "args": ["C:\\path\\to\\MCP-Server\\build\\index.js"],
         "env": {
           "REVIT_VERSION": "2022"
         }
       }
     }
   }
   ```

#### 啟動步驟

1. 確認 Revit MCP 服務已啟動
2. 開啟 VS Code
3. 開啟 Copilot Chat (Ctrl+Shift+I)
4. 使用 `@mcp` 或直接詢問 Revit 相關問題

---

### 方案 3：Claude Desktop

Anthropic 官方桌面應用程式。

#### 設定步驟

1. 找到 Claude Desktop 設定檔位置：
   ```
   Windows: %APPDATA%\Claude\claude_desktop_config.json
   macOS: ~/Library/Application Support/Claude/claude_desktop_config.json
   ```

2. 加入 MCP Server 設定：
   ```json
   {
     "mcpServers": {
       "revit-mcp": {
         "command": "node",
         "args": ["C:\\path\\to\\MCP-Server\\build\\index.js"],
         "env": {
           "REVIT_VERSION": "2022"
         }
       }
     }
   }
   ```

3. 您也可以複製本專案的範本：
   ```powershell
   Copy-Item "MCP-Server\claude_desktop_config.json" "$env:APPDATA\Claude\claude_desktop_config.json"
   ```

#### 啟動步驟

1. 確認 Revit MCP 服務已啟動
2. 啟動 Claude Desktop
3. 在對話中使用 Revit 工具

---

### 方案 4：Google Antigravity (Project IDX)

Google 的雲端 AI 開發環境。

#### 設定步驟

1. 在 Project IDX 專案中建立 `.idx/mcp.json`：
   ```json
   {
     "mcpServers": {
       "revit-mcp": {
         "command": "node",
         "args": ["/path/to/MCP-Server/build/index.js"],
         "env": {
           "REVIT_VERSION": "2022"
         }
       }
     }
   }
   ```

2. 或使用 Antigravity 的 MCP 設定介面：
   - 開啟 Settings → MCP Servers
   - 新增伺服器，填入名稱 `revit-mcp`
   - Command: `node`
   - Args: MCP Server 的完整路徑

#### 注意事項

- Antigravity 運行在雲端，需要確保 MCP Server 可透過網路存取
- 建議在本地網路環境使用，或透過安全通道連線

---

## 🛠️ 可用的 MCP 工具

| 工具名稱 | 說明 |
|---------|------|
| `create_wall` | 建立牆 |
| `create_floor` | 建立樓板 |
| `create_door` | 建立門 |
| `create_window` | 建立窗 |
| `get_project_info` | 取得專案資訊 |
| `query_elements` | 查詢元素 |
| `get_element_info` | 取得元素詳細資訊 |
| `modify_element_parameter` | 修改元素參數 |
| `delete_element` | 刪除元素 |
| `get_all_levels` | 取得所有樓層 |

## 🔒 安全注意事項

⚠️ **重要安全提醒**：

1. **Port 管理** - MCP Server 預設監聽 `localhost:8765`，僅限本機存取
2. **防火牆** - 不建議對外開放連接埠
3. **程式碼審查** - 執行前請確認程式碼來源可信
4. **備份** - 操作前請備份 Revit 專案

## 📝 常見問題

### Q: Revit 沒有顯示 MCP Tools 面板？
A: 確認 `RevitMCP.addin` 已正確放置在 Add-in 目錄，並重新啟動 Revit。

### Q: MCP Server 無法連線到 Revit？
A: 
1. 確認 Revit 中已點擊「MCP 服務 (開/關)」啟動服務
2. 確認 Port 8765 未被其他程式佔用
3. 檢查防火牆設定

### Q: AI 說找不到 Revit 工具？
A: 確認 MCP Server 設定檔路徑正確，並重新啟動 AI 應用程式。

## 📄 授權

MIT License

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request！

---

**Enjoy your AI-powered Revit development! 🚀**
