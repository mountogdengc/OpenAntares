import hashlib,json,subprocess
from pathlib import Path
root=Path.cwd()
base=root/'reports/conflict-ui-integration-20261104'
def git(*args):
    return subprocess.check_output(['git',*args],cwd=root).decode().strip()
assert json.loads((base/'premerge-invariants.json').read_text())['passed']
assert git('rev-parse','HEAD')=='d49e63c79e7e7389b134e9f015646a38d84562b6'
paths=['.gitattributes','AGENTS.md','PROJECT_STATUS.md','CLAUDE.md','docs/TEST_RUNNER.md',
       'reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md','reports/conflict-ui-integration-20261104',
       'reports/native-tests/20261104-145029-b7f192b4','reports/native-tests/20261104-145117-b06d0f85',
       'reports/native-tests/20261104-175934-b4b3ba90','reports/native-tests/20261104-175942-27b7792c',
       'reports/conflict-execution/save-tests/db1bc7b49f29479cbe3eadc47fb59c49',
       'reports/conflict-execution/save-tests/e70ee31a84544b43b5cb0f16b25e09ec',
       'reports/scout-execution/task14-generated-20261104-145209-8313216.json',
       'reports/scout-execution/task14-generated-20261104-180020-7953955.json',
       'reports/scout-execution/task14-parity-20261104-145219-4815484.json',
       'reports/scout-execution/task14-parity-20261104-180030-5307183.json']
existing=git('diff','--cached','--name-only').splitlines()
assert all(any(p==prefix or p.startswith(prefix+'/') for prefix in paths) for p in existing), 'Do not include unrelated staged changes'
git('add','--','.gitattributes')
git('add','--',*paths)
staged=git('diff','--cached','--name-only').splitlines()
assert all(any(p==prefix or p.startswith(prefix+'/') for prefix in paths) for p in staged)
assert not any('__pycache__' in p or p.endswith('.uid') for p in staged)
git('diff','--cached','--check')
git('commit','-m','Record combined UI and conflict acceptance and preserve integration evidence')
# Verify raw acceptance artifacts survived Git storage byte for byte.
index={}
for line in git('ls-files','--stage','--',*paths).splitlines():
    fields,path=line.split('\t',1)
    index[path]=fields.split()[1]
raw=[p for p in index if p.startswith('reports/conflict-ui-integration-20261104/') or p.startswith('reports/native-tests/')]
for name in raw:
    data=(root/name).read_bytes()
    assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==index[name],name
print(json.dumps({'passed':True,'commit':git('rev-parse','HEAD'),'rawCommittedArtifacts':len(raw),'stagedFiles':len(staged)}))
