using Autodesk.Revit.DB;



using Autodesk.Revit.DB.Architecture;



using Autodesk.Revit.UI;



using Newtonsoft.Json.Linq;



using System;



using System.Collections.Generic;



using System.Globalization;



using System.IO;



using System.Linq;



using System.Threading;







namespace RevitMCP;







/// <summary>



/// IExternalEventHandler runs on Revit's UI thread.



/// All Revit API calls MUST happen here.



/// </summary>



public partial class RevitEventHandler : IExternalEventHandler



{



    public sealed class Request



    {



        public Request(string action, JObject payload)



        {



            Action = action;



            Payload = payload;



        }







        public string Action { get; }



        public JObject Payload { get; }



        public JObject? Result { get; set; }



        public string? Error { get; set; }



        public ManualResetEventSlim Done { get; } = new(false);



    }







    public static readonly SemaphoreSlim RequestGate = new(1, 1);
    private readonly System.Collections.Concurrent.ConcurrentQueue<Request> _queue = new();
    private readonly object _processLock = new();

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private const uint WM_NULL = 0x0000;
    private static IntPtr _cachedMainWindowHandle = IntPtr.Zero;

    public static void WakeUpRevit()
    {
        try
        {
            if (_cachedMainWindowHandle == IntPtr.Zero)
            {
                _cachedMainWindowHandle = App.UiApp?.MainWindowHandle ?? IntPtr.Zero;
                if (_cachedMainWindowHandle == IntPtr.Zero)
                {
                    _cachedMainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                }
            }
            if (_cachedMainWindowHandle != IntPtr.Zero)
            {
                PostMessage(_cachedMainWindowHandle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
            }
        }
        catch { }
    }

    public Request SetRequest(string action, JObject payload)
    {
        var request = new Request(action, payload);
        _queue.Enqueue(request);
        WakeUpRevit();
        return request;
    }

    public static bool WaitForResult(Request request, TimeSpan timeout) => request.Done.Wait(timeout);

    public static (JObject? result, string? error) GetResult(Request request) => (request.Result, request.Error);

    public string GetName() => "RevitMCPHandler";

    public void OnIdling(object? sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
    {
        if (sender is not UIApplication app) return;

        if (!_queue.IsEmpty)
        {
            ProcessQueue(app);
            if (!_queue.IsEmpty)
            {
                e.SetRaiseWithoutDelay();
            }
        }
    }

    public void Execute(UIApplication app)
    {
        ProcessQueue(app);
    }

    public void ProcessQueue(UIApplication app)
    {
        lock (_processLock)
        {
            while (_queue.TryDequeue(out var request))
            {
                try
                {
                    OpenDocumentsCache.SyncFromRevit(app);
                    DispatchInternal(app, null, request);
                }
                catch (Exception ex)
                {
                    request.Result = RevitMcpException.FromException(ex);
                    request.Error = ex.Message;
                }
                finally
                {
                    request.Done.Set();
                }
            }
        }
    }

    public void DispatchInternal(UIApplication app, Document? doc, Request request)



    {



        var action = request.Action;



        var payload = request.Payload;







        if (doc == null && action != "documents/list" && action != "documents/open" && action != "documents/new" && action != "documents/new-project" && action != "ui/state" && action != "ui/bring-to-front" && action != "dynamo/list-scripts" && action != "security/get" && action != "security/set" && action != "lca/inies-lookup")



        {



            doc = DocumentResolver.ResolveDocument(app, payload);



        }







        request.Result = action switch



        {



            // Core & Existing



            "model/info" => GetModelInfo(doc!),



            "model/warnings" => GetModelWarnings(doc!),



            "selection/get" => GetCurrentSelection(app.ActiveUIDocument),



            "selection/set" => SetCurrentSelection(app.ActiveUIDocument, payload),



            "categories/list" => ListCategories(doc!),



            "elements/query" => QueryElementsEnhanced(doc!, payload),



            "elements/count" => CountElements(doc!, payload),



            "elements/parameters" => GetParameters(doc!, payload),



            "elements/create-wall" => CreateWall(doc!, payload),



            "elements/create-floor" => CreateFloor(doc!, payload),



            "elements/place-instance" => PlaceInstanceEnhanced(doc!, payload),



            "elements/set-parameter" => SetParameter(doc!, payload),



            "elements/delete" => DeleteElement(doc!, payload),



            "elements/move" => MoveElement(doc!, payload),



            "elements/isolate" => IsolateElements(app.ActiveUIDocument, payload),



            "views/list" => ListViews(doc!, payload),



            "views/activate" => ActivateView(app, doc!, payload),



            "views/reset-temporary-override" => ResetTemporaryOverride(app.ActiveUIDocument),



            "sheets/list" => ListSheets(doc!),



            "sheets/create" => CreateSheet(doc!, payload),



            "sheets/place-view" => PlaceViewOnSheet(doc!, payload),



            "levels/list" => ListLevels(doc!),



            "families/list" => ListFamiliesEnhanced(doc!, payload),



            "rooms/list" => ListRoomsEnhanced(doc!, payload),



            "rooms/create" => CreateRoom(doc!, payload),



            "export/pdf" => ExportPdfEnhanced(doc!, payload),



            "export/dwg" => ExportDwgEnhanced(doc!, payload),



            "export/dxf" => ExportDwgEnhanced(doc!, payload, true),



            "export/ifc" => ExportIfc(doc!, payload),



            "export/schedule" => ExportSchedule(doc!, payload),



            "commands/run" => RunCommand(app, payload),



            "commands/run-dynamo" => RunDynamo(app, payload),



            "takeoff/materials" => GetMaterialTakeoff(doc!, payload),



            "takeoff/export-csv" => ExportMaterialTakeoffCsv(doc!, payload),



            "takeoff/create-schedule" => CreateMaterialSchedule(doc!, payload),



            "clash/detect" => DetectClashes(doc!, payload),



            "clash/export-report" => ExportClashReport(doc!, payload),



            "revisions/issue-drawings" => IssueDrawings(doc!, payload),



            "revisions/history" => GetRevisionHistory(doc!),



            "rooms/finishes" => GetRoomFinishes(doc!, payload),



            "rooms/set-finishes-bulk" => SetRoomFinishesBulk(doc!, payload),



            "views/apply-template-bulk" => ApplyViewTemplateBulk(doc!, payload),



            "views/bulk-rename" => BulkRenameViews(doc!, payload),



            "views/create-plans-for-levels" => CreatePlanViewsForLevels(doc!, payload),



            "views/duplicate-bulk" => DuplicateViewsBulk(doc!, payload),



            "audit/families" => AuditFamilies(doc!, payload),



            "elements/batch-set-parameters" => BatchSetParameters(doc!, payload),



            "elements/import-csv-params" => ImportParametersFromCsv(doc!, payload),



            "sheets/auto-populate" => AutoPopulateSheets(doc!, payload),



            "views/zoom-elements" => ZoomToElements(app.ActiveUIDocument, payload),



            "elements/set-color" => ColorElements(doc!, payload),



            "elements/reset-color" => ResetColorOverrides(doc!, payload),



            "grids/create" => CreateGrid(doc!, payload),



            "phases/list" => ListPhases(doc!),



            "links/list" => ListLinks(doc!),



            "elements/rotate" => RotateElement(doc!, payload),



            "export/image" => ExportViewImageEnhanced(doc!, payload),



            "model/save" => SaveDocument(doc!, payload),



            "worksets/list" => ListWorksets(doc!),



            "revisions/list" => ListRevisions(doc!),



            "materials/list" => ListMaterials(doc!),



            "views/list-templates" => ListViewTemplates(doc!),



            "views/create-section-box" => CreateSectionBox(app, doc!, payload),



            "elements/find-by-parameter" => FindElementsByParameter(doc!, payload),



            "parameters/copy-values" => CopyParameterValues(doc!, payload),



            "sheets/duplicate" => DuplicateSheet(doc!, payload),



            "model/purge-unused" => PurgeUnusedEnhanced(doc!, payload),



            "annotations/create-detail-line" => CreateDetailLine(doc!, payload),







            // ── Section 4.A (Code & Transactions) ──



            "code/csharp" => ExecuteCSharp(app, doc!, payload),



            "code/python" => ExecutePython(app, doc!, payload),



            "batch/execute" => BatchExecute(app, doc!, payload),



            "transactions/undo" => UndoTransaction(app, doc!, payload),



            "transactions/redo" => RedoTransaction(app, doc!, payload),



            "transactions/history" => GetTransactionHistory(doc!, payload),







            // ── Section 4.B (Documents) ──



            "documents/list" => ListOpenDocuments(app),
            "cloud/info" => GetCloudInfo(doc!),
            "cloud/sync" => SyncWithCentral(doc!, payload),
            "diagnostics/info" => GetDiagnostics(app, doc!),
            "snapshots/create" => CreateSnapshot(doc!, payload),
            "snapshots/list" => ListSnapshots(doc!),
            "branch/create" => BranchCreate(doc!, payload),
            "branch/list" => BranchList(doc!),
            "branch/compare" => BranchCompare(doc!, payload),
            "security/get" => GetSecurityMode(),
            "security/set" => SetSecurityMode(payload),
            "bim/classification" => GetBimClassification(doc!, payload),
            "documents/new" => NewProject(app, payload),
            "documents/new-project" => NewProject(app, payload),



            "documents/activate" => ActivateDocument(app, payload),



            "documents/open" => OpenDocument(app, payload),



            "documents/close" => CloseDocument(app, payload),



            "documents/save-as" => SaveAsDocument(doc!, payload),



            "documents/set-project-info" => SetProjectInfo(doc!, payload),







            // ── Section 4.C (Geometry & Spatial) ──



            "elements/location" => GetElementLocation(doc!, payload),



            "elements/geometry" => GetElementGeometry(doc!, payload),



            "elements/in-region" => GetElementsInRegion(doc!, payload),



            "geometry/measure-distance" => MeasureDistance(doc!, payload),



            "geometry/height-at-point" => GetHeightAtPoint(doc!, payload),



            "geometry/check-clearance" => CheckClearance(doc!, payload),



            "walls/openings" => GetWallOpenings(doc!, payload),



            "rooms/at-point" => GetRoomAtPoint(doc!, payload),







            // ── Section 4.D & 4.E (Element modifications & Types) ──



            "elements/set-location" => SetElementLocation(doc!, payload),



            "elements/change-type" => ChangeElementType(doc!, payload),



            "elements/copy" => CopyElements(doc!, payload),



            "elements/mirror" => MirrorElements(doc!, payload),



            "elements/align" => AlignElements(doc!, payload),



            "elements/copy-between-docs" => CopyElementsBetweenDocuments(app, payload),



            "elements/flip" => FlipElement(doc!, payload),



            "elements/hosted-set-position" => HostedSetPosition(doc!, payload),



            "elements/set-pinned" => SetElementPinned(doc!, payload),



            "elements/join-geometry" => JoinGeometry(doc!, payload),



            "elements/group" => GroupElements(doc!, payload),



            "elements/ungroup" => UngroupElements(doc!, payload),



            "types/duplicate" => DuplicateType(doc!, payload),



            "types/set-parameters" => SetTypeParameters(doc!, payload),



            "types/get-parameters" => GetTypeParameters(doc!, payload),



            "types/create-window-type" => CreateWindowOrDoorType(doc!, payload, false),



            "types/create-door-type" => CreateWindowOrDoorType(doc!, payload, true),



            "elements/create-directshape" => CreateDirectShape(doc!, payload),







            // ── Section 4.F (Walls, Levels, etc.) ──



            "walls/modify" => ModifyWall(doc!, payload),



            "walls/split" => SplitWall(doc!, payload),



            "walls/attach" => AttachWall(doc!, payload),



            "levels/create" => CreateLevel(doc!, payload),



            "levels/modify" => ModifyLevel(doc!, payload),







            // ── Section 4.G (Rooms & Zoning) ──



            "rooms/create-separation-lines" => CreateRoomSeparationLines(doc!, payload),



            "rooms/place-at-point" => PlaceRoomAtPoint(doc!, payload),



            "rooms/delete-unplaced" => DeleteUnplacedRooms(doc!, payload),



            "rooms/tag-rooms" => TagRooms(doc!, payload),







            // ── Section 4.H (Views) ──



            "views/create-section" => CreateSectionView(doc!, payload),



            "views/create-3d" => Create3DView(doc!, payload),



            "views/duplicate" => DuplicateView(doc!, payload),



            "views/set-properties" => SetViewProperties(doc!, payload),



            "views/set-category-visibility" => SetCategoryVisibility(doc!, payload),



            "views/hide-elements" => HideElementsInView(doc!, payload),







            // ── Section 4.I (Annotations) ──



            "annotations/create-linear-dimension" => CreateLinearDimension(doc!, payload),



            "annotations/create-text-note" => CreateTextNote(doc!, payload),



            "annotations/create-detail-curve" => CreateDetailCurve(doc!, payload),







            // ── Section 4.J (Sheets) ──



            "sheets/get-viewports" => GetViewports(doc!, payload),



            "sheets/move-viewport" => MoveViewport(doc!, payload),







            // ── Section 4.K (Schedules) ──



            "schedules/create" => CreateSchedule(doc!, payload),



            "schedules/get-data" => GetScheduleData(doc!, payload),







            // ── Section 4.L (Materials) ──



            "materials/create" => CreateMaterial(doc!, payload),



            "materials/assign" => AssignMaterial(doc!, payload),







            // ── Section 4.M (Variants & Design Options) ──



            "design-options/list" => GetDesignOptions(doc!),



            "design-options/create" => CreateDesignOption(doc!, payload),



            "design-options/move-elements" => MoveElementsToDesignOption(doc!, payload),



            "design-options/set-view-option" => SetViewDesignOption(doc!, payload),



            "phases/set-element-phase" => SetElementPhase(doc!, payload),







            // ── Section 4.N (Imports & Exports) ──



            "import/cad" => ImportCad(doc!, payload),



            "import/get-cad-geometry" => GetCadGeometry(doc!, payload),



            "import/create-walls-from-cad" => CreateWallsFromCad(doc!, payload),



            "import/image" => ImportImage(doc!, payload),







            // ── Section 4.O & 4.P (UI & Dynamo) ──



            "ui/state" => GetUiState(app),



            "ui/bring-to-front" => BringToFront(app, payload),



            "dynamo/list-scripts" => ListDynamoScripts(payload),







            // ── Additional v2.1 Routes ──



            "ui/capture" => CaptureUi(app, payload),



            "system/get-log" => GetLog(payload),



            "dialogs/dismiss" => DismissDialogs(payload),



            "families/load" => LoadFamily(doc!, payload),



            "families/list-library" => ListFamilyLibrary(payload),



            "roofs/create-footprint" => CreateRoofFootprint(doc!, payload),



            "ceilings/create" => CreateCeiling(doc!, payload),



            "railings/create" => CreateRailing(doc!, payload),



            "openings/create" => CreateOpening(doc!, payload),



            "ladders/create" => CreateLadder(doc!, payload),



            "annotations/create-spot-elevation" => CreateSpotElevation(doc!, payload),



            "annotations/create-tag" => CreateTag(doc!, payload),



            "annotations/tag-all" => TagAll(doc!, payload),



            "annotations/create-filled-region" => CreateFilledRegion(doc!, payload),



            "materials/paint-face" => PaintFace(doc!, payload),



            // Nouveaux outils architecturaux v2.1.0

            "roofs/create-extrusion" => CreateRoofExtrusion(doc!, payload),

            "roofs/modify" => ModifyRoof(doc!, payload),

            "floors/modify-boundary" => ModifyFloorBoundary(doc!, payload),

            "stairs/create" => CreateStair(doc!, payload),

            "families/create-simple" => CreateSimpleFamily(app, doc!, payload),

            "families/edit-parameters" => EditFamilyParameters(doc!, payload),

            "views/create-elevations" => CreateElevationViews(doc!, payload),

            "views/create-drafting" => CreateDraftingView(doc!, payload),

            "views/filters/create" => CreateViewFilter(doc!, payload),

            "views/filters/apply" => ApplyViewFilter(doc!, payload),

            "views/create-template" => CreateViewTemplateFromView(doc!, payload),

            "views/delete" => DeleteViews(doc!, payload),

            "views/close-inactive" => CloseInactiveViews(app),

            "annotations/auto-dimension-walls" => AutoDimensionWalls(doc!, payload),

            "annotations/dimension-openings" => DimensionOpenings(doc!, payload),

            "lines/create-style" => CreateLineStyle(doc!, payload),

            "annotations/place-symbol" => PlaceAnnotationSymbol(doc!, payload),

            "details/place-component" => PlaceDetailComponent(doc!, payload),

            "views/create-legend" => CreateLegendView(doc!, payload),

            "legends/place-component" => PlaceLegendComponent(doc!, payload),

            "colors/create-scheme" => CreateColorFillScheme(doc!, payload),

            "colors/apply-legend" => ApplyColorFill(doc!, payload),

            "areas/create-plan" => CreateAreaPlan(doc!, payload),

            "areas/create" => CreateArea(doc!, payload),

            "sheets/change-titleblock" => ChangeTitleBlock(doc!, payload),

            "sheets/set-parameters" => SetSheetParameters(doc!, payload),

            "viewports/set-type" => SetViewportType(doc!, payload),

            "sheets/place-schedule" => PlaceScheduleOnSheet(doc!, payload),

            "viewports/align" => AlignViewports(doc!, payload),

            "views/render" => RenderView(doc!, payload),

            "dynamo/run-inline" => RunDynamoInline(app, doc!, payload),
            "view/screenshot-active" => ScreenshotActiveView(doc!, payload),
            "audit/pmr" => AuditPmrCompliance(doc!, payload),
            "generate/spatial-layout" => GenerateSpatialLayout(doc!, payload),
            "proactive/alerts" => GetProactiveAlerts(doc!, payload),
                        "proactive/clear" => ClearProactiveAlerts(doc!, payload),

            // ── Holographic DirectContext3D HUD & Ghosting ──
            "overlay/ghost-preview" => PreviewGhostGeometry(app, doc!, payload),
            "overlay/clash-heatmap" => OverlayClashHeatmap(app, doc!, payload),
            "overlay/clear" => ClearHolographicOverlay(app, doc!, payload),
            "overlay/status" => GetHolographicStatus(app, doc!, payload),

            // ── Interactive 3D Web Viewer Three.js ──
            "view/export-interactive-3d" => ExportInteractive3DViewer(doc!, payload),

            // ── Real-time Voice Control Speech-to-BIM ──
            "voice/execute-command" => ExecuteVoiceCommand(app, doc!, payload),

            // ── Bilan Carbone ACV & RE2020 INIES ──
            "lca/carbon-footprint" => CalculateLcaCarbonFootprint(doc!, payload),
            "lca/low-carbon-alternatives" => SuggestLowCarbonAlternatives(doc!, payload),
                        "lca/inies-lookup" => LookupIniesDatabase(payload),

            // ── Auto-Healing Clash MEP ──
            "mep/auto-resolve-clash" => AutoResolveClash(doc!, payload),
            "mep/plan-reroute" => PlanMepReroute(doc!, payload),

            // ── Topologie Générative & Floorplans ──
            "generate/complete-floorplan" => GenerateCompleteFloorplan(doc!, payload),

            // ── Estimatif Budgétaire CCTP Live ──
            "budget/cctp-takeoff" => ComputeLiveCctpBudget(doc!, payload),

            // ── Mémoire Partagée & Live Deltas ──
            "mesh/export-shared-memory" => ExportFastMeshBinary(doc!, payload),
            "deltas/get-recent" => LiveDeltaMonitor.GetDeltas(payload["sinceSequence"]?.Value<long>() ?? 0),







            _ => throw new NotSupportedException($"Unknown action: {action}")



        };







        if (request.Result != null)
        {
            if (!request.Result.ContainsKey("success"))
            {
                request.Result["success"] = true;
            }
            if (doc != null && !request.Result.ContainsKey("documentTitle"))
            {
                try
                {
                    if (doc.IsValidObject)
                        request.Result["documentTitle"] = doc.Title;
                }
                catch { }
            }
        }




    }







    // ─── Helpers ─────────────────────────────────────────────────────────────







    private static long GetLong(JObject p, string key) =>



        p[key]?.Value<long>() ?? throw new RevitMcpException("INVALID_ARGUMENT", $"Missing required parameter: {key}");







    private static int GetInt(JObject p, string key) =>



        (int)GetLong(p, key);







    private static string GetStr(JObject p, string key) =>



        p[key]?.Value<string>() ?? throw new ArgumentException($"Missing required parameter: {key}");







    private static double MmToFt(double mm) => mm / 304.8;







    private static bool TrySetParameterValue(Parameter param, JToken value, out string? error)



    {



        error = null;







        if (param.IsReadOnly)



        {



            error = "read-only";



            return false;



        }







        try



        {



            switch (param.StorageType)



            {



                case StorageType.String:



                    param.Set(value.Type == JTokenType.Null ? "" : value.Value<string>());



                    return true;



                case StorageType.Integer:



                    if (value.Type == JTokenType.Boolean)



                    {



                        param.Set(value.Value<bool>() ? 1 : 0);



                        return true;



                    }



                    param.Set(value.Value<int>());



                    return true;



                case StorageType.Double:



                    if (value.Type == JTokenType.String)



                    {



                        var text = value.Value<string>() ?? "";



                        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))



                        {



                            error = $"cannot parse double from '{text}'";



                            return false;



                        }



                        param.Set(parsed);



                        return true;



                    }



                    param.Set(value.Value<double>());



                    return true;



                case StorageType.ElementId:



                    param.Set(new ElementId(value.Value<int>()));



                    return true;



                default:



                    error = $"unsupported storage type {param.StorageType}";



                    return false;



            }



        }



        catch (Exception ex)



        {



            error = ex.Message;



            return false;



        }



    }







    private static T WithTransaction<T>(Document doc, string name, Func<T> action, JObject? payload = null)
    {
        if (ConfigManager.IsReadOnly)
        {
            throw new InvalidOperationException($"Impossible d'exécuter l'action '{name}' : le mode sécurisé READ-ONLY est actif.");
        }

        if (doc.IsModifiable)
        {
            throw new InvalidOperationException($"Impossible de démarrer la transaction '{name}' : le document est déjà en cours de modification dans une autre transaction.");
        }

        var isDryRun = payload?["dryRun"]?.Value<bool>() ?? false;
        var txName = name.StartsWith("MCP:", StringComparison.OrdinalIgnoreCase) ? name : $"MCP: {name}";

        using var tx = new Transaction(doc, txName);
        SafeFailurePreprocessor? failurePreprocessor = null;

        if (!string.Equals(TransactionRunner.DialogPolicy, "off", StringComparison.OrdinalIgnoreCase))
        {
            var options = tx.GetFailureHandlingOptions();
            failurePreprocessor = new SafeFailurePreprocessor();
            options.SetFailuresPreprocessor(failurePreprocessor);
            options.SetClearAfterRollback(true);
            options.SetForcedModalHandling(false);
            tx.SetFailureHandlingOptions(options);
        }

        tx.Start();
        try
        {
            var result = action();
            if (isDryRun)
            {
                tx.RollBack();
                if (result is JObject dryObj)
                {
                    dryObj["dryRun"] = true;
                    if (!dryObj.ContainsKey("success"))
                        dryObj["success"] = true;
                    if (failurePreprocessor != null)
                        dryObj["bimWarnings"] = new JArray(failurePreprocessor.Warnings);
                }
                return result;
            }

            var status = tx.Commit();
            if (status != TransactionStatus.Committed)
            {
                var errorMsg = (failurePreprocessor != null && failurePreprocessor.Errors.Count > 0)
                    ? string.Join("; ", failurePreprocessor.Errors)
                    : $"La transaction Revit '{txName}' n'a pas pu être validée (statut: {status}).";

                throw new InvalidOperationException(errorMsg);
            }

            if (result is JObject jObj)
            {
                if (!jObj.ContainsKey("success"))
                    jObj["success"] = true;
                if (failurePreprocessor != null)
                    jObj["bimWarnings"] = new JArray(failurePreprocessor.Warnings);
            }

            return result;
        }
        catch (Exception ex)
        {
            if (tx.GetStatus() == TransactionStatus.Started)
            {
                try { tx.RollBack(); } catch { }
            }

            if (failurePreprocessor != null && failurePreprocessor.Errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("; ", failurePreprocessor.Errors), ex);
            }

            throw;
        }
    }

    // ─── Read / Query ─────────────────────────────────────────────────────────







    private static JObject GetModelInfo(Document doc)



    {



        var info = doc.ProjectInformation;



        return new JObject



        {



            ["filePath"]      = doc.PathName,



            ["title"]         = doc.Title,



            ["projectName"]   = info?.Name,



            ["projectNumber"] = info?.Number,



            ["author"]        = info?.Author,



            ["status"]        = info?.Status,



            ["isWorkshared"]  = doc.IsWorkshared,



            ["isReadOnly"]    = doc.IsReadOnly,



            ["activeView"]    = doc.ActiveView?.Name,



            ["activeViewId"]  = doc.ActiveView?.Id.Value,



            ["revitVersion"]  = doc.Application.VersionNumber,



            ["revitName"]     = doc.Application.VersionName,



            ["revitBuild"]    = doc.Application.VersionBuild,



        };



    }







    private static JObject QueryElements(Document doc, JObject p)



    {



        var category   = p["category"]?.Value<string>();



        var familyName = p["familyName"]?.Value<string>();



        var levelName  = p["levelName"]?.Value<string>();



        var viewName   = p["viewName"]?.Value<string>();



        var limit      = p["limit"]?.Value<int>() ?? 100;







        FilteredElementCollector col;







        if (viewName != null)



        {



            var view = new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .FirstOrDefault(v => v.Name == viewName)



                ?? throw new ArgumentException($"View '{viewName}' not found.");



            col = new FilteredElementCollector(doc, view.Id);



        }



        else



        {



            col = new FilteredElementCollector(doc);



        }







        col = col.WhereElementIsNotElementType();







        if (category != null)



        {



            var bic = GetBuiltInCategory(category);



            col = col.OfCategory(bic);



        }







        var elements = col.ToElements()



            .Where(e =>



            {



                if (familyName != null && e is FamilyInstance fi && fi.Symbol.FamilyName != familyName)



                    return false;



                if (levelName != null)



                {



                    var lvlParam = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)



                                 ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM);



                    if (lvlParam?.AsValueString() != levelName) return false;



                }



                return true;



            })



            .Take(limit)



            .Select(e => new JObject



            {



                ["id"]       = e.Id.Value,



                ["uniqueId"] = e.UniqueId,



                ["name"]     = e.Name,



                ["category"] = e.Category?.Name,



                ["family"]   = (e is FamilyInstance fi2) ? fi2.Symbol.FamilyName : null,



                ["typeName"] = doc.GetElement(e.GetTypeId())?.Name,



            });







        return new JObject { ["elements"] = new JArray(elements) };



    }







    private static JObject GetParameters(Document doc, JObject p)



    {



        var id = new ElementId(GetInt(p, "elementId"));



        var el = doc.GetElement(id) ?? throw new ArgumentException($"Element {id.Value} not found.");







        var parms = el.Parameters.Cast<Parameter>()



            .Select(param => new JObject



            {



                ["name"]       = param.Definition.Name,



                ["value"]      = param.StorageType switch



                {



                    StorageType.String  => param.AsString(),



                    StorageType.Double  => param.AsDouble(),



                    StorageType.Integer => param.AsInteger(),



                    StorageType.ElementId => param.AsElementId()?.Value,



                    _ => null



                },



                ["valueString"]= param.AsValueString(),



                ["isReadOnly"] = param.IsReadOnly,



                ["storageType"]= param.StorageType.ToString(),



            });







        return new JObject { ["parameters"] = new JArray(parms) };



    }







    private static JObject ListViews(Document doc, JObject p)



    {



        var typeFilter = p["viewType"]?.Value<string>();



        var views = new FilteredElementCollector(doc)



            .OfClass(typeof(View))



            .Cast<View>()



            .Where(v => !v.IsTemplate)



            .Where(v => typeFilter == null || v.ViewType.ToString() == typeFilter)



            .Select(v => new JObject



            {



                ["id"]       = v.Id.Value,



                ["name"]     = v.Name,



                ["viewType"] = v.ViewType.ToString(),



                ["scale"]    = v.Scale,



            });







        return new JObject { ["views"] = new JArray(views) };



    }







    private static JObject ListSheets(Document doc)



    {



        var sheets = new FilteredElementCollector(doc)



            .OfClass(typeof(ViewSheet))



            .Cast<ViewSheet>()



            .Select(s =>



            {



                var viewports = s.GetAllViewports()



                    .Select(vpId => doc.GetElement(vpId) as Viewport)



                    .Where(vp => vp != null)



                    .Select(vp => new JObject



                    {



                        ["viewId"]   = vp!.ViewId.Value,



                        ["viewName"] = doc.GetElement(vp.ViewId)?.Name,



                    });







                return new JObject



                {



                    ["id"]          = s.Id.Value,



                    ["sheetNumber"] = s.SheetNumber,



                    ["name"]        = s.Name,



                    ["views"]       = new JArray(viewports),



                };



            });







        return new JObject { ["sheets"] = new JArray(sheets) };



    }







    private static JObject ListLevels(Document doc)



    {



        var levels = new FilteredElementCollector(doc)



            .OfClass(typeof(Level))



            .Cast<Level>()



            .OrderBy(l => l.Elevation)



            .Select(l => new JObject



            {



                ["id"]         = l.Id.Value,



                ["name"]       = l.Name,



                ["elevationFt"]= l.Elevation,



                ["elevationMm"]= l.Elevation * 304.8,



            });







        return new JObject { ["levels"] = new JArray(levels) };



    }







    private static JObject ListFamilies(Document doc, JObject p)



    {



        var category = p["category"]?.Value<string>();



        var families = new FilteredElementCollector(doc)



            .OfClass(typeof(Family))



            .Cast<Family>()



            .Where(f => category == null || f.FamilyCategory?.Name == category)



            .Select(f => new JObject



            {



                ["id"]       = f.Id.Value,



                ["name"]     = f.Name,



                ["category"] = f.FamilyCategory?.Name,



                ["types"]    = new JArray(



                    f.GetFamilySymbolIds()



                     .Select(id => doc.GetElement(id))



                     .Where(e => e != null)



                     .Select(e => new JObject { ["id"] = e.Id.Value, ["name"] = e.Name })



                ),



            });







        return new JObject { ["families"] = new JArray(families) };



    }







    private static JObject ListRooms(Document doc, JObject p)



    {



        var levelName = p["levelName"]?.Value<string>();



        var rooms = new FilteredElementCollector(doc)



            .OfClass(typeof(SpatialElement))



            .OfType<Room>()



            .Where(r => levelName == null || r.Level?.Name == levelName)



            .Select(r => new JObject



            {



                ["id"]      = r.Id.Value,



                ["number"]  = r.Number,



                ["name"]    = r.Name,



                ["level"]   = r.Level?.Name,



                ["areaSqm"] = r.Area * 0.0929,



            });







        return new JObject { ["rooms"] = new JArray(rooms) };



    }







    // ─── Create / Modify ─────────────────────────────────────────────────────







    private static JObject CreateWall(Document doc, JObject p)



    {



        var startX    = p["startX"]?.Value<double>() ?? throw new ArgumentException("startX required");



        var startY    = p["startY"]?.Value<double>() ?? throw new ArgumentException("startY required");



        var endX      = p["endX"]?.Value<double>()   ?? throw new ArgumentException("endX required");



        var endY      = p["endY"]?.Value<double>()   ?? throw new ArgumentException("endY required");



        var levelName = GetStr(p, "levelName");



        var wallTypeName = p["wallTypeName"]?.Value<string>();



        var heightMm  = p["height"]?.Value<double>();







        return WithTransaction(doc, "RevitMCP: Create Wall", () =>



        {



            var level = new FilteredElementCollector(doc)



                .OfClass(typeof(Level))



                .Cast<Level>()



                .FirstOrDefault(l => l.Name == levelName)



                ?? throw new ArgumentException($"Level '{levelName}' not found.");







            WallType? wallType = null;



            if (wallTypeName != null)



            {



                wallType = new FilteredElementCollector(doc)



                    .OfClass(typeof(WallType))



                    .Cast<WallType>()



                    .FirstOrDefault(wt => wt.Name == wallTypeName)



                    ?? throw new ArgumentException($"Wall type '{wallTypeName}' not found.");



            }







            var line = Line.CreateBound(



                new XYZ(MmToFt(startX), MmToFt(startY), 0),



                new XYZ(MmToFt(endX),   MmToFt(endY),   0)



            );







            var wall = Wall.Create(doc, line, level.Id, false);







            if (wallType != null)



                wall.WallType = wallType;







            if (heightMm.HasValue)



            {



                var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);



                heightParam?.Set(MmToFt(heightMm.Value));



            }







            return new JObject { ["elementId"] = wall.Id.Value, ["success"] = true };



        }, p);



    }







    private static JObject PlaceInstance(Document doc, JObject p)



    {



        var familyName    = GetStr(p, "familyName");



        var typeName      = GetStr(p, "typeName");



        var x             = p["x"]?.Value<double>() ?? 0;



        var y             = p["y"]?.Value<double>() ?? 0;



        var z             = p["z"]?.Value<double>() ?? 0;



        var levelName     = GetStr(p, "levelName");



        var hostId        = p["hostElementId"]?.Value<int?>();



        var rotation      = p["rotation"]?.Value<double?>() ?? 0;







        return WithTransaction(doc, "RevitMCP: Place Instance", () =>



        {



            // Find symbol



            var symbol = new FilteredElementCollector(doc)



                .OfClass(typeof(FamilySymbol))



                .Cast<FamilySymbol>()



                .FirstOrDefault(s => s.FamilyName == familyName && s.Name == typeName)



                ?? throw new ArgumentException($"Family '{familyName}' / type '{typeName}' not found.");







            if (!symbol.IsActive) symbol.Activate();







            var level = new FilteredElementCollector(doc)



                .OfClass(typeof(Level))



                .Cast<Level>()



                .FirstOrDefault(l => l.Name == levelName)



                ?? throw new ArgumentException($"Level '{levelName}' not found.");







            var point = new XYZ(MmToFt(x), MmToFt(y), MmToFt(z));







            FamilyInstance instance;



            if (hostId.HasValue)



            {



                var host = doc.GetElement(new ElementId(hostId.Value)) as Element



                           ?? throw new ArgumentException($"Host element {hostId} not found.");



                instance = doc.Create.NewFamilyInstance(point, symbol, host, level,



                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);



            }



            else



            {



                instance = doc.Create.NewFamilyInstance(point, symbol, level,



                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);



            }







            if (rotation != 0)



            {



                var axis = Line.CreateBound(point, point + XYZ.BasisZ);



                ElementTransformUtils.RotateElement(doc, instance.Id, axis,



                    rotation * Math.PI / 180.0);



            }







            return new JObject { ["elementId"] = instance.Id.Value, ["success"] = true };



        }, p);



    }







    private static JObject SetParameter(Document doc, JObject p)



    {



        var id    = new ElementId(GetInt(p, "elementId"));



        var name  = GetStr(p, "parameterName");



        var value = p["value"] ?? throw new ArgumentException("value required");



        var el    = doc.GetElement(id) ?? throw new ArgumentException($"Element {id.Value} not found.");







        return WithTransaction(doc, "RevitMCP: Set Parameter", () =>



        {



            var param = el.LookupParameter(name)



                        ?? throw new ArgumentException($"Parameter '{name}' not found on element {id.Value}.");







            if (param.IsReadOnly)



                throw new InvalidOperationException($"Parameter '{name}' is read-only.");







            switch (param.StorageType)



            {



                case StorageType.String:  param.Set(value.Value<string>()); break;



                case StorageType.Integer: param.Set(value.Value<int>()); break;



                case StorageType.Double:  param.Set(value.Value<double>()); break;



                case StorageType.ElementId:



                    param.Set(new ElementId(value.Value<int>())); break;



                default:



                    throw new NotSupportedException($"StorageType {param.StorageType} not supported.");



            }







            return new JObject { ["success"] = true };



        }, p);



    }







    private static JObject DeleteElement(Document doc, JObject p)



    {



        var id = new ElementId(GetInt(p, "elementId"));



        return WithTransaction(doc, "RevitMCP: Delete Element", () =>



        {



            doc.Delete(id);



            return new JObject { ["success"] = true, ["deletedId"] = id.Value };



        }, p);



    }







    private static JObject MoveElement(Document doc, JObject p)



    {



        var id     = new ElementId(GetInt(p, "elementId"));



        var dx     = p["deltaX"]?.Value<double>() ?? 0;



        var dy     = p["deltaY"]?.Value<double>() ?? 0;



        var dz     = p["deltaZ"]?.Value<double>() ?? 0;







        return WithTransaction(doc, "RevitMCP: Move Element", () =>



        {



            var el = doc.GetElement(id) ?? throw new ArgumentException($"Element {id.Value} not found.");



            ElementTransformUtils.MoveElement(doc, id, new XYZ(MmToFt(dx), MmToFt(dy), MmToFt(dz)));



            return new JObject { ["success"] = true };



        }, p);



    }







    // ─── Sheets ──────────────────────────────────────────────────────────────







    private static JObject CreateSheet(Document doc, JObject p)



    {



        var number    = GetStr(p, "sheetNumber");



        var name      = GetStr(p, "sheetName");



        var tbName    = p["titleBlockName"]?.Value<string>();







        return WithTransaction(doc, "RevitMCP: Create Sheet", () =>



        {



            FamilySymbol? tb = null;



            if (tbName != null)



            {



                tb = new FilteredElementCollector(doc)



                    .OfCategory(BuiltInCategory.OST_TitleBlocks)



                    .OfClass(typeof(FamilySymbol))



                    .Cast<FamilySymbol>()



                    .FirstOrDefault(s => s.Name == tbName)



                    ?? throw new ArgumentException($"Title block '{tbName}' not found.");



            }







            var sheet = tb != null



                ? ViewSheet.Create(doc, tb.Id)



                : ViewSheet.Create(doc, ElementId.InvalidElementId);







            sheet.SheetNumber = number;



            sheet.Name = name;







            return new JObject { ["sheetId"] = sheet.Id.Value, ["success"] = true };



        }, p);



    }







    private static JObject PlaceViewOnSheet(Document doc, JObject p)



    {



        var sheetId = new ElementId(GetInt(p, "sheetId"));



        var viewId  = new ElementId(GetInt(p, "viewId"));



        var cx      = p["centerX"]?.Value<double>() ?? 0;



        var cy      = p["centerY"]?.Value<double>() ?? 0;







        return WithTransaction(doc, "RevitMCP: Place View on Sheet", () =>



        {



            var sheet = doc.GetElement(sheetId) as ViewSheet



                        ?? throw new ArgumentException("Sheet not found.");



            var view  = doc.GetElement(viewId)  as View



                        ?? throw new ArgumentException("View not found.");







            var vp = Viewport.Create(doc, sheet.Id, view.Id,



                new XYZ(MmToFt(cx), MmToFt(cy), 0));







            return new JObject { ["viewportId"] = vp.Id.Value, ["success"] = true };



        }, p);



    }







    private static JObject ActivateView(UIApplication app, Document doc, JObject p)



    {



        View? view = null;



        var viewId   = p["viewId"]?.Value<int?>();



        var viewName = p["viewName"]?.Value<string>();







        if (viewId.HasValue)



            view = doc.GetElement(new ElementId(viewId.Value)) as View;



        else if (viewName != null)



            view = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()



                       .FirstOrDefault(v => v.Name == viewName);







        if (view == null) throw new ArgumentException("View not found.");







        app.ActiveUIDocument.ActiveView = view;



        return new JObject { ["success"] = true, ["activatedView"] = view.Name };



    }







    // ─── Export ──────────────────────────────────────────────────────────────







    private static JObject ExportPdf(Document doc, JObject p)



    {



        var outputPath  = GetStr(p, "outputPath");



        var sheetIds    = p["sheetIds"]?.ToObject<int[]>();



        var viewIds     = p["viewIds"]?.ToObject<int[]>();



        var combine     = p["combineIntoOne"]?.Value<bool>() ?? false;



        var paperSize   = p["paperSize"]?.Value<string>() ?? "A1";







        Directory.CreateDirectory(outputPath);







        var opts = new PDFExportOptions



        {



            Combine = combine,



            PaperFormat = paperSize switch



            {



                "A0" => ExportPaperFormat.ISO_A0,



                "A2" => ExportPaperFormat.ISO_A2,



                "A3" => ExportPaperFormat.ISO_A3,



                "A4" => ExportPaperFormat.ISO_A4,



                _    => ExportPaperFormat.ISO_A1,



            },



        };







        var ids = new List<ElementId>();



        if (sheetIds != null) ids.AddRange(sheetIds.Select(i => new ElementId(i)));



        if (viewIds  != null) ids.AddRange(viewIds.Select(i => new ElementId(i)));







        if (ids.Count == 0)



        {



            // Export all sheets



            ids.AddRange(new FilteredElementCollector(doc)



                .OfClass(typeof(ViewSheet))



                .Select(e => e.Id));



        }







        doc.Export(outputPath, ids, opts);



        return new JObject { ["success"] = true, ["outputPath"] = outputPath };



    }







    private static JObject ExportDwg(Document doc, JObject p)



    {



        var outputPath = GetStr(p, "outputPath");



        var viewIds    = p["viewIds"]?.ToObject<int[]>();



        var format     = p["format"]?.Value<string>() ?? "DWG";







        Directory.CreateDirectory(outputPath);







        var opts = new DWGExportOptions



        {



            FileVersion = ACADVersion.R2013,



        };







        var ids = viewIds != null



            ? viewIds.Select(i => new ElementId(i)).ToList()



            : new List<ElementId> { doc.ActiveView.Id };







        doc.Export(outputPath, outputPath, ids, opts);



        return new JObject { ["success"] = true, ["outputPath"] = outputPath };



    }







    private static JObject ExportIfc(Document doc, JObject p)



    {



        var outputPath = GetStr(p, "outputPath");



        var ifcVersion = p["ifcVersion"]?.Value<string>() ?? "IFC4";



        var baseQty    = p["exportBaseQuantities"]?.Value<bool>() ?? true;







        var opts = new IFCExportOptions



        {



            FileVersion = ifcVersion switch



            {



                "IFC2x3" => IFCVersion.IFC2x3CV2,



                "IFC4x3" => IFCVersion.IFCSG,



                _        => IFCVersion.IFC4,



            },



            ExportBaseQuantities = baseQty,



        };







        using var tx = new Transaction(doc, "RevitMCP: Export IFC");



        tx.Start();



        doc.Export(Path.GetDirectoryName(outputPath)!, Path.GetFileNameWithoutExtension(outputPath), opts);



        tx.Commit();







        return new JObject { ["success"] = true, ["outputPath"] = outputPath };



    }







    private static JObject ExportSchedule(Document doc, JObject p)



    {



        var schedId   = new ElementId(GetInt(p, "scheduleViewId"));



        var outPath   = GetStr(p, "outputPath");



        var delimiter = p["delimiter"]?.Value<string>() ?? "comma";







        var schedule = doc.GetElement(schedId) as ViewSchedule



                       ?? throw new ArgumentException("Schedule view not found.");







        var opts = new ViewScheduleExportOptions



        {



            FieldDelimiter = delimiter switch



            {



                "tab"       => "\t",



                "semicolon" => ";",



                _           => ",",



            },



            Title = false,



        };







        schedule.Export(Path.GetDirectoryName(outPath)!, Path.GetFileName(outPath), opts);



        return new JObject { ["success"] = true, ["outputPath"] = outPath };



    }







    // ─── Commands ────────────────────────────────────────────────────────────







    private static JObject RunCommand(UIApplication app, JObject p)



    {



        var commandId = GetStr(p, "commandId");



        if (!Enum.TryParse<PostableCommand>(commandId, out var cmd))



            throw new ArgumentException($"Unknown PostableCommand: {commandId}");







        var revitCommandId = RevitCommandId.LookupPostableCommandId(cmd);



        app.PostCommand(revitCommandId);



        return new JObject { ["success"] = true, ["command"] = commandId };



    }







    private static JObject RunDynamo(UIApplication app, JObject p)



    {



        var scriptPath   = GetStr(p, "scriptPath");



        var journalData  = p["journalData"]?.ToObject<Dictionary<string, string>>() ?? new();







        if (!File.Exists(scriptPath))



            throw new FileNotFoundException($"Dynamo script not found: {scriptPath}");







        // Dynamo must be triggered via journal. We build a minimal journal key dict.



        journalData["dynamo_script_path"] = scriptPath;







        // The actual Dynamo automation API requires DynamoRevit references which are



        // version-dependent. Here we use the standard journal approach.



        var cmdId = RevitCommandId.LookupCommandId("ID_VISUAL_PROGRAMMING_DYNAMO");



        app.PostCommand(cmdId);







        return new JObject



        {



            ["success"]    = true,



            ["scriptPath"] = scriptPath,



            ["note"]       = "Dynamo command posted. Script must be configured to auto-run in Dynamo settings."



        };



    }







    // ─── Category helper ─────────────────────────────────────────────────────







    private static BuiltInCategory GetBuiltInCategory(string name)

    {

        var resolved = CategoryResolver.Resolve(name);

        if (resolved.HasValue) return resolved.Value;

        return name switch

        {

            "Walls" => BuiltInCategory.OST_Walls,

            "Doors" => BuiltInCategory.OST_Doors,

            "Windows" => BuiltInCategory.OST_Windows,

            "Floors" => BuiltInCategory.OST_Floors,

            "Roofs" => BuiltInCategory.OST_Roofs,

            "Ceilings" => BuiltInCategory.OST_Ceilings,

            "Rooms" => BuiltInCategory.OST_Rooms,

            "Columns" => BuiltInCategory.OST_Columns,

            "Beams" => BuiltInCategory.OST_StructuralFraming,

            "Furniture" => BuiltInCategory.OST_Furniture,

            "Stairs" => BuiltInCategory.OST_Stairs,

            "Railings" => BuiltInCategory.OST_StairsRailing,

            "Grids" => BuiltInCategory.OST_Grids,

            "Levels" => BuiltInCategory.OST_Levels,

            "Sheets" => BuiltInCategory.OST_Sheets,

            "MEP" => BuiltInCategory.OST_MechanicalEquipment,

            _ => throw new ArgumentException($"Unknown category: '{name}'. Use standard Revit category names.")

        };

    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — MATERIAL TAKEOFF



    // ════════════════════════════════════════════════════════════════════════







    private static readonly string[] DefaultTakeoffCategories =



        { "Walls", "Floors", "Roofs", "Ceilings", "Columns", "Beams" };







    private static JObject GetMaterialTakeoff(Document doc, JObject p)



    {



        var cats     = p["categories"]?.ToObject<string[]>() ?? DefaultTakeoffCategories;



        var level    = p["levelName"]?.Value<string>();



        var layers   = p["includeLayerBreakdown"]?.Value<bool>() ?? true;







        var rows = CollectMaterialRows(doc, cats, level, layers);



        return new JObject { ["materials"] = new JArray(rows) };



    }







    private static JObject ExportMaterialTakeoffCsv(Document doc, JObject p)



    {



        var outPath  = GetStr(p, "outputPath");



        var cats     = p["categories"]?.ToObject<string[]>() ?? DefaultTakeoffCategories;



        var level    = p["levelName"]?.Value<string>();



        var layers   = p["includeLayerBreakdown"]?.Value<bool>() ?? true;







        var rows = CollectMaterialRows(doc, cats, level, layers);



        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);







        using var w = new StreamWriter(outPath, false, System.Text.Encoding.UTF8);



        w.WriteLine("Material,Category,TotalAreaM2,TotalVolM3,ElementCount");



        foreach (var row in rows)



            w.WriteLine($"\"{row["material"]}\",\"{row["category"]}\",{row["totalAreaM2"]:F3},{row["totalVolM3"]:F4},{row["elementCount"]}");







        return new JObject { ["success"] = true, ["outputPath"] = outPath, ["rowCount"] = rows.Count };



    }







    private static List<JObject> CollectMaterialRows(Document doc, string[] cats, string? levelFilter, bool layers)



    {



        // key = (materialName, category)



        var agg = new Dictionary<(string, string), (double area, double vol, int count)>();







        foreach (var catName in cats)



        {



            BuiltInCategory bic;



            try { bic = GetBuiltInCategory(catName); } catch { continue; }







            var elements = new FilteredElementCollector(doc)



                .OfCategory(bic)



                .WhereElementIsNotElementType()



                .ToElements()



                .Where(e =>



                {



                    if (levelFilter == null) return true;



                    var lp = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)



                           ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM);



                    return lp?.AsValueString() == levelFilter;



                });







            foreach (var el in elements)



            {



                if (layers && el is Wall wall && wall.WallType.Kind == WallKind.Basic)



                {



                    // Compound wall — iterate layers



                    var cs = wall.WallType.GetCompoundStructure();



                    if (cs != null)



                    {



                        var wallArea = wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;



                        foreach (var layer in cs.GetLayers())



                        {



                            var matEl = doc.GetElement(layer.MaterialId);



                            var matName = matEl?.Name ?? "No Material";



                            var layerVol = wallArea * layer.Width; // ft³



                            var key = (matName, catName);



                            var cur = agg.GetValueOrDefault(key);



                            agg[key] = (cur.area + wallArea * 0.0929, cur.vol + layerVol * 0.0283168, cur.count + 1);



                        }



                        continue;



                    }



                }







                // Generic: get primary material



                var matParam = el.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)



                             ?? el.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);



                var materialName = "No Material";



                if (matParam != null)



                {



                    var matId = matParam.AsElementId();



                    materialName = doc.GetElement(matId)?.Name ?? "No Material";



                }







                var area = el.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;



                var vol  = el.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0;



                var k2   = (materialName, catName);



                var c2   = agg.GetValueOrDefault(k2);



                agg[k2] = (c2.area + area * 0.0929, c2.vol + vol * 0.0283168, c2.count + 1);



            }



        }







        return agg.Select(kv => new JObject



        {



            ["material"]    = kv.Key.Item1,



            ["category"]    = kv.Key.Item2,



            ["totalAreaM2"] = Math.Round(kv.Value.area, 3),



            ["totalVolM3"]  = Math.Round(kv.Value.vol, 4),



            ["elementCount"]= kv.Value.count,



        }).OrderBy(r => r["category"]!.ToString()).ThenBy(r => r["material"]!.ToString())



          .ToList();



    }







    private static JObject CreateMaterialSchedule(Document doc, JObject p)



    {



        var catName      = GetStr(p, "category");



        var scheduleName = p["scheduleName"]?.Value<string>() ?? $"{catName} Material Takeoff";



        var bic          = GetBuiltInCategory(catName);







        return WithTransaction(doc, "RevitMCP: Create Material Schedule", () =>



        {



            var catId    = new ElementId(bic);



            var schedule = ViewSchedule.CreateMaterialTakeoff(doc, catId);



            schedule.Name = scheduleName;







            // Add standard fields



            var def = schedule.Definition;



            var fields = new[] { "Material: Name", "Material: Area", "Material: Volume", "Count" };



            foreach (var fieldName in fields)



            {



                var sf = def.GetSchedulableFields()



                            .FirstOrDefault(f => f.GetName(doc) == fieldName);



                if (sf != null) def.AddField(sf);



            }







            return new JObject { ["scheduleId"] = schedule.Id.Value, ["name"] = schedule.Name, ["success"] = true };



        }, p);



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — CLASH DETECTION



    // ════════════════════════════════════════════════════════════════════════







    private static JObject DetectClashes(Document doc, JObject p)



    {



        var catA      = GetStr(p, "categoryA");



        var catB      = GetStr(p, "categoryB");



        var level     = p["levelName"]?.Value<string>();



        var tolerance = p["tolerance"]?.Value<double>() ?? 0; // mm



        var tolFt     = MmToFt(tolerance);







        var clashes = FindClashes(doc, catA, catB, level, tolFt);



        return new JObject



        {



            ["clashCount"] = clashes.Count,



            ["clashes"]    = new JArray(clashes),



        };



    }







    private static JObject ExportClashReport(Document doc, JObject p)



    {



        var outPath   = GetStr(p, "outputPath");



        var catA      = GetStr(p, "categoryA");



        var catB      = GetStr(p, "categoryB");



        var format    = p["format"]?.Value<string>() ?? "html";



        var tolerance = p["tolerance"]?.Value<double>() ?? 0;



        var tolFt     = MmToFt(tolerance);







        var clashes = FindClashes(doc, catA, catB, null, tolFt);



        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);







        if (format == "csv")



        {



            using var w = new StreamWriter(outPath, false, System.Text.Encoding.UTF8);



            w.WriteLine("ClashId,ElementA_Id,ElementA_Category,ElementA_Type,ElementB_Id,ElementB_Category,ElementB_Type,LocationX_mm,LocationY_mm,LocationZ_mm");



            int i = 1;



            foreach (var c in clashes)



                w.WriteLine($"{i++},{c["elementA_Id"]},{c["elementA_Category"]},{c["elementA_Type"]},{c["elementB_Id"]},{c["elementB_Category"]},{c["elementB_Type"]},{c["locationX_mm"]},{c["locationY_mm"]},{c["locationZ_mm"]}");



        }



        else



        {



            var rows = string.Join("\n", clashes.Select((c, i) =>



                $"<tr><td>{i+1}</td><td>{c["elementA_Id"]}</td><td>{c["elementA_Category"]}</td><td>{c["elementA_Type"]}</td>" +



                $"<td>{c["elementB_Id"]}</td><td>{c["elementB_Category"]}</td><td>{c["elementB_Type"]}</td>" +



                $"<td>{c["locationX_mm"]}, {c["locationY_mm"]}, {c["locationZ_mm"]}</td></tr>"));







            var html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>Clash Report</title>



<style>body{{font-family:Arial;font-size:12px}} table{{border-collapse:collapse;width:100%}}



th,td{{border:1px solid #ccc;padding:6px 8px}} th{{background:#2c3e50;color:#fff}} tr:nth-child(even){{background:#f5f5f5}}</style></head>



<body><h2>Clash Detection Report</h2>



<p>Categories: <b>{catA}</b> vs <b>{catB}</b> &nbsp;|&nbsp; Clashes found: <b>{clashes.Count}</b> &nbsp;|&nbsp; Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</p>



<table><thead><tr><th>#</th><th>ID A</th><th>Cat A</th><th>Type A</th><th>ID B</th><th>Cat B</th><th>Type B</th><th>Location (mm)</th></tr></thead>



<tbody>{rows}</tbody></table></body></html>";



            File.WriteAllText(outPath, html, System.Text.Encoding.UTF8);



        }







        return new JObject { ["success"] = true, ["clashCount"] = clashes.Count, ["outputPath"] = outPath };



    }







    private static List<JObject> FindClashes(Document doc, string catA, string catB, string? levelFilter, double tolFt)



    {



        var elementsA = new FilteredElementCollector(doc)



            .OfCategory(GetBuiltInCategory(catA))



            .WhereElementIsNotElementType()



            .ToElements()



            .Where(e => levelFilter == null || (e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString() == levelFilter))



            .ToList();







        var elementsB = new FilteredElementCollector(doc)



            .OfCategory(GetBuiltInCategory(catB))



            .WhereElementIsNotElementType()



            .ToElements()



            .Where(e => levelFilter == null || (e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString() == levelFilter))



            .ToList();







        var clashes = new List<JObject>();







        foreach (var a in elementsA)



        {



            var solidA = GetSolid(a);



            if (solidA == null) continue;







            foreach (var b in elementsB)



            {



                if (a.Id == b.Id) continue;



                var solidB = GetSolid(b);



                if (solidB == null) continue;







                try



                {



                    var intersection = BooleanOperationsUtils.ExecuteBooleanOperation(



                        solidA, solidB, BooleanOperationsType.Intersect);



                    if (intersection != null && intersection.Volume > tolFt * tolFt * tolFt)



                    {



                        var bb = intersection.GetBoundingBox();



                        var center = (bb.Min + bb.Max) / 2;



                        clashes.Add(new JObject



                        {



                            ["elementA_Id"]       = a.Id.Value,



                            ["elementA_Category"] = catA,



                            ["elementA_Type"]     = doc.GetElement(a.GetTypeId())?.Name,



                            ["elementB_Id"]       = b.Id.Value,



                            ["elementB_Category"] = catB,



                            ["elementB_Type"]     = doc.GetElement(b.GetTypeId())?.Name,



                            ["locationX_mm"]      = Math.Round(center.X * 304.8),



                            ["locationY_mm"]      = Math.Round(center.Y * 304.8),



                            ["locationZ_mm"]      = Math.Round(center.Z * 304.8),



                            ["volumeMM3"]         = Math.Round(intersection.Volume * 304.8 * 304.8 * 304.8),



                        });



                    }



                }



                catch { /* Boolean op failure on complex geometry — skip */ }



            }



        }



        return clashes;



    }







    private static Solid? GetSolid(Element el)



    {



        var opts = new Options { ComputeReferences = false, DetailLevel = ViewDetailLevel.Coarse };



        var geom = el.get_Geometry(opts);



        if (geom == null) return null;



        foreach (var obj in geom)



        {



            if (obj is Solid s && s.Volume > 0) return s;



            if (obj is GeometryInstance gi)



                foreach (var inner in gi.GetInstanceGeometry())



                    if (inner is Solid si && si.Volume > 0) return si;



        }



        return null;



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — REVISION / DRAWING ISSUE AUTOMATION



    // ════════════════════════════════════════════════════════════════════════







    private static JObject IssueDrawings(Document doc, JObject p)



    {



        var description  = GetStr(p, "revisionDescription");



        var date         = GetStr(p, "revisionDate");



        var sheetIds     = p["sheetIds"]?.ToObject<int[]>();



        var exportPath   = p["exportPdfPath"]?.Value<string>();







        return WithTransaction(doc, "RevitMCP: Issue Drawings", () =>



        {



            // Create revision



            var rev = Revision.Create(doc);



            rev.Description = description;



            rev.RevisionDate = date;







            // Stamp sheets



            var sheets = sheetIds != null



                ? sheetIds.Select(id => doc.GetElement(new ElementId(id)) as ViewSheet).Where(s => s != null)



                : new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>();







            var stamped = 0;



            foreach (var sheet in sheets)



            {



                if (sheet == null) continue;



                var revIds = sheet.GetAdditionalRevisionIds().ToList();



                revIds.Add(rev.Id);



                sheet.SetAdditionalRevisionIds(revIds);



                stamped++;



            }







            var result = new JObject



            {



                ["success"]     = true,



                ["revisionId"]  = rev.Id.Value,



                ["sheetsStamped"] = stamped,



            };







            if (exportPath != null)



            {



                Directory.CreateDirectory(exportPath);



                var exportIds = new FilteredElementCollector(doc)



                    .OfClass(typeof(ViewSheet))



                    .Select(e => e.Id)



                    .ToList();



                doc.Export(exportPath, exportIds, new PDFExportOptions { Combine = true });



                result["pdfExportPath"] = exportPath;



            }







            return result;



        }, p);



    }







    private static JObject GetRevisionHistory(Document doc)



    {



        var revisions = Revision.GetAllRevisionIds(doc)



            .Select(id => doc.GetElement(id) as Revision)



            .Where(r => r != null)



            .Select(r =>



            {



                var sheetCount = new FilteredElementCollector(doc)



                    .OfClass(typeof(ViewSheet))



                    .Cast<ViewSheet>()



                    .Count(s => s.GetAllRevisionIds().Contains(r!.Id));







                return new JObject



                {



                    ["id"]          = r!.Id.Value,



                    ["sequence"]    = r.SequenceNumber,



                    ["description"] = r.Description,



                    ["date"]        = r.RevisionDate,



                    ["sheetCount"]  = sheetCount,



                };



            });







        return new JObject { ["revisions"] = new JArray(revisions) };



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — ROOM FINISHES



    // ════════════════════════════════════════════════════════════════════════







    private static readonly string[] FinishParams =



        { "Floor Finish", "Wall Finish", "Ceiling Finish", "Base Finish", "Comments" };







    private static JObject GetRoomFinishes(Document doc, JObject p)



    {



        var levelName = p["levelName"]?.Value<string>();



        var rooms = new FilteredElementCollector(doc)



            .OfClass(typeof(SpatialElement))



            .OfType<Room>()



            .Where(r => levelName == null || r.Level?.Name == levelName)



            .Select(r =>



            {



                var obj = new JObject



                {



                    ["id"]     = r.Id.Value,



                    ["number"] = r.Number,



                    ["name"]   = r.Name,



                    ["level"]  = r.Level?.Name,



                };



                foreach (var fp in FinishParams)



                {



                    var param = r.LookupParameter(fp);



                    obj[fp.Replace(" ", "")] = param?.AsString() ?? "";



                }



                return obj;



            });







        return new JObject { ["rooms"] = new JArray(rooms) };



    }







    private static JObject SetRoomFinishesBulk(Document doc, JObject p)



    {



        var updates = p["updates"]?.ToObject<JArray>()



                      ?? throw new ArgumentException("updates array required");







        return WithTransaction(doc, "RevitMCP: Set Room Finishes Bulk", () =>



        {



            int updated = 0;



            var errors  = new JArray();







            foreach (var upd in updates.Cast<JObject>())



            {



                var roomId = upd["roomId"]?.Value<int>() ?? -1;



                var room   = doc.GetElement(new ElementId(roomId)) as Room;



                if (room == null) { errors.Add($"Room {roomId} not found"); continue; }







                var map = new Dictionary<string, string?>



                {



                    { "Floor Finish",   upd["floorFinish"]?.Value<string>() },



                    { "Wall Finish",    upd["wallFinish"]?.Value<string>() },



                    { "Ceiling Finish", upd["ceilingFinish"]?.Value<string>() },



                    { "Base Finish",    upd["baseFinish"]?.Value<string>() },



                    { "Comments",       upd["comments"]?.Value<string>() },



                };







                foreach (var (paramName, val) in map)



                {



                    if (val == null) continue;



                    room.LookupParameter(paramName)?.Set(val);



                }



                updated++;



            }







            return new JObject { ["success"] = true, ["roomsUpdated"] = updated, ["errors"] = errors };



        }, p);



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — VIEW TEMPLATE & BULK RENAME



    // ════════════════════════════════════════════════════════════════════════







    private static JObject ApplyViewTemplateBulk(Document doc, JObject p)



    {



        var templateName = GetStr(p, "templateName");



        var viewIds      = p["viewIds"]?.ToObject<int[]>();



        var viewType     = p["viewType"]?.Value<string>();



        var nameContains = p["nameContains"]?.Value<string>();







        // Find template



        var template = new FilteredElementCollector(doc)



            .OfClass(typeof(View))



            .Cast<View>()



            .FirstOrDefault(v => v.IsTemplate && v.Name == templateName)



            ?? throw new ArgumentException($"View template '{templateName}' not found.");







        return WithTransaction(doc, "RevitMCP: Apply View Template Bulk", () =>



        {



            IEnumerable<View> targets;



            if (viewIds != null)



            {



                targets = viewIds.Select(id => doc.GetElement(new ElementId(id)) as View).Where(v => v != null)!;



            }



            else



            {



                targets = new FilteredElementCollector(doc)



                    .OfClass(typeof(View))



                    .Cast<View>()



                    .Where(v => !v.IsTemplate)



                    .Where(v => viewType == null || v.ViewType.ToString() == viewType)



                    .Where(v => nameContains == null || v.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));



            }







            int count = 0;



            foreach (var view in targets)



            {



                view.ViewTemplateId = template.Id;



                count++;



            }







            return new JObject { ["success"] = true, ["viewsUpdated"] = count, ["template"] = templateName };



        }, p);



    }







    private static JObject BulkRenameViews(Document doc, JObject p)



    {



        var targets      = p["targets"]?.Value<string>() ?? "views";



        var findText     = p["findText"]?.Value<string>();



        var replaceText  = p["replaceText"]?.Value<string>() ?? "";



        var addPrefix    = p["addPrefix"]?.Value<string>() ?? "";



        var addSuffix    = p["addSuffix"]?.Value<string>() ?? "";



        var nameContains = p["nameContains"]?.Value<string>();



        var dryRun       = p["dryRun"]?.Value<bool>() ?? false;







        IEnumerable<Element> items = targets switch



        {



            "sheets" => new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).ToElements(),



            "both"   => new FilteredElementCollector(doc).OfClass(typeof(View)).ToElements(),



            _        => new FilteredElementCollector(doc).OfClass(typeof(View))



                            .Cast<View>().Where(v => !v.IsTemplate).Cast<Element>()



        };







        if (nameContains != null)



            items = items.Where(e => e.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase));







        var plan = items.Select(e =>



        {



            var oldName = e.Name;



            var newName = oldName;



            if (findText != null) newName = newName.Replace(findText, replaceText);



            if (!string.IsNullOrEmpty(addPrefix)) newName = addPrefix + newName;



            if (!string.IsNullOrEmpty(addSuffix)) newName = newName + addSuffix;



            return (el: e, oldName, newName);



        }).Where(x => x.oldName != x.newName).ToList();







        if (dryRun)



        {



            var preview = plan.Select(x => new JObject { ["id"] = x.el.Id.Value, ["from"] = x.oldName, ["to"] = x.newName });



            return new JObject { ["dryRun"] = true, ["changes"] = new JArray(preview), ["count"] = plan.Count };



        }







        return WithTransaction(doc, "RevitMCP: Bulk Rename", () =>



        {



            foreach (var (el, _, newName) in plan)



            {



                try { el.Name = newName; } catch { /* some views disallow rename */ }



            }



            return new JObject { ["success"] = true, ["renamed"] = plan.Count };



        }, p);



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — FAMILY AUDIT



    // ════════════════════════════════════════════════════════════════════════







    private static string UniqueViewName(Document doc, string desiredName)



    {



        var existing = new FilteredElementCollector(doc)



            .OfClass(typeof(View))



            .Cast<View>()



            .Select(v => v.Name)



            .ToHashSet(StringComparer.OrdinalIgnoreCase);







        if (!existing.Contains(desiredName)) return desiredName;







        for (var i = 2; i < 1000; i++)



        {



            var candidate = $"{desiredName} ({i})";



            if (!existing.Contains(candidate)) return candidate;



        }







        throw new InvalidOperationException($"Could not create a unique view name for '{desiredName}'.");



    }







    private static JObject CreatePlanViewsForLevels(Document doc, JObject p)



    {



        var levelNames = p["levelNames"]?.ToObject<string[]>();



        var viewFamilyText = p["viewFamily"]?.Value<string>() ?? "FloorPlan";



        var nameTemplate = p["nameTemplate"]?.Value<string>() ?? "{levelName} - {viewFamily}";



        var templateName = p["viewTemplateName"]?.Value<string>();



        var skipExisting = p["skipExisting"]?.Value<bool>() ?? true;



        var dryRun = p["dryRun"]?.Value<bool>() ?? true;







        var viewFamily = viewFamilyText.ToLowerInvariant() switch



        {



            "ceilingplan" or "ceiling" => ViewFamily.CeilingPlan,



            "structuralplan" or "structural" => ViewFamily.StructuralPlan,



            _ => ViewFamily.FloorPlan



        };







        var viewFamilyType = new FilteredElementCollector(doc)



            .OfClass(typeof(ViewFamilyType))



            .Cast<ViewFamilyType>()



            .FirstOrDefault(vft => vft.ViewFamily == viewFamily)



            ?? throw new ArgumentException($"No ViewFamilyType found for {viewFamily}.");







        var template = templateName == null



            ? null



            : new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .FirstOrDefault(v => v.IsTemplate && v.Name == templateName)



                ?? throw new ArgumentException($"View template '{templateName}' not found.");







        var levels = new FilteredElementCollector(doc)



            .OfClass(typeof(Level))



            .Cast<Level>()



            .Where(l => levelNames == null || levelNames.Contains(l.Name))



            .OrderBy(l => l.Elevation)



            .ToList();







        var existingNames = new FilteredElementCollector(doc)



            .OfClass(typeof(View))



            .Cast<View>()



            .Select(v => v.Name)



            .ToHashSet(StringComparer.OrdinalIgnoreCase);







        var plan = levels.Select(level =>



        {



            var name = nameTemplate



                .Replace("{levelName}", level.Name)



                .Replace("{viewFamily}", viewFamily.ToString());



            return new JObject



            {



                ["levelId"] = level.Id.Value,



                ["levelName"] = level.Name,



                ["viewName"] = name,



                ["exists"] = existingNames.Contains(name),



            };



        }).ToList();







        if (dryRun)



            return new JObject { ["dryRun"] = true, ["planned"] = new JArray(plan), ["count"] = plan.Count };







        return WithTransaction(doc, "RevitMCP: Create Plan Views For Levels", () =>



        {



            var created = new JArray();







            foreach (var item in plan)



            {



                if (skipExisting && item["exists"]!.Value<bool>())



                {



                    item["skipped"] = true;



                    item["reason"] = "view name already exists";



                    created.Add(item);



                    continue;



                }







                var view = ViewPlan.Create(doc, viewFamilyType.Id, new ElementId(item["levelId"]!.Value<int>()));



                view.Name = UniqueViewName(doc, item["viewName"]!.Value<string>()!);



                if (template != null) view.ViewTemplateId = template.Id;







                item["viewId"] = view.Id.Value;



                item["created"] = true;



                created.Add(item);



            }







            return new JObject



            {



                ["success"] = true,



                ["viewsCreated"] = created.Count(v => v["created"]?.Value<bool>() == true),



                ["details"] = created,



            };



        }, p);



    }







    private static JObject DuplicateViewsBulk(Document doc, JObject p)



    {



        var viewIds = p["viewIds"]?.ToObject<int[]>();



        var viewType = p["viewType"]?.Value<string>();



        var nameContains = p["nameContains"]?.Value<string>();



        var nameTemplate = p["nameTemplate"]?.Value<string>() ?? "{viewName} - Copy";



        var templateName = p["viewTemplateName"]?.Value<string>();



        var dryRun = p["dryRun"]?.Value<bool>() ?? true;



        var optionText = p["duplicateOption"]?.Value<string>() ?? "WithDetailing";







        var duplicateOption = optionText.ToLowerInvariant() switch



        {



            "asdependent" or "dependent" => ViewDuplicateOption.AsDependent,



            "duplicate" => ViewDuplicateOption.Duplicate,



            _ => ViewDuplicateOption.WithDetailing



        };







        var template = templateName == null



            ? null



            : new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .FirstOrDefault(v => v.IsTemplate && v.Name == templateName)



                ?? throw new ArgumentException($"View template '{templateName}' not found.");







        IEnumerable<View> query = viewIds != null



            ? viewIds.Select(id => doc.GetElement(new ElementId(id)) as View).OfType<View>()



            : new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .Where(v => !v.IsTemplate && v.CanBePrinted);







        query = query



            .Where(v => viewType == null || v.ViewType.ToString() == viewType)



            .Where(v => nameContains == null || v.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))



            .Where(v => v.CanViewBeDuplicated(duplicateOption));







        var plan = query.Select((view, index) =>



        {



            var newName = nameTemplate



                .Replace("{viewName}", view.Name)



                .Replace("{index}", (index + 1).ToString("D2", CultureInfo.InvariantCulture));



            return new JObject



            {



                ["sourceViewId"] = view.Id.Value,



                ["sourceViewName"] = view.Name,



                ["newViewName"] = newName,



            };



        }).ToList();







        if (dryRun)



            return new JObject { ["dryRun"] = true, ["planned"] = new JArray(plan), ["count"] = plan.Count };







        return WithTransaction(doc, "RevitMCP: Duplicate Views Bulk", () =>



        {



            var duplicated = new JArray();







            foreach (var item in plan)



            {



                var source = doc.GetElement(new ElementId(item["sourceViewId"]!.Value<int>())) as View;



                if (source == null) continue;







                var newId = source.Duplicate(duplicateOption);



                var newView = doc.GetElement(newId) as View;



                if (newView == null) continue;







                newView.Name = UniqueViewName(doc, item["newViewName"]!.Value<string>()!);



                if (template != null) newView.ViewTemplateId = template.Id;







                item["newViewId"] = newView.Id.Value;



                item["created"] = true;



                duplicated.Add(item);



            }







            return new JObject { ["success"] = true, ["viewsDuplicated"] = duplicated.Count, ["details"] = duplicated };



        }, p);



    }







    private static JObject BatchSetParameters(Document doc, JObject p)



    {



        var values = p["parameterValues"] as JObject



                     ?? throw new ArgumentException("parameterValues object required");



        var category = p["category"]?.Value<string>();



        var familyName = p["familyName"]?.Value<string>();



        var levelName = p["levelName"]?.Value<string>();



        var viewName = p["viewName"]?.Value<string>();



        var nameContains = p["nameContains"]?.Value<string>();



        var dryRun = p["dryRun"]?.Value<bool>() ?? true;



        var limit = p["limit"]?.Value<int>() ?? 1000;







        FilteredElementCollector col;



        if (viewName != null)



        {



            var view = new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .FirstOrDefault(v => v.Name == viewName)



                ?? throw new ArgumentException($"View '{viewName}' not found.");



            col = new FilteredElementCollector(doc, view.Id);



        }



        else



        {



            col = new FilteredElementCollector(doc);



        }







        col = col.WhereElementIsNotElementType();



        if (category != null) col = col.OfCategory(GetBuiltInCategory(category));







        var elements = col.ToElements()



            .Where(e => familyName == null || e is FamilyInstance fi && fi.Symbol.FamilyName == familyName)



            .Where(e => nameContains == null || e.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))



            .Where(e =>



            {



                if (levelName == null) return true;



                var lvlParam = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)



                             ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM);



                return lvlParam?.AsValueString() == levelName;



            })



            .Take(limit)



            .ToList();







        var preview = new JArray(elements.Select(e => new JObject



        {



            ["id"] = e.Id.Value,



            ["name"] = e.Name,



            ["category"] = e.Category?.Name,



        }));







        if (dryRun)



            return new JObject { ["dryRun"] = true, ["matchedElements"] = elements.Count, ["parameterValues"] = values, ["preview"] = preview };







        return WithTransaction(doc, "RevitMCP: Batch Set Parameters", () =>



        {



            var changed = 0;



            var errors = new JArray();







            foreach (var element in elements)



            {



                foreach (var prop in values.Properties())



                {



                    var param = element.LookupParameter(prop.Name);



                    if (param == null)



                    {



                        errors.Add(new JObject { ["elementId"] = element.Id.Value, ["parameter"] = prop.Name, ["error"] = "parameter not found" });



                        continue;



                    }







                    if (TrySetParameterValue(param, prop.Value, out var error))



                    {



                        changed++;



                    }



                    else



                    {



                        errors.Add(new JObject { ["elementId"] = element.Id.Value, ["parameter"] = prop.Name, ["error"] = error });



                    }



                }



            }







            return new JObject



            {



                ["success"] = true,



                ["matchedElements"] = elements.Count,



                ["valuesApplied"] = changed,



                ["errors"] = errors,



            };



        }, p);



    }







    private static JObject AuditFamilies(Document doc, JObject p)



    {



        var libPath = p["libraryPath"]?.Value<string>();







        var families = new FilteredElementCollector(doc)



            .OfClass(typeof(Family))



            .Cast<Family>()



            .ToList();







        // Count instances per family



        var instanceCounts = new FilteredElementCollector(doc)



            .OfClass(typeof(FamilyInstance))



            .Cast<FamilyInstance>()



            .GroupBy(fi => fi.Symbol.Family.Id.Value)



            .ToDictionary(g => g.Key, g => g.Count());







        var report = families.Select(f =>



        {



            var count = instanceCounts.GetValueOrDefault(f.Id.Value, 0);



            var isSystem = f.IsInPlace;



            var path = "";



            try { path = f.get_Parameter(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)?.AsString() ?? ""; } catch { }







            return new JObject



            {



                ["id"]          = f.Id.Value,



                ["name"]        = f.Name,



                ["category"]    = f.FamilyCategory?.Name,



                ["instanceCount"]= count,



                ["isUnused"]    = count == 0,



                ["isInPlace"]   = isSystem,



                ["isExternal"]  = libPath != null && !string.IsNullOrEmpty(path) && !path.StartsWith(libPath, StringComparison.OrdinalIgnoreCase),



            };



        }).OrderBy(f => f["category"]!.ToString()).ThenBy(f => f["name"]!.ToString()).ToList();







        return new JObject



        {



            ["totalFamilies"]  = families.Count,



            ["unusedFamilies"] = report.Count(r => r["isUnused"]!.Value<bool>()),



            ["families"]       = new JArray(report),



        };



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — CSV PARAMETER IMPORT



    // ════════════════════════════════════════════════════════════════════════







    private static JObject ImportParametersFromCsv(Document doc, JObject p)



    {



        var csvPath   = GetStr(p, "csvPath");



        var delimiter = p["delimiter"]?.Value<string>() ?? "comma";



        var dryRun    = p["dryRun"]?.Value<bool>() ?? false;







        char sep = delimiter switch { "tab" => '\t', "semicolon" => ';', _ => ',' };







        if (!File.Exists(csvPath)) throw new FileNotFoundException($"CSV not found: {csvPath}");







        var lines = File.ReadAllLines(csvPath, System.Text.Encoding.UTF8);



        if (lines.Length < 2) throw new InvalidOperationException("CSV must have a header row and at least one data row.");







        var headers = lines[0].Split(sep).Select(h => h.Trim('"')).ToArray();



        if (headers[0].ToLower() != "elementid") throw new InvalidOperationException("First column must be 'ElementId'.");







        var paramNames = headers.Skip(1).ToArray();



        var changes = new JArray();



        var errors  = new JArray();







        foreach (var line in lines.Skip(1))



        {



            if (string.IsNullOrWhiteSpace(line)) continue;



            var cols = line.Split(sep).Select(c => c.Trim('"')).ToArray();



            if (!int.TryParse(cols[0], out var eid)) { errors.Add($"Invalid ElementId: {cols[0]}"); continue; }







            var el = doc.GetElement(new ElementId(eid));



            if (el == null) { errors.Add($"Element {eid} not found"); continue; }







            for (int i = 0; i < paramNames.Length; i++)



            {



                var val = i + 1 < cols.Length ? cols[i + 1] : "";



                changes.Add(new JObject { ["elementId"] = eid, ["parameter"] = paramNames[i], ["value"] = val });



            }



        }







        if (dryRun)



            return new JObject { ["dryRun"] = true, ["changeCount"] = changes.Count, ["errors"] = errors, ["preview"] = changes };







        return WithTransaction(doc, "RevitMCP: Import CSV Parameters", () =>



        {



            int applied = 0;



            foreach (var c in changes.Cast<JObject>())



            {



                var el = doc.GetElement(new ElementId(c["elementId"]!.Value<int>()));



                var param = el?.LookupParameter(c["parameter"]!.Value<string>()!);



                if (param == null || param.IsReadOnly) continue;



                try



                {



                    switch (param.StorageType)



                    {



                        case StorageType.String:  param.Set(c["value"]!.Value<string>()); break;



                        case StorageType.Integer: param.Set(int.Parse(c["value"]!.Value<string>()!)); break;



                        case StorageType.Double:  param.Set(double.Parse(c["value"]!.Value<string>()!)); break;



                    }



                    applied++;



                }



                catch { errors.Add($"Failed to set {c["parameter"]} on {c["elementId"]}"); }



            }



            return new JObject { ["success"] = true, ["applied"] = applied, ["errors"] = errors };



        }, p);



    }







    // ════════════════════════════════════════════════════════════════════════



    // V2 — AUTO SHEET POPULATION



    // ════════════════════════════════════════════════════════════════════════







    private static JObject AutoPopulateSheets(Document doc, JObject p)



    {



        var rules       = p["rules"]?.ToObject<JArray>() ?? throw new ArgumentException("rules required");



        var skipExisting = p["skipExistingSheets"]?.Value<bool>() ?? true;







        return WithTransaction(doc, "RevitMCP: Auto Populate Sheets", () =>



        {



            var allViews = new FilteredElementCollector(doc)



                .OfClass(typeof(View))



                .Cast<View>()



                .Where(v => !v.IsTemplate && v.CanBePrinted)



                .ToList();







            var existingSheetNumbers = new FilteredElementCollector(doc)



                .OfClass(typeof(ViewSheet))



                .Cast<ViewSheet>()



                .Select(s => s.SheetNumber)



                .ToHashSet();







            // Default title block



            var defaultTb = new FilteredElementCollector(doc)



                .OfCategory(BuiltInCategory.OST_TitleBlocks)



                .OfClass(typeof(FamilySymbol))



                .Cast<FamilySymbol>()



                .FirstOrDefault();







            var created = new JArray();



            int sheetIndex = 0;







            foreach (var rule in rules.Cast<JObject>())



            {



                var pattern    = rule["viewNamePattern"]?.Value<string>() ?? "";



                var numPrefix  = rule["sheetNumberPrefix"]?.Value<string>() ?? "A-";



                var nameTemplate = rule["sheetNameTemplate"]?.Value<string>() ?? "{viewName}";



                var tbName     = rule["titleBlockName"]?.Value<string>();







                FamilySymbol? tb = defaultTb;



                if (tbName != null)



                    tb = new FilteredElementCollector(doc)



                        .OfCategory(BuiltInCategory.OST_TitleBlocks)



                        .OfClass(typeof(FamilySymbol))



                        .Cast<FamilySymbol>()



                        .FirstOrDefault(s => s.Name == tbName) ?? defaultTb;







                var matchingViews = allViews



                    .Where(v => v.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));







                foreach (var view in matchingViews)



                {



                    sheetIndex++;



                    var sheetNum  = $"{numPrefix}{sheetIndex:D2}";



                    var sheetName = nameTemplate.Replace("{viewName}", view.Name);







                    if (skipExisting && existingSheetNumbers.Contains(sheetNum))



                    {



                        created.Add(new JObject { ["skipped"] = true, ["sheetNumber"] = sheetNum, ["reason"] = "already exists" });



                        continue;



                    }







                    var sheet = tb != null



                        ? ViewSheet.Create(doc, tb.Id)



                        : ViewSheet.Create(doc, ElementId.InvalidElementId);







                    sheet.SheetNumber = sheetNum;



                    sheet.Name = sheetName;



                    existingSheetNumbers.Add(sheetNum);







                    // Place view centred on sheet



                    if (Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))



                    {



                        Viewport.Create(doc, sheet.Id, view.Id, new XYZ(0.5, 0.35, 0));



                    }







                    created.Add(new JObject



                    {



                        ["sheetId"]     = sheet.Id.Value,



                        ["sheetNumber"] = sheetNum,



                        ["sheetName"]   = sheetName,



                        ["viewName"]    = view.Name,



                    });



                }



            }







            return new JObject { ["success"] = true, ["sheetsCreated"] = created.Count(c => c["skipped"] == null), ["details"] = created };



        }, p);



    }







    private static JObject GetCurrentSelection(UIDocument? uiDoc)



    {



        if (uiDoc == null) return new JObject { ["count"] = 0, ["elements"] = new JArray() };



        var ids = uiDoc.Selection.GetElementIds();



        var doc = uiDoc.Document;



        var elements = ids.Select(id =>



        {



            var el = doc.GetElement(id);



            return new JObject



            {



                ["id"] = id.Value,



                ["uniqueId"] = el?.UniqueId,



                ["name"] = el?.Name,



                ["category"] = el?.Category?.Name



            };



        });



        return new JObject



        {



            ["count"] = ids.Count,



            ["elements"] = new JArray(elements)



        };



    }







    private static JObject SetCurrentSelection(UIDocument? uiDoc, JObject p)



    {



        if (uiDoc == null) throw new InvalidOperationException("No active UI document");



        var idsArray = p["elementIds"] as JArray ?? throw new ArgumentException("elementIds array is required");



        var ids = idsArray.Select(t => new ElementId(t.Value<int>())).ToList();



        uiDoc.Selection.SetElementIds(ids);



        return new JObject { ["success"] = true, ["selectedCount"] = ids.Count };



    }







    private static JObject IsolateElements(UIDocument? uiDoc, JObject p)



    {



        if (uiDoc == null) throw new InvalidOperationException("No active UI document");



        var idsArray = p["elementIds"] as JArray ?? throw new ArgumentException("elementIds array is required");



        var ids = idsArray.Select(t => new ElementId(t.Value<int>())).ToList();



        var view = uiDoc.Document.ActiveView ?? throw new InvalidOperationException("No active view");



        view.IsolateElementsTemporary(ids);



        return new JObject { ["success"] = true, ["isolatedCount"] = ids.Count, ["viewName"] = view.Name };



    }







    private static JObject ResetTemporaryOverride(UIDocument? uiDoc)



    {



        if (uiDoc == null) throw new InvalidOperationException("No active UI document");



        var view = uiDoc.Document.ActiveView;



        if (view != null && view.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate))



        {



            view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);



        }



        return new JObject { ["success"] = true, ["viewName"] = view?.Name };



    }







    private static JObject ListCategories(Document doc)



    {



        var categories = doc.Settings.Categories.Cast<Category>()



            .Where(c => c.CategoryType == CategoryType.Model || c.AllowsBoundParameters)



            .OrderBy(c => c.Name)



            .Select(c => new JObject



            {



                ["name"] = c.Name,



                ["id"] = c.Id.Value,



                ["categoryType"] = c.CategoryType.ToString(),



            });



        return new JObject { ["categories"] = new JArray(categories) };



    }







    private static JObject CountElements(Document doc, JObject p)



    {



        var category = p["category"]?.Value<string>();



        var col = new FilteredElementCollector(doc).WhereElementIsNotElementType();



        if (category != null)



        {



            var bic = GetBuiltInCategory(category);



            col = col.OfCategory(bic);



        }



        var count = col.GetElementCount();



        return new JObject



        {



            ["category"] = category ?? "All",



            ["count"] = count



        };



    }







    private static JObject GetModelWarnings(Document doc)



    {



        var warnings = doc.GetWarnings();



        var list = warnings.Select(w => new JObject



        {



            ["description"] = w.GetDescriptionText(),



            ["severity"] = w.GetSeverity().ToString(),



            ["failingElementIds"] = new JArray(w.GetFailingElements().Select(id => id.Value))



        });



        return new JObject { ["count"] = warnings.Count, ["warnings"] = new JArray(list) };



    }







    private static JObject CreateFloor(Document doc, JObject p)



    {



        var levelId = new ElementId(GetInt(p, "levelId"));



        var widthMm = p["widthMm"]?.Value<double>() ?? 5000;



        var lengthMm = p["lengthMm"]?.Value<double>() ?? 5000;



        var originX = MmToFt(p["originX"]?.Value<double>() ?? 0);



        var originY = MmToFt(p["originY"]?.Value<double>() ?? 0);



        var w = MmToFt(widthMm);



        var l = MmToFt(lengthMm);







        return WithTransaction(doc, "RevitMCP: Create Floor", () =>



        {



            var p0 = new XYZ(originX, originY, 0);



            var p1 = new XYZ(originX + w, originY, 0);



            var p2 = new XYZ(originX + w, originY + l, 0);



            var p3 = new XYZ(originX, originY + l, 0);







            var loop = new CurveLoop();



            loop.Append(Line.CreateBound(p0, p1));



            loop.Append(Line.CreateBound(p1, p2));



            loop.Append(Line.CreateBound(p2, p3));



            loop.Append(Line.CreateBound(p3, p0));







            var floorTypeId = Floor.GetDefaultFloorType(doc, false);



            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorTypeId, levelId);







            return new JObject



            {



                ["floorId"] = floor.Id.Value,



                ["levelId"] = levelId.Value,



                ["widthMm"] = widthMm,



                ["lengthMm"] = lengthMm



            };



        }, p);



    }







    private static JObject CreateRoom(Document doc, JObject p)



    {



        var levelId = new ElementId(GetInt(p, "levelId"));



        var level = doc.GetElement(levelId) as Level ?? throw new ArgumentException($"Level {levelId.Value} not found.");



        var x = MmToFt(p["xMm"]?.Value<double>() ?? 0);



        var y = MmToFt(p["yMm"]?.Value<double>() ?? 0);



        var roomName = p["name"]?.Value<string>();



        var roomNumber = p["number"]?.Value<string>();







        return WithTransaction(doc, "RevitMCP: Create Room", () =>



        {



            var uv = new UV(x, y);



            var room = doc.Create.NewRoom(level, uv);



            if (roomName != null) room.Name = roomName;



            if (roomNumber != null) room.Number = roomNumber;







            return new JObject



            {



                ["roomId"] = room.Id.Value,



                ["name"] = room.Name,



                ["number"] = room.Number,



                ["areaSqM"] = Math.Round(room.Area * 0.092903, 2)



            };



        }, p);



    }







    private static JObject ZoomToElements(UIDocument? uiDoc, JObject p)



    {



        if (uiDoc == null) throw new InvalidOperationException("No active UI document");



        var idsArray = p["elementIds"] as JArray ?? throw new ArgumentException("elementIds array is required");



        var ids = idsArray.Select(t => new ElementId(t.Value<long>())).ToList();



        if (ids.Count == 0) return new JObject { ["success"] = true, ["elementsCount"] = 0 };







        var doc = uiDoc.Document;



        BoundingBoxXYZ? bbox = null;



        foreach (var id in ids)



        {



            var el = doc.GetElement(id);



            var b = el?.get_BoundingBox(null);



            if (b != null)



            {



                if (bbox == null)



                    bbox = new BoundingBoxXYZ { Min = new XYZ(b.Min.X, b.Min.Y, b.Min.Z), Max = new XYZ(b.Max.X, b.Max.Y, b.Max.Z) };



                else



                {



                    bbox.Min = new XYZ(Math.Min(bbox.Min.X, b.Min.X), Math.Min(bbox.Min.Y, b.Min.Y), Math.Min(bbox.Min.Z, b.Min.Z));



                    bbox.Max = new XYZ(Math.Max(bbox.Max.X, b.Max.X), Math.Max(bbox.Max.Y, b.Max.Y), Math.Max(bbox.Max.Z, b.Max.Z));



                }



            }



        }







        var openViews = uiDoc.GetOpenUIViews();



        var activeUIView = openViews.FirstOrDefault(v => v.ViewId == doc.ActiveView?.Id) ?? openViews.FirstOrDefault();



        if (activeUIView != null && bbox != null)



        {



            activeUIView.ZoomAndCenterRectangle(bbox.Min, bbox.Max);



        }



        else



        {



            uiDoc.Selection.SetElementIds(ids);



        }







        return new JObject { ["success"] = true, ["elementsCount"] = ids.Count };



    }







    private static JObject ColorElements(Document doc, JObject p)



    {



        var idsArray = p["elementIds"] as JArray ?? throw new ArgumentException("elementIds array is required");



        var r = p["red"]?.Value<byte>() ?? 255;



        var g = p["green"]?.Value<byte>() ?? 0;



        var b = p["blue"]?.Value<byte>() ?? 0;



        var view = doc.ActiveView ?? throw new InvalidOperationException("No active view");







        return WithTransaction(doc, "RevitMCP: Override Color", () =>



        {



            var ogs = new OverrideGraphicSettings();



            var color = new Color(r, g, b);



            ogs.SetProjectionLineColor(color);



            ogs.SetSurfaceForegroundPatternColor(color);







            var fillPattern = new FilteredElementCollector(doc)



                .OfClass(typeof(FillPatternElement))



                .Cast<FillPatternElement>()



                .FirstOrDefault(fp => fp.Name.Contains("Solid", StringComparison.OrdinalIgnoreCase) || fp.Name.Contains("Uni", StringComparison.OrdinalIgnoreCase) || fp.Name.Contains("Plein", StringComparison.OrdinalIgnoreCase))



                ?? new FilteredElementCollector(doc)



                .OfClass(typeof(FillPatternElement))



                .Cast<FillPatternElement>()



                .FirstOrDefault();



            if (fillPattern != null)



            {



                ogs.SetSurfaceForegroundPatternId(fillPattern.Id);



            }







            foreach (var t in idsArray)



            {



                var id = new ElementId(t.Value<int>());



                view.SetElementOverrides(id, ogs);



            }







            return new JObject { ["success"] = true, ["coloredCount"] = idsArray.Count, ["color"] = $"{r},{g},{b}" };



        }, p);



    }







    private static JObject ResetColorOverrides(Document doc, JObject p)



    {



        var idsArray = p["elementIds"] as JArray;



        var view = doc.ActiveView ?? throw new InvalidOperationException("No active view");







        return WithTransaction(doc, "RevitMCP: Reset Color Overrides", () =>



        {



            var defaultOgs = new OverrideGraphicSettings();



            if (idsArray != null && idsArray.Count > 0)



            {



                foreach (var t in idsArray)



                {



                    view.SetElementOverrides(new ElementId(t.Value<int>()), defaultOgs);



                }



            }



            return new JObject { ["success"] = true, ["viewName"] = view.Name };



        }, p);



    }







    private static JObject CreateGrid(Document doc, JObject p)



    {



        var startX = MmToFt(p["startX"]?.Value<double>() ?? 0);



        var startY = MmToFt(p["startY"]?.Value<double>() ?? 0);



        var endX = MmToFt(p["endX"]?.Value<double>() ?? 10000);



        var endY = MmToFt(p["endY"]?.Value<double>() ?? 0);



        var name = p["name"]?.Value<string>();







        return WithTransaction(doc, "RevitMCP: Create Grid", () =>



        {



            var line = Line.CreateBound(new XYZ(startX, startY, 0), new XYZ(endX, endY, 0));



            var grid = Grid.Create(doc, line);



            if (name != null)



            {



                grid.Name = name;



            }



            return new JObject



            {



                ["gridId"] = grid.Id.Value,



                ["name"] = grid.Name



            };



        }, p);



    }







    private static JObject ListPhases(Document doc)



    {



        var phases = doc.Phases.Cast<Phase>().Select(ph => new JObject



        {



            ["id"] = ph.Id.Value,



            ["name"] = ph.Name



        });



        return new JObject { ["phases"] = new JArray(phases) };



    }







    private static JObject ListLinks(Document doc)



    {



        var rvtLinks = new FilteredElementCollector(doc)



            .OfClass(typeof(RevitLinkInstance))



            .Cast<RevitLinkInstance>()



            .Select(li => new JObject



            {



                ["id"] = li.Id.Value,



                ["name"] = li.Name,



                ["type"] = "RevitLink"



            });







        var cadLinks = new FilteredElementCollector(doc)



            .OfClass(typeof(ImportInstance))



            .Cast<ImportInstance>()



            .Select(ii => new JObject



            {



                ["id"] = ii.Id.Value,



                ["name"] = ii.Category?.Name ?? "CAD Link",



                ["type"] = ii.IsLinked ? "CADLink" : "CADImport"



            });







        return new JObject



        {



            ["links"] = new JArray(rvtLinks.Concat(cadLinks))



        };



    }







    private static JObject RotateElement(Document doc, JObject p)



    {



        var id = new ElementId(GetInt(p, "elementId"));



        var angleDeg = p["angleDegrees"]?.Value<double>() ?? 90;



        var angleRad = angleDeg * (Math.PI / 180.0);



        var el = doc.GetElement(id) ?? throw new ArgumentException($"Element {id.Value} not found");







        return WithTransaction(doc, "RevitMCP: Rotate Element", () =>



        {



            var bbox = el.get_BoundingBox(null);



            var center = bbox != null ? (bbox.Min + bbox.Max) * 0.5 : XYZ.Zero;



            var axis = Line.CreateBound(center, center + XYZ.BasisZ);



            ElementTransformUtils.RotateElement(doc, id, axis, angleRad);



            return new JObject { ["success"] = true, ["elementId"] = id.Value, ["angleDegrees"] = angleDeg };



        }, p);



    }







    private static JObject ExportViewImage(Document doc, JObject p)



    {



        var viewId = p["viewId"]?.Value<int>();



        var view = viewId.HasValue ? doc.GetElement(new ElementId(viewId.Value)) as View : doc.ActiveView;



        if (view == null) throw new InvalidOperationException("No active view for image export");







        var outPath = p["filePath"]?.Value<string>();



        if (string.IsNullOrWhiteSpace(outPath))



        {



            outPath = Path.Combine(Path.GetTempPath(), $"RevitMCP_View_{view.Id.Value}_{DateTime.Now:yyyyMMddHHmmss}.png");



        }







        var opt = new ImageExportOptions



        {



            ExportRange = ExportRange.SetOfViews,



            FilePath = outPath,



            FitDirection = FitDirectionType.Horizontal,



            HLRandWFViewsFileType = ImageFileType.PNG,



            ImageResolution = ImageResolution.DPI_150,



            PixelSize = p["pixelSize"]?.Value<int>() ?? 1920,



            ZoomType = ZoomFitType.FitToPage,



            ShadowViewsFileType = ImageFileType.PNG



        };



        opt.SetViewsAndSheets(new List<ElementId> { view.Id });







        doc.ExportImage(opt);



        return new JObject { ["success"] = true, ["filePath"] = outPath, ["viewName"] = view.Name };



    }







    private static JObject SaveDocument(Document doc, JObject p)



    {



        var newPath = p["filePath"]?.Value<string>();



        if (!string.IsNullOrWhiteSpace(newPath))



        {



            var saveAsOpt = new SaveAsOptions { OverwriteExistingFile = true };



            doc.SaveAs(newPath, saveAsOpt);



            return new JObject { ["success"] = true, ["savedAs"] = newPath };



        }



        else



        {



            doc.Save();



            return new JObject { ["success"] = true, ["savedPath"] = doc.PathName };



        }



    }







    // ─── SUITE EXPERTE NONICATAB PRO / REVIT AUTOMATION ───────────────────────







    private static JObject ListWorksets(Document doc)



    {



        if (!doc.IsWorkshared)



        {



            return new JObject



            {



                ["isWorkshared"] = false,



                ["worksets"] = new JArray()



            };



        }







        var worksets = new FilteredWorksetCollector(doc)



            .OfKind(WorksetKind.UserWorkset)



            .Select(w => new JObject



            {



                ["id"] = w.Id.IntegerValue,



                ["name"] = w.Name,



                ["isOpen"] = w.IsOpen,



                ["isDefault"] = w.IsDefaultWorkset,



                ["isEditable"] = w.IsEditable



            });







        return new JObject



        {



            ["isWorkshared"] = true,



            ["worksets"] = new JArray(worksets)



        };



    }







    private static JObject ListRevisions(Document doc)



    {



        var revisions = new FilteredElementCollector(doc)



            .OfCategory(BuiltInCategory.OST_Revisions)



            .WhereElementIsNotElementType()



            .Cast<Revision>()



            .Select(r => new JObject



            {



                ["id"] = r.Id.Value,



                ["sequenceNumber"] = r.SequenceNumber,



                ["revisionNumber"] = r.RevisionNumber,



                ["revisionDate"] = r.RevisionDate,



                ["description"] = r.Description,



                ["issued"] = r.Issued,



                ["issuedTo"] = r.IssuedTo,



                ["issuedBy"] = r.IssuedBy



            });







        return new JObject { ["revisions"] = new JArray(revisions) };



    }







    private static JObject ListMaterials(Document doc)



    {



        var materials = new FilteredElementCollector(doc)



            .OfClass(typeof(Material))



            .Cast<Material>()



            .OrderBy(m => m.Name)



            .Select(m => new JObject



            {



                ["id"] = m.Id.Value,



                ["name"] = m.Name,



                ["materialClass"] = m.MaterialClass,



                ["color"] = new JObject



                {



                    ["red"] = m.Color.Red,



                    ["green"] = m.Color.Green,



                    ["blue"] = m.Color.Blue



                },



                ["transparency"] = m.Transparency,



                ["shininess"] = m.Shininess,



                ["smoothness"] = m.Smoothness



            });







        return new JObject { ["materials"] = new JArray(materials) };



    }







    private static JObject ListViewTemplates(Document doc)



    {



        var templates = new FilteredElementCollector(doc)



            .OfClass(typeof(View))



            .Cast<View>()



            .Where(v => v.IsTemplate)



            .OrderBy(v => v.Name)



            .Select(v => new JObject



            {



                ["id"] = v.Id.Value,



                ["name"] = v.Name,



                ["viewType"] = v.ViewType.ToString()



            });







        return new JObject { ["templates"] = new JArray(templates) };



    }







    private static JObject CreateSectionBox(UIApplication app, Document doc, JObject p)



    {



        var elementIds = new List<ElementId>();



        if (p["elementIds"] is JArray arr && arr.Count > 0)



        {



            elementIds.AddRange(arr.Select(t => new ElementId(t.Value<long>())));



        }



        else



        {



            var selIds = app.ActiveUIDocument?.Selection?.GetElementIds();



            if (selIds != null && selIds.Count > 0)



            {



                elementIds.AddRange(selIds);



            }



        }







        if (elementIds.Count == 0)



            throw new InvalidOperationException("No element IDs specified and active selection is empty.");







        BoundingBoxXYZ? unionBox = null;



        foreach (var id in elementIds)



        {



            var el = doc.GetElement(id);



            if (el == null) continue;



            var bbox = el.get_BoundingBox(null);



            if (bbox == null) continue;







            if (unionBox == null)



            {



                unionBox = new BoundingBoxXYZ



                {



                    Min = new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z),



                    Max = new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Max.Z)



                };



            }



            else



            {



                unionBox.Min = new XYZ(



                    Math.Min(unionBox.Min.X, bbox.Min.X),



                    Math.Min(unionBox.Min.Y, bbox.Min.Y),



                    Math.Min(unionBox.Min.Z, bbox.Min.Z)



                );



                unionBox.Max = new XYZ(



                    Math.Max(unionBox.Max.X, bbox.Max.X),



                    Math.Max(unionBox.Max.Y, bbox.Max.Y),



                    Math.Max(unionBox.Max.Z, bbox.Max.Z)



                );



            }



        }







        if (unionBox == null)



            throw new InvalidOperationException("Could not compute bounding box for the specified elements.");







        double padFt = p["paddingMm"] != null ? MmToFt(p["paddingMm"]!.Value<double>()) : 1.0;



        unionBox.Min = new XYZ(unionBox.Min.X - padFt, unionBox.Min.Y - padFt, unionBox.Min.Z - padFt);



        unionBox.Max = new XYZ(unionBox.Max.X + padFt, unionBox.Max.Y + padFt, unionBox.Max.Z + padFt);







        return WithTransaction(doc, "MCP: Create Section Box", () =>



        {



            View3D? view3d = doc.ActiveView as View3D;



            if (view3d == null || view3d.IsTemplate)



            {



                view3d = new FilteredElementCollector(doc)



                    .OfClass(typeof(View3D))



                    .Cast<View3D>()



                    .FirstOrDefault(v => !v.IsTemplate && !v.IsPerspective);







                if (view3d == null)



                {



                    var viewFamilyType = new FilteredElementCollector(doc)



                        .OfClass(typeof(ViewFamilyType))



                        .Cast<ViewFamilyType>()



                        .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional)



                        ?? throw new InvalidOperationException("No 3D ViewFamilyType found.");



                    view3d = View3D.CreateIsometric(doc, viewFamilyType.Id);



                    view3d.Name = $"MCP_3D_Box_{DateTime.Now:HHmmss}";



                }



            }







            view3d.SetSectionBox(unionBox);



            view3d.IsSectionBoxActive = true;







            if (app.ActiveUIDocument != null)



            {



                app.ActiveUIDocument.ActiveView = view3d;



            }







            return new JObject



            {



                ["success"] = true,



                ["viewId"] = view3d.Id.Value,



                ["viewName"] = view3d.Name,



                ["min"] = $"{unionBox.Min.X:F2}, {unionBox.Min.Y:F2}, {unionBox.Min.Z:F2}",



                ["max"] = $"{unionBox.Max.X:F2}, {unionBox.Max.Y:F2}, {unionBox.Max.Z:F2}"



            };



        }, p);



    }







    private static JObject FindElementsByParameter(Document doc, JObject p)



    {



        var paramName = GetStr(p, "parameterName");



        var searchValue = GetStr(p, "searchValue");



        var exactMatch = p["exactMatch"]?.Value<bool>() ?? false;



        var limit = p["limit"]?.Value<int>() ?? 100;







        var elements = new FilteredElementCollector(doc)



            .WhereElementIsNotElementType()



            .Cast<Element>()



            .Where(e =>



            {



                var param = e.LookupParameter(paramName);



                if (param == null) return false;



                var valStr = param.AsValueString() ?? param.AsString();



                if (valStr == null) return false;



                return exactMatch



                    ? string.Equals(valStr, searchValue, StringComparison.OrdinalIgnoreCase)



                    : valStr.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;



            })



            .Take(limit)



            .Select(e => new JObject



            {



                ["id"] = e.Id.Value,



                ["name"] = e.Name,



                ["category"] = e.Category?.Name ?? "Unknown",



                ["parameterValue"] = e.LookupParameter(paramName)?.AsValueString() ?? e.LookupParameter(paramName)?.AsString() ?? ""



            });







        return new JObject { ["matches"] = new JArray(elements) };



    }







    private static JObject CopyParameterValues(Document doc, JObject p)



    {



        var sourceParam = GetStr(p, "sourceParameter");



        var targetParam = GetStr(p, "targetParameter");



        var elementIds = p["elementIds"] is JArray arr



            ? arr.Select(t => new ElementId(t.Value<long>())).ToList()



            : new List<ElementId>();







        return WithTransaction(doc, "MCP: Copy Parameter Values", () =>



        {



            int copiedCount = 0;



            var errors = new JArray();







            var elements = elementIds.Count > 0



                ? elementIds.Select(id => doc.GetElement(id)).Where(e => e != null)



                : new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();







            foreach (var el in elements)



            {



                var src = el.LookupParameter(sourceParam);



                var tgt = el.LookupParameter(targetParam);



                if (src == null || tgt == null || tgt.IsReadOnly) continue;







                try



                {



                    switch (src.StorageType)



                    {



                        case StorageType.String:



                            tgt.Set(src.AsString() ?? "");



                            copiedCount++;



                            break;



                        case StorageType.Double:



                            tgt.Set(src.AsDouble());



                            copiedCount++;



                            break;



                        case StorageType.Integer:



                            tgt.Set(src.AsInteger());



                            copiedCount++;



                            break;



                        case StorageType.ElementId:



                            tgt.Set(src.AsElementId());



                            copiedCount++;



                            break;



                    }



                }



                catch (Exception ex)



                {



                    errors.Add($"Element {el.Id.Value}: {ex.Message}");



                }



            }







            return new JObject



            {



                ["success"] = true,



                ["copiedCount"] = copiedCount,



                ["errors"] = errors



            };



        }, p);



    }







    private static JObject DuplicateSheet(Document doc, JObject p)



    {



        var sourceSheetId = new ElementId(p["sheetId"]!.Value<long>());



        var sourceSheet = doc.GetElement(sourceSheetId) as ViewSheet



            ?? throw new InvalidOperationException($"Sheet not found with ID {sourceSheetId.Value}");







        var newNumber = p["sheetNumber"]?.Value<string>() ?? $"{sourceSheet.SheetNumber}-COPY";



        var newName = p["sheetName"]?.Value<string>() ?? $"{sourceSheet.Name} (Copie)";







        return WithTransaction(doc, "MCP: Duplicate Sheet", () =>



        {



            var tb = new FilteredElementCollector(doc, sourceSheet.Id)



                .OfCategory(BuiltInCategory.OST_TitleBlocks)



                .FirstOrDefault();







            ViewSheet newSheet = tb != null



                ? ViewSheet.Create(doc, tb.GetTypeId())



                : ViewSheet.Create(doc, ElementId.InvalidElementId);







            newSheet.SheetNumber = newNumber;



            newSheet.Name = newName;







            return new JObject



            {



                ["success"] = true,



                ["sheetId"] = newSheet.Id.Value,



                ["sheetNumber"] = newSheet.SheetNumber,



                ["sheetName"] = newSheet.Name



            };



        }, p);



    }







    private static JObject PurgeUnusedTypes(Document doc, JObject p)



    {



        var allSymbols = new FilteredElementCollector(doc)



            .OfClass(typeof(FamilySymbol))



            .Cast<FamilySymbol>()



            .ToList();







        var placedTypeIds = new HashSet<long>(



            new FilteredElementCollector(doc)



                .WhereElementIsNotElementType()



                .Select(e => e.GetTypeId().Value)



        );







        var unused = new JArray();



        foreach (var sym in allSymbols)



        {



            if (!placedTypeIds.Contains(sym.Id.Value))



            {



                unused.Add(new JObject



                {



                    ["symbolId"] = sym.Id.Value,



                    ["familyName"] = sym.FamilyName,



                    ["typeName"] = sym.Name



                });



            }



        }







        return new JObject



        {



            ["unusedCount"] = unused.Count,



            ["unusedTypes"] = unused



        };



    }







    private static JObject CreateDetailLine(Document doc, JObject p)



    {



        var startX = MmToFt(p["startX"]!.Value<double>());



        var startY = MmToFt(p["startY"]!.Value<double>());



        var endX = MmToFt(p["endX"]!.Value<double>());



        var endY = MmToFt(p["endY"]!.Value<double>());







        var activeView = doc.ActiveView;



        if (activeView.ViewType == ViewType.ThreeD)



            throw new InvalidOperationException("Detail lines cannot be created in 3D views. Switch to a floor plan, ceiling plan, section, or drafting view.");







        return WithTransaction(doc, "MCP: Create Detail Line", () =>



        {



            var p1 = new XYZ(startX, startY, 0);



            var p2 = new XYZ(endX, endY, 0);



            var geomLine = Line.CreateBound(p1, p2);



            var detailCurve = doc.Create.NewDetailCurve(activeView, geomLine);







            return new JObject



            {



                ["success"] = true,



                ["lineId"] = detailCurve.Id.Value,



                ["lengthMm"] = (p2 - p1).GetLength() * 304.8



            };



        }, p);



    }



}



