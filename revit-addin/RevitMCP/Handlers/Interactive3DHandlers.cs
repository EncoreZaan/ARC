using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RevitMCP;

public partial class RevitEventHandler
{
    /// <summary>
    /// Exporte la maquette ou la vue 3D Revit sous la forme d'un visualiseur Web 3D interactif Three.js
    /// autonome avec OrbitControls, inspection d'éléments au clic et support Generative UI.
    /// </summary>
    private static JObject ExportInteractive3DViewer(Document doc, JObject p)
    {
        var viewIdVal = p["viewId"]?.Value<long?>();
        var view = viewIdVal.HasValue
            ? doc.GetElement(new ElementId(viewIdVal.Value)) as View
            : doc.ActiveView;

        if (view == null)
            throw new RevitMcpException("VIEW_NOT_FOUND", "Aucune vue valide n'a été trouvée pour l'export 3D interactif.");

        var maxElements = p["maxElements"]?.Value<int>() ?? 500;
        var categoriesFilter = p["categories"] as JArray;
        var customOutputDir = p["outputDir"]?.Value<string>();

        var collector = new FilteredElementCollector(doc, view.Id)
            .WhereElementIsNotElementType();

        var elements = collector.ToElements();
        var modelNodes = new JArray();
        var totalTriangles = 0;

        var opt = new Options
        {
            View = view,
            ComputeReferences = false,
            DetailLevel = ViewDetailLevel.Coarse
        };

        var validCategories = new HashSet<BuiltInCategory>
        {
            BuiltInCategory.OST_Walls,
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_Roofs,
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Windows,
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_Columns,
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_Stairs,
            BuiltInCategory.OST_Ceilings,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_PipeCurves
        };

        var count = 0;
        foreach (var el in elements)
        {
            if (count >= maxElements) break;
            if (el.Category == null) continue;

            var bic = (BuiltInCategory)el.Category.Id.Value;
            if (!validCategories.Contains(bic) && categoriesFilter == null) continue;

            var geomElem = el.get_Geometry(opt);
            var (r, g, b, alpha) = GetCategoryColor(bic);

            var verticesList = new List<double>();
            var indicesList = new List<int>();

            if (geomElem != null)
            {
                ExtractTrianglesFromGeometry(geomElem, verticesList, indicesList, 100);
            }

            if (indicesList.Count == 0)
            {
                var bbox = el.get_BoundingBox(view);
                if (bbox != null)
                {
                    GenerateBBoxMesh(bbox, verticesList, indicesList);
                }
            }

            if (indicesList.Count > 0)
            {
                totalTriangles += indicesList.Count / 3;
                modelNodes.Add(new JObject
                {
                    ["id"] = el.Id.Value,
                    ["name"] = el.Name,
                    ["category"] = el.Category.Name,
                    ["r"] = r,
                    ["g"] = g,
                    ["b"] = b,
                    ["alpha"] = alpha,
                    ["vertices"] = new JArray(verticesList),
                    ["indices"] = new JArray(indicesList)
                });
                count++;
            }
        }

        var htmlContent = GenerateThreeJsViewerHtml(doc.Title, view.Name, modelNodes, totalTriangles);

        string targetDir = !string.IsNullOrEmpty(customOutputDir) && Directory.Exists(customOutputDir)
            ? customOutputDir
            : Path.Combine(Path.GetTempPath(), "RevitMCP_3D_Viewer");

        Directory.CreateDirectory(targetDir);
        var filename = $"Revit3D_{view.Id.Value}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var fullPath = Path.Combine(targetDir, filename);
        File.WriteAllText(fullPath, htmlContent, Encoding.UTF8);

        return new JObject
        {
            ["success"] = true,
            ["message"] = $"Visualiseur 3D interactif généré avec succès ({count} éléments, {totalTriangles} triangles).",
            ["viewerFilePath"] = fullPath,
            ["elementsCount"] = count,
            ["totalTriangles"] = totalTriangles,
            ["viewName"] = view.Name,
            ["modelTitle"] = doc.Title,
            ["embedCode"] = $"<agent-embed src=\"file:///{fullPath.Replace('\\', '/')}\"></agent-embed>"
        };
    }

    private static (byte r, byte g, byte b, double alpha) GetCategoryColor(BuiltInCategory cat)
    {
        return cat switch
        {
            BuiltInCategory.OST_Walls => (220, 220, 225, 1.0),
            BuiltInCategory.OST_Floors => (180, 185, 190, 1.0),
            BuiltInCategory.OST_Roofs => (140, 70, 60, 1.0),
            BuiltInCategory.OST_Doors => (170, 110, 60, 1.0),
            BuiltInCategory.OST_Windows => (130, 200, 250, 0.45),
            BuiltInCategory.OST_Furniture => (60, 160, 230, 1.0),
            BuiltInCategory.OST_Columns or BuiltInCategory.OST_StructuralColumns => (200, 200, 205, 1.0),
            BuiltInCategory.OST_StructuralFraming => (160, 160, 170, 1.0),
            BuiltInCategory.OST_DuctCurves => (70, 190, 130, 1.0),
            BuiltInCategory.OST_PipeCurves => (80, 130, 220, 1.0),
            _ => (190, 190, 195, 1.0)
        };
    }

    private static void ExtractTrianglesFromGeometry(GeometryElement geomElem, List<double> vertices, List<int> indices, int maxSolids)
    {
        var solidCount = 0;
        foreach (var obj in geomElem)
        {
            if (solidCount >= maxSolids) break;
            if (obj is Solid solid && solid.Faces.Size > 0 && solid.Volume > 0.0001)
            {
                foreach (Face face in solid.Faces)
                {
                    var mesh = face.Triangulate(0.5);
                    if (mesh == null) continue;

                    var baseIdx = vertices.Count / 3;
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        var ptMm = UnitConversions.FeetToMm(mesh.Vertices[i]);
                        vertices.Add(Math.Round(ptMm.X / 1000.0, 3));
                        vertices.Add(Math.Round(ptMm.Z / 1000.0, 3));
                        vertices.Add(Math.Round(-ptMm.Y / 1000.0, 3));
                    }

                    for (int i = 0; i < mesh.NumTriangles; i++)
                    {
                        var t = mesh.get_Triangle(i);
                        indices.Add(baseIdx + (int)t.get_Index(0));
                        indices.Add(baseIdx + (int)t.get_Index(1));
                        indices.Add(baseIdx + (int)t.get_Index(2));
                    }
                }
                solidCount++;
            }
            else if (obj is GeometryInstance gi)
            {
                var instGeom = gi.GetInstanceGeometry();
                if (instGeom != null)
                {
                    ExtractTrianglesFromGeometry(instGeom, vertices, indices, maxSolids - solidCount);
                }
            }
        }
    }

    private static void GenerateBBoxMesh(BoundingBoxXYZ bbox, List<double> vertices, List<int> indices)
    {
        var min = UnitConversions.FeetToMm(bbox.Min);
        var max = UnitConversions.FeetToMm(bbox.Max);

        double x0 = min.X / 1000.0, x1 = max.X / 1000.0;
        double y0 = min.Z / 1000.0, y1 = max.Z / 1000.0;
        double z0 = -max.Y / 1000.0, z1 = -min.Y / 1000.0;

        var baseIdx = vertices.Count / 3;
        double[][] pts = new double[][]
        {
            new[] { x0, y0, z0 }, new[] { x1, y0, z0 }, new[] { x1, y1, z0 }, new[] { x0, y1, z0 },
            new[] { x0, y0, z1 }, new[] { x1, y0, z1 }, new[] { x1, y1, z1 }, new[] { x0, y1, z1 }
        };

        foreach (var p in pts)
        {
            vertices.Add(Math.Round(p[0], 3));
            vertices.Add(Math.Round(p[1], 3));
            vertices.Add(Math.Round(p[2], 3));
        }

        int[] boxIndices = new int[]
        {
            0, 2, 1, 0, 3, 2,  4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,  2, 3, 7, 2, 7, 6,
            0, 4, 7, 0, 7, 3,  1, 2, 6, 1, 6, 5
        };

        foreach (var idx in boxIndices)
        {
            indices.Add(baseIdx + idx);
        }
    }

    private static string GenerateThreeJsViewerHtml(string title, string viewName, JArray modelNodes, int totalTriangles)
    {
        var nodesJson = modelNodes.ToString(Newtonsoft.Json.Formatting.None);

        return $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
  <meta charset=""UTF-8"">
  <title>Revit 3D Viewer - {title}</title>
  <script src=""https://www.gstatic.com/antigravity/web/dev/tailwindcss.min.js""></script>
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js""></script>
  <script src=""https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js""></script>
  <style>
    body, html {{ margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; font-family: ui-sans-serif, system-ui, sans-serif; }}
    #canvas3d {{ width: 100%; height: 100%; display: block; }}
  </style>
</head>
<body class=""bg-slate-950 text-slate-100 flex flex-col h-screen"">

  <header class=""absolute top-3 left-3 right-3 z-10 flex items-center justify-between pointer-events-none"">
    <div class=""bg-slate-900/85 backdrop-blur border border-slate-700/60 rounded-xl px-4 py-2.5 shadow-lg pointer-events-auto flex items-center gap-3"">
      <div class=""w-3 h-3 rounded-full bg-cyan-400 animate-pulse""></div>
      <div>
        <div class=""text-sm font-semibold text-slate-100"">{title}</div>
        <div class=""text-xs text-slate-400"">Vue 3D : {viewName} • {modelNodes.Count} éléments • {totalTriangles:N0} facettes</div>
      </div>
    </div>

    <div class=""bg-slate-900/85 backdrop-blur border border-slate-700/60 rounded-xl px-3 py-1.5 shadow-lg pointer-events-auto flex items-center gap-2 text-xs"">
      <button id=""btnReset"" class=""px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 transition"">⟲ Recentrer</button>
      <button id=""btnWireframe"" class=""px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 transition"">Fil de fer</button>
      <button id=""btnGhost"" class=""px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 transition"">Mode Fantôme</button>
    </div>
  </header>

  <main class=""flex-1 relative w-full h-full"">
    <canvas id=""canvas3d""></canvas>

    <div id=""tooltip"" class=""absolute bottom-4 left-4 z-10 bg-slate-900/90 backdrop-blur border border-cyan-500/40 rounded-xl p-3 shadow-xl max-w-xs transition-opacity duration-200 pointer-events-auto hidden"">
      <div id=""tooltipCat"" class=""text-[11px] font-bold uppercase tracking-wider text-cyan-400"">MUR DE BASE</div>
      <div id=""tooltipName"" class=""text-sm font-semibold text-slate-100 mt-0.5"">Mur générique 200mm</div>
      <div id=""tooltipId"" class=""text-xs text-slate-400 mt-1 font-mono"">ID: #123456</div>
    </div>
  </main>

  <script>
    const dataNodes = {nodesJson};

    const container = document.getElementById('canvas3d');
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0b0f19);

    const camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 1000);
    camera.position.set(20, 20, 20);

    const renderer = new THREE.WebGLRenderer({{ canvas: container, antialias: true, alpha: true }});
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    const controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.65);
    scene.add(ambientLight);

    const dirLight1 = new THREE.DirectionalLight(0xffffff, 0.7);
    dirLight1.position.set(30, 50, 40);
    scene.add(dirLight1);

    const dirLight2 = new THREE.DirectionalLight(0x7dd3fc, 0.3);
    dirLight2.position.set(-30, -20, -30);
    scene.add(dirLight2);

    const grid = new THREE.GridHelper(50, 50, 0x1e293b, 0x0f172a);
    grid.position.y = -0.01;
    scene.add(grid);

    const meshGroup = new THREE.Group();
    scene.add(meshGroup);

    const meshes = [];
    let isWireframe = false;
    let isGhost = false;

    dataNodes.forEach(node => {{
      const geom = new THREE.BufferGeometry();
      geom.setAttribute('position', new THREE.Float32BufferAttribute(node.vertices, 3));
      geom.setIndex(node.indices);
      geom.computeVertexNormals();

      const color = new THREE.Color(node.r / 255, node.g / 255, node.b / 255);
      const isTrans = node.alpha < 0.99;

      const mat = new THREE.MeshStandardMaterial({{
        color: color,
        roughness: 0.4,
        metalness: 0.1,
        transparent: isTrans,
        opacity: node.alpha,
        side: THREE.DoubleSide
      }});

      const mesh = new THREE.Mesh(geom, mat);
      mesh.userData = {{
        id: node.id,
        name: node.name,
        category: node.category,
        originalOpacity: node.alpha
      }};

      meshGroup.add(mesh);
      meshes.push(mesh);
    }});

    const bbox = new THREE.Box3().setFromObject(meshGroup);
    if (!bbox.isEmpty()) {{
      const center = bbox.getCenter(new THREE.Vector3());
      const size = bbox.getSize(new THREE.Vector3());
      const maxDim = Math.max(size.x, size.y, size.z);

      meshGroup.position.sub(center);
      grid.position.y = -size.y * 0.5 - 0.1;

      camera.position.set(maxDim * 1.3, maxDim * 1.1, maxDim * 1.3);
      camera.lookAt(0, 0, 0);
      controls.target.set(0, 0, 0);
    }}

    const raycaster = new THREE.Raycaster();
    const mouse = new THREE.Vector2();
    const tooltip = document.getElementById('tooltip');
    let hoveredMesh = null;

    window.addEventListener('pointermove', (e) => {{
      mouse.x = (e.clientX / window.innerWidth) * 2 - 1;
      mouse.y = -(e.clientY / window.innerHeight) * 2 + 1;

      raycaster.setFromCamera(mouse, camera);
      const intersects = raycaster.intersectObjects(meshes);

      if (intersects.length > 0) {{
        const target = intersects[0].object;
        if (hoveredMesh !== target) {{
          if (hoveredMesh && !hoveredMesh.userData.selected) {{
            hoveredMesh.material.emissive?.setHex(0x000000);
          }}
          hoveredMesh = target;
          hoveredMesh.material.emissive?.setHex(0x004466);

          document.getElementById('tooltipCat').textContent = target.userData.category;
          document.getElementById('tooltipName').textContent = target.userData.name;
          document.getElementById('tooltipId').textContent = 'ID Revit: #' + target.userData.id;
          tooltip.classList.remove('hidden');
        }}
      }} else {{
        if (hoveredMesh && !hoveredMesh.userData.selected) {{
          hoveredMesh.material.emissive?.setHex(0x000000);
        }}
        hoveredMesh = null;
        tooltip.classList.add('hidden');
      }}
    }});

    document.getElementById('btnReset').addEventListener('click', () => {{
      controls.reset();
    }});

    document.getElementById('btnWireframe').addEventListener('click', () => {{
      isWireframe = !isWireframe;
      meshes.forEach(m => m.material.wireframe = isWireframe);
    }});

    document.getElementById('btnGhost').addEventListener('click', () => {{
      isGhost = !isGhost;
      meshes.forEach(m => {{
        m.material.transparent = true;
        m.material.opacity = isGhost ? 0.25 : m.userData.originalOpacity;
      }});
    }});

    window.addEventListener('resize', () => {{
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    }});

    function animate() {{
      requestAnimationFrame(animate);
      controls.update();
      renderer.render(scene, camera);
    }}
    animate();
  </script>
</body>
</html>";
    }
}
