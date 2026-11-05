import subprocess
import json
import os

exe = os.path.expandvars(r'%APPDATA%\RevitMCP\mcp-server\revit-mcp-server.exe')
proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')

# Init
proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'id': 1, 'method': 'initialize', 'params': {'protocolVersion': '2024-11-05'}}) + '\n')
proc.stdin.flush()
proc.stdout.readline()

# Tools list
proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'id': 2, 'method': 'tools/list', 'params': {}}) + '\n')
proc.stdin.flush()
tools_resp = json.loads(proc.stdout.readline())
tools_list = tools_resp['result']['tools']
print(f"Total tools in deployed server: {len(tools_list)}")

tools_to_test = [
    ('revit_auto_dimension_plan', {'offsetBaseMeters': 0.80, 'offsetSpacingMeters': 0.50}),
    ('revit_compare_variants_visual', {'viewTitle': 'Plan de Démolition & Création RDC'}),
    ('revit_geometry_doctor', {'angleToleranceDeg': 0.85, 'gapToleranceMm': 25.0}),
    ('revit_layout_casework_millwork', {'layoutType': 'DressingPlacard', 'totalLengthMeters': 3.20, 'depthMeters': 0.60, 'heightMeters': 2.50}),
    ('revit_extract_takeoff_artisan_table', {'projectTitle': 'Appartement Londonien Reflet'})
]

req_id = 3
all_passed = True
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
    success = parsed.get('success', False)
    if not success or is_err:
        all_passed = False
    print(f"[TEST PASS] {name} -> Success: {success} | Engine: {engine}")

proc.terminate()
print("All 5 pragmatic tools tested successfully!" if all_passed else "Some tests failed!")
