from pathlib import Path
from collections import Counter
import argparse,json,re,hashlib
PATTERNS={'override':r'\bAddTheme\w*Override\s*\(','color':r'\b(?:new\s+)?Color\s*\(|\bColors\.','stylebox':r'\bnew\s+(?:Godot\.)?StyleBox\w*','font':r'\bnew\s+(?:Godot\.)?(?:FontFile|SystemFont)\b'}
def occurrences(root):
 rows=[]
 for p in sorted((Path(root)/'game').rglob('*.cs')):
  for n,line in enumerate(p.read_text(encoding='utf-8-sig').splitlines(),1):
   code=line.split('//',1)[0].strip()
   if not code:continue
   kinds=[k for k,v in PATTERNS.items() if re.search(v,code)]
   if 'CustomMinimumSize' in code and re.search(r'CustomMinimumSize\s*=',code):
    rhs=code.split('CustomMinimumSize',1)[1].split(';',1)[0];rhs=re.sub(r'"[^"]*"','""',rhs)
    if re.search(r'(?<![\w])(?:[1-9]\d*(?:\.\d*)?|0\.\d*[1-9]\d*)\b',rhs):kinds.append('literal_minimum')
   for kind in kinds:rows.append({'path':p.relative_to(root).as_posix(),'line':n,'kind':kind,'fragment':code,'sha256':hashlib.sha256(code.encode()).hexdigest()})
 return rows
def key(row):return row['path'],row['kind'],row['sha256']
def scan(root,allowlist=()):
 remaining=Counter(key(x) for x in allowlist);bad=[]
 for row in occurrences(Path(root)):
  k=key(row)
  if remaining[k]>0:remaining[k]-=1
  else:bad.append(row)
 return bad
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--root',default='.');p.add_argument('--allowlist');p.add_argument('--report');a=p.parse_args();root=Path(a.root).resolve();allowed=json.loads(Path(a.allowlist).read_text(encoding='utf-8-sig'))['entries'] if a.allowlist else [];v=scan(root,allowed);d={'passed':not v,'violations':v,'remainingLegacyOccurrences':len(occurrences(root))-len(v),'baselineAllowance':len(allowed)};s=json.dumps(d,indent=2);print(s)
 if a.report:Path(a.report).write_text(s+'\n',encoding='utf-8')
 raise SystemExit(bool(v))
