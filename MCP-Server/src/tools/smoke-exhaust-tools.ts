/**
 * 排煙窗法規檢討工具 — mep Profile
 * 來源：PR#12 (@7alexhuang-ux)，經跨版本修正後整合
 * 法規：建技規§101① + 消防§188
 * 通用標註工具（create_section_view 等）已移至 annotation-tools.ts
 */

import { Tool } from "@modelcontextprotocol/sdk/types.js";

export const smokeExhaustTools: Tool[] = [
    {
        name: "check_smoke_exhaust_windows",
        description: "排煙窗檢討：檢查天花板下 80cm 內可開啟窗面積是否 ≥ 區劃面積 2%。同時判定無窗居室。法源：建技規§101① + 消防§188。自動上色：綠=全開、黃=折減、紅=固定。",
        inputSchema: {
            type: "object",
            properties: {
                levelName: { type: "string", description: "樓層名稱" },
                ceilingHeightSource: { type: "string", enum: ["room_parameter", "ceiling_element"], description: "天花板高度來源", default: "room_parameter" },
                colorize: { type: "boolean", description: "是否自動上色窗戶", default: true },
                smokeZoneHeight: { type: "number", description: "有效帶高度（mm），預設 800", default: 800 },
                excludeKeywords: { type: "array", items: { type: "string" }, description: "非居室排除關鍵字" },
            },
            required: ["levelName"],
        },
    },
    {
        name: "check_floor_effective_openings",
        description: "無開口樓層判定：檢查樓層外牆有效開口面積是否 ≥ 樓地板面積 1/30。法源：消防§4 + §28③。",
        inputSchema: {
            type: "object",
            properties: {
                levelName: { type: "string", description: "樓層名稱" },
                colorize: { type: "boolean", description: "是否自動上色開口", default: true },
            },
            required: ["levelName"],
        },
    },
    {
        name: "check_exterior_wall_openings",
        description: "依據台灣建築技術規則第45條及第110條檢討外牆開口。自動讀取地界線計算距離，以顏色標示違規。",
        inputSchema: {
            type: "object",
            properties: {
                checkArticle45: { type: "boolean", description: "檢查第45條", default: true },
                checkArticle110: { type: "boolean", description: "檢查第110條", default: true },
                colorizeViolations: { type: "boolean", description: "以顏色標示", default: true },
                exportReport: { type: "boolean", description: "匯出 JSON 報表", default: false },
                reportPath: { type: "string", description: "報表輸出路徑" },
            },
        },
    },
    {
        name: "export_smoke_review_excel",
        description: "匯出排煙窗檢討 Excel 報告（.xlsx），含樓層總覽、房間明細、窗戶明細、改善建議、§101補充檢討（排風量提醒+中央管理室偵測）五個工作表。",
        inputSchema: {
            type: "object",
            properties: {
                levelName: { type: "string", description: "樓層名稱" },
                ceilingHeightSource: { type: "string", enum: ["room_parameter", "ceiling_element"], default: "room_parameter" },
                outputPath: { type: "string", description: "輸出路徑（選填）" },
            },
            required: ["levelName"],
        },
    },
];
