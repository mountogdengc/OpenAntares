"""Verify exact frozen save/evidence bytes through real Git checkout filters."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args()
output=Path(args.output).resolve();assert not output.exists(),'Use a fresh probe directory; preserve previous evidence.'
output.mkdir(parents=True)
expected={}
def add(path,sha):
    assert not Path(path).is_absolute() and '..' not in Path(path).parts
    if path in expected:assert expected[path]==sha
    expected[path]=sha
summary=json.loads((ROOT/'reports/scout-execution/final-machine-summary.json').read_text())
for run in summary['runtime']:
    for file in run['files']:add(file['path'],file['sha256'])
polish=json.loads((ROOT/'reports/scout-polish-20261026/summary.json').read_text())
for file in polish['runtime']['files']:add(file['path'],file['sha256'])
base='tests/OpenAntares.Simulation.Tests/Fixtures/rules6-scout-baselines/'
manifest=json.loads((ROOT/base/'manifest.json').read_text())
for case in manifest['cases']:
    for pathkey,hashkey in [('save','saveSha256'),('futures','futureSha256'),('commands','commandSha256')]:add(base+case[pathkey],case[hashkey])
prefix=output.as_posix().rstrip('/')+'/'
subprocess.run(['git','checkout-index','-z','--stdin','--prefix='+prefix],cwd=ROOT,input=('\0'.join(expected)+'\0').encode(),check=True)
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
rows=[dict(path=path,expected=sha,checkout=digest(output/path),working=digest(ROOT/path)) for path,sha in sorted(expected.items())]
failures=[row for row in rows if row['expected']!=row['checkout']]
(output/'audit.json').write_text(json.dumps(dict(passed=not failures,checked=len(rows),failures=failures,files=rows),indent=2))
print(f'Actual checkout: {len(rows)} files, {len(failures)} hash mismatches.')
for row in failures[:5]:print(row['path'],row['expected'],'!=',row['checkout'])
raise SystemExit(1 if failures else 0)
