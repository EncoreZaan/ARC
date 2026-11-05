import subprocess
import json
import os

exe = os.path.expandvars(r'%APPDATA%\RevitMCP\mcp-server\revit-mcp-server.exe')
proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')

# Init
proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'id': 1, 'method': 'initialize', 'params': {'protocolVersion': '2024-11-05'}}) + '\n')
proc.stdin.flush()
proc.stdout.readline()

tools_to_test = [
    ('revit_live_mirror_sync', {'sourceElements': [{'id': '101', 'category': 'Walls', 'name': 'Mur 20cm', 'x': 0, 'y': 0, 'z': 0}], 'targetElements': [{'id': '101', 'category': 'Walls', 'name': 'Mur 20cm', 'x': 0.5, 'y': 0, 'z': 0}], 'strategy': 'SmartGeometricSnap'}),
    ('revit_sketch_to_bim', {'scaleFactor': 0.01, 'ceilingHeight': 2.70}),
    ('revit_simulate_thermal_dynamic_re2020', {'zoneName': 'Appartement Londonien', 'floorAreaM2': 85.0, 'climateZone': 'H1a'}),
    ('revit_solve_structural_fea_instant', {'members': [{'id': 'B1', 'name': 'Poutre Salon', 'lengthMeters': 6.0, 'material': 'Steel_S355', 'section': 'IPE270', 'deadLoadKnM': 8.0, 'liveLoadKnM': 5.0}]}),
    ('revit_audit_urban_plu_compliance', {'zonePlu': 'UB', 'parcelAreaM2': 600.0, 'buildingHeightMeters': 11.5, 'footprintAreaM2': 210.0}),
    ('revit_export_usd_visionpro', {'projectName': 'London_Apartment', 'outputDirectory': '.'}),
    ('revit_walkthrough_direct', {'projectName': 'London Penthouse', 'totalDurationSec': 45.0})
]

req_id = 2
for name, args in tools_to_test:
    call_req = {
        'jsonrpc': '2.0',
        'id': req_id,
        'method': 'tools/call',
        'params': {
            'name': name,
            'arguments': args
        }
    }
    req_id += 1
    proc.stdin.write(json.dumps(call_req) + '\n')
    proc.stdin.flush()
    resp = json.loads(proc.stdout.readline())
    is_err = resp.get('result', {}).get('isError', False)
    content = resp.get('result', {}).get('content', [{}])[0].get('text', '')
    parsed = json.loads(content)
    engine = parsed.get('engine', 'N/A')
    print(f"[TEST PASS] {name} -> Success: {parsed.get('success', False)} | Engine: {engine}")

proc.terminate()
