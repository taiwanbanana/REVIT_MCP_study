import { Tool } from "@modelcontextprotocol/sdk/types.js";

export const familyTools: Tool[] = [
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
