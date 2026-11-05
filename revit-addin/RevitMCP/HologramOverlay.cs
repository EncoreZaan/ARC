using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace RevitMCP;

/// <summary>
/// Hologram Ghost Geometry descriptor.
/// </summary>
public class GhostGeometry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Label { get; set; } = "Ghost Object";
    public string Type { get; set; } = "box"; // box, wall, heatmap, heatmap_clash
    public XYZ Origin { get; set; } = XYZ.Zero;
    public XYZ Dimensions { get; set; } = new XYZ(1, 1, 1); // width, depth, height in feet
    public byte R { get; set; } = 0;
    public byte G { get; set; } = 200;
    public byte B { get; set; } = 255;
    public byte Alpha { get; set; } = 120; // 0-255 (semi-transparent)
    public double RotationDeg { get; set; } = 0;
}

/// <summary>
/// DirectContext3D server for live holographic ghost display and HUD heatmaps in Revit 3D viewport.
/// Operates directly in Revit's DirectX viewport without modifying the document model.
/// </summary>
public class HologramOverlayServer : IDirectContext3DServer
{
    private static HologramOverlayServer? _instance;
    public static HologramOverlayServer Instance => _instance ??= new HologramOverlayServer();

    private readonly Guid _serverId = new("4A6E829B-98F1-4E1B-9430-18D37B47E201");
    private bool _isRegistered;
    private bool _isActive = true;
    private readonly List<GhostGeometry> _geometries = new();
    private readonly object _lock = new();

    public Guid GetServerId() => _serverId;
    public ExternalServiceId GetServiceId() => ExternalServices.BuiltInExternalServices.DirectContext3DService;
    public string GetName() => "RevitMCP Holographic Ghost Overlay";
    public string GetDescription() => "HUD Augmenté et prévisualisation fantôme temps réel dans la vue 3D Revit.";
    public string GetVendorId() => "RevitMCP";
    public string GetApplicationId() => "RevitMCP.Hologram";
    public string GetSourceId() => "RevitMCP";
    public bool UsesHandles() => false;

    public bool CanExecute(View view)
    {
        return view is View3D && _isActive;
    }

    public Outline GetBoundingBox(View view)
    {
        lock (_lock)
        {
            if (_geometries.Count == 0)
                return new Outline(new XYZ(-500, -500, -100), new XYZ(500, 500, 100));

            XYZ min = new(double.MaxValue, double.MaxValue, double.MaxValue);
            XYZ max = new(double.MinValue, double.MinValue, double.MinValue);

            foreach (var g in _geometries)
            {
                var halfDim = g.Dimensions * 0.5;
                min = new XYZ(
                    Math.Min(min.X, g.Origin.X - halfDim.X - 5),
                    Math.Min(min.Y, g.Origin.Y - halfDim.Y - 5),
                    Math.Min(min.Z, g.Origin.Z - halfDim.Z - 5)
                );
                max = new XYZ(
                    Math.Max(max.X, g.Origin.X + halfDim.X + 5),
                    Math.Max(max.Y, g.Origin.Y + halfDim.Y + 5),
                    Math.Max(max.Z, g.Origin.Z + halfDim.Z + 5)
                );
            }
            return new Outline(min, max);
        }
    }

    public bool UseInTransparentPass(View view) => true;

    public void RenderScene(View view, DisplayStyle displayStyle)
    {
        List<GhostGeometry> list;
        lock (_lock)
        {
            if (_geometries.Count == 0) return;
            list = new List<GhostGeometry>(_geometries);
        }

        try
        {
            DrawContext.SetWorldTransform(Transform.Identity);
            var vf = new VertexFormat(VertexFormatBits.PositionColored);
            var effect = new EffectInstance(VertexFormatBits.PositionColored);

            foreach (var g in list)
            {
                RenderGhostBox(g, vf, effect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectContext3D] Render error: {ex.Message}");
        }
    }

    private static void RenderGhostBox(GhostGeometry g, VertexFormat vf, EffectInstance effect)
    {
        var halfX = g.Dimensions.X * 0.5;
        var halfY = g.Dimensions.Y * 0.5;
        var halfZ = g.Dimensions.Z * 0.5;
        var ox = g.Origin.X;
        var oy = g.Origin.Y;
        var oz = g.Origin.Z;

        var color = new ColorWithTransparency(g.R, g.G, g.B, g.Alpha);

        XYZ[] pts = new XYZ[8]
        {
            new(ox - halfX, oy - halfY, oz - halfZ), // 0
            new(ox + halfX, oy - halfY, oz - halfZ), // 1
            new(ox + halfX, oy + halfY, oz - halfZ), // 2
            new(ox - halfX, oy + halfY, oz - halfZ), // 3
            new(ox - halfX, oy - halfY, oz + halfZ), // 4
            new(ox + halfX, oy - halfY, oz + halfZ), // 5
            new(ox + halfX, oy + halfY, oz + halfZ), // 6
            new(ox - halfX, oy + halfY, oz + halfZ)  // 7
        };

        if (Math.Abs(g.RotationDeg) > 0.01)
        {
            var rad = g.RotationDeg * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            for (int i = 0; i < 8; i++)
            {
                var dx = pts[i].X - ox;
                var dy = pts[i].Y - oy;
                pts[i] = new XYZ(ox + dx * cos - dy * sin, oy + dx * sin + dy * cos, pts[i].Z);
            }
        }

        int[] triIndices = new int[]
        {
            0, 1, 2,  0, 2, 3,
            4, 6, 5,  4, 7, 6,
            0, 5, 1,  0, 4, 5,
            2, 7, 3,  2, 6, 7,
            0, 7, 4,  0, 3, 7,
            1, 6, 2,  1, 5, 6
        };

        int vertexCount = triIndices.Length;
        int vertexBufferSize = VertexPositionColored.GetSizeInFloats() * sizeof(float) * vertexCount;

        using var vb = new VertexBuffer(vertexBufferSize);
        vb.Map(vertexBufferSize);
        var stream = vb.GetVertexStreamPositionColored();

        for (int i = 0; i < vertexCount; i++)
        {
            stream.AddVertex(new VertexPositionColored(pts[triIndices[i]], color));
        }
        vb.Unmap();

        int indexBufferSize = sizeof(int) * vertexCount;
        using var ib = new IndexBuffer(indexBufferSize);
        ib.Map(indexBufferSize);
        var indexStream = ib.GetIndexStreamTriangle();
        for (int i = 0; i < vertexCount; i += 3)
        {
            indexStream.AddTriangle(new IndexTriangle(i, i + 1, i + 2));
        }
        ib.Unmap();

        DrawContext.FlushBuffer(vb, vertexCount, ib, vertexCount, vf, effect, PrimitiveType.TriangleList, 0, vertexCount / 3);
    }

    public void RegisterService()
    {
        if (_isRegistered) return;
        try
        {
            var service = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
            if (service != null)
            {
                var registered = service.GetRegisteredServerIds();
                if (!registered.Contains(_serverId))
                {
                    service.AddServer(this);
                }
                if (service is MultiServerService msService)
                {
                    var activeServers = msService.GetActiveServerIds();
                    if (!activeServers.Contains(_serverId))
                    {
                        activeServers.Add(_serverId);
                        msService.SetActiveServers(activeServers);
                    }
                }
                _isRegistered = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DirectContext3D] Register error: {ex.Message}");
        }
    }

    public void UnregisterService()
    {
        if (!_isRegistered) return;
        try
        {
            var service = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
            if (service != null && service.GetRegisteredServerIds().Contains(_serverId))
            {
                if (service is MultiServerService msService)
                {
                    var activeServers = msService.GetActiveServerIds();
                    activeServers.Remove(_serverId);
                    msService.SetActiveServers(activeServers);
                }
                service.RemoveServer(_serverId);
            }
            _isRegistered = false;
        }
        catch { }
    }

    public void AddGhost(GhostGeometry ghost)
    {
        lock (_lock)
        {
            _geometries.Add(ghost);
            _isActive = true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _geometries.Clear();
        }
    }

    public int Count
    {
        get { lock (_lock) { return _geometries.Count; } }
    }

    public IReadOnlyList<GhostGeometry> GetGeometries()
    {
        lock (_lock) { return _geometries.ToArray(); }
    }
}
