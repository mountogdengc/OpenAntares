"""Negative controls for the explicit, exact recorder-budget equivalence rule."""
import ast,hashlib,json,subprocess,tempfile,types
from pathlib import Path
r=Path.cwd();base=r/'reports/conflict-ui-integration-20261104';out=base/'source-equivalence-negative-controls.json'
if out.exists():raise SystemExit('Retain earlier evidence; choose a fresh output name')
module=ast.parse((base/'Build-Summary.py').read_text(encoding='utf-8'))
nodes=[n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='source_changes' or isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'BUDGET_FILES','OLD_LIMIT','NEW_LIMIT'} for t in n.targets)]
source=json.loads((base/'gate06-Legacy/source.json').read_text(encoding='utf-8-sig'))
paths={'tools/ConflictValidation/Run-NativeMatrix.ps1','tests/OpenAntares.Game.Validation/Run-Validation.ps1','game/Screens/GameScreen.cs'}
rows=[row for row in source['files'] if row['path'] in paths]
assert len(rows)==3
owned_root=Path('C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.conflict-ui-equivalence-fixtures').resolve();owned_root.mkdir(exist_ok=True)
fixture=Path(tempfile.mkdtemp(prefix='owned-',dir=owned_root)).resolve();assert fixture.parent==owned_root
original={}
for row in rows:
 data=subprocess.check_output(['git','show',source['commit']+':'+row['path']]);assert hashlib.sha256(data).hexdigest()==row['sha256'];original[row['path']]=data
 path=fixture/row['path'];assert path.resolve().is_relative_to(fixture);path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
ns={'r':fixture,'expected':paths,'a':types.SimpleNamespace(rival_budget_equivalence=False),'hashlib':hashlib,'subprocess':subprocess}
exec(compile(ast.fix_missing_locations(ast.Module(body=nodes,type_ignores=[])),'actual-source-equivalence-rule','exec'),ns)
check=ns['source_changes'];sample=dict(source,files=rows);results=[]
def expect(name,value):
 results.append({'name':name,'passed':bool(value)})
 if not value:raise AssertionError(name)
error=None
try:
 expect('Exact committed inputs pass with strict mode',check(sample)==[])
 for path in ns['BUDGET_FILES']:
  assert original[path].count(ns['OLD_LIMIT'])==1
  (fixture/path).write_bytes(original[path].replace(ns['OLD_LIMIT'],ns['NEW_LIMIT']))
 expect('Budget changes fail when equivalence is not explicitly enabled',check(sample) is None)
 ns['a'].rival_budget_equivalence=True
 expect('Only the exact Rival safety-cap array edit is accepted', {x['path'] for x in check(sample)}==ns['BUDGET_FILES'])
 game=fixture/'game/Screens/GameScreen.cs';game.write_bytes(original['game/Screens/GameScreen.cs']+b'\n// Changed UI input\n')
 expect('A changed game/UI input is rejected',check(sample) is None);game.write_bytes(original['game/Screens/GameScreen.cs'])
 launcher=fixture/'tools/ConflictValidation/Run-NativeMatrix.ps1';budget=launcher.read_bytes();launcher.write_bytes(budget+b'\n# Additional launcher change\n')
 expect('Any additional launcher edit is rejected',check(sample) is None);launcher.write_bytes(budget)
 launcher.write_bytes(budget.replace(b'72000',b'720000'))
 expect('A different frame-cap change is rejected',check(sample) is None);launcher.write_bytes(budget)
 held=game.with_suffix('.held');assert held.resolve().is_relative_to(fixture) and not held.exists();game.rename(held)
 expect('A missing game/UI input is rejected',check(sample) is None);held.rename(game)
 expect('A subset inventory is rejected',check(dict(sample,files=rows[:-1])) is None)
 ns['expected']=paths|{'new-required.cs'}
 expect('An unrecorded required input is rejected',check(sample) is None)
except Exception as e:error=str(e)
record={'passed':error is None,'error':error,'checks':results,'sourceCommit':source['commit'],'retainedOwnedFixture':str(fixture),'fixturePreservation':'Fresh synthetic copies remain outside every game checkout; no QA/source cleanup performed.'}
out.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8');print(json.dumps({'passed':record['passed'],'checks':len(results),'error':error}))
raise SystemExit(0 if error is None else 1)
