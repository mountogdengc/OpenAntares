"""Read-only inventory of actual native result/metadata/diagnostic artifacts."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def runtime(folder, log):
    folder, log = ROOT / folder, ROOT / log
    result = json.loads((folder / 'results.json').read_text(encoding='utf-8-sig'))
    optin = json.loads((folder / 'opt-in-metadata.json').read_text())
    ordinary = json.loads((folder / 'normal-metadata.json').read_text())
    diagnostics = []
    for path in (folder / 'godot.log', log):
        diagnostics.extend(f'{path.name}:{line}' for line in path.read_text(encoding='utf-8-sig').splitlines() if 'ERROR:' in line or 'WARNING:' in line)
    assert result['passed'] and not diagnostics, (folder, diagnostics)
    assert len(optin['presentScenarioTypes']) == 7
    assert not ordinary['presentScenarioTypes'] and not ordinary['presentHelpers']
    records = result.get('checks', result.get('results', []))
    assert records and all(row.get('passed', True) for row in records)
    files = sorted(p for p in folder.rglob('*') if p.is_file())
    saves = [Path(path) for path in result.get('retainedQaSavePaths', [])]
    assert all(path.is_file() for path in saves)
    return dict(scenario=result.get('scenario', folder.name), records=len(records), passed=True,
                diagnostics=diagnostics, optInCount=7, ordinaryCount=0, ordinaryHelpers=0,
                artifact=folder.relative_to(ROOT).as_posix(),
                processLog=log.relative_to(ROOT).as_posix(),
                qaSaves=[dict(path=str(p), sha256=digest(p)) for p in saves],
                pngCount=sum(p.suffix == '.png' for p in files),
                files=[dict(path=p.relative_to(ROOT).as_posix(), sha256=digest(p)) for p in files])


if __name__ == '__main__':
    import sys
    print(json.dumps(runtime(*sys.argv[1:]), indent=2))
