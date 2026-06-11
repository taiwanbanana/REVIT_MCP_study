using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

#if REVIT2025_OR_GREATER
using IdType = System.Int64;
#else
using IdType = System.Int32;
#endif

namespace RevitMCP.Core
{
    /// <summary>
    /// NONICAPRO 功能差距補充模組（18 個新工具）
    /// </summary>
    public partial class CommandExecutor
    {
        // ══════════════════════════════════════════════════════════
        //  操作類（5 個）
        // ══════════════════════════════════════════════════════════

        #region copy_elements

        private object CopyElements(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var idList = (parameters["elementIds"] as JArray)
                ?.Select(t => new ElementId(t.Value<IdType>())).ToList();
            if (idList == null || idList.Count == 0)
                throw new Exception("請提供 elementIds");

            double tx = (parameters["translationX"]?.Value<double>() ?? 0) / 304.8;
            double ty = (parameters["translationY"]?.Value<double>() ?? 0) / 304.8;
            double tz = (parameters["translationZ"]?.Value<double>() ?? 0) / 304.8;
            XYZ translation = new XYZ(tx, ty, tz);

            using (Transaction trans = new Transaction(doc, "MCP: 複製元素"))
            {
                trans.Start();
                ICollection<ElementId> newIds = ElementTransformUtils.CopyElements(doc, idList, translation);
                trans.Commit();

                return new
                {
                    OriginalCount = idList.Count,
                    CopiedCount = newIds.Count,
                    NewElementIds = newIds.Select(id => id.GetIdValue()).ToList(),
                    Message = $"成功複製 {newIds.Count} 個元素"
                };
            }
        }

        #endregion

        #region rotate_elements

        private object RotateElements(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var idList = (parameters["elementIds"] as JArray)
                ?.Select(t => new ElementId(t.Value<IdType>())).ToList();
            if (idList == null || idList.Count == 0)
                throw new Exception("請提供 elementIds");

            double angleDeg = parameters["angleDegrees"]?.Value<double>() ?? 0;
            double angleRad = angleDeg * Math.PI / 180.0;

            // 決定旋轉軸心
            XYZ pivot;
            if (parameters.ContainsKey("pivotX") && parameters["pivotX"] != null &&
                parameters.ContainsKey("pivotY") && parameters["pivotY"] != null)
            {
                double px = parameters["pivotX"].Value<double>() / 304.8;
                double py = parameters["pivotY"].Value<double>() / 304.8;
                double pz = (parameters["pivotZ"]?.Value<double>() ?? 0) / 304.8;
                pivot = new XYZ(px, py, pz);
            }
            else
            {
                // 使用各元素 BoundingBox 中心的平均值
                var centers = idList
                    .Select(id => doc.GetElement(id)?.get_BoundingBox(null))
                    .Where(bb => bb != null)
                    .Select(bb => new XYZ((bb.Min.X + bb.Max.X) / 2, (bb.Min.Y + bb.Max.Y) / 2, (bb.Min.Z + bb.Max.Z) / 2))
                    .ToList();

                if (centers.Count == 0)
                    pivot = XYZ.Zero;
                else
                    pivot = new XYZ(centers.Average(c => c.X), centers.Average(c => c.Y), centers.Average(c => c.Z));
            }

            Line axis = Line.CreateUnbound(pivot, XYZ.BasisZ);

            using (Transaction trans = new Transaction(doc, "MCP: 旋轉元素"))
            {
                trans.Start();
                ElementTransformUtils.RotateElements(doc, idList, axis, angleRad);
                trans.Commit();
            }

            return new
            {
                Count = idList.Count,
                AngleDegrees = angleDeg,
                PivotX = Math.Round(pivot.X * 304.8, 2),
                PivotY = Math.Round(pivot.Y * 304.8, 2),
                Message = $"成功旋轉 {idList.Count} 個元素 {angleDeg}°"
            };
        }

        #endregion

        #region isolate_elements_in_view

        private object IsolateElementsInView(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            View view = _uiApp.ActiveUIDocument.ActiveView;

            bool isolate = parameters["isolate"]?.Value<bool>() ?? true;
            bool temporary = parameters["temporary"]?.Value<bool>() ?? true;

            var idList = (parameters["elementIds"] as JArray)
                ?.Select(t => new ElementId(t.Value<IdType>())).ToList()
                ?? new List<ElementId>();

            if (isolate && idList.Count == 0)
                throw new Exception("隔離模式（isolate=true）需要提供 elementIds");

            using (Transaction trans = new Transaction(doc, isolate ? "MCP: 隔離元素" : "MCP: 取消隔離"))
            {
                trans.Start();

                if (!isolate)
                {
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }
                else if (temporary)
                {
                    view.IsolateElementsTemporary(idList);
                }
                else
                {
                    // 永久隱藏：隱藏視圖中不在清單內的元素
                    var toHide = new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType()
                        .ToElementIds()
                        .Except(idList)
                        .Where(id => {
                            Element e = doc.GetElement(id);
                            return e != null && e.CanBeHidden(view);
                        })
                        .ToList();

                    if (toHide.Count > 0)
                        view.HideElements(toHide);
                }

                trans.Commit();
            }

            return new
            {
                Count = idList.Count,
                Mode = isolate ? (temporary ? "臨時隔離" : "永久隱藏其他") : "取消隔離",
                ViewName = view.Name,
                Message = isolate
                    ? $"已在視圖 {view.Name} 中隔離 {idList.Count} 個元素"
                    : $"已取消隔離，視圖 {view.Name} 恢復正常顯示"
            };
        }

        #endregion

        #region set_sheet_revisions

        private object SetSheetRevisions(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var sheetIds = (parameters["sheetIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (sheetIds == null || sheetIds.Count == 0)
                throw new Exception("請提供 sheetIds");

            var revisionIds = (parameters["revisionIds"] as JArray)
                ?.Select(t => new ElementId(t.Value<IdType>())).ToList();

            // 查詢模式（未提供 revisionIds）
            if (revisionIds == null || revisionIds.Count == 0)
            {
                var queryResult = sheetIds.Select(sid => {
                    ViewSheet sheet = doc.GetElement(new ElementId(sid)) as ViewSheet;
                    if (sheet == null) return (object)new { SheetId = sid, Error = "找不到圖紙" };

                    var currentRevIds = sheet.GetAllRevisionIds();
                    return (object)new
                    {
                        SheetId = sid,
                        SheetNumber = sheet.SheetNumber,
                        SheetName = sheet.Name,
                        Revisions = currentRevIds.Select(rid => {
                            Revision rev = doc.GetElement(rid) as Revision;
                            return new
                            {
                                RevisionId = rid.GetIdValue(),
                                Description = rev?.Description ?? "",
                                Date = rev?.RevisionDate ?? ""
                            };
                        }).ToList()
                    };
                }).ToList();

                return new { Mode = "查詢", Sheets = queryResult };
            }

            // 設定模式
            using (Transaction trans = new Transaction(doc, "MCP: 設定圖紙版次"))
            {
                trans.Start();

                var results = new List<object>();
                foreach (var sid in sheetIds)
                {
                    ViewSheet sheet = doc.GetElement(new ElementId(sid)) as ViewSheet;
                    if (sheet == null)
                    {
                        results.Add(new { SheetId = sid, Success = false, Error = "找不到圖紙" });
                        continue;
                    }

                    try
                    {
                        sheet.SetAdditionalRevisionIds(revisionIds);
                        results.Add(new
                        {
                            SheetId = sid,
                            SheetNumber = sheet.SheetNumber,
                            SheetName = sheet.Name,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { SheetId = sid, Success = false, Error = ex.Message });
                    }
                }

                trans.Commit();

                return new
                {
                    Mode = "設定",
                    RevisionIds = revisionIds.Select(rid => rid.GetIdValue()).ToList(),
                    Results = results
                };
            }
        }

        #endregion

        #region copy_view_filters

        private object CopyViewFilters(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            IdType sourceViewId = parameters["sourceViewId"]?.Value<IdType>() ?? 0;
            if (sourceViewId == 0) throw new Exception("請提供 sourceViewId");

            var targetViewIds = (parameters["targetViewIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (targetViewIds == null || targetViewIds.Count == 0)
                throw new Exception("請提供 targetViewIds");

            View sourceView = doc.GetElement(new ElementId(sourceViewId)) as View;
            if (sourceView == null) throw new Exception($"找不到來源視圖 ID: {sourceViewId}");

            var filterIds = sourceView.GetFilters().ToList();
            if (filterIds.Count == 0)
                return new { Message = "來源視圖沒有任何篩選器", FilterCount = 0 };

            using (Transaction trans = new Transaction(doc, "MCP: 複製視圖篩選器"))
            {
                trans.Start();

                int totalOps = 0;
                var viewResults = new List<object>();

                foreach (var tid in targetViewIds)
                {
                    View targetView = doc.GetElement(new ElementId(tid)) as View;
                    if (targetView == null)
                    {
                        viewResults.Add(new { ViewId = tid, Success = false, Error = "找不到視圖" });
                        continue;
                    }

                    int copied = 0;
                    var existingFilters = targetView.GetFilters();

                    foreach (var fid in filterIds)
                    {
                        try
                        {
                            if (!existingFilters.Contains(fid))
                                targetView.AddFilter(fid);

                            bool visible = sourceView.GetFilterVisibility(fid);
                            targetView.SetFilterVisibility(fid, visible);

                            OverrideGraphicSettings ogs = sourceView.GetFilterOverrides(fid);
                            targetView.SetFilterOverrides(fid, ogs);

                            copied++;
                        }
                        catch { /* 跳過單個篩選器失敗 */ }
                    }

                    totalOps += copied;
                    viewResults.Add(new
                    {
                        ViewId = tid,
                        ViewName = targetView.Name,
                        FiltersCopied = copied,
                        Success = true
                    });
                }

                trans.Commit();

                return new
                {
                    SourceViewId = sourceViewId,
                    FilterCount = filterIds.Count,
                    TargetViewCount = targetViewIds.Count,
                    TotalOperations = totalOps,
                    ViewResults = viewResults,
                    Message = $"共將 {filterIds.Count} 個篩選器複製到 {targetViewIds.Count} 個目標視圖"
                };
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        //  視圖建立類（8 個）
        // ══════════════════════════════════════════════════════════

        #region create_floor_plan_view

        private object CreateFloorPlanView(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            string levelName = parameters["levelName"]?.Value<string>();
            if (string.IsNullOrEmpty(levelName)) throw new Exception("請提供 levelName");

            string viewName = parameters["viewName"]?.Value<string>();
            IdType vftIdVal = parameters["viewFamilyTypeId"]?.Value<IdType>() ?? 0;
            IdType tmplIdVal = parameters["viewTemplateId"]?.Value<IdType>() ?? 0;

            Level level = FindLevel(doc, levelName, false);

            ViewFamilyType vft;
            if (vftIdVal > 0)
            {
                vft = doc.GetElement(vftIdVal.ToElementId()) as ViewFamilyType;
                if (vft == null) throw new Exception($"找不到 ViewFamilyType ID: {vftIdVal}");
            }
            else
            {
                vft = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);
                if (vft == null) throw new Exception("找不到可用的樓板平面圖 ViewFamilyType");
            }

            using (Transaction trans = new Transaction(doc, "MCP: 建立平面視圖"))
            {
                trans.Start();

                ViewPlan view = ViewPlan.Create(doc, vft.Id, level.Id);

                if (!string.IsNullOrEmpty(viewName))
                {
                    try { view.Name = viewName; }
                    catch { /* 名稱衝突時保留自動命名 */ }
                }

                if (tmplIdVal > 0)
                {
                    ElementId tmplId = tmplIdVal.ToElementId();
                    if (view.IsValidViewTemplate(tmplId))
                        view.ViewTemplateId = tmplId;
                }

                trans.Commit();

                return new
                {
                    ElementId = view.Id.GetIdValue(),
                    ViewName = view.Name,
                    Level = level.Name,
                    LevelElevation = Math.Round(level.Elevation * 304.8, 2),
                    Message = $"成功建立平面視圖：{view.Name}"
                };
            }
        }

        #endregion

        #region create_3d_view

        private object Create3DView(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            string viewName = parameters["viewName"]?.Value<string>();
            bool perspective = parameters["perspective"]?.Value<bool>() ?? false;
            IdType tmplIdVal = parameters["viewTemplateId"]?.Value<IdType>() ?? 0;

            ViewFamilyType vft3D = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            if (vft3D == null) throw new Exception("找不到 3D ViewFamilyType");

            using (Transaction trans = new Transaction(doc, "MCP: 建立 3D 視圖"))
            {
                trans.Start();

                View3D view = perspective
                    ? View3D.CreatePerspective(doc, vft3D.Id)
                    : View3D.CreateIsometric(doc, vft3D.Id);

                if (!string.IsNullOrEmpty(viewName))
                {
                    try { view.Name = viewName; }
                    catch { /* 名稱衝突時保留自動命名 */ }
                }

                if (tmplIdVal > 0)
                {
                    ElementId tmplId = tmplIdVal.ToElementId();
                    if (view.IsValidViewTemplate(tmplId))
                        view.ViewTemplateId = tmplId;
                }

                trans.Commit();

                return new
                {
                    ElementId = view.Id.GetIdValue(),
                    ViewName = view.Name,
                    ViewKind = perspective ? "透視圖 (Perspective)" : "等角視圖 (Isometric)",
                    Message = $"成功建立 3D 視圖：{view.Name}"
                };
            }
        }

        #endregion

        #region create_legend_view

        private object CreateLegendView(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            string viewName = parameters["viewName"]?.Value<string>();
            if (string.IsNullOrEmpty(viewName)) throw new Exception("請提供 viewName");

            string viewType = parameters["viewType"]?.Value<string>() ?? "Legend";
            int scale = parameters["scale"]?.Value<int>() ?? 100;

            using (Transaction trans = new Transaction(doc, "MCP: 建立圖例/繪圖視圖"))
            {
                trans.Start();

                View newView;
                string actualType;

                if (viewType == "DraftingView")
                {
                    ViewFamilyType draftVFT = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);

                    if (draftVFT == null) throw new Exception("找不到繪圖視圖 ViewFamilyType");

                    newView = ViewDrafting.Create(doc, draftVFT.Id);
                    actualType = "DraftingView";
                }
                else // Legend
                {
                    // 嘗試複製現有 Legend；若無則改用 Drafting View
                    View existingLegend = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .FirstOrDefault(v => v.ViewType == ViewType.Legend && !v.IsTemplate);

                    if (existingLegend != null)
                    {
                        ElementId dupId = existingLegend.Duplicate(ViewDuplicateOption.Duplicate);
                        newView = doc.GetElement(dupId) as View;
                        actualType = "Legend";
                    }
                    else
                    {
                        // 後備：Drafting View
                        ViewFamilyType draftVFT = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);

                        if (draftVFT == null) throw new Exception("找不到任何可用的視圖族群類型");

                        newView = ViewDrafting.Create(doc, draftVFT.Id);
                        actualType = "DraftingView (Legend 後備)";
                    }
                }

                newView.Name = viewName;
                newView.Scale = scale;

                trans.Commit();

                return new
                {
                    ElementId = newView.Id.GetIdValue(),
                    ViewName = newView.Name,
                    ActualType = actualType,
                    RequestedType = viewType,
                    Scale = scale,
                    Message = $"成功建立視圖：{newView.Name}（{actualType}）"
                };
            }
        }

        #endregion

        #region create_room_elevation_views

        private object CreateRoomElevationViews(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            IdType roomId = parameters["roomId"]?.Value<IdType>() ?? 0;
            if (roomId == 0) throw new Exception("請提供 roomId");

            string prefix = parameters["viewNamePrefix"]?.Value<string>() ?? "";
            int scale = parameters["scale"]?.Value<int>() ?? 50;

            Room room = doc.GetElement(new ElementId(roomId)) as Room;
            if (room == null) throw new Exception($"找不到房間 ID: {roomId}");

            LocationPoint locPoint = room.Location as LocationPoint;
            if (locPoint == null) throw new Exception("無法取得房間位置點（房間未封閉？）");
            XYZ roomCenter = locPoint.Point;

            // 取得房間所在樓層及其平面視圖（用於放置立面標記）
            Level level = doc.GetElement(room.LevelId) as Level;
            if (level == null) throw new Exception("無法取得房間所在樓層");

            View floorPlan = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v =>
                    v.GenLevel != null &&
                    v.GenLevel.Id == level.Id &&
                    !v.IsTemplate &&
                    v.ViewType == ViewType.FloorPlan);

            if (floorPlan == null)
                throw new Exception($"找不到樓層 {level.Name} 的平面視圖，無法放置立面標記");

            // 找立面 ViewFamilyType
            ViewFamilyType elevVFT = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.Elevation);

            if (elevVFT == null) throw new Exception("找不到立面圖 ViewFamilyType");

            string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString()
                              ?? $"Room_{roomId}";

            string[] dirNames = { "東", "北", "西", "南" };

            var successViews = new List<object>();
            var errors = new List<object>();

            using (Transaction trans = new Transaction(doc, "MCP: 建立房間立面視圖"))
            {
                trans.Start();

                ElevationMarker marker = ElevationMarker.CreateElevationMarker(
                    doc, elevVFT.Id, roomCenter, scale);

                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        ViewSection elev = marker.CreateElevation(doc, floorPlan.Id, i);
                        elev.Scale = scale;

                        string newName = $"{prefix}{roomName}-{dirNames[i]}";
                        try { elev.Name = newName; }
                        catch { /* 名稱衝突時保留自動命名 */ }

                        successViews.Add(new
                        {
                            ElementId = elev.Id.GetIdValue(),
                            ViewName = elev.Name,
                            Direction = dirNames[i],
                            MarkerIndex = i
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new { Direction = dirNames[i], MarkerIndex = i, Error = ex.Message });
                    }
                }

                trans.Commit();
            }

            return new
            {
                RoomId = roomId,
                RoomName = roomName,
                SuccessCount = successViews.Count,
                ErrorCount = errors.Count,
                ElevationViews = successViews,
                Errors = errors.Count > 0 ? errors : null,
                Message = $"為房間 {roomName} 建立了 {successViews.Count} 個立面視圖"
            };
        }

        #endregion

        #region create_tags_on_view

        private object CreateTagsOnView(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            IdType viewId = parameters["viewId"]?.Value<IdType>() ?? 0;
            if (viewId == 0) throw new Exception("請提供 viewId");

            var elementIds = (parameters["elementIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (elementIds == null || elementIds.Count == 0)
                throw new Exception("請提供 elementIds");

            bool addLeader = parameters["addLeader"]?.Value<bool>() ?? false;
            double offsetX = (parameters["offsetX"]?.Value<double>() ?? 500) / 304.8;
            double offsetY = (parameters["offsetY"]?.Value<double>() ?? 500) / 304.8;

            View view = doc.GetElement(viewId.ToElementId()) as View;
            if (view == null) throw new Exception($"找不到視圖 ID: {viewId}");

            var results = new List<object>();
            int successCount = 0;

            using (Transaction trans = new Transaction(doc, "MCP: 建立標籤"))
            {
                trans.Start();

                foreach (var eid in elementIds)
                {
                    Element element = doc.GetElement(new ElementId(eid));
                    if (element == null)
                    {
                        results.Add(new { ElementId = eid, Success = false, Error = "找不到元素" });
                        continue;
                    }

                    try
                    {
                        // 計算標籤放置位置
                        XYZ location = XYZ.Zero;

                        if (element.Location is LocationPoint lp)
                            location = lp.Point + new XYZ(offsetX, offsetY, 0);
                        else if (element.Location is LocationCurve lc)
                            location = lc.Curve.Evaluate(0.5, true) + new XYZ(offsetX, offsetY, 0);
                        else
                        {
                            BoundingBoxXYZ bb = element.get_BoundingBox(view);
                            if (bb != null)
                                location = new XYZ(
                                    (bb.Min.X + bb.Max.X) / 2.0 + offsetX,
                                    (bb.Min.Y + bb.Max.Y) / 2.0 + offsetY,
                                    0);
                        }

                        Reference reference = new Reference(element);
                        IndependentTag tag = IndependentTag.Create(
                            doc,
                            viewId.ToElementId(),
                            reference,
                            addLeader,
                            TagMode.TM_ADDBY_CATEGORY,
                            TagOrientation.Horizontal,
                            location);

                        successCount++;
                        results.Add(new
                        {
                            ElementId = eid,
                            TagId = tag.Id.GetIdValue(),
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { ElementId = eid, Success = false, Error = ex.Message });
                    }
                }

                trans.Commit();
            }

            return new
            {
                TotalRequested = elementIds.Count,
                SuccessCount = successCount,
                FailCount = elementIds.Count - successCount,
                Results = results,
                Message = $"成功建立 {successCount} 個標籤（共 {elementIds.Count} 個請求）"
            };
        }

        #endregion

        #region place_view_on_sheet

        private object PlaceViewOnSheet(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            IdType sheetId = parameters["sheetId"]?.Value<IdType>() ?? 0;
            IdType targetViewId = parameters["viewId"]?.Value<IdType>() ?? 0;

            if (sheetId == 0) throw new Exception("請提供 sheetId");
            if (targetViewId == 0) throw new Exception("請提供 viewId");

            double locX = (parameters["locationX"]?.Value<double>() ?? 0) / 304.8;
            double locY = (parameters["locationY"]?.Value<double>() ?? 0) / 304.8;
            XYZ location = new XYZ(locX, locY, 0);

            ViewSheet sheet = doc.GetElement(sheetId.ToElementId()) as ViewSheet;
            if (sheet == null) throw new Exception($"找不到圖紙 ID: {sheetId}");

            View view = doc.GetElement(targetViewId.ToElementId()) as View;
            if (view == null) throw new Exception($"找不到視圖 ID: {targetViewId}");

            if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                throw new Exception($"無法將視圖 {view.Name} 放置到圖紙（可能已放置於其他圖紙，或此視圖類型不支援放置）");

            using (Transaction trans = new Transaction(doc, "MCP: 放置視圖到圖紙"))
            {
                trans.Start();
                Viewport viewport = Viewport.Create(doc, sheet.Id, view.Id, location);
                trans.Commit();

                return new
                {
                    ViewportId = viewport.Id.GetIdValue(),
                    SheetId = sheetId,
                    SheetNumber = sheet.SheetNumber,
                    SheetName = sheet.Name,
                    ViewId = targetViewId,
                    ViewName = view.Name,
                    Message = $"成功將視圖 {view.Name} 放置到圖紙 {sheet.SheetNumber}"
                };
            }
        }

        #endregion

        #region create_grids

        private object CreateGrids(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var gridsArray = parameters["grids"] as JArray;
            if (gridsArray == null || gridsArray.Count == 0)
                throw new Exception("請提供 grids 清單");

            using (Transaction trans = new Transaction(doc, "MCP: 建立軸網"))
            {
                trans.Start();

                var results = new List<object>();
                int successCount = 0;

                foreach (var g in gridsArray)
                {
                    string name = g["name"]?.Value<string>();
                    double sx = g["startX"]?.Value<double>() ?? 0;
                    double sy = g["startY"]?.Value<double>() ?? 0;
                    double ex = g["endX"]?.Value<double>() ?? 0;
                    double ey = g["endY"]?.Value<double>() ?? 0;

                    try
                    {
                        XYZ start = new XYZ(sx / 304.8, sy / 304.8, 0);
                        XYZ end = new XYZ(ex / 304.8, ey / 304.8, 0);

                        if (start.DistanceTo(end) < 0.001 / 304.8)
                            throw new Exception("起點與終點距離過近（< 1mm）");

                        Line line = Line.CreateBound(start, end);
                        Grid grid = Grid.Create(doc, line);

                        if (!string.IsNullOrEmpty(name))
                        {
                            try { grid.Name = name; }
                            catch { /* 名稱衝突時保留自動命名 */ }
                        }

                        successCount++;
                        results.Add(new
                        {
                            ElementId = grid.Id.GetIdValue(),
                            Name = grid.Name,
                            Success = true
                        });
                    }
                    catch (Exception ex2)
                    {
                        results.Add(new { Name = name, Success = false, Error = ex2.Message });
                    }
                }

                trans.Commit();

                return new
                {
                    Total = gridsArray.Count,
                    SuccessCount = successCount,
                    Results = results,
                    Message = $"成功建立 {successCount} 條軸網線"
                };
            }
        }

        #endregion

        #region create_levels

        private object CreateLevels(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var levelsArray = parameters["levels"] as JArray;
            if (levelsArray == null || levelsArray.Count == 0)
                throw new Exception("請提供 levels 清單");

            using (Transaction trans = new Transaction(doc, "MCP: 建立樓層"))
            {
                trans.Start();

                var results = new List<object>();
                int successCount = 0;

                foreach (var l in levelsArray)
                {
                    string name = l["name"]?.Value<string>();
                    double elevMm = l["elevation"]?.Value<double>() ?? 0;

                    try
                    {
                        Level level = Level.Create(doc, elevMm / 304.8);

                        if (!string.IsNullOrEmpty(name))
                        {
                            try { level.Name = name; }
                            catch { /* 名稱衝突時保留自動命名 */ }
                        }

                        successCount++;
                        results.Add(new
                        {
                            ElementId = level.Id.GetIdValue(),
                            Name = level.Name,
                            ElevationMm = elevMm,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { Name = name, ElevationMm = elevMm, Success = false, Error = ex.Message });
                    }
                }

                trans.Commit();

                return new
                {
                    Total = levelsArray.Count,
                    SuccessCount = successCount,
                    Results = results,
                    Message = $"成功建立 {successCount} 個樓層"
                };
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        //  查詢補強類（5 個）
        // ══════════════════════════════════════════════════════════

        #region get_element_parameters_bulk

        private object GetElementParametersBulk(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var elementIds = (parameters["elementIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (elementIds == null || elementIds.Count == 0)
                throw new Exception("請提供 elementIds");

            // 指定要讀取的參數名稱（選填）
            var paramFilter = (parameters["parameterNames"] as JArray)
                ?.Select(t => t.Value<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool hasFilter = paramFilter != null && paramFilter.Count > 0;

            var results = new List<object>();

            foreach (var eid in elementIds)
            {
                Element elem = doc.GetElement(new ElementId(eid));
                if (elem == null)
                {
                    results.Add(new { ElementId = eid, Error = "找不到元素" });
                    continue;
                }

                var paramList = new List<object>();
                foreach (Parameter param in elem.Parameters)
                {
                    if (!param.HasValue) continue;
                    if (hasFilter && !paramFilter.Contains(param.Definition.Name)) continue;

                    string val;
                    switch (param.StorageType)
                    {
                        case StorageType.String:
                            val = param.AsString() ?? "";
                            break;
                        case StorageType.Double:
                            val = param.AsValueString() ?? param.AsDouble().ToString("G6");
                            break;
                        case StorageType.Integer:
                            val = param.AsValueString() ?? param.AsInteger().ToString();
                            break;
                        case StorageType.ElementId:
                            val = param.AsElementId()?.GetIdValue().ToString() ?? "";
                            break;
                        default:
                            val = "";
                            break;
                    }

                    paramList.Add(new
                    {
                        Name = param.Definition.Name,
                        Value = val,
                        StorageType = param.StorageType.ToString()
                    });
                }

                results.Add(new
                {
                    ElementId = eid,
                    Name = elem.Name,
                    Category = elem.Category?.Name,
                    ParameterCount = paramList.Count,
                    Parameters = paramList
                });
            }

            return new
            {
                ElementCount = elementIds.Count,
                FilteredByNames = hasFilter ? paramFilter.ToList() : null,
                Results = results
            };
        }

        #endregion

        #region get_model_warnings

        private object GetModelWarnings(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;
            string categoryFilter = parameters["category"]?.Value<string>();

            IList<FailureMessage> warnings;
            try
            {
                warnings = doc.GetWarnings();
            }
            catch (Exception ex)
            {
                throw new Exception($"無法取得模型警告：{ex.Message}");
            }

            var result = new List<object>();

            foreach (var w in warnings)
            {
                var failingIds = w.GetFailingElements().ToList();
                var additionalIds = w.GetAdditionalElements().ToList();

                // 品類篩選
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    bool match = failingIds.Any(id =>
                    {
                        Element e = doc.GetElement(id);
                        return e?.Category?.Name.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    });
                    if (!match) continue;
                }

                string desc;
                try { desc = w.GetDescriptionText(); }
                catch { desc = "(無法取得描述)"; }

                result.Add(new
                {
                    Description = desc,
                    Severity = w.GetSeverity().ToString(),
                    FailingElementIds = failingIds.Select(id => id.GetIdValue()).ToList(),
                    AdditionalElementIds = additionalIds.Select(id => id.GetIdValue()).ToList()
                });
            }

            return new
            {
                TotalWarnings = warnings.Count,
                FilteredCount = result.Count,
                CategoryFilter = categoryFilter ?? "(全部)",
                Warnings = result
            };
        }

        #endregion

        #region get_workset_info

        private object GetWorksetInfo(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            if (!doc.IsWorkshared)
                return new
                {
                    IsWorkshared = false,
                    Message = "此文件未啟用工作集（Worksharing not enabled）"
                };

            // 取得所有使用者工作集
            var allWorksets = new FilteredWorksetCollector(doc)
                .OfKind(WorksetKind.UserWorkset)
                .Select(w => new
                {
                    WorksetId = w.Id.IntegerValue,
                    Name = w.Name,
                    Owner = w.Owner,
                    IsOpen = w.IsOpen,
                    IsEditable = w.IsEditable
                })
                .OrderBy(w => w.Name)
                .ToList();

            var elementIds = (parameters["elementIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();

            if (elementIds != null && elementIds.Count > 0)
            {
                // 同時傳回各元素所屬工作集
                WorksetTable wst = doc.GetWorksetTable();

                var elementWorksets = elementIds.Select(eid =>
                {
                    Element elem = doc.GetElement(new ElementId(eid));
                    if (elem == null) return (object)new { ElementId = eid, Error = "找不到元素" };

                    WorksetId wsId = elem.WorksetId;
                    string wsName = "(未知)";
                    try
                    {
                        Workset ws = wst.GetWorkset(wsId);
                        wsName = ws?.Name ?? "(未知)";
                    }
                    catch { }

                    return (object)new
                    {
                        ElementId = eid,
                        ElementName = elem.Name,
                        Category = elem.Category?.Name,
                        WorksetId = wsId.IntegerValue,
                        WorksetName = wsName
                    };
                }).ToList();

                return new
                {
                    IsWorkshared = true,
                    AllWorksets = allWorksets,
                    ElementWorksets = elementWorksets
                };
            }

            return new
            {
                IsWorkshared = true,
                WorksetCount = allWorksets.Count,
                Worksets = allWorksets
            };
        }

        #endregion

        #region get_element_graphic_overrides

        private object GetElementGraphicOverrides(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var elementIds = (parameters["elementIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (elementIds == null || elementIds.Count == 0)
                throw new Exception("請提供 elementIds");

            IdType viewIdVal = parameters["viewId"]?.Value<IdType>() ?? 0;
            View view;
            if (viewIdVal > 0)
            {
                view = doc.GetElement(viewIdVal.ToElementId()) as View;
                if (view == null) throw new Exception($"找不到視圖 ID: {viewIdVal}");
            }
            else
            {
                view = _uiApp.ActiveUIDocument.ActiveView;
            }

            var results = new List<object>();

            foreach (var eid in elementIds)
            {
                Element elem = doc.GetElement(new ElementId(eid));
                if (elem == null)
                {
                    results.Add(new { ElementId = eid, Error = "找不到元素" });
                    continue;
                }

                OverrideGraphicSettings ogs = view.GetElementOverrides(new ElementId(eid));

                string projLineColor = ogs.ProjectionLineColor.IsValid
                    ? $"R:{ogs.ProjectionLineColor.Red},G:{ogs.ProjectionLineColor.Green},B:{ogs.ProjectionLineColor.Blue}"
                    : "(無覆寫)";

                string cutLineColor = ogs.CutLineColor.IsValid
                    ? $"R:{ogs.CutLineColor.Red},G:{ogs.CutLineColor.Green},B:{ogs.CutLineColor.Blue}"
                    : "(無覆寫)";

                string surfFgColor = ogs.SurfaceForegroundPatternColor.IsValid
                    ? $"R:{ogs.SurfaceForegroundPatternColor.Red},G:{ogs.SurfaceForegroundPatternColor.Green},B:{ogs.SurfaceForegroundPatternColor.Blue}"
                    : "(無覆寫)";

                results.Add(new
                {
                    ElementId = eid,
                    ElementName = elem.Name,
                    Category = elem.Category?.Name,
                    ProjectionLineColor = projLineColor,
                    CutLineColor = cutLineColor,
                    SurfaceForegroundColor = surfFgColor,
                    SurfaceTransparency = ogs.Transparency,
                    IsHalftone = ogs.Halftone,
                    DetailLevel = ogs.DetailLevel.ToString()
                });
            }

            return new
            {
                ViewId = view.Id.GetIdValue(),
                ViewName = view.Name,
                ElementCount = elementIds.Count,
                Results = results
            };
        }

        #endregion

        #region get_type_material_layers

        private object GetTypeMaterialLayers(JObject parameters)
        {
            Document doc = _uiApp.ActiveUIDocument.Document;

            var typeIds = (parameters["typeIds"] as JArray)
                ?.Select(t => t.Value<IdType>()).ToList();
            if (typeIds == null || typeIds.Count == 0)
                throw new Exception("請提供 typeIds");

            var results = new List<object>();

            foreach (var tid in typeIds)
            {
                Element typeElem = doc.GetElement(new ElementId(tid));
                if (typeElem == null)
                {
                    results.Add(new { TypeId = tid, Error = "找不到類型元素" });
                    continue;
                }

                string typeName = typeElem.Name;
                string typeCategory = typeElem.Category?.Name ?? "Unknown";

                CompoundStructure cs = null;
                try
                {
                    if (typeElem is WallType wt)
                        cs = wt.GetCompoundStructure();
                    else if (typeElem is FloorType ft)
                        cs = ft.GetCompoundStructure();
                    else if (typeElem is RoofType rt)
                        cs = rt.GetCompoundStructure();
                    else
                    {
                        results.Add(new
                        {
                            TypeId = tid,
                            TypeName = typeName,
                            Category = typeCategory,
                            Error = "此類型不支援圖層查詢（僅支援 WallType / FloorType / RoofType）"
                        });
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new { TypeId = tid, TypeName = typeName, Error = ex.Message });
                    continue;
                }

                if (cs == null)
                {
                    results.Add(new
                    {
                        TypeId = tid,
                        TypeName = typeName,
                        Category = typeCategory,
                        Layers = new List<object>(),
                        Note = "此類型無複合結構（CompoundStructure）"
                    });
                    continue;
                }

                var layers = new List<object>();
                IList<CompoundStructureLayer> csl = cs.GetLayers();

                for (int i = 0; i < csl.Count; i++)
                {
                    CompoundStructureLayer layer = csl[i];
                    Material mat = null;
                    if (layer.MaterialId != ElementId.InvalidElementId)
                        mat = doc.GetElement(layer.MaterialId) as Material;

                    layers.Add(new
                    {
                        LayerIndex = i,
                        Function = layer.Function.ToString(),
                        ThicknessMm = Math.Round(layer.Width * 304.8, 2),
                        MaterialId = layer.MaterialId?.GetIdValue(),
                        MaterialName = mat?.Name ?? "(無材料)"
                    });
                }

                results.Add(new
                {
                    TypeId = tid,
                    TypeName = typeName,
                    Category = typeCategory,
                    TotalThicknessMm = Math.Round(cs.GetWidth() * 304.8, 2),
                    LayerCount = layers.Count,
                    Layers = layers
                });
            }

            return new
            {
                TypeCount = typeIds.Count,
                Results = results
            };
        }

        #endregion
    }
}
