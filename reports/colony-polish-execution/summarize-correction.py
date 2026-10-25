"""Summarize retained runtime outputs without counting provenance as assertions."""
import json
import re
import subprocess
import sys
from pathlib import Path

root = Path(sys.argv[1])
source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
summary = {'source': source, 'root': str(root), 'scenarios': {}, 'method': 'Windows Native actual controls/signals; no human-input or release claim.'}
simulation = (root / 'simulation.log').read_text(encoding='utf-8-sig')
match = re.search(r'Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+)', simulation)
assert match, 'Missing simulation result'
summary['simulation'] = dict(zip(['failed', 'passed', 'skipped'], map(int, match.groups())))
assert summary['simulation']['failed'] == summary['simulation']['skipped'] == 0
for name in sys.argv[2].split(','):
    folder = Path(sys.argv[3]) if name == 'colony' and len(sys.argv) > 3 else root / name
    result = json.loads((folder / 'results.json').read_text(encoding='utf-8-sig'))
    rows = result['results']
    provenance = [r for r in rows if 'provenance' in r]
    geometry = [r for r in rows if 'actualEmbedded' in r]
    assertions = [r for r in rows if 'passed' in r and 'provenance' not in r and 'actualEmbedded' not in r]
    assert result['passed'] and not result.get('error'), (name, result.get('error'))
    assert all(r.get('passed') is True for r in assertions + provenance + geometry), name
    missing = [p for p in result['retainedQaSavePaths'] if not Path(p).is_file()]
    assert not missing, missing
    diagnostics = re.findall(r'^(?:ERROR|WARNING|SCRIPT ERROR):.*', (folder / 'godot.log').read_text(encoding='utf-8-sig'), re.M)
    assert not diagnostics, (name, diagnostics)
    normal = (folder / 'normal-metadata.log').read_text(encoding='utf-8-sig')
    optin = (folder / 'opt-in-metadata.log').read_text(encoding='utf-8-sig')
    assert 'absent' in normal.lower(), (name, normal)
    assert 'GraphicalValidation' in optin, (name, optin)
    summary['scenarios'][name] = {'evidencePath': str(folder), 'assertions': len(assertions), 'provenanceRows': len(provenance), 'geometryRows': len(geometry), 'qaPaths': len(result['retainedQaSavePaths']), 'pngs': len(list(folder.rglob('*.png'))), 'diagnostics': diagnostics, 'generatedCampaigns': provenance}
(root / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
print(json.dumps({**summary, 'scenarios': {k:{kk:vv for kk,vv in v.items() if kk!='generatedCampaigns'} for k,v in summary['scenarios'].items()}}, indent=2))
