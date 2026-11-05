import os
import sys
import math
import json
from pathlib import Path
from typing import Optional, List, Dict, Any

from mcp.server.mcpserver import MCPServer
import ezdxf
from ezdxf.addons.drawing import Frontend, RenderContext, layout as drawing_layout, config as drawing_config
from ezdxf.addons.drawing.pymupdf import PyMuPdfBackend
from ezdxf.addons.drawing.matplotlib import MatplotlibBackend
import pymupdf
import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages

mcp = MCPServer("cad-mcp")

def _resolve_path(path_str: str) -> Path:
    if not path_str:
        raise ValueError("Chemin de fichier vide.")
    clean = str(path_str).strip().strip("'\"")
    return Path(clean).expanduser().resolve()

def _check_file_locked(p: Path) -> None:
    if not p.is_file():
        raise FileNotFoundError(
            f"Fichier DXF introuvable au chemin résolu : '{p.as_posix()}'. "
            f"Système hôte : {sys.platform} ({os.name})."
        )
    try:
        with open(p, "r+"):
            pass
    except PermissionError:
        raise PermissionError(f"Le fichier DXF '{p.as_posix()}' est actuellement verrouillé ou ouvert dans AutoCAD/Revit.")
    except Exception:
        pass


@mcp.tool()
def dxf_info(path: str) -> str:
    """
    Retourne les métadonnées complètes d'un fichier DXF : version d'AutoCAD, unités (insunits),
    calques (avec couleur et statut gelé/masqué), limites spatiales extmin/extmax et compteurs d'entités.
    Exemple : dxf_info("C:/Dessins/MicroHabitat.dxf")
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        doc = ezdxf.readfile(str(p))
        msp = doc.modelspace()
        
        layers = []
        for layer in doc.layers:
            layers.append({
                "name": layer.dxf.name,
                "color": layer.dxf.color,
                "is_off": layer.is_off(),
                "is_locked": layer.is_locked()
            })
            
        units_code = doc.header.get("$INSUNITS", 0)
        units_map = {
            0: "Unspecified", 1: "Inches", 2: "Feet", 3: "Miles", 4: "Millimeters",
            5: "Centimeters", 6: "Meters"
        }
        
        extmin = doc.header.get("$EXTMIN", (0, 0, 0))
        extmax = doc.header.get("$EXTMAX", (0, 0, 0))
        
        entity_counts = {}
        for e in msp:
            t = e.dxftype()
            entity_counts[t] = entity_counts.get(t, 0) + 1
            
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "dxfversion": doc.dxfversion,
            "acadRelease": doc.acad_release,
            "units": units_map.get(units_code, f"Code {units_code}"),
            "extmin": {"x": round(extmin[0], 2), "y": round(extmin[1], 2), "z": round(extmin[2], 2)},
            "extmax": {"x": round(extmax[0], 2), "y": round(extmax[1], 2), "z": round(extmax[2], 2)},
            "layerCount": len(layers),
            "layers": layers,
            "entityCounts": entity_counts
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "DXF_READ_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_list_entities(path: str, layer: Optional[str] = None, layout: Optional[str] = "Model") -> str:
    """
    Liste les entités géométriques d'un fichier DXF (lignes, polylignes, arcs, cercles, textes)
    avec leurs coordonnées en millimètres.
    Exemple : dxf_list_entities("C:/Dessins/MicroHabitat.dxf", layer="MURS")
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        doc = ezdxf.readfile(str(p))
        space = doc.layout(layout) if layout and layout != "Model" else doc.modelspace()
        
        entities = []
        for e in space:
            if layer and e.dxf.layer.upper() != layer.upper():
                continue
            
            ent_info = {
                "handle": e.dxf.handle,
                "dxftype": e.dxftype(),
                "layer": e.dxf.layer,
                "color": e.dxf.color
            }
            
            if e.dxftype() == "LINE":
                ent_info["start"] = {"x": round(e.dxf.start.x, 2), "y": round(e.dxf.start.y, 2), "z": round(e.dxf.start.z, 2)}
                ent_info["end"] = {"x": round(e.dxf.end.x, 2), "y": round(e.dxf.end.y, 2), "z": round(e.dxf.end.z, 2)}
            elif e.dxftype() == "TEXT":
                ent_info["text"] = e.dxf.text
                ent_info["insert"] = {"x": round(e.dxf.insert.x, 2), "y": round(e.dxf.insert.y, 2)}
                ent_info["height"] = round(e.dxf.height, 2)
            elif e.dxftype() == "CIRCLE":
                ent_info["center"] = {"x": round(e.dxf.center.x, 2), "y": round(e.dxf.center.y, 2)}
                ent_info["radius"] = round(e.dxf.radius, 2)
            elif e.dxftype() in ("LWPOLYLINE", "POLYLINE"):
                ent_info["is_closed"] = e.is_closed
                ent_info["points"] = [{"x": round(pt[0], 2), "y": round(pt[1], 2)} for pt in e.get_points()[:20]]
                
            entities.append(ent_info)
            if len(entities) >= 500:
                break
                
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "entityCount": len(entities),
            "entities": entities
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "DXF_READ_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


def _get_layout_paper_size_mm(space) -> tuple[float, float]:
    pw, ph = None, None
    if hasattr(space, "dxf") and hasattr(space.dxf, "paper_width") and space.dxf.paper_width > 0:
        pw = float(space.dxf.paper_width)
        ph = float(space.dxf.paper_height)
    elif hasattr(space, "get_paper_limits"):
        try:
            lim_min, lim_max = space.get_paper_limits()
            pw = float(abs(lim_max.x - lim_min.x))
            ph = float(abs(lim_max.y - lim_min.y))
        except Exception:
            pass
    if not pw or not ph or pw <= 0 or ph <= 0:
        pw, ph = 420.0, 297.0
    return pw, ph


@mcp.tool()
def dxf_render(path: str, outputPath: str, layout: Optional[str] = "Model", dpi: Optional[int] = 150, format: Optional[str] = "png", scale: Optional[float] = None) -> str:
    """
    Rend fidèlement un fichier DXF à l'échelle sous forme d'image PNG ou document PDF vectoriel avec tirets réels.
    Exemple : dxf_render("C:/Dessins/Planche.dxf", "C:/Rendus/Planche.pdf", format="pdf", dpi=300)
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        out_p = _resolve_path(outputPath)
        out_p.parent.mkdir(parents=True, exist_ok=True)

        doc = ezdxf.readfile(str(p))
        space = doc.layout(layout) if layout and layout != "Model" else doc.modelspace()
        
        ext = out_p.suffix.lower().replace(".", "")
        fmt = (format or (ext if ext in ("png", "pdf") else "png")).lower()
        
        if fmt == "pdf":
            be = PyMuPdfBackend()
            cfg = drawing_config.Configuration(
                background_policy=drawing_config.BackgroundPolicy.WHITE,
                color_policy=drawing_config.ColorPolicy.BLACK,
                lineweight_scaling=1.0,
                min_lineweight=0.09
            )
            Frontend(RenderContext(doc), be, config=cfg).draw_layout(space, finalize=True)
            pw, ph = _get_layout_paper_size_mm(space)
            page = drawing_layout.Page(pw, ph, drawing_layout.Units.mm, margins=drawing_layout.Margins.all(0))
            settings = drawing_layout.Settings(scale=scale or 1, fit_page=False)
            pdf_bytes = be.get_pdf_bytes(page, settings=settings)
            with open(str(out_p), "wb") as f:
                f.write(pdf_bytes)
        else:
            fig = plt.figure(dpi=dpi or 150)
            ax = fig.add_axes([0, 0, 1, 1])
            ctx = RenderContext(doc)
            out = MatplotlibBackend(ax)
            Frontend(ctx, out).draw_layout(space, finalize=True)
            if scale and scale > 0:
                ax.set_aspect("equal")
            fig.savefig(str(out_p), dpi=dpi or 150, format=fmt)
            plt.close(fig)
        
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "outputPath": out_p.as_posix(),
            "layout": layout or "Model",
            "format": fmt,
            "scale": scale or 1,
            "dpi": dpi or 150
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "RENDER_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_render_all_layouts_pdf(path: str, outputPath: str, dpi: Optional[int] = 150) -> str:
    """
    Rend l'ensemble des présentations (Model et Paper Space Layouts) d'un fichier DXF dans un unique document PDF multi-pages.
    Exemple : dxf_render_all_layouts_pdf("C:/Dessins/DossierComplet.dxf", "C:/Rendus/Dossier.pdf")
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        out_p = _resolve_path(outputPath)
        out_p.parent.mkdir(parents=True, exist_ok=True)

        doc = ezdxf.readfile(str(p))
        
        rendered_layouts = []
        merged_pdf = pymupdf.open()
        
        for lyt_name in doc.layout_names():
            space = doc.layout(lyt_name)
            # Skip empty layouts without graphic entities
            if len(space) == 0:
                continue
            try:
                be = PyMuPdfBackend()
                cfg = drawing_config.Configuration(
                    background_policy=drawing_config.BackgroundPolicy.WHITE,
                    color_policy=drawing_config.ColorPolicy.BLACK,
                    lineweight_scaling=1.0,
                    min_lineweight=0.09
                )
                Frontend(RenderContext(doc), be, config=cfg).draw_layout(space, finalize=True)
                pw, ph = _get_layout_paper_size_mm(space)
                page = drawing_layout.Page(pw, ph, drawing_layout.Units.mm, margins=drawing_layout.Margins.all(0))
                settings = drawing_layout.Settings(scale=1, fit_page=False)
                page_pdf_bytes = be.get_pdf_bytes(page, settings=settings)
                page_doc = pymupdf.open(stream=page_pdf_bytes, filetype="pdf")
                merged_pdf.insert_pdf(page_doc)
                page_doc.close()
                rendered_layouts.append(lyt_name)
            except Exception:
                continue
            
        if len(rendered_layouts) == 0:
            # Fallback to modelspace if layouts failed
            space = doc.modelspace()
            be = PyMuPdfBackend()
            cfg = drawing_config.Configuration(
                background_policy=drawing_config.BackgroundPolicy.WHITE,
                color_policy=drawing_config.ColorPolicy.BLACK,
                lineweight_scaling=1.0,
                min_lineweight=0.09
            )
            Frontend(RenderContext(doc), be, config=cfg).draw_layout(space, finalize=True)
            pw, ph = _get_layout_paper_size_mm(space)
            page = drawing_layout.Page(pw, ph, drawing_layout.Units.mm, margins=drawing_layout.Margins.all(0))
            settings = drawing_layout.Settings(scale=1, fit_page=True)
            page_pdf_bytes = be.get_pdf_bytes(page, settings=settings)
            page_doc = pymupdf.open(stream=page_pdf_bytes, filetype="pdf")
            merged_pdf.insert_pdf(page_doc)
            page_doc.close()
            rendered_layouts.append("Model")
            
        merged_pdf.save(str(out_p))
        merged_pdf.close()
                
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "outputPath": out_p.as_posix(),
            "layoutsRendered": rendered_layouts,
            "pageCount": len(rendered_layouts)
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "RENDER_PDF_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_audit(path: str) -> str:
    """
    Analyse l'intégrité d'un fichier DXF avec le moteur d'audit ezdxf.audit et renvoie les erreurs ou corrections automatiques.
    Exemple : dxf_audit("C:/Dessins/Planche.dxf")
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        doc = ezdxf.readfile(str(p))
        auditor = doc.audit()
        
        errors = [f"[{e.code}] {e.message}" for e in auditor.errors]
        fixes = [f"[{f.code}] {f.message}" for f in auditor.fixes]
        
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "isValid": len(errors) == 0,
            "errorCount": len(errors),
            "errors": errors,
            "fixCount": len(fixes),
            "fixes": fixes
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "AUDIT_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_edit(path: str, operations: List[Dict[str, Any]], outputPath: str) -> str:
    """
    Applique une suite d'opérations géométriques sur un DXF (ajout de lignes, textes, suppression, création de calques).
    Exemple : dxf_edit("C:/In.dxf", [{"action": "add_line", "start": [0,0], "end": [6400,0], "layer": "MURS"}], "C:/Out.dxf")
    """
    try:
        p = _resolve_path(path)
        _check_file_locked(p)
        out_p = _resolve_path(outputPath)
        out_p.parent.mkdir(parents=True, exist_ok=True)

        doc = ezdxf.readfile(str(p))
        msp = doc.modelspace()
        
        applied = 0
        for op in operations:
            act = op.get("action")
            layer = op.get("layer", "0")
            if layer not in doc.layers:
                doc.layers.add(layer)
                
            if act == "add_line":
                s = op.get("start", [0, 0])
                e = op.get("end", [0, 0])
                msp.add_line((s[0], s[1]), (e[0], e[1]), dxfattribs={"layer": layer})
                applied += 1
            elif act == "add_text":
                text = op.get("text", "")
                pos = op.get("position", [0, 0])
                height = op.get("height", 250)
                msp.add_text(text, dxfattribs={"layer": layer, "height": height}).set_placement((pos[0], pos[1]))
                applied += 1
            elif act == "delete_entity":
                handle = op.get("handle")
                if handle:
                    entity = doc.entitydb.get(handle)
                    if entity:
                        msp.delete_entity(entity)
                        applied += 1

        doc.saveas(str(out_p))
        
        return json.dumps({
            "success": True,
            "path": p.as_posix(),
            "outputPath": out_p.as_posix(),
            "appliedOperations": applied
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(path).as_posix()
        except Exception:
            p_res = str(path)
        return json.dumps({
            "success": False,
            "error": {
                "code": "EDIT_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_diff(pathA: str, pathB: str, toleranceMm: Optional[float] = 1.0) -> str:
    """
    Compare géométriquement et structurellement deux fichiers DXF (versions successives d'un plan).
    Identifie les entités ajoutées, supprimées ou modifiées avec tolérance en millimètres.
    Exemple : dxf_diff("C:/Plans/IndiceA.dxf", "C:/Plans/IndiceB.dxf", toleranceMm=2.0)
    """
    try:
        pA = _resolve_path(pathA)
        pB = _resolve_path(pathB)
        _check_file_locked(pA)
        _check_file_locked(pB)

        docA = ezdxf.readfile(str(pA))
        docB = ezdxf.readfile(str(pB))
        mspA = docA.modelspace()
        mspB = docB.modelspace()
        
        tol = toleranceMm or 1.0
        
        def extract_fingerprints(msp):
            items = {}
            for e in msp:
                h = e.dxf.handle
                t = e.dxftype()
                layer = e.dxf.layer
                if t == "LINE":
                    geom = (round(e.dxf.start.x, 1), round(e.dxf.start.y, 1), round(e.dxf.end.x, 1), round(e.dxf.end.y, 1))
                elif t == "TEXT":
                    geom = (e.dxf.text, round(e.dxf.insert.x, 1), round(e.dxf.insert.y, 1))
                elif t == "CIRCLE":
                    geom = (round(e.dxf.center.x, 1), round(e.dxf.center.y, 1), round(e.dxf.radius, 1))
                else:
                    geom = str(h)
                items[h] = {"type": t, "layer": layer, "geom": geom}
            return items

        itemsA = extract_fingerprints(mspA)
        itemsB = extract_fingerprints(mspB)
        
        added = []
        removed = []
        modified = []
        
        for h, infoB in itemsB.items():
            if h not in itemsA:
                added.append({"handle": h, "type": infoB["type"], "layer": infoB["layer"]})
            else:
                infoA = itemsA[h]
                if infoA["layer"] != infoB["layer"] or infoA["geom"] != infoB["geom"]:
                    modified.append({
                        "handle": h,
                        "type": infoB["type"],
                        "layerBefore": infoA["layer"],
                        "layerAfter": infoB["layer"],
                        "geomBefore": str(infoA["geom"]),
                        "geomAfter": str(infoB["geom"])
                    })
                    
        for h, infoA in itemsA.items():
            if h not in itemsB:
                removed.append({"handle": h, "type": infoA["type"], "layer": infoA["layer"]})
                
        return json.dumps({
            "success": True,
            "pathA": pA.as_posix(),
            "pathB": pB.as_posix(),
            "toleranceMm": tol,
            "summary": {
                "totalA": len(itemsA),
                "totalB": len(itemsB),
                "addedCount": len(added),
                "removedCount": len(removed),
                "modifiedCount": len(modified)
            },
            "added": added[:50],
            "removed": removed[:50],
            "modified": modified[:50]
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = f"{_resolve_path(pathA).as_posix()} vs {_resolve_path(pathB).as_posix()}"
        except Exception:
            p_res = f"{pathA} vs {pathB}"
        return json.dumps({
            "success": False,
            "error": {
                "code": "DIFF_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_compare_with_revit(dxfPath: str, revitElements: List[Dict[str, Any]], toleranceMm: Optional[float] = 20.0) -> str:
    """
    Compare les éléments d'un plan DXF validé et les éléments correspondants d'un modèle Revit ou BIMCraft.
    Détecte les écarts de position > toleranceMm (ex: portes décalées, fenêtres manquantes).
    Prend en charge la structure de points native Revit ou BIMCraft (`at`: [x, y], `point`: [x, y], ou `location`: {type: point}).
    Exemple : dxf_compare_with_revit("C:/Planche.dxf", revitElements=[...], toleranceMm=20.0)
    """
    try:
        p = _resolve_path(dxfPath)
        _check_file_locked(p)
        doc = ezdxf.readfile(str(p))
        msp = doc.modelspace()
        
        tol = toleranceMm or 20.0
        discrepancies = []
        
        dxf_lines = []
        for e in msp:
            if e.dxftype() == "LINE":
                dxf_lines.append((e.dxf.start.x, e.dxf.start.y, e.dxf.end.x, e.dxf.end.y))

        checked_count = 0
        for el in revitElements:
            checked_count += 1
            px, py = None, None

            # Support natif BIMCraft & formats variés
            if "at" in el and isinstance(el["at"], (list, tuple)) and len(el["at"]) >= 2:
                px, py = float(el["at"][0]), float(el["at"][1])
            elif "point" in el:
                pt = el["point"]
                if isinstance(pt, (list, tuple)) and len(pt) >= 2:
                    px, py = float(pt[0]), float(pt[1])
                elif isinstance(pt, dict):
                    px, py = float(pt.get("x", 0)), float(pt.get("y", 0))
            elif "points" in el and isinstance(el["points"], (list, tuple)) and len(el["points"]) >= 2:
                # Segment de mur BIMCraft: points: [[x1, y1], [x2, y2]] -> point médian
                p1, p2 = el["points"][0], el["points"][1]
                px, py = (float(p1[0]) + float(p2[0])) / 2.0, (float(p1[1]) + float(p2[1])) / 2.0
            else:
                loc = el.get("location")
                if loc and loc.get("type") == "point":
                    pt = loc.get("point", {})
                    px, py = float(pt.get("x", 0)), float(pt.get("y", 0))

            if px is None or py is None:
                continue

            min_dist = float("inf")
            for (x1, y1, x2, y2) in dxf_lines:
                dx = x2 - x1
                dy = y2 - y1
                if dx == 0 and dy == 0:
                    dist = math.hypot(px - x1, py - y1)
                else:
                    t = max(0, min(1, ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy)))
                    proj_x = x1 + t * dx
                    proj_y = y1 + t * dy
                    dist = math.hypot(px - proj_x, py - proj_y)
                if dist < min_dist:
                    min_dist = dist
                    
            if min_dist > tol and min_dist < 2000:
                discrepancies.append({
                    "elementId": el.get("id"),
                    "name": el.get("name") or el.get("type"),
                    "category": el.get("category"),
                    "revitPosition": {"x": px, "y": py},
                    "deviationMm": round(min_dist, 1),
                    "status": "DISCREPANCY"
                })

        return json.dumps({
            "success": True,
            "dxfPath": p.as_posix(),
            "elementsChecked": checked_count,
            "discrepancyCount": len(discrepancies),
            "toleranceMm": tol,
            "discrepancies": discrepancies
        }, indent=2)
    except Exception as ex:
        p_res = ""
        try:
            p_res = _resolve_path(dxfPath).as_posix()
        except Exception:
            p_res = str(dxfPath)
        return json.dumps({
            "success": False,
            "error": {
                "code": "COMPARE_ERROR",
                "message": str(ex),
                "resolvedPath": p_res,
                "platform": sys.platform
            }
        }, indent=2)


@mcp.tool()
def dxf_compare_with_bimcraft(dxfPath: str, bimcraftElements: List[Dict[str, Any]], toleranceMm: Optional[float] = 20.0) -> str:
    """
    Compare directement les éléments d'un projet BIMCraft (extraits via query_elements ou inspect_model) avec un plan DXF.
    Détecte les écarts d'implantation de cloisons, portes, fenêtres et équipements.
    Exemple : dxf_compare_with_bimcraft("C:/Plan.dxf", bimcraftElements=[...], toleranceMm=20.0)
    """
    return dxf_compare_with_revit(dxfPath, bimcraftElements, toleranceMm)


@mcp.tool()
def bimcraft_export_dxf(bimcraftPath: str, dxfOutputPath: str) -> str:
    """
    Exporte et convertit un modèle .bimcraft ou .rvt vers un format vectoriel DXF/SVG via le moteur haute vitesse bimcraft-cli.
    Exemple : bimcraft_export_dxf("C:/Modele.bimcraft", "C:/Dessin.svg")
    """
    import subprocess
    try:
        p_in = _resolve_path(bimcraftPath)
        p_out = _resolve_path(dxfOutputPath)
        p_out.parent.mkdir(parents=True, exist_ok=True)
        
        cli_path = r"C:\Users\teoba\.cargo\bin\bimcraft-cli.exe"
        cmd = [cli_path, "convert", str(p_in), str(p_out)]
        res = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
        
        if res.returncode != 0:
            return json.dumps({
                "success": False,
                "error": {
                    "code": "BIMCRAFT_CONVERT_ERROR",
                    "message": res.stderr or res.stdout
                }
            }, indent=2)
            
        return json.dumps({
            "success": True,
            "input": p_in.as_posix(),
            "output": p_out.as_posix(),
            "details": res.stdout.strip()
        }, indent=2)
    except Exception as ex:
        return json.dumps({
            "success": False,
            "error": {
                "code": "BIMCRAFT_CONVERT_EXCEPTION",
                "message": str(ex)
            }
        }, indent=2)


if __name__ == "__main__":
    mcp.run()
