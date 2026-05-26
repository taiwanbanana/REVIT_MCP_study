# 門窗表建立 RFA (Door/Window Family Creation Skill)

本指南規範自動化建立門窗 RFA (Revit Family) 檔案的核心技能、流程與防呆機制，確保模型生成的穩定性與資料追溯性。

## 1. 圖說解析與尺寸判定 (Drawing Parsing)
- **開口大小 (Opening Size)**：解析 DXF 或外部圖說時，必須精確捕捉門窗的整體外框開口寬度與高度。
- **豎框尺寸 (Mullion Dimensions)**：需正確辨識立面分割，提取窗框、豎框、橫梃與門扇的具體寬度、深度尺寸，作為幾何生成的基礎參數。

## 2. 幾何生成與子品類指派 (Geometry & Subcategory Assignment)
- **致命錯誤防範 (Category NULL Crash)**：
  當為擠出物件 (Extrusion) 等幾何元素指派「子品類」(Subcategory) 參數時，**絕對禁止寫入 `GraphicsStyle.Id`**。寫入錯誤的 ID 類型雖不會在當下報錯，但會導致幾何物件內部狀態損毀，進而引發 `Category is unexpectedly NULL` 的嚴重系統崩潰。
- **正確指派方式**：
  必須從 `Document.Settings.Categories` 中迭代尋找對應主品類下的子 `Category`，並嚴格寫入該 `Category.Id` (例如「構架/豎框」的正統 ID 為 `-2000029`)。

## 3. 模板基準與核心骨架的動態抓取 (Dynamic Skeleton Retrieval)
在建立或修改族群前，嚴禁直接寫死 Element ID。必須透過品類與名稱動態查詢：
- **基本牆 (Host Wall)**：透過查詢主品類為 `牆` (Walls) 的物件，作為實體宿主基準。
- **開口切割 (Opening Cut)**：透過查詢主品類為 `開口切割` (Opening) 的物件。此為挖空牆壁的關鍵。
- **參考平面與基準 (Reference Planes & Levels)**：
  - 高度基準：查詢名稱包含 `Ref. Level`、`Sill`、`Head` 等參考平面或樓層。
  - 寬度與中心基準：查詢名稱包含 `Left`、`Right`、`Center` 的參考平面。
  - 內外基準：查詢名稱包含 `Exterior`、`Interior` 的參考平面。
  *(註：可使用 MCP 工具 `query_elements` 搭配正則表示式或字串比對達成)*

## 4. 檔案操作流程 (File Operation Workflow)
從模板產出新 RFA 的標準流程：
1. **確認模板為活躍文件**：確保 Revit 當前開啟的是公版模板。
2. **`copy_document`**：複製副本至**與模板相同的資料夾**，以新門窗編號命名（如 `W1_固定窗_120x150.rfa`）。
3. **`open_document`**：立即開啟該副本，切換為活躍文件以進行編輯。

**路徑規則**：
- 輸出路徑必須與模板所在資料夾一致（透過 `doc.PathName` 動態取得目錄）。
- 嚴禁將 RFA 輸出到無關的暫存資料夾。

## 5. 建立 AI 判讀用日誌 (Creation Logs)
- **目的**：為了讓 AI 在多階段任務中能精確掌控已生成的元件，每次建立或更新 RFA 時，必須輸出結構化的日誌。
- **必備欄位資訊**：
  1. **檔案名稱與 ID (File Name & Document ID)**：明確標示操作的目標族群檔 (如 `MCP-DW4`)。
  2. **所有生成的 Element ID 清單 (All Element IDs)**：詳列框架、玻璃、五金等個別幾何元件的 ID。
  3. **關鍵參數變更結果**：記錄指派的材質與子品類結果。
- **輸出規範**：日誌需遵循統一格式寫入 `output/mod_history.log` 或專用的 `output/reports/`，確保後續查閱或自動診斷的穩定性。

## 6. 檔案管理與命名規範 (Strict Document Management)

為了維護專案檔案的完整性與命名規則的嚴謹性，Agent 執行時必須遵守以下強制規則：

1. **禁止未經授權的另存新檔**：
   - 嚴禁在未取得使用者明確指令的情況下，擅自對族群檔案執行 `SaveAs`。
   - 嚴禁自行生成帶有測試後綴（如 `_Final`、`_Test-V1`、`_Fix`）的副本。
   - 所有生成的 RFA 檔案名稱必須符合專案指定的命名標準（如 `DW4-225x230.rfa`）。

2. **直接修改授權原則**：
   - 當使用者確認特定 RFA 為正確檔案時，Agent 的幾何位移與參數調整應**直接作用於該原始檔案**。
   - 若操作過程中發生幾何衝突或彈窗，應優先透過 API 的 Failure Handling 機制自動解決，而非透過另存新檔來規避。

3. **操作透明度**：
   - 在執行任何會永久更動檔案內容的操作前，必須明確告知操作對象路徑。
   - 若因技術限制必須生成副本，必須先向使用者說明理由並獲得許可。
