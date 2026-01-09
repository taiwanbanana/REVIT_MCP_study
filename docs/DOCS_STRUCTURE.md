# 文檔目錄結構說明

## 目錄職責

| 目錄 | 用途 | 讀者 |
|------|------|------|
| **`docs/tools/`** | 工具 API 技術文檔 | 開發者 |
| **`domain/`** | 領域知識與工作流程 | AI Agent |

---

## docs/tools/ - 技術文檔

**目的：** 記錄 MCP 工具的技術設計和 API 使用方式

**內容類型：**
- 工具設計規格
- API 參數說明
- 使用範例代碼

**目前檔案：**
- `override_element_color_design.md` - 元素圖形覆寫工具設計
- `override_graphics_examples.md` - 圖形覆寫 API 範例

---

## domain/ - 領域知識

**目的：** 給 AI 讀取的工作流程和業務知識

**內容類型：**
- 操作工作流程
- 業務規則
- 確認事項清單

**目前檔案：**
- `element-coloring-workflow.md` - 元素上色工作流程
- `room-boundary.md` - 房間邊界處理
- `wall-check.md` - 牆體檢查

---

## 新增文檔時的選擇

| 如果要記錄... | 放在... |
|--------------|--------|
| 工具的 API 設計和參數 | `docs/tools/` |
| 如何一步步執行某任務 | `domain/` |
| 業務規則和注意事項 | `domain/` |
| 代碼範例和技術細節 | `docs/tools/` |
