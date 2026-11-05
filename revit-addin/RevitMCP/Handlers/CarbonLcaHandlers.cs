using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCP;

public partial class RevitEventHandler
{
    public class IniesEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "m3";
        public double FactorKgCO2eq { get; set; }
        public double BiogenicCarbonKg { get; set; }
        public bool IsBiosourced { get; set; }
        public string? AlternativeFor { get; set; }
        public string Description { get; set; } = "";
    }

    private static readonly List<IniesEntry> IniesDatabase = new()
    {
        new() { Id = "INIES_BETON_STANDARD_C25", Name = "Béton armé courant C25/30", Category = "Structure", Unit = "m3", FactorKgCO2eq = 295.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Béton prêt à l'emploi standard CEM II" },
        new() { Id = "INIES_BETON_BAS_CARBONE", Name = "Béton bas-carbone CEM III/A", Category = "Structure", Unit = "m3", FactorKgCO2eq = 145.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, AlternativeFor = "INIES_BETON_STANDARD_C25", Description = "Béton à haute teneur en laitier de haut fourneau (-51% CO2)" },
        new() { Id = "INIES_BOIS_CLT", Name = "Panneaux CLT bois lamellé-croisé", Category = "Structure", Unit = "m3", FactorKgCO2eq = 35.0, BiogenicCarbonKg = 680.0, IsBiosourced = true, AlternativeFor = "INIES_BETON_STANDARD_C25", Description = "Structure bois massif CLT biosourcée avec stockage carbone" },
        new() { Id = "INIES_BRIQUE_TERRE_CUITE", Name = "Brique alvéolaire terre cuite", Category = "Enveloppe", Unit = "m3", FactorKgCO2eq = 180.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Brique de structure 20cm" },
        new() { Id = "INIES_CLOISON_BA13_STANDARD", Name = "Cloison plâtre BA13 standard (ossature métallique)", Category = "Cloisonnement", Unit = "m2", FactorKgCO2eq = 14.8, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Cloison 72/48 standard laine minérale" },
        new() { Id = "INIES_CLOISON_OSSATURE_BOIS", Name = "Cloison ossature bois biosourcée + fermacell", Category = "Cloisonnement", Unit = "m2", FactorKgCO2eq = 4.2, BiogenicCarbonKg = 18.5, IsBiosourced = true, AlternativeFor = "INIES_CLOISON_BA13_STANDARD", Description = "Montants bois certifiés FSC, panneaux gypse-cellulose et isolant biosourcé (-71% CO2)" },
        new() { Id = "INIES_CARREAU_PLATRE", Name = "Carreaux de plâtre massif 7cm", Category = "Cloisonnement", Unit = "m2", FactorKgCO2eq = 18.5, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Cloison plâtre massive sans ossature" },
        new() { Id = "INIES_ISOL_PUR", Name = "Isolant Polyuréthane (PUR)", Category = "Isolation", Unit = "m3", FactorKgCO2eq = 110.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Mousse polyuréthane rigide haute performance thermique" },
        new() { Id = "INIES_ISOL_EPS", Name = "Isolant Polystyrène expansé (EPS)", Category = "Isolation", Unit = "m3", FactorKgCO2eq = 75.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Polystyrène expansé blanc/gris" },
        new() { Id = "INIES_ISOL_LAINE_MINERALE", Name = "Laine de verre / Laine de roche", Category = "Isolation", Unit = "m3", FactorKgCO2eq = 38.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Laine minérale semi-rigide" },
        new() { Id = "INIES_ISOL_FIBRE_BOIS", Name = "Isolant fibre de bois biosourcé", Category = "Isolation", Unit = "m3", FactorKgCO2eq = 8.5, BiogenicCarbonKg = 92.0, IsBiosourced = true, AlternativeFor = "INIES_ISOL_PUR", Description = "Panneau isolant en fibres de bois d'éclaircie (-92% CO2)" },
        new() { Id = "INIES_ISOL_OUATE_CELLULOSE", Name = "Ouate de cellulose insufflée", Category = "Isolation", Unit = "m3", FactorKgCO2eq = 6.2, BiogenicCarbonKg = 75.0, IsBiosourced = true, AlternativeFor = "INIES_ISOL_EPS", Description = "Isolant issu du recyclage papier journal" },
        new() { Id = "INIES_FENETRE_ALU_STANDARD", Name = "Fenêtre double vitrage aluminium", Category = "Menuiserie", Unit = "m2", FactorKgCO2eq = 95.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Châssis alu rupture de pont thermique" },
        new() { Id = "INIES_FENETRE_ALU_RECYCLE", Name = "Fenêtre aluminium recyclé bas-carbone", Category = "Menuiserie", Unit = "m2", FactorKgCO2eq = 52.0, BiogenicCarbonKg = 0.0, IsBiosourced = false, AlternativeFor = "INIES_FENETRE_ALU_STANDARD", Description = "Alu à 75% recyclé post-consommation" },
        new() { Id = "INIES_FENETRE_BOIS_PEFC", Name = "Fenêtre double vitrage bois local certifié PEFC", Category = "Menuiserie", Unit = "m2", FactorKgCO2eq = 28.0, BiogenicCarbonKg = 34.0, IsBiosourced = true, AlternativeFor = "INIES_FENETRE_ALU_STANDARD", Description = "Menuiserie bois massif chêne/pin avec vernis à l'eau (-70% CO2)" },
        new() { Id = "INIES_PORTE_STANDARD", Name = "Bloc-porte intérieur âme alvéolaire", Category = "Menuiserie", Unit = "unit", FactorKgCO2eq = 24.0, BiogenicCarbonKg = 5.0, IsBiosourced = false, Description = "Porte standard prépeinte" },
        new() { Id = "INIES_PORTE_BOIS_MASSIF", Name = "Bloc-porte bois massif certifié", Category = "Menuiserie", Unit = "unit", FactorKgCO2eq = 11.0, BiogenicCarbonKg = 22.0, IsBiosourced = true, AlternativeFor = "INIES_PORTE_STANDARD", Description = "Porte battante en bois massif éco-conçue (-54% CO2)" },
        new() { Id = "INIES_CARRELAGE_GRES", Name = "Carrelage grès cérame 10mm", Category = "Finitions", Unit = "m2", FactorKgCO2eq = 16.5, BiogenicCarbonKg = 0.0, IsBiosourced = false, Description = "Carrelage cuit haute température" },
        new() { Id = "INIES_PARQUET_MASSIF_CHENE", Name = "Parquet chêne massif français 14mm", Category = "Finitions", Unit = "m2", FactorKgCO2eq = 3.8, BiogenicCarbonKg = 16.0, IsBiosourced = true, AlternativeFor = "INIES_CARRELAGE_GRES", Description = "Parquet bois massif régional (-77% CO2)" },
        new() { Id = "INIES_LINO_NATUREL", Name = "Linoléum naturel biosourcé (huile de lin)", Category = "Finitions", Unit = "m2", FactorKgCO2eq = 4.5, BiogenicCarbonKg = 4.2, IsBiosourced = true, Description = "Revêtement souple naturel recyclable" }
    };

    private static JObject CalculateLcaCarbonFootprint(Document doc, JObject p)
    {
        var elementsByCategory = new Dictionary<string, (double co2, double volume, double area, int count)>();
        double totalCo2Kg = 0;
        double totalBiogenicCarbonKg = 0;

        var walls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Cast<Wall>()
            .ToList();

        foreach (var wall in walls)
        {
            var wallType = doc.GetElement(wall.GetTypeId()) as WallType;
            var typeName = (wallType?.Name ?? wall.Name).ToLowerInvariant();

            var areaSqFt = wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;
            var volCuFt = wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0;
            var areaM2 = UnitConversions.SqFtToM2(areaSqFt);
            var volM3 = UnitConversions.CuFtToM3(volCuFt);

            IniesEntry entry;
            if (typeName.Contains("platre") || typeName.Contains("plâtre") || typeName.Contains("ba13") || typeName.Contains("cloison"))
            {
                entry = IniesDatabase.First(e => e.Id == "INIES_CLOISON_BA13_STANDARD");
                var co2 = areaM2 * entry.FactorKgCO2eq;
                totalCo2Kg += co2;
                totalBiogenicCarbonKg += areaM2 * entry.BiogenicCarbonKg;
                AccumulateCategory(elementsByCategory, "Cloisonnement", co2, volM3, areaM2);
            }
            else if (typeName.Contains("brique") || typeName.Contains("terracotta"))
            {
                entry = IniesDatabase.First(e => e.Id == "INIES_BRIQUE_TERRE_CUITE");
                var co2 = volM3 * entry.FactorKgCO2eq;
                totalCo2Kg += co2;
                AccumulateCategory(elementsByCategory, "Enveloppe", co2, volM3, areaM2);
            }
            else if (typeName.Contains("bois") || typeName.Contains("clt") || typeName.Contains("timber"))
            {
                entry = IniesDatabase.First(e => e.Id == "INIES_BOIS_CLT");
                var co2 = volM3 * entry.FactorKgCO2eq;
                totalCo2Kg += co2;
                totalBiogenicCarbonKg += volM3 * entry.BiogenicCarbonKg;
                AccumulateCategory(elementsByCategory, "Structure Biosourcée", co2, volM3, areaM2);
            }
            else
            {
                entry = IniesDatabase.First(e => e.Id == "INIES_BETON_STANDARD_C25");
                var co2 = volM3 * entry.FactorKgCO2eq;
                totalCo2Kg += co2;
                AccumulateCategory(elementsByCategory, "Structure", co2, volM3, areaM2);
            }
        }

        var floors = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .WhereElementIsNotElementType()
            .Cast<Floor>()
            .ToList();

        foreach (var floor in floors)
        {
            var volCuFt = floor.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0;
            var areaSqFt = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;
            var volM3 = UnitConversions.CuFtToM3(volCuFt);
            var areaM2 = UnitConversions.SqFtToM2(areaSqFt);

            var entry = IniesDatabase.First(e => e.Id == "INIES_BETON_STANDARD_C25");
            var co2 = volM3 * entry.FactorKgCO2eq;
            totalCo2Kg += co2;
            AccumulateCategory(elementsByCategory, "Planchers & Dalles", co2, volM3, areaM2);
        }

        var doors = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Doors)
            .WhereElementIsNotElementType()
            .GetElementCount();

        var doorEntry = IniesDatabase.First(e => e.Id == "INIES_PORTE_STANDARD");
        var doorCo2 = doors * doorEntry.FactorKgCO2eq;
        totalCo2Kg += doorCo2;
        AccumulateCategory(elementsByCategory, "Menuiseries Intérieures", doorCo2, 0, 0, doors);

        var windows = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Windows)
            .WhereElementIsNotElementType()
            .ToElements();

        double winAreaTotalM2 = 0;
        foreach (var win in windows)
        {
            var w = win.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)?.AsDouble() ?? UnitConversions.MmToFeet(1200);
            var h = win.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)?.AsDouble() ?? UnitConversions.MmToFeet(1400);
            winAreaTotalM2 += UnitConversions.SqFtToM2(w * h);
        }

        var winEntry = IniesDatabase.First(e => e.Id == "INIES_FENETRE_ALU_STANDARD");
        var winCo2 = winAreaTotalM2 * winEntry.FactorKgCO2eq;
        totalCo2Kg += winCo2;
        AccumulateCategory(elementsByCategory, "Menuiseries Extérieures", winCo2, 0, winAreaTotalM2, windows.Count);

        var rooms = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<SpatialElement>()
            .ToList();

        double totalFloorAreaM2 = 0;
        foreach (var r in rooms)
        {
            if (r.Area > 0)
            {
                totalFloorAreaM2 += UnitConversions.SqFtToM2(r.Area);
            }
        }

        if (totalFloorAreaM2 < 1.0)
        {
            totalFloorAreaM2 = floors.Sum(f => UnitConversions.SqFtToM2(f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0));
            if (totalFloorAreaM2 < 1.0) totalFloorAreaM2 = 100.0;
        }

        double icConstruction = Math.Round(totalCo2Kg / totalFloorAreaM2, 1);

        const double seuil2022 = 640.0;
        const double seuil2025 = 530.0;
        const double seuil2028 = 415.0;
        const double seuil2031 = 350.0;

        string complianceStatus;
        if (icConstruction <= seuil2031) complianceStatus = "EXCELLENT - Conforme RE2031 (Bâtiment exemplaire bas-carbone)";
        else if (icConstruction <= seuil2028) complianceStatus = "TRÈS BON - Conforme RE2028";
        else if (icConstruction <= seuil2025) complianceStatus = "CONFORME - Conforme RE2025";
        else if (icConstruction <= seuil2022) complianceStatus = "CONFORME LIMITE - Conforme RE2022 uniquement";
        else complianceStatus = "NON CONFORME RE2020 - Dépassement du seuil maximal réglementaire";

        var catSummary = new JArray();
        foreach (var kvp in elementsByCategory)
        {
            catSummary.Add(new JObject
            {
                ["category"] = kvp.Key,
                ["totalCo2Kg"] = Math.Round(kvp.Value.co2, 1),
                ["percentOfTotal"] = Math.Round((kvp.Value.co2 / totalCo2Kg) * 100.0, 1),
                ["volumeM3"] = Math.Round(kvp.Value.volume, 2),
                ["areaM2"] = Math.Round(kvp.Value.area, 2),
                ["count"] = kvp.Value.count
            });
        }

        return new JObject
        {
            ["success"] = true,
            ["modelTitle"] = doc.Title,
            ["totalEmbodiedCo2Kg"] = Math.Round(totalCo2Kg, 1),
            ["totalEmbodiedCo2Tons"] = Math.Round(totalCo2Kg / 1000.0, 2),
            ["totalFloorAreaM2"] = Math.Round(totalFloorAreaM2, 1),
            ["icConstructionKgCO2eqM2"] = icConstruction,
            ["biogenicCarbonStorageKg"] = Math.Round(totalBiogenicCarbonKg, 1),
            ["re2020Compliance"] = new JObject
            {
                ["status"] = complianceStatus,
                ["ic_construction"] = icConstruction,
                ["seuil_2022"] = seuil2022,
                ["seuil_2025"] = seuil2025,
                ["seuil_2028"] = seuil2028,
                ["seuil_2031"] = seuil2031,
                ["compliantWithSeuil2025"] = icConstruction <= seuil2025
            },
            ["categoriesBreakdown"] = catSummary
        };
    }

    private static void AccumulateCategory(Dictionary<string, (double co2, double volume, double area, int count)> dict, string cat, double co2, double vol, double area, int count = 1)
    {
        if (dict.TryGetValue(cat, out var existing))
        {
            dict[cat] = (existing.co2 + co2, existing.volume + vol, existing.area + area, existing.count + count);
        }
        else
        {
            dict[cat] = (co2, vol, area, count);
        }
    }

    private static JObject SuggestLowCarbonAlternatives(Document doc, JObject p)
    {
        var suggestions = new JArray();
        double potentialTotalSavingsKg = 0;

        var walls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Cast<Wall>()
            .ToList();

        double plasterAreaM2 = 0;
        foreach (var w in walls)
        {
            var typeName = w.Name.ToLowerInvariant();
            if (typeName.Contains("platre") || typeName.Contains("plâtre") || typeName.Contains("ba13") || typeName.Contains("cloison"))
            {
                var areaSqFt = w.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;
                plasterAreaM2 += UnitConversions.SqFtToM2(areaSqFt);
            }
        }

        if (plasterAreaM2 > 5.0)
        {
            var standardFactor = IniesDatabase.First(e => e.Id == "INIES_CLOISON_BA13_STANDARD").FactorKgCO2eq;
            var bioFactor = IniesDatabase.First(e => e.Id == "INIES_CLOISON_OSSATURE_BOIS").FactorKgCO2eq;
            var savingsKg = plasterAreaM2 * (standardFactor - bioFactor);
            potentialTotalSavingsKg += savingsKg;

            suggestions.Add(new JObject
            {
                ["targetCategory"] = "Cloisonnement",
                ["currentMaterial"] = "Cloisons plâtre standard BA13 sur ossature métallique",
                ["proposedAlternative"] = "Cloisons ossature bois biosourcée + fermacell",
                ["currentTotalCo2Kg"] = Math.Round(plasterAreaM2 * standardFactor, 1),
                ["alternativeTotalCo2Kg"] = Math.Round(plasterAreaM2 * bioFactor, 1),
                ["co2SavingsKg"] = Math.Round(savingsKg, 1),
                ["co2SavingsTons"] = Math.Round(savingsKg / 1000.0, 2),
                ["reductionPercent"] = Math.Round((1.0 - bioFactor / standardFactor) * 100.0, 1),
                ["biogenicStorageGainKg"] = Math.Round(plasterAreaM2 * 18.5, 1),
                ["recommendation"] = $"Remplacer {plasterAreaM2:F1} m² de cloisons BA13 standard par une ossature bois biosourcée permet d'économiser {(savingsKg / 1000.0):F2} tonnes de CO₂ (-71%) et de stocker {(plasterAreaM2 * 18.5 / 1000.0):F2} tonnes de carbone biogénique."
            });
        }

        var windows = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Windows)
            .WhereElementIsNotElementType()
            .ToElements();

        if (windows.Count > 0)
        {
            double winAreaTotalM2 = 0;
            foreach (var win in windows)
            {
                var w = win.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM)?.AsDouble() ?? UnitConversions.MmToFeet(1200);
                var h = win.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)?.AsDouble() ?? UnitConversions.MmToFeet(1400);
                winAreaTotalM2 += UnitConversions.SqFtToM2(w * h);
            }

            var stdAluFactor = IniesDatabase.First(e => e.Id == "INIES_FENETRE_ALU_STANDARD").FactorKgCO2eq;
            var bioWoodFactor = IniesDatabase.First(e => e.Id == "INIES_FENETRE_BOIS_PEFC").FactorKgCO2eq;
            var savingsKg = winAreaTotalM2 * (stdAluFactor - bioWoodFactor);
            potentialTotalSavingsKg += savingsKg;

            suggestions.Add(new JObject
            {
                ["targetCategory"] = "Menuiseries Extérieures",
                ["currentMaterial"] = "Fenêtres aluminium standard",
                ["proposedAlternative"] = "Fenêtres bois massif local certifié PEFC / FSC",
                ["currentTotalCo2Kg"] = Math.Round(winAreaTotalM2 * stdAluFactor, 1),
                ["alternativeTotalCo2Kg"] = Math.Round(winAreaTotalM2 * bioWoodFactor, 1),
                ["co2SavingsKg"] = Math.Round(savingsKg, 1),
                ["co2SavingsTons"] = Math.Round(savingsKg / 1000.0, 2),
                ["reductionPercent"] = Math.Round((1.0 - bioWoodFactor / stdAluFactor) * 100.0, 1),
                ["recommendation"] = $"Remplacer {winAreaTotalM2:F1} m² de châssis alu par des menuiseries bois PEFC permet d'économiser {(savingsKg / 1000.0):F2} tonnes de CO₂ (-70%)."
            });
        }

        var floors = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .WhereElementIsNotElementType()
            .Cast<Floor>()
            .ToList();

        double concreteVolM3 = floors.Sum(f => UnitConversions.CuFtToM3(f.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0));

        if (concreteVolM3 > 1.0)
        {
            var stdConcreteFactor = IniesDatabase.First(e => e.Id == "INIES_BETON_STANDARD_C25").FactorKgCO2eq;
            var lowCarbonFactor = IniesDatabase.First(e => e.Id == "INIES_BETON_BAS_CARBONE").FactorKgCO2eq;
            var savingsKg = concreteVolM3 * (stdConcreteFactor - lowCarbonFactor);
            potentialTotalSavingsKg += savingsKg;

            suggestions.Add(new JObject
            {
                ["targetCategory"] = "Planchers & Dalles",
                ["currentMaterial"] = "Béton armé standard CEM II C25/30",
                ["proposedAlternative"] = "Béton bas-carbone CEM III/A (haute teneur en laitier)",
                ["currentTotalCo2Kg"] = Math.Round(concreteVolM3 * stdConcreteFactor, 1),
                ["alternativeTotalCo2Kg"] = Math.Round(concreteVolM3 * lowCarbonFactor, 1),
                ["co2SavingsKg"] = Math.Round(savingsKg, 1),
                ["co2SavingsTons"] = Math.Round(savingsKg / 1000.0, 2),
                ["reductionPercent"] = Math.Round((1.0 - lowCarbonFactor / stdConcreteFactor) * 100.0, 1),
                ["recommendation"] = $"Passer {concreteVolM3:F1} m³ de planchers béton en formule bas-carbone CEM III économise {(savingsKg / 1000.0):F2} tonnes de CO₂ (-51%)."
            });
        }

        return new JObject
        {
            ["success"] = true,
            ["totalPotentialSavingsTons"] = Math.Round(potentialTotalSavingsKg / 1000.0, 2),
            ["suggestionsCount"] = suggestions.Count,
            ["suggestions"] = suggestions
        };
    }

    private static JObject LookupIniesDatabase(JObject p)
    {
        var query = (p["query"]?.Value<string>() ?? "").ToLowerInvariant().Trim();
        var category = p["category"]?.Value<string>()?.ToLowerInvariant().Trim();

        var results = IniesDatabase.Where(e =>
        {
            if (!string.IsNullOrEmpty(category) && !e.Category.ToLowerInvariant().Contains(category))
                return false;

            if (string.IsNullOrEmpty(query)) return true;

            return e.Name.ToLowerInvariant().Contains(query)
                || e.Id.ToLowerInvariant().Contains(query)
                || e.Description.ToLowerInvariant().Contains(query);
        }).ToList();

        var items = new JArray();
        foreach (var r in results)
        {
            items.Add(new JObject
            {
                ["id"] = r.Id,
                ["name"] = r.Name,
                ["category"] = r.Category,
                ["unit"] = r.Unit,
                ["factorKgCO2eqPerUnit"] = r.FactorKgCO2eq,
                ["biogenicCarbonStorageKg"] = r.BiogenicCarbonKg,
                ["isBiosourced"] = r.IsBiosourced,
                ["alternativeFor"] = r.AlternativeFor,
                ["description"] = r.Description
            });
        }

        return new JObject
        {
            ["success"] = true,
            ["totalMatches"] = results.Count,
            ["results"] = items
        };
    }
}
