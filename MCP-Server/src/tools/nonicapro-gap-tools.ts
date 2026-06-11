/**
 * NONICAPRO 功能差距補充工具
 * 這 18 個工具涵蓋 NONICAPRO 有但 RevitMCP 原本缺少的真正新功能
 *
 * 分類：
 *   操作類  (5) — copy_elements, rotate_elements, isolate_elements_in_view,
 *                  set_sheet_revisions, copy_view_filters
 *   視圖建立 (8) — create_floor_plan_view, create_3d_view, create_legend_view,
 *                  create_room_elevation_views, create_tags_on_view,
 *                  place_view_on_sheet, create_grids, create_levels
 *   查詢補強 (5) — get_element_parameters_bulk, get_model_warnings,
 *                  get_workset_info, get_element_graphic_overrides,
 *                  get_type_material_layers
 */

import { Tool } from "@modelcontextprotocol/sdk/types.js";

export const nonicaGapTools: Tool[] = [

    // ══════════════════════════════════════════
    //  操作類
    // ══════════════════════════════════════════

    {
        name: "copy_elements",
        description: "複製一或多個 Revit 元素，可指定 XYZ 偏移量（公釐）。傳回新元素的 ID 清單。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "要複製的元素 ID 清單",
                },
                translationX: { type: "number", description: "X 方向偏移 (mm)，預設 0", default: 0 },
                translationY: { type: "number", description: "Y 方向偏移 (mm)，預設 0", default: 0 },
                translationZ: { type: "number", description: "Z 方向偏移 (mm)，預設 0", default: 0 },
            },
            required: ["elementIds"],
        },
    },

    {
        name: "rotate_elements",
        description: "旋轉一或多個 Revit 元素，繞指定軸心點旋轉指定角度（度）。若不指定軸心點，則使用所有元素的幾何中心。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "要旋轉的元素 ID 清單",
                },
                angleDegrees: {
                    type: "number",
                    description: "旋轉角度（度），正值為逆時針（右手定則，繞 Z 軸）",
                },
                pivotX: { type: "number", description: "旋轉軸心 X 座標 (mm)，省略則自動計算中心" },
                pivotY: { type: "number", description: "旋轉軸心 Y 座標 (mm)，省略則自動計算中心" },
                pivotZ: { type: "number", description: "旋轉軸心 Z 座標 (mm)，預設 0", default: 0 },
            },
            required: ["elementIds", "angleDegrees"],
        },
    },

    {
        name: "isolate_elements_in_view",
        description: "在目前視圖中隔離（Isolate）或取消隔離指定元素。temporary=true 使用臨時隔離（Temporary Isolate），false 則永久隱藏其他元素。isolate=false 時重設視圖可見性。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "要隔離的元素 ID 清單（isolate=false 時可為空）",
                },
                isolate: {
                    type: "boolean",
                    description: "true=隔離元素，false=取消隔離（恢復視圖正常顯示）",
                    default: true,
                },
                temporary: {
                    type: "boolean",
                    description: "true=臨時隔離（Temporary Isolate Mode），false=永久隱藏其他元素",
                    default: true,
                },
            },
            required: ["elementIds"],
        },
    },

    {
        name: "set_sheet_revisions",
        description: "為指定圖紙設定版次（Revision）。若不提供 revisionIds，則查詢並回傳各圖紙現有的版次清單。",
        inputSchema: {
            type: "object",
            properties: {
                sheetIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "圖紙 Element ID 清單",
                },
                revisionIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "要套用的版次 Element ID 清單（選填；省略則改為查詢模式）",
                },
            },
            required: ["sheetIds"],
        },
    },

    {
        name: "copy_view_filters",
        description: "將來源視圖的所有篩選器（Filter）連同其顏色覆寫設定，批量複製到一或多個目標視圖。",
        inputSchema: {
            type: "object",
            properties: {
                sourceViewId: { type: "number", description: "來源視圖 Element ID" },
                targetViewIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "目標視圖 Element ID 清單",
                },
            },
            required: ["sourceViewId", "targetViewIds"],
        },
    },

    // ══════════════════════════════════════════
    //  視圖建立類
    // ══════════════════════════════════════════

    {
        name: "create_floor_plan_view",
        description: "依指定樓層建立新的樓板平面視圖（Floor Plan View）。可選填視圖樣板。",
        inputSchema: {
            type: "object",
            properties: {
                levelName: { type: "string", description: "樓層名稱（必填）" },
                viewName: {
                    type: "string",
                    description: "新視圖名稱（選填，省略則自動以樓層名稱命名）",
                },
                viewTemplateId: {
                    type: "number",
                    description: "視圖樣板 Element ID（選填）",
                },
                viewFamilyTypeId: {
                    type: "number",
                    description: "ViewFamilyType ID（選填，省略則使用第一個可用的 FloorPlan 類型）",
                },
            },
            required: ["levelName"],
        },
    },

    {
        name: "create_3d_view",
        description: "建立新的 3D 視圖。perspective=false（預設）建立等角視圖（Isometric）；true 建立透視圖（Perspective）。",
        inputSchema: {
            type: "object",
            properties: {
                viewName: { type: "string", description: "視圖名稱（選填，省略則自動命名）" },
                perspective: {
                    type: "boolean",
                    description: "true=透視圖（Perspective），false=等角視圖（Isometric）",
                    default: false,
                },
                viewTemplateId: {
                    type: "number",
                    description: "視圖樣板 Element ID（選填）",
                },
            },
        },
    },

    {
        name: "create_legend_view",
        description: "建立新的圖例視圖（Legend）或繪圖視圖（Drafting View）。若專案中尚無任何圖例，Legend 類型會以繪圖視圖代替建立。",
        inputSchema: {
            type: "object",
            properties: {
                viewName: { type: "string", description: "視圖名稱（必填）" },
                viewType: {
                    type: "string",
                    enum: ["Legend", "DraftingView"],
                    description: "視圖類型：Legend 或 DraftingView",
                    default: "Legend",
                },
                scale: {
                    type: "number",
                    description: "視圖比例（如 100 代表 1:100）",
                    default: 100,
                },
            },
            required: ["viewName"],
        },
    },

    {
        name: "create_room_elevation_views",
        description: "為指定房間自動建立四個方向（東/北/西/南）的立面視圖。需要該樓層已有對應的平面視圖作為立面標記的宿主視圖。",
        inputSchema: {
            type: "object",
            properties: {
                roomId: { type: "number", description: "房間 Element ID（必填）" },
                viewNamePrefix: {
                    type: "string",
                    description: "視圖名稱前綴（選填，如「客廳-」）",
                    default: "",
                },
                scale: {
                    type: "number",
                    description: "立面視圖比例（如 50 代表 1:50）",
                    default: 50,
                },
            },
            required: ["roomId"],
        },
    },

    {
        name: "create_tags_on_view",
        description: "在指定視圖中為一組元素批量建立標籤（IndependentTag）。自動根據元素品類選擇對應的預設標籤族群。",
        inputSchema: {
            type: "object",
            properties: {
                viewId: { type: "number", description: "要放置標籤的視圖 Element ID（必填）" },
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "要標記的元素 ID 清單（必填）",
                },
                addLeader: {
                    type: "boolean",
                    description: "是否加上引線",
                    default: false,
                },
                offsetX: {
                    type: "number",
                    description: "標籤相對於元素位置的 X 偏移量（mm）",
                    default: 500,
                },
                offsetY: {
                    type: "number",
                    description: "標籤相對於元素位置的 Y 偏移量（mm）",
                    default: 500,
                },
            },
            required: ["viewId", "elementIds"],
        },
    },

    {
        name: "place_view_on_sheet",
        description: "將一個視圖放置到指定圖紙上，建立 Viewport（視埠）。locationX/Y 為相對於圖紙原點的座標（公釐）。",
        inputSchema: {
            type: "object",
            properties: {
                sheetId: { type: "number", description: "圖紙 Element ID（必填）" },
                viewId: { type: "number", description: "要放置的視圖 Element ID（必填）" },
                locationX: {
                    type: "number",
                    description: "視埠放置位置 X 座標 (mm，相對於圖紙原點)",
                    default: 0,
                },
                locationY: {
                    type: "number",
                    description: "視埠放置位置 Y 座標 (mm，相對於圖紙原點)",
                    default: 0,
                },
            },
            required: ["sheetId", "viewId"],
        },
    },

    {
        name: "create_grids",
        description: "批量建立軸網線（Grid Lines）。每條軸線需提供起點、終點座標（公釐）及名稱。",
        inputSchema: {
            type: "object",
            properties: {
                grids: {
                    type: "array",
                    description: "軸網定義清單（必填）",
                    items: {
                        type: "object",
                        properties: {
                            name: { type: "string", description: "軸網名稱（如 'A', '1'）" },
                            startX: { type: "number", description: "起點 X (mm)" },
                            startY: { type: "number", description: "起點 Y (mm)" },
                            endX: { type: "number", description: "終點 X (mm)" },
                            endY: { type: "number", description: "終點 Y (mm)" },
                        },
                        required: ["name", "startX", "startY", "endX", "endY"],
                    },
                },
            },
            required: ["grids"],
        },
    },

    {
        name: "create_levels",
        description: "批量建立樓層（Levels）。elevation 為相對於專案基準點的標高（公釐）。",
        inputSchema: {
            type: "object",
            properties: {
                levels: {
                    type: "array",
                    description: "樓層定義清單（必填）",
                    items: {
                        type: "object",
                        properties: {
                            name: { type: "string", description: "樓層名稱（如 'B1F', '1F'）" },
                            elevation: {
                                type: "number",
                                description: "標高 (mm，相對於專案基準點，正值往上，負值往下)",
                            },
                        },
                        required: ["name", "elevation"],
                    },
                },
            },
            required: ["levels"],
        },
    },

    // ══════════════════════════════════════════
    //  查詢補強類
    // ══════════════════════════════════════════

    {
        name: "get_element_parameters_bulk",
        description: "批量取得多個元素的參數值。效率遠高於逐一呼叫 get_element_info。可指定 parameterNames 僅傳回特定參數，省略則傳回全部參數。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "元素 ID 清單（必填）",
                },
                parameterNames: {
                    type: "array",
                    items: { type: "string" },
                    description: "要讀取的參數名稱清單（選填，省略則傳回所有已設值的參數）",
                },
            },
            required: ["elementIds"],
        },
    },

    {
        name: "get_model_warnings",
        description: "取得模型中所有的警告（Warnings）清單，包含警告描述、嚴重程度、相關元素 ID。可依品類名稱篩選。",
        inputSchema: {
            type: "object",
            properties: {
                category: {
                    type: "string",
                    description: "品類名稱篩選（選填，如 'Walls'；省略則傳回所有警告）",
                },
            },
        },
    },

    {
        name: "get_workset_info",
        description: "取得工作集（Worksets）資訊。若不提供 elementIds，傳回全部工作集清單（名稱、擁有者、開啟狀態）；提供 elementIds 時，額外傳回各元素所屬工作集。非工作集文件會直接回報。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "元素 ID 清單（選填）；提供時同時傳回各元素所屬工作集",
                },
            },
        },
    },

    {
        name: "get_element_graphic_overrides",
        description: "讀取一或多個元素在指定視圖（或目前視圖）中的圖形覆寫設定，包含線色、透明度、半色調、Detail Level。用於審核覆寫狀態。",
        inputSchema: {
            type: "object",
            properties: {
                elementIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "元素 ID 清單（必填）",
                },
                viewId: {
                    type: "number",
                    description: "視圖 Element ID（選填，省略則使用目前活動視圖）",
                },
            },
            required: ["elementIds"],
        },
    },

    {
        name: "get_type_material_layers",
        description: "取得 WallType / FloorType / RoofType 的材料圖層（CompoundStructure）組成，包含各層功能、材料名稱、厚度（mm）。批量傳入多個類型 ID。",
        inputSchema: {
            type: "object",
            properties: {
                typeIds: {
                    type: "array",
                    items: { type: "number" },
                    description: "類型 Element ID 清單（WallType / FloorType / RoofType）（必填）",
                },
            },
            required: ["typeIds"],
        },
    },
];
