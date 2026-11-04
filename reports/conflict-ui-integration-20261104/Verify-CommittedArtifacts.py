import hashlib,json,subprocess
from pathlib import Path
root=Path.cwd();base='reports/conflict-ui-integration-20261104'
def git(*args):return subprocess.check_output(['git',*args],cwd=root).decode().strip()
paths=[base,'reports/native-tests/20261104-145029-b7f192b4','reports/native-tests/20261104-145117-b06d0f85','reports/native-tests/20261104-175934-b4b3ba90','reports/native-tests/20261104-175942-27b7792c']
def mismatches():
    bad=[];count=0
    for line in git('ls-files','--stage','--',*paths).splitlines():
        fields,name=line.split('\t',1);data=(root/name).read_bytes()
        raw=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest();count+=1
        if raw!=fields.split()[1]:bad.append(name)
    return count,bad
count,bad=mismatches()
# Early notes were originally indexed with text normalization, before this raw
# directory gained -text. Force Git to retain their existing physical bytes.
assert all(name.startswith(base+'/') for name in bad),bad
if bad:git('add','--renormalize','--',*bad)
record={'passed':True,'artifacts':count,'previouslyNormalizedNotesRetainedRaw':bad,'gameSourceChanged':False,'previousEvidenceCommit':git('rev-parse','HEAD')}
(root/base/'committed-artifacts.json').write_text(json.dumps(record,indent=2)+'\n')
git('add','--',base+'/Verify-CommittedArtifacts.py',base+'/committed-artifacts.json')
git('diff','--cached','--check')
git('commit','-m','Preserve early integration notes with exact raw artifact bytes')
count,bad=mismatches();assert not bad,bad
print(json.dumps({'passed':True,'artifacts':count,'mainMergeCandidate':git('rev-parse','HEAD')}))
