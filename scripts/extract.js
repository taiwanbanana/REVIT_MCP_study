import WebSocket from 'ws';
import fs from 'fs';

const ws = new WebSocket('ws://localhost:8964');

// 幾何實體與巢狀物件的關鍵字
const geometryKeywords = ['擠出', '掃掠', '混成', '旋轉', '實體', '空心', 'Extrusion', 'Sweep', 'Blend', 'Revolution', 'Solid', 'Void', '一般模型', '門', '窗'];

ws.on('open', async () => {
    ws.send(JSON.stringify({ 
        CommandName: 'query_elements', 
        Parameters: { documentName: 'template', maxCount: 5000 }, 
        RequestId: 'req_all' 
    }));
});

let geomIds = [];
const results = [];
let currentIndex = 0;

const saveAndExit = () => {
    results.sort((a,b) => a.ID - b.ID);
    let md = '# 實際使用之幾何物件清單\n\n';
    md += '| ID | 類型 (Name) | 主品類 | 子品類 | 材質 |\n';
    md += '|---|---|---|---|---|\n';
    for(const r of results) {
        md += `| ${r.ID} | ${r.Name} | ${r.Category} | ${r.SubCategory} | ${r.Material} |\n`;
    }
    
    if (!fs.existsSync('output/reports')) {
        fs.mkdirSync('output/reports', { recursive: true });
    }
    
    fs.writeFileSync('output/reports/template_geometries.md', md);
    console.log(md); // 直接印出給使用者看
    console.log(`\n過濾完成！共分析 ${results.length} 個實體元件，已寫入 output/reports/template_geometries.md`);
    ws.close();
    process.exit(0);
};

const requestNext = () => {
    if (currentIndex >= geomIds.length) {
        saveAndExit();
        return;
    }
    const id = geomIds[currentIndex];
    ws.send(JSON.stringify({ 
        CommandName: 'get_element_info', 
        Parameters: { documentName: 'template', elementId: id }, 
        RequestId: 'info_' + id 
    }));
};

ws.on('message', (d) => { 
    const resp = JSON.parse(d.toString());
    
    if (resp.RequestId === 'req_all' && resp.Data && resp.Data.Elements) {
        // 先保留所有具名元素，延後到取得 info 後再過濾
        geomIds = resp.Data.Elements
            .filter(e => e.Name.trim() !== '')
            .map(e => e.ElementId);
            
        console.log(`開始讀取 ${geomIds.length} 個物件屬性以進行深度過濾...\n`);
        if (geomIds.length > 0) {
            requestNext();
        } else {
            saveAndExit();
        }
    }
    else if (resp.RequestId && resp.RequestId.startsWith('info_')) {
        const data = resp.Data;
        if (data) {
            const subCatParam = data.Parameters ? data.Parameters.find(p => p.Name === '子品類') : null;
            const matParam = data.Parameters ? data.Parameters.find(p => p.Name === '材料' || p.Name === '材質') : null;
            
            const cat = data.Category || '無';
            const name = data.Name || '';
            
            // 判斷是否為我們要追蹤的實體、基準或宿主
            const isGeometry = geometryKeywords.some(kw => name.includes(kw));
            const isTargetCategory = ['參考平面', '樓層', '基準面', '牆', '基本牆', '開口切割', '門', '窗', '一般模型', '構架/豎框'].some(c => cat.includes(c));
            const isRefPlaneName = ['Left', 'Right', 'Center', 'Front', 'Back', 'Top', 'Bottom', 'Ref. Level', 'Elevation'].some(n => name.includes(n));
            
            // 例外排除純系統設定或視圖標籤等干擾項
            const isNoise = ['材料', '標籤', '視圖', '專案瀏覽器', '對齊線', '標註', '尺寸'].some(c => cat.includes(c) || name.includes(c));

            if ((isGeometry || isTargetCategory || isRefPlaneName) && !isNoise) {
                results.push({
                    ID: data.ElementId,
                    Name: data.Name,
                    Category: cat,
                    SubCategory: subCatParam ? subCatParam.Value : '無',
                    Material: matParam ? matParam.Value : '無'
                });
            }
        }
        
        currentIndex++;
        requestNext();
    }
});
