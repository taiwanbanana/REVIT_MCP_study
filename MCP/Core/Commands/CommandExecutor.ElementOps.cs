using System;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

#if REVIT2025_OR_GREATER
using IdType = System.Int64;
#else
using IdType = System.Int32;
#endif

namespace RevitMCP.Core
{
    public partial class CommandExecutor
    {
        #region 元素翻轉（門/窗 facing / hand）

        /// <summary>
        /// 翻轉元素 facing 或 hand（門/窗）
        /// 來源: flowbim-group/flowbim-project (CommandExecutor.ElementOps.cs)
        /// </summary>
        private object FlipElement(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            IdType elementId = parameters["elementId"]?.Value<IdType>() ?? 0;
            string flipType = parameters["flipType"]?.Value<string>() ?? "facing";

            Element element = doc.GetElement(new ElementId(elementId));
            if (element == null)
                throw new Exception($"找不到元素 ID: {elementId}");

            if (!(element is FamilyInstance familyInstance))
                throw new Exception($"元素 ID: {elementId} 不是有效的 FamilyInstance，無法翻轉");

            using (Transaction trans = new Transaction(doc, $"MCP: 翻轉元素 {elementId}"))
            {
                trans.Start();

                switch (flipType.ToLower())
                {
                    case "facing":
                        if (!familyInstance.CanFlipFacing)
                            throw new Exception("此元素不支援翻轉面向 (Facing)");
                        familyInstance.flipFacing();
                        break;

                    case "hand":
                        if (!familyInstance.CanFlipHand)
                            throw new Exception("此元素不支援翻轉開向 (Hand)");
                        familyInstance.flipHand();
                        break;

                    default:
                        throw new Exception("無效的翻轉類型，請使用 'facing' 或 'hand'");
                }

                trans.Commit();

                return new
                {
                    ElementId = elementId,
                    FlipType = flipType,
                    Message = $"成功翻轉元素（{flipType}）"
                };
            }
        }

        #endregion

        #region 元素參數複製（建立門/窗時 sourceElementId 支援）

        /// <summary>
        /// 複製來源 Element 的 instance parameters 到 target Element
        /// 排除：唯讀、樓層 / 主體 / ID 類別、標記欄位
        /// 來源: flowbim-group/flowbim-project (CommandExecutor.ElementOps.cs)
        /// </summary>
        private void CopyInstanceParameters(Element source, Element target)
        {
            foreach (Parameter sourceParam in source.Parameters)
            {
                if (sourceParam.IsReadOnly || !sourceParam.HasValue) continue;

                string paramName = sourceParam.Definition.Name;
                if (paramName.Contains("樓層") || paramName.Contains("Level") ||
                    paramName.Contains("主體") || paramName.Contains("Host") ||
                    paramName.Contains("ID") ||
                    paramName == "標記" || paramName == "Mark")
                    continue;

                Parameter targetParam = target.LookupParameter(sourceParam.Definition.Name);
                if (targetParam == null || targetParam.IsReadOnly) continue;

                try
                {
                    switch (sourceParam.StorageType)
                    {
                        case StorageType.String:
                            targetParam.Set(sourceParam.AsString());
                            break;
                        case StorageType.Double:
                            targetParam.Set(sourceParam.AsDouble());
                            break;
                        case StorageType.Integer:
                            targetParam.Set(sourceParam.AsInteger());
                            break;
                        case StorageType.ElementId:
                            targetParam.Set(sourceParam.AsElementId());
                            break;
                    }
                }
                catch { /* 忽略個別參數設定失敗 */ }
            }
        }

        #endregion
    }
}
