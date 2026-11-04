import hashlib,json,subprocess
from pathlib import Path
root=Path.cwd()
main=Path('F:/Projects-PCGames/OpenAntares')
base='reports/conflict-ui-integration-20261104'
def git(*args,cwd=root):
    return subprocess.check_output(['git','-C',str(cwd),*args]).decode().strip()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert json.loads((root/base/'final-gates09.json').read_text(encoding='utf-8-sig'))['passed']
assert json.loads((root/base/'premerge-invariants.json').read_text())['passed']
tested=json.loads((root/base/'final-summary.json').read_text())['productSource']
source=git('rev-parse','HEAD')
assert git('branch','--show-current',cwd=main)=='main'
before=git('rev-parse','HEAD',cwd=main)
assert before=='234b45e5eab968d768095d7c858c95bfa12e92f0'
assert not git('status','--porcelain',cwd=main)
assert not git('diff','HEAD','--',*[r['path'] for r in tested['files']])
recovery='codex/conflict-ui-premerge-20261104'
existing=git('branch','--list',recovery)
if existing:
    assert git('rev-parse',recovery)==before
else:
    git('branch',recovery,before)
merge_output=git('merge','--ff-only','codex/conflict-ui-integration',cwd=main)
assert git('rev-parse','HEAD',cwd=main)==source
for row in tested['files']:
    assert sha(main/row['path'])==row['sha256'],row['path']
for ancestor in ['f2dc9bf','b806821']:
    git('merge-base','--is-ancestor',ancestor,'HEAD',cwd=main)
# Check all committed raw integration/native evidence against its Git object.
count=0
for line in git('ls-files','--stage','--',base,'reports/native-tests',cwd=main).splitlines():
    fields,name=line.split('\t',1)
    data=(main/name).read_bytes()
    assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==fields.split()[1],name
    count+=1
out=main/base/'postmerge'
out.mkdir(exist_ok=False)
(out/'merge.log').write_text(merge_output+'\n')
(out/'merge.json').write_text(json.dumps({'passed':True,'mainBefore':before,'mainAfter':source,'recoveryBranch':recovery,'testedSourceCommit':tested['commit'],'exactSourceFiles':len(tested['files']),'rawCommittedArtifacts':count,'uiAncestor':'f2dc9bf','conflictAncestor':'b806821','postmergeBuildAndSuite':'pending'},indent=2)+'\n')
print(json.dumps({'passed':True,'mergedMain':source,'exactSourceFiles':len(tested['files']),'rawCommittedArtifacts':count}))
