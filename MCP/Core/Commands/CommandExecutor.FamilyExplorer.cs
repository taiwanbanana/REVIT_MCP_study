using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace RevitMCP.Core
{
    public partial class CommandExecutor
    {
        /// <summary>
        /// 取得模型中所有已載入的族群 (不含系統族群)
        /// </summary>
        private object GetAllUsedFamiliesInModel()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            
            var families = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .ToList();

            var result = new List<object>();
            foreach (var fam in families)
            {
                result.Add(new
                {
                    Id = fam.Id.ToString(),
                    Name = fam.Name,
                    Category = fam.FamilyCategory?.Name ?? "Unknown"
                });
            }

            return new
            {
                Success = true,
                Message = $"找到 {result.Count} 個載入的族群",
                Families = result
            };
        }

        /// <summary>
        /// 取得指定族群清單中的所有類型
        /// </summary>
        private object GetAllUsedTypesOfFamilies(JObject parameters)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var familyNamesToken = parameters["familyNames"];
            
            if (familyNamesToken == null || !familyNamesToken.HasValues)
            {
                throw new ArgumentException("請提供 familyNames 參數 (陣列)");
            }

            var targetFamilyNames = familyNamesToken.ToObject<List<string>>();
            var result = new List<object>();

            // 取得所有 FamilySymbol (類型)
            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(s => s.Family != null && targetFamilyNames.Contains(s.Family.Name))
                .ToList();

            // 依族群分組
            var grouped = symbols.GroupBy(s => s.Family.Name);

            foreach (var group in grouped)
            {
                var typeIdsAndNames = new Dictionary<string, string>();
                foreach (var symbol in group)
                {
                    typeIdsAndNames[symbol.Id.ToString()] = symbol.Name;
                }

                result.Add(new
                {
                    NameOfFamily = group.Key,
                    TypeIdsAndNames = typeIdsAndNames
                });
            }

            return new
            {
                Success = true,
                Message = $"查詢到 {grouped.Count()} 個族群的類型資訊",
                context = new[]
                {
                    new { family_types_and_names = result }
                }
            };
        }
    }
}
