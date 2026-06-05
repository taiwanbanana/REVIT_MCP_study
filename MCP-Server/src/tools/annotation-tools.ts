/**
 * 通用標註工具 — 剖面視圖、詳圖線、填充區域、文字標註
 * 適用所有 Profile（architect、mep、rfa 等）
 */

import { Tool } from "@modelcontextprotocol/sdk/types.js";

export const annotationTools: Tool[] = [
    {
        name: "create_section_view",
        description: "建立剖面視圖，面向指定牆面。可用於檢視窗戶與天花板的高度關係。",
        inputSchema: {
            type: "object",
            properties: {
                wallId: { type: "number", description: "目標牆的 Element ID" },
                viewName: { type: "string", description: "視圖名稱", default: "剖面視圖" },
                offset: { type: "number", description: "剖面距牆偏移（mm），預設 1000", default: 1000 },
                scale: { type: "number", description: "比例尺（如 50 = 1:50），預設 50", default: 50 },
            },
            required: ["wallId"],
        },
    },
    {
        name: "create_detail_lines",
        description: "在視圖上繪製詳圖線，可指定顏色和標籤。",
        inputSchema: {
            type: "object",
            properties: {
                viewId: { type: "number", description: "目標視圖的 Element ID" },
                lines: {
                    type: "array",
                    description: "線段陣列",
                    items: {
                        type: "object",
                        properties: {
                            startX: { type: "number", description: "起點 X（mm）" },
                            startY: { type: "number", description: "起點 Y（mm）" },
                            endX: { type: "number", description: "終點 X（mm）" },
                            endY: { type: "number", description: "終點 Y（mm）" },
                            color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
                            lineStyle: { type: "string", description: "線條樣式（選填）" },
                            label: { type: "string", description: "標籤（選填）" },
                        },
                        required: ["startX", "startY", "endX", "endY"],
                    },
                },
            },
            required: ["viewId", "lines"],
        },
    },
    {
        name: "create_filled_region",
        description: "建立填充區域（如色塊標示範圍），可設定顏色和透明度。",
        inputSchema: {
            type: "object",
            properties: {
                viewId: { type: "number", description: "目標視圖的 Element ID" },
                points: {
                    type: "array",
                    description: "多邊形頂點（至少 3 個點，自動封閉）",
                    items: { type: "object", properties: { x: { type: "number" }, y: { type: "number" } }, required: ["x", "y"] },
                },
                color: { type: "object", properties: { r: { type: "number" }, g: { type: "number" }, b: { type: "number" } } },
                transparency: { type: "number", description: "透明度 0-100，預設 50", default: 50 },
                regionType: { type: "string", description: "填充區域類型名稱（選填）" },
            },
            required: ["viewId", "points"],
        },
    },
    {
        name: "create_text_note",
        description: "在視圖上建立文字標註。",
        inputSchema: {
            type: "object",
            properties: {
                viewId: { type: "number", description: "目標視圖的 Element ID" },
                x: { type: "number", description: "X 座標（mm）" },
                y: { type: "number", description: "Y 座標（mm）" },
                text: { type: "string", description: "文字內容" },
                textSize: { type: "number", description: "文字大小（mm），預設 2.5", default: 2.5 },
            },
            required: ["viewId", "x", "y", "text"],
        },
    },
];
