import unittest
import os
import json
import tempfile
import ezdxf
from server import dxf_info, dxf_list_entities, dxf_audit, dxf_edit, dxf_compare_with_revit, dxf_compare_with_bimcraft, bimcraft_export_dxf, dxf_render, dxf_render_all_layouts_pdf, dxf_diff

class TestCadMcp(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.dxf_path = os.path.join(self.temp_dir.name, "test_sample.dxf")
        
        # Create a sample DXF
        doc = ezdxf.new("R2018")
        msp = doc.modelspace()
        doc.layers.add("MURS", color=1)
        doc.layers.add("OUVERTURES", color=2)
        
        # Room 6400 x 6200 mm
        msp.add_line((0, 0), (6400, 0), dxfattribs={"layer": "MURS"})
        msp.add_line((6400, 0), (6400, 6200), dxfattribs={"layer": "MURS"})
        msp.add_line((6400, 6200), (0, 6200), dxfattribs={"layer": "MURS"})
        msp.add_line((0, 6200), (0, 0), dxfattribs={"layer": "MURS"})
        
        # Door opening at x = 820
        msp.add_text("Porte Entree", dxfattribs={"layer": "OUVERTURES", "height": 200}).set_placement((820, 0))
        doc.saveas(self.dxf_path)

    def tearDown(self):
        self.temp_dir.cleanup()

    def test_dxf_info(self):
        res = json.loads(dxf_info(self.dxf_path))
        self.assertTrue(res["success"])
        self.assertEqual(res["dxfversion"], "AC1032")
        layers = [l["name"] for l in res["layers"]]
        self.assertIn("MURS", layers)
        self.assertIn("OUVERTURES", layers)

    def test_dxf_list_entities(self):
        res = json.loads(dxf_list_entities(self.dxf_path, layer="MURS"))
        self.assertTrue(res["success"])
        self.assertEqual(res["entityCount"], 4)

    def test_dxf_audit(self):
        res = json.loads(dxf_audit(self.dxf_path))
        self.assertTrue(res["success"])
        self.assertTrue(res["isValid"])
        self.assertEqual(res["errorCount"], 0)

    def test_dxf_edit(self):
        out_path = os.path.join(self.temp_dir.name, "edited.dxf")
        ops = [
            {"action": "add_line", "start": [1000, 1000], "end": [2000, 1000], "layer": "CLOISON"}
        ]
        res = json.loads(dxf_edit(self.dxf_path, ops, out_path))
        self.assertTrue(res["success"])
        self.assertTrue(os.path.exists(out_path))

    def test_dxf_render_pdf(self):
        pdf_out = os.path.join(self.temp_dir.name, "render.pdf")
        res = json.loads(dxf_render(self.dxf_path, pdf_out, format="pdf"))
        self.assertTrue(res["success"])
        self.assertTrue(os.path.exists(pdf_out))
        self.assertGreater(os.path.getsize(pdf_out), 0)

    def test_dxf_render_all_layouts_pdf(self):
        multi_pdf = os.path.join(self.temp_dir.name, "all_layouts.pdf")
        res = json.loads(dxf_render_all_layouts_pdf(self.dxf_path, multi_pdf))
        self.assertTrue(res["success"])
        self.assertTrue(os.path.exists(multi_pdf))
        self.assertGreaterEqual(res["pageCount"], 1)

    def test_dxf_diff(self):
        dxf_b = os.path.join(self.temp_dir.name, "test_sample_b.dxf")
        doc_b = ezdxf.readfile(self.dxf_path)
        msp_b = doc_b.modelspace()
        msp_b.add_line((100, 100), (500, 500), dxfattribs={"layer": "MURS"})
        doc_b.saveas(dxf_b)

        res = json.loads(dxf_diff(self.dxf_path, dxf_b))
        self.assertTrue(res["success"])
        self.assertEqual(res["summary"]["addedCount"], 1)

    def test_dxf_compare_with_revit(self):
        revit_elements = [
            {
                "id": 12345,
                "name": "Porte simple",
                "category": "Portes",
                "location": {"type": "point", "point": {"x": 860, "y": 0, "z": 0}}
            }
        ]
        res = json.loads(dxf_compare_with_revit(self.dxf_path, revit_elements, toleranceMm=20.0))
        self.assertTrue(res["success"])
        self.assertEqual(res["discrepancyCount"], 0)

    def test_dxf_compare_with_bimcraft(self):
        bimcraft_elements = [
            {
                "id": 100003,
                "type": "Single Door 900 x 2100",
                "category": "door",
                "at": [860.0, 0.0]
            },
            {
                "id": 100004,
                "type": "Generic Wall 200",
                "category": "wall",
                "points": [[0.0, 0.0], [6400.0, 0.0]]
            }
        ]
        res = json.loads(dxf_compare_with_bimcraft(self.dxf_path, bimcraft_elements, toleranceMm=20.0))
        self.assertTrue(res["success"])
        self.assertEqual(res["discrepancyCount"], 0)
        self.assertEqual(res["elementsChecked"], 2)

    def test_bimcraft_export_dxf_conversion(self):
        # Test converting a sample bimcraft or rvt file if present
        sample_bimcraft = r"C:\Users\teoba\Documents\Projet Appart Londonien\06_Revit\REFLET_00_EXISTANT.bimcraft"
        if os.path.exists(sample_bimcraft):
            out_svg = os.path.join(self.temp_dir.name, "bimcraft_plan.svg")
            res = json.loads(bimcraft_export_dxf(sample_bimcraft, out_svg))
            self.assertTrue(res["success"])
            self.assertTrue(os.path.exists(out_svg))

if __name__ == "__main__":
    unittest.main()
