"""M14 evidence is complete only with every frame, input, diagnostic and restoration witness."""
import argparse,json,re,struct,importlib.util
from pathlib import Path
from collections import Counter
spec=importlib.util.spec_from_file_location('m13_source_contract',Path(__file__).with_name('Verify-PlayabilityEvidence.py'));core=importlib.util.module_from_spec(spec);spec.loader.exec_module(core)
core.SOURCE_ROOTS+=('art','tools/Check-NoStylingOverrides.py','tools/Check-NoStylingOverrides.ps1','tools/styling-allowlist.json','tests/OpenAntares.Presentation.Tests','OpenAntares.sln','LICENSING.md')
core.SOURCE_SUFFIXES|={'.png','.svg','.ttf','.otf','.gdshader','.txt','.md','.sln'}
CASES={'Theme','CommandBar','Status','Console','Fleets','Workspaces','Ledger','Map','Copy','Contrast'}
SIZES={(800,600),(1152,648),(1600,900),(1920,1080),(2560,1440)}
def verify_case(record,root,development=False,expected_paths=None):
 root=Path(root).resolve();errors=[]
 def artifact(ref,label,document=False):
  try:
   if not isinstance(ref,dict) or not isinstance(ref.get('path'),str):raise ValueError('Missing artifact: '+label)
   p=(root/ref['path']).resolve()
   if not p.is_relative_to(root):raise ValueError('Escaping artifact: '+label)
   b=p.read_bytes()
   if core.sha(b)!=ref.get('sha256'):raise ValueError('Changed artifact: '+label)
   return json.loads(b.decode('utf-8-sig')) if document else b
  except (OSError,ValueError,UnicodeError) as e:errors.append(str(e));return None
 source=record.get('source')
 if not isinstance(source,dict):errors.append('Missing source');source={}
 if not development and source.get('committed') is not True:errors.append('Uncommitted source cannot pass recorded acceptance')
 rows=source.get('files',[])
 try:
  expected=core.inventory(root) if expected_paths is None else set(expected_paths);paths=[x['path'] for x in rows]
  if not expected or set(paths)!=expected or len(paths)!=len(set(paths)):errors.append('Incomplete authoritative source inventory')
  for row in rows:
   p=(root/row['path']).resolve()
   if not p.is_relative_to(root) or core.sha(p.read_bytes())!=row['sha256']:errors.append('Current source changed: '+row['path'])
  if not development:
   committed=core.committed_hashes(root,source.get('commit',''),paths)
   if any(committed[x['path']]!=x.get('canonicalSha256') for x in rows):errors.append('Source is not the recorded commit')
 except (OSError,ValueError,KeyError,Exception) as e:errors.append('Source identity unavailable: '+str(e))
 assembly=artifact(record.get('assembly'),'assembly')
 if assembly is not None and core.sha(assembly)!=source.get('assemblySha256'):errors.append('Executed assembly differs from source record')
 result=artifact(record.get('result'),'result',True)
 if result is not None:
  if result.get('case')!=record.get('case') or result.get('passed') is not True or result.get('error') is not None or not result.get('checks') or any(x.get('passed') is not True for x in result['checks']):errors.append('Native assertions incomplete or failed')
  frames=result.get('frames',[])
  expected_sizes={(800,600),(1920,1080)} if record.get('case') in {'FleetRoster','SelectionConsole','PlaytestFeedback','FleetPanel','MultiFleet'} else {(1920,1080)} if record.get('case') in {'PopulationForecast','MapKeys'} else SIZES
  if {(x.get('width'),x.get('height')) for x in frames}!=expected_sizes:errors.append('Required selected-case frames incomplete')
  labels=[]
  for frame in frames:
   labels.append((frame.get('label'),frame.get('width'),frame.get('height')))
   pixels=artifact(frame.get('frame'),'frame');inventory=artifact(frame.get('inventory'),'control inventory',True)
   if pixels is not None and (not pixels.startswith(b'\x89PNG\r\n\x1a\n') or len(pixels)<24 or struct.unpack('>II',pixels[16:24])!=(frame.get('width'),frame.get('height'))):errors.append('Frame PNG dimensions differ')
   if not isinstance(inventory,list) or not inventory:errors.append('Control inventory empty')
   for window in frame.get('windows',[]):
    data=artifact(window.get('frame'),'native window frame')
    if data is not None and (len(data)<24 or not data.startswith(b'\x89PNG\r\n\x1a\n') or struct.unpack('>II',data[16:24])!=(window.get('width'),window.get('height'))):errors.append('Native window frame dimensions differ')
  if len(labels)!=len(set(labels)):errors.append('Duplicate frame coverage')
 opt=artifact(record.get('optInMetadata'),'opt-in metadata',True);normal=artifact(record.get('normalMetadata'),'normal metadata',True)
 if opt is not None and (opt.get('presentScenarioTypes')!=['VisualOverhaulValidation'] or 'VisualOverhaulValidation' not in opt.get('validationNamespaceTypes',[])):errors.append('M14 isolated opt-in metadata incomplete')
 if normal is not None and any(normal.get(k)!=[] for k in ['presentScenarioTypes','presentHelpers','validationNamespaceTypes']):errors.append('Ordinary namespace restoration incomplete')
 guard=artifact(record.get('styleGuard'),'styling guard',True);baseline=artifact(record.get('baselineAllowance'),'baseline style allowance',True)
 if guard is not None and (guard.get('passed') is not True or guard.get('violations')):errors.append('Styling boundary failed')
 if baseline is not None:
  try:
   actual=json.loads((root/'tools/styling-allowlist.json').read_text(encoding='utf-8-sig'))['entries'];keys=lambda items:Counter((x['path'],x['kind'],x['sha256']) for x in items)
   if keys(actual)-keys(baseline['entries']):errors.append('Legacy styling allowance grew')
  except (OSError,ValueError,KeyError) as e:errors.append('Styling allowance unavailable: '+str(e))
 for name in ['engineLog','consoleLog']:
  data=artifact(record.get(name),name)
  if data is not None and re.search(rb'(?m)^\s*(ERROR:|SCRIPT ERROR:|WARNING:)|Object was deleted while awaiting a callback\.|CrashHandlerException:',data):errors.append('Native diagnostics: '+name)
 if record.get('exitCode')!=0:errors.append('Native exit failed')
 if record.get('assemblyStable') is not True:errors.append('Executed DLL changed')
 if record.get('normalRestored') is not True:errors.append('Ordinary restoration missing')
 return errors

def verify_suite(summary,root,development=False):
 errors=[];records=summary.get('records',[])
 if len(records)!=len(CASES) or {x.get('case') for x in records}!=CASES:errors.append('All ten unique M14 cases are required')
 if summary.get('passed') is not True or summary.get('normalRestored') is not True:errors.append('Suite execution/restoration incomplete')
 epochs={core.sha(json.dumps(x.get('source'),sort_keys=True).encode()) for x in records}
 if len(epochs)!=1:errors.append('M14 cases do not share one source/DLL epoch')
 for record in records:errors.extend(record.get('case','Unknown')+': '+e for e in verify_case(record,root,development))
 return errors
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('record',nargs='?');p.add_argument('--root',default='.');p.add_argument('--development',action='store_true');p.add_argument('--capture-source');p.add_argument('--assembly');p.add_argument('--suite',action='store_true');a=p.parse_args();root=Path(a.root)
 if a.capture_source:print(json.dumps(core.capture_source(root,Path(a.capture_source),Path(a.assembly),a.development),indent=2));raise SystemExit(0)
 if not a.record:p.error('execution record required')
 d=json.loads(Path(a.record).read_text(encoding='utf-8-sig'));errors=verify_suite(d,root,a.development) if a.suite else verify_case(d,root,a.development);print(json.dumps({'passed':not errors,'accepted':not errors and not a.development,'scope':'M14 automated suite; human visual acceptance separate' if a.suite else 'Selected M14 case; full coverage separate','errors':errors},indent=2));raise SystemExit(bool(errors))
