/**
 * 工具註冊中心 — 根據 MCP_PROFILE 篩選載入的工具模組
 *
 * Profile 設定方式（AI Client config）：
 *   "env": { "MCP_PROFILE": "architect" }
 *
 * 可用 Profile：
 *   full        — 全部工具（開發/測試用）
 *   architect   — 建築設計（含結構碰撞）
 *   mep         — 機電配管（含排煙法規）
 *   rfa         — 族群編輯（.rfa 開發）
 */

import { Tool } from "@modelcontextprotocol/sdk/types.js";
import { baseTools } from "./base-tools.js";
import { wallTools } from "./wall-tools.js";
import { roomTools } from "./room-tools.js";
import { visualizationTools } from "./visualization-tools.js";
import { scheduleTools } from "./schedule-tools.js";
import { mepTools } from "./mep-tools.js";
import { curtainWallTools } from "./curtain-wall-tools.js";
import { smokeExhaustTools } from "./smoke-exhaust-tools.js";
import { STAIR_COMPLIANCE_TOOLS } from "./stair-compliance-tools.js";
import { sheetTools } from "./sheet-tools.js";
import { detailComponentTools } from "./detail-component-tools.js";
import { dimensionTools } from "./dimension-tools.js";
import { dependentViewTools } from "./dependent-view-tools.js";
import { clashTools } from "./clash-tools.js";
import { familyTools } from "./family-tools.js";
import { annotationTools } from "./annotation-tools.js";

/**
 * Profile 對照表
 *
 * 每個 profile 都包含 baseTools（查詢、元素操作等核心功能）
 */
const PROFILE_MODULES: Record<string, Tool[][]> = {

    // ── full：全部工具，開發測試用 ──────────────────────────────────
    full: [
        baseTools, wallTools, roomTools, visualizationTools,
        scheduleTools, mepTools, curtainWallTools, smokeExhaustTools,
        STAIR_COMPLIANCE_TOOLS, sheetTools, detailComponentTools,
        dimensionTools, dependentViewTools, clashTools,
        familyTools, annotationTools,
    ],

    // ── architect：建築設計 + 結構碰撞 ──────────────────────────────
    // 涵蓋：牆/門/窗/柱/地板、房間、視覺化、明細表、帷幕牆、
    //        樓梯法規、圖紙、詳圖、尺寸標註、從屬視圖、碰撞偵測、
    //        族群管理、通用標註
    architect: [
        baseTools,
        wallTools,
        roomTools,
        visualizationTools,
        scheduleTools,
        curtainWallTools,
        STAIR_COMPLIANCE_TOOLS,
        sheetTools,
        detailComponentTools,
        dimensionTools,
        dependentViewTools,
        clashTools,
        familyTools,
        annotationTools,
    ],

    // ── mep：機電配管 + 排煙法規 ────────────────────────────────────
    // 涵蓋：MEP 管件、明細表、視覺化、排煙法規、碰撞偵測、
    //        房間資料（MEP 區劃需要）、通用標註
    mep: [
        baseTools,
        mepTools,
        scheduleTools,
        visualizationTools,
        smokeExhaustTools,
        clashTools,
        roomTools,
        annotationTools,
    ],

    // ── rfa：族群編輯（.rfa 開發）───────────────────────────────────
    // 涵蓋：族群查詢/批次操作、RFA 幾何編輯、詳圖元件清單、視覺化
    rfa: [
        baseTools,
        familyTools,
        detailComponentTools,
        visualizationTools,
    ],
};

/**
 * 根據 MCP_PROFILE 環境變數載入對應工具組
 */
export function registerRevitTools(): Tool[] {
    const profile = process.env.MCP_PROFILE || "full";
    const modules = PROFILE_MODULES[profile];

    if (!modules) {
        console.error(`[Tools] Unknown MCP_PROFILE="${profile}", falling back to "full"`);
        return PROFILE_MODULES.full.flat();
    }

    const tools = modules.flat();
    console.error(`[Tools] Profile="${profile}", loaded ${tools.length} tools`);
    return tools;
}
