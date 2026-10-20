using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RevitMCP;

/// <summary>
/// Universal category resolver supporting:
/// - BuiltInCategory enum names (e.g. OST_Furniture or Furniture)
/// - English display names (e.g. "Furniture", "Walls", "Doors", "Generic Models")
/// - French localized display names (e.g. "Mobilier", "Murs", "Portes", "Modèles génériques")
/// - Dynamic lookup in doc.Settings.Categories
/// </summary>
public static class CategoryResolver
{
    private static readonly Dictionary<string, BuiltInCategory> KnownMappings = new(StringComparer.OrdinalIgnoreCase);

    static CategoryResolver()
    {
        // Populate standard pairs
        Register(BuiltInCategory.OST_Walls, "Walls", "Murs", "Mur");
        Register(BuiltInCategory.OST_Doors, "Doors", "Portes", "Porte");
        Register(BuiltInCategory.OST_Windows, "Windows", "Fenêtres", "Fenetres", "Fenêtre", "Fenetre");
        Register(BuiltInCategory.OST_Floors, "Floors", "Sols", "Sol", "Planchers");
        Register(BuiltInCategory.OST_Roofs, "Roofs", "Toits", "Toit", "Toitures");
        Register(BuiltInCategory.OST_Ceilings, "Ceilings", "Plafonds", "Plafond");
        Register(BuiltInCategory.OST_Rooms, "Rooms", "Pièces", "Pieces", "Pièce", "Piece");
        Register(BuiltInCategory.OST_Columns, "Columns", "Poteaux", "Poteau", "Colonnes");
        Register(BuiltInCategory.OST_StructuralColumns, "Structural Columns", "Poteaux porteurs", "Poteau porteur");
        Register(BuiltInCategory.OST_StructuralFraming, "Structural Framing", "Beams", "Poutres", "Ossature structurelle", "Poutre");
        Register(BuiltInCategory.OST_Furniture, "Furniture", "Mobilier", "Meubles", "Meuble");
        Register(BuiltInCategory.OST_FurnitureSystems, "Furniture Systems", "Systèmes de mobilier");
        Register(BuiltInCategory.OST_Stairs, "Stairs", "Escaliers", "Escalier");
        Register(BuiltInCategory.OST_StairsRailing, "Railings", "Garde-corps", "Garde-corps", "Railing");
        Register(BuiltInCategory.OST_Grids, "Grids", "Quadrillages", "Quadrillage", "Files");
        Register(BuiltInCategory.OST_Levels, "Levels", "Niveaux", "Niveau");
        Register(BuiltInCategory.OST_Sheets, "Sheets", "Feuilles", "Feuille");
        Register(BuiltInCategory.OST_Views, "Views", "Vues", "Vue");
        Register(BuiltInCategory.OST_TitleBlocks, "Title Blocks", "TitleBlocks", "Cartouches", "Cartouche");
        Register(BuiltInCategory.OST_Lines, "Lines", "Lignes", "Ligne", "Lignes de détail");
        Register(BuiltInCategory.OST_GenericModel, "Generic Models", "Generic Model", "Modèles génériques", "Modele generique");
        Register(BuiltInCategory.OST_Materials, "Materials", "Matériaux", "Materiaux", "Matériau", "Materiau");
        Register(BuiltInCategory.OST_Areas, "Areas", "Area", "Surfaces", "Surfaces (brutes)");
        Register(BuiltInCategory.OST_MechanicalEquipment, "Mechanical Equipment", "MEP", "Équipement de génie climatique", "Equipement mecanique");
        Register(BuiltInCategory.OST_PlumbingFixtures, "Plumbing Fixtures", "Appareils sanitaires", "Sanitaires");
        Register(BuiltInCategory.OST_LightingFixtures, "Lighting Fixtures", "Appareils d'éclairage", "Luminaires");
        Register(BuiltInCategory.OST_ElectricalFixtures, "Electrical Fixtures", "Appareils électriques");
        Register(BuiltInCategory.OST_ElectricalEquipment, "Electrical Equipment", "Équipement électrique");
        Register(BuiltInCategory.OST_SpecialityEquipment, "Specialty Equipment", "Équipement spécialisé");
        Register(BuiltInCategory.OST_CurtainWallPanels, "Curtain Panels", "Panneaux de mur-rideau");
        Register(BuiltInCategory.OST_CurtainWallMullions, "Curtain Wall Mullions", "Meneaux");
        Register(BuiltInCategory.OST_Topography, "Topography", "Site", "Topographie");
        Register(BuiltInCategory.OST_Planting, "Planting", "Plantations", "Plantes");
        Register(BuiltInCategory.OST_Parking, "Parking", "Parkings", "Stationnement");
        Register(BuiltInCategory.OST_Dimensions, "Dimensions", "Cotes", "Cote");
        Register(BuiltInCategory.OST_SpotElevations, "Spot Elevations", "Cotes d'élévation", "Cote d'elevation");
        Register(BuiltInCategory.OST_TextNotes, "Text Notes", "Text Notes", "Notes textuelles", "Textes");
        Register(BuiltInCategory.OST_GenericAnnotation, "Generic Annotations", "Annotations génériques", "Symboles");
        Register(BuiltInCategory.OST_RoomTags, "Room Tags", "Étiquettes de pièce", "Etiquettes de piece");
        Register(BuiltInCategory.OST_DoorTags, "Door Tags", "Étiquettes de porte", "Etiquettes de porte");
        Register(BuiltInCategory.OST_WindowTags, "Window Tags", "Étiquettes de fenêtre", "Etiquettes de fenetre");
        Register(BuiltInCategory.OST_WallTags, "Wall Tags", "Étiquettes de mur", "Etiquettes de mur");
        Register(BuiltInCategory.OST_FurnitureTags, "Furniture Tags", "Étiquettes de mobilier", "Etiquettes de mobilier");
        Register(BuiltInCategory.OST_Viewports, "Viewports", "Fenêtres de présentation", "Fenetres de presentation");
        Register(BuiltInCategory.OST_DetailComponents, "Detail Components", "Éléments de détail", "Composants de détail");
        Register(BuiltInCategory.OST_CeilingOpening, "Ceiling Openings", "Réservations de plafond");
        Register(BuiltInCategory.OST_FloorOpening, "Floor Openings", "Trémies de sol", "Réservations de sol");
        Register(BuiltInCategory.OST_ShaftOpening, "Shaft Openings", "Cages d'ouverture", "Cages");
    }

    private static void Register(BuiltInCategory bic, params string[] names)
    {
        // Add exact enum name and stripped name
        var enumName = bic.ToString();
        KnownMappings[enumName] = bic;
        if (enumName.StartsWith("OST_"))
        {
            KnownMappings[enumName[4..]] = bic;
            KnownMappings[NormalizeKey(enumName[4..])] = bic;
        }

        foreach (var n in names)
        {
            KnownMappings[n] = bic;
            KnownMappings[NormalizeKey(n)] = bic;
        }
    }

    public static string NormalizeKey(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(c) && c != '_' && c != '-')
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Resolves any category name (BuiltInCategory, English, French, normalized) to BuiltInCategory.
    /// Returns null if not recognized.
    /// </summary>
    public static BuiltInCategory? Resolve(string? categoryName, Document? doc = null)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        var trimmed = categoryName.Trim();

        // 1. Check direct map
        if (KnownMappings.TryGetValue(trimmed, out var bic))
            return bic;

        // 2. Check normalized map
        var norm = NormalizeKey(trimmed);
        if (KnownMappings.TryGetValue(norm, out bic))
            return bic;

        // 3. Try parse enum name
        if (Enum.TryParse<BuiltInCategory>(trimmed, true, out var parsedEnum))
            return parsedEnum;
        if (!trimmed.StartsWith("OST_", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<BuiltInCategory>("OST_" + trimmed, true, out parsedEnum))
            return parsedEnum;

        // 4. Try matching Document Categories if document is available
        if (doc != null)
        {
            foreach (Category cat in doc.Settings.Categories)
            {
                if (string.Equals(cat.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    NormalizeKey(cat.Name) == norm)
                {
                    return (BuiltInCategory)cat.Id.Value;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether an Element or Family Category matches a category filter string.
    /// </summary>
    public static bool Matches(Category? elementCat, string categoryFilter, Document? doc = null)
    {
        if (elementCat == null || string.IsNullOrWhiteSpace(categoryFilter))
            return false;

        var resolvedTargetBic = Resolve(categoryFilter, doc);
        if (resolvedTargetBic.HasValue)
        {
            if (elementCat.Id.Value == (long)resolvedTargetBic.Value)
                return true;
        }

        // Compare category names
        if (string.Equals(elementCat.Name, categoryFilter, StringComparison.OrdinalIgnoreCase))
            return true;

        var normFilter = NormalizeKey(categoryFilter);
        if (NormalizeKey(elementCat.Name) == normFilter)
            return true;

        return false;
    }
}
