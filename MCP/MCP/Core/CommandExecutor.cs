using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCP.Models;

namespace RevitMCP.Core
{
    /// <summary>
    /// 命令執行器 - 執行各種 Revit 操作
    /// </summary>
    public class CommandExecutor
    {
        private readonly UIApplication _uiApp;

        public CommandExecutor(UIApplication uiApp)
        {
            _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        public RevitCommandResponse ExecuteCommand(RevitCommandRequest request)
        {
            try
            {
                var parameters = request.Parameters as JObject ?? new JObject();
                object result = null;

                switch (request.CommandName.ToLower())
                {
                    case "create_wall":
                        result = CreateWall(parameters);
                        break;
                    
                    case "get_project_info":
                        result = GetProjectInfo();
                        break;
                    
                    case "query_elements":
                        result = QueryElements(parameters);
                        break;
                    
                    case "get_all_levels":
                        result = GetAllLevels();
                        break;
                    
                    case "get_element_info":
                        result = GetElementInfo(parameters);
                        break;
                    
                    case "delete_element":
                        result = DeleteElement(parameters);
                        break;
                    
                    default:
                        throw new NotImplementedException($"未實作的命令: {request.CommandName}");
                }

                return new RevitCommandResponse
                {
                    Success = true,
                    Data = result,
                    RequestId = request.RequestId
                };
            }
            catch (Exception ex)
            {
                return new RevitCommandResponse
                {
                    Success = false,
                    Error = ex.Message,
                    RequestId = request.RequestId
                };
            }
        }

        #region 命令實作

        /// <summary>
        /// 建立牆
        /// </summary>
        private object CreateWall(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            double startX = parameters["startX"]?.Value<double>() ?? 0;
            double startY = parameters["startY"]?.Value<double>() ?? 0;
            double endX = parameters["endX"]?.Value<double>() ?? 0;
            double endY = parameters["endY"]?.Value<double>() ?? 0;
            double height = parameters["height"]?.Value<double>() ?? 3000;

            // 轉換為英尺 (Revit 內部單位)
            XYZ start = new XYZ(startX / 304.8, startY / 304.8, 0);
            XYZ end = new XYZ(endX / 304.8, endY / 304.8, 0);

            using (Transaction trans = new Transaction(doc, "建立牆"))
            {
                trans.Start();

                // 建立線
                Line line = Line.CreateBound(start, end);

                // 取得預設樓層
                Level level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault();

                if (level == null)
                {
                    throw new Exception("找不到樓層");
                }

                // 建立牆
                Wall wall = Wall.Create(doc, line, level.Id, false);
                
                // 設定高度
                Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                if (heightParam != null && !heightParam.IsReadOnly)
                {
                    heightParam.Set(height / 304.8);
                }

                trans.Commit();

                return new
                {
                    ElementId = wall.Id.IntegerValue,
                    Message = $"成功建立牆，ID: {wall.Id.IntegerValue}"
                };
            }
        }

        /// <summary>
        /// 取得專案資訊
        /// </summary>
        private object GetProjectInfo()
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            ProjectInfo projInfo = doc.ProjectInformation;

            return new
            {
                ProjectName = doc.Title,
                BuildingName = projInfo.BuildingName,
                OrganizationName = projInfo.OrganizationName,
                Author = projInfo.Author,
                Address = projInfo.Address,
                ClientName = projInfo.ClientName,
                ProjectNumber = projInfo.Number,
                ProjectStatus = projInfo.Status
            };
        }

        /// <summary>
        /// 查詢元素
        /// </summary>
        private object QueryElements(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            string category = parameters["category"]?.Value<string>();

            var collector = new FilteredElementCollector(doc);
            
            if (!string.IsNullOrEmpty(category))
            {
                // 依類別篩選
                BuiltInCategory builtInCategory;
                if (Enum.TryParse($"OST_{category}", true, out builtInCategory))
                {
                    collector.OfCategory(builtInCategory);
                }
            }

            var elements = collector
                .WhereElementIsNotElementType()
                .ToElements()
                .Take(100) // 限制回傳數量
                .Select(e => new
                {
                    ElementId = e.Id.IntegerValue,
                    Name = e.Name,
                    Category = e.Category?.Name,
                    LevelName = doc.GetElement(e.LevelId)?.Name
                })
                .ToList();

            return new
            {
                Count = elements.Count,
                Elements = elements
            };
        }

        /// <summary>
        /// 取得所有樓層
        /// </summary>
        private object GetAllLevels()
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new
                {
                    ElementId = l.Id.IntegerValue,
                    Name = l.Name,
                    Elevation = Math.Round(l.Elevation * 304.8, 2) // 轉換為公釐
                })
                .ToList();

            return new
            {
                Count = levels.Count,
                Levels = levels
            };
        }

        /// <summary>
        /// 取得元素資訊
        /// </summary>
        private object GetElementInfo(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            int elementId = parameters["elementId"]?.Value<int>() ?? 0;

            Element element = doc.GetElement(new ElementId(elementId));
            if (element == null)
            {
                throw new Exception($"找不到元素 ID: {elementId}");
            }

            var parameterList = new List<object>();
            foreach (Parameter param in element.Parameters)
            {
                if (param.HasValue)
                {
                    parameterList.Add(new
                    {
                        Name = param.Definition.Name,
                        Value = param.AsValueString() ?? param.AsString(),
                        Type = param.StorageType.ToString()
                    });
                }
            }

            return new
            {
                ElementId = element.Id.IntegerValue,
                Name = element.Name,
                Category = element.Category?.Name,
                Type = doc.GetElement(element.GetTypeId())?.Name,
                Level = doc.GetElement(element.LevelId)?.Name,
                Parameters = parameterList
            };
        }

        /// <summary>
        /// 刪除元素
        /// </summary>
        private object DeleteElement(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            int elementId = parameters["elementId"]?.Value<int>() ?? 0;

            using (Transaction trans = new Transaction(doc, "刪除元素"))
            {
                trans.Start();

                Element element = doc.GetElement(new ElementId(elementId));
                if (element == null)
                {
                    throw new Exception($"找不到元素 ID: {elementId}");
                }

                doc.Delete(new ElementId(elementId));
                trans.Commit();

                return new
                {
                    Message = $"成功刪除元素 ID: {elementId}"
                };
            }
        }

        #endregion
    }
}
