/**
 * 族群工具 — RFA 編輯 + 族群管理（查詢、批次操作）
 * 適用 rfa、architect、full Profile
 */

import { Tool } from "@modelcontextprotocol/sdk/types.js";

export const familyTools: Tool[] = [
    {
        name: "get_all_used_families_in_model",
        description: "取得模型中所有已載入的族群名稱與 ID（不含系統族群）。",
        inputSchema: { type: "object", properties: {} },
    },
    {
        name: "get_all_used_types_of_families",
        description: "取得指定族群清單中的所有類型 ID 與名稱。",
        inputSchema: {
            type: "object",
            properties: {
                familyNames: {
                    type: "array",
                    items: { type: "string" },
                    description: "族群名稱清單",
                },
            },
            required: ["familyNames"],
        },
    },
    {
        name: "batch_modify_family_parameters",
        description: "批次修改所有開啟族群的參數值。",
        inputSchema: {
            type: "object",
            properties: {
                parameters: {
                    type: "array",
                    items: {
                        type: "object",
                        properties: {
                            name: { type: "string" },
                            value: { type: "string" },
                        },
                        required: ["name", "value"],
                    },
                },
                allDocuments: { type: "boolean", description: "是否對所有開啟的文件執行（預設 true）" },
            },
            required: ["parameters"],
        },
    },
    {
        name: "batch_rename_family_types",
        description: "批次修改所有開啟族群的類型名稱。",
        inputSchema: {
            type: "object",
            properties: {
                newName: { type: "string", description: "新的類型名稱" },
                allDocuments: { type: "boolean", description: "是否對所有開啟的文件執行（預設 true）" },
            },
            required: ["newName"],
        },
    },
    {
        name: "batch_switch_to_3d_view",
        description: "批次將所有開啟的文件切換至其 3D 視圖。",
        inputSchema: {
            type: "object",
            properties: {
                allDocuments: { type: "boolean", description: "是否對所有開啟的文件執行（預設 true）" },
            },
        },
    },
    {
        name: "batch_save_and_close",
        description: "批次儲存並關閉檔案。自動關閉非 3D 視圖，最後關閉檔案。",
        inputSchema: {
            type: "object",
            properties: {
                allDocuments: { type: "boolean", description: "是否對所有開啟的文件執行（預設 true）" },
            },
        },
    },
    {
        name: "rfa_create_extrusion",
        description: "在族群編輯器 (.rfa) 中建立擠出實體 (Extrusion)。需要提供閉合點位座標。",
        inputSchema: {
            type: "object",
            properties: {
                points: {
                    type: "array",
                    description: "閉合路徑點位陣列，例如 [{x:0, y:0}, {x:100, y:0}, {x:100, y:100}, {x:0, y:100}]",
                    items: {
                        type: "object",
                        properties: {
                            x: { type: "number", description: "X 座標 (mm)" },
                            y: { type: "number", description: "Y 座標 (mm)" }
                        },
                        required: ["x", "y"]
                    }
                },
                height: { type: "number", description: "擠出高度 (mm)，預設 1000", default: 1000 }
            },
            required: ["points"]
        }
    },
    {
        name: "rfa_set_category",
        description: "修改族群編輯器 (.rfa) 中族群的品類 (Category)。",
        inputSchema: {
            type: "object",
            properties: {
                category: { type: "string", description: "目標品類名稱 (例如 'Windows', 'Doors', 'Furniture')" }
            },
            required: ["category"]
        }
    },
    {
        name: "rfa_modify_parameter",
        description: "在族群編輯器 (.rfa) 中修改族群參數 (Family Parameter) 的值。這會影響驅動幾何的參數。",
        inputSchema: {
            type: "object",
            properties: {
                name: { type: "string", description: "族群參數名稱" },
                value: { type: "string", description: "新的參數值 (如果是長度則以 mm 為單位)" }
            },
            required: ["name", "value"]
        }
    }
];
