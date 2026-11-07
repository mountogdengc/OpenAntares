"""Fail closed on incomplete M13 evidence; development checks are never final acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

SOURCE_ROOTS=('src','game','content','tests/OpenAntares.Game.Validation',
              'tools/ConflictCampaignProbe','tools/ConflictValidation','OpenAntares.csproj','project.godot')
SOURCE_SUFFIXES={'.cs','.csproj','.json','.tscn','.tres','.targets','.py','.ps1','.godot'}

def sha(data):return hashlib.sha256(data).hexdigest()
def canonical(data):return data.replace(b'\r\n',b'\n')
def git(root,*args,input=None):
    return subprocess.run(['git','-C',str(root),*args],input=input,capture_output=True,check=True).stdout

def inventory(root):
    tracked=set(git(root,'ls-files','-z','--',*SOURCE_ROOTS).decode('utf-8').split('\0'))-{''}
    extra=set(git(root,'ls-files','-z','--others','--exclude-standard','--',*SOURCE_ROOTS).decode('utf-8').split('\0'))-{''}
    extra={p for p in extra if Path(p).suffix.lower() in SOURCE_SUFFIXES and not {'bin','obj','__pycache__'}.intersection(Path(p).parts)}
    return tracked|extra

def committed_hashes(root,commit,paths):
    if not re.fullmatch(r'[0-9a-f]{40}',commit):raise ValueError('Invalid source commit')
    ordered=sorted(paths)
    data=git(root,'cat-file','--batch',input=('\n'.join(commit+':'+p for p in ordered)+'\n').encode())
    result={};offset=0
    for path in ordered:
        end=data.index(b'\n',offset);header=data[offset:end].split();offset=end+1
        if len(header)!=3 or header[1]!=b'blob':raise ValueError('Source is absent from recorded commit: '+path)
        size=int(header[2]);blob=data[offset:offset+size];offset+=size+1
        if len(blob)!=size:raise ValueError('Truncated Git source blob')
        result[path]=sha(canonical(blob))
    return result

def capture_source(root,output,assembly,development=False):
    root=root.resolve();output=output.resolve();assembly=assembly.resolve()
    if not output.is_relative_to(root) or output.exists():raise ValueError('Choose a fresh source record inside the active checkout')
    paths=inventory(root)
    if not paths:raise ValueError('No authoritative inputs found')
    rows=[]
    for name in sorted(paths):
        p=(root/name).resolve()
        if not p.is_relative_to(root):raise ValueError('Source escapes checkout: '+name)
        data=p.read_bytes();rows.append({'path':name,'sha256':sha(data),'canonicalSha256':sha(canonical(data))})
    commit=git(root,'rev-parse','HEAD').decode().strip()
    try:
        blobs=committed_hashes(root,commit,paths)
        committed=all(blobs[r['path']]==r['canonicalSha256'] for r in rows)
    except (ValueError,subprocess.CalledProcessError):committed=False
    if not committed and not development:raise ValueError('Authoritative inputs are uncommitted; final acceptance requires committed source')
    output.parent.mkdir(parents=True,exist_ok=True)
    retained=output.parent/'validation-assembly.dll'
    if retained.exists():raise ValueError('Retain earlier DLL evidence; choose a fresh directory')
    shutil.copyfile(assembly,retained)
    record={'commit':commit,'committed':committed,'development':development,'sourceRoot':str(root),
            'assemblySha256':sha(retained.read_bytes()),'files':rows}
    if paths!=inventory(root) or any(sha((root/r['path']).read_bytes())!=r['sha256'] for r in rows):raise ValueError('Source changed during capture')
    output.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
    return {'path':output.relative_to(root).as_posix(),'sha256':sha(output.read_bytes()),'files':len(rows),'committed':committed,'development':development}

def verify_run(record,root,expected_paths=None,require_committed=True):
    root=Path(root).resolve();errors=[]
    def artifact(key,as_json=False):
        ref=record.get(key)
        if not isinstance(ref,dict) or not isinstance(ref.get('path'),str):errors.append('Missing artifact: '+key);return None
        try:
            path=(root/ref['path']).resolve()
            if not path.is_relative_to(root):raise ValueError('Artifact escapes source root')
            data=path.read_bytes()
            if sha(data)!=ref.get('sha256'):raise ValueError('Artifact hash differs')
            return json.loads(data.decode('utf-8-sig')) if as_json else data
        except (OSError,ValueError,UnicodeError) as e:errors.append(key+': '+str(e));return None
    source=record.get('source')
    if not isinstance(source,dict):errors.append('Missing source');source={}
    if require_committed and source.get('committed') is not True:errors.append('Uncommitted source cannot establish final acceptance')
    rows=source.get('files',[])
    try:
        expected=inventory(root) if expected_paths is None else set(expected_paths)
        paths=[r['path'] for r in rows]
        if not expected or len(set(paths))!=len(paths) or set(paths)!=expected:errors.append('Authoritative input inventory differs')
        for row in rows:
            path=(root/row['path']).resolve()
            if not path.is_relative_to(root) or sha(path.read_bytes())!=row.get('sha256'):errors.append('Source bytes differ: '+row['path'])
        if require_committed and expected_paths is None:
            blobs=committed_hashes(root,source.get('commit',''),expected)
            if any(blobs.get(r['path'])!=sha(canonical((root/r['path']).read_bytes())) for r in rows):errors.append('Recorded commit differs from actual inputs')
    except (OSError,ValueError,TypeError,KeyError,subprocess.CalledProcessError) as e:errors.append('Invalid source inventory: '+str(e))
    assembly=artifact('assembly')
    if source.get('assemblySha256') is not None and assembly is not None and sha(assembly)!=source['assemblySha256']:errors.append('Source/DLL identity differs')
    result=artifact('result',True)
    if not isinstance(result,dict) or result.get('case')!=record.get('case') or result.get('passed') is not True or result.get('error') is not None or not result.get('checks') or any(c.get('passed') is not True for c in result.get('checks',[])):
        errors.append('Native case assertions are missing or failed')
    if record.get('case')=='Campaigns':
        try:
            ref=result.get('campaignMatrix') if isinstance(result,dict) else None
            if not isinstance(ref,dict):raise ValueError('Missing campaign matrix artifact')
            matrix_path=(root/ref['path']).resolve()
            if not matrix_path.is_relative_to(root) or sha(matrix_path.read_bytes())!=ref.get('sha256'):raise ValueError('Campaign matrix changed or escaped the source root')
            source_path=(root/record['result']['path']).parent/'source.json'
            if json.loads(source_path.read_text(encoding='utf-8-sig'))!=source:raise ValueError('Campaign compiled source record differs')
            errors.extend(verify_campaign_matrix(json.loads(matrix_path.read_text(encoding='utf-8-sig')),root,sha(source_path.read_bytes())))
        except (OSError,ValueError,KeyError,TypeError) as exc:errors.append('Invalid campaign coverage: '+str(exc))
    normal=artifact('normalMetadata',True)
    if not isinstance(normal,dict) or any(not isinstance(normal.get(k),list) or normal[k] for k in ['presentScenarioTypes','presentHelpers','validationNamespaceTypes']):errors.append('Ordinary validation namespace is not empty')
    opt=artifact('optInMetadata',True)
    if not isinstance(opt,dict) or opt.get('presentScenarioTypes')!=['PlayabilityGraphicalValidation'] or not {'PlayabilityGraphicalValidation','PlayabilityCampaignChecks'}.issubset(opt.get('validationNamespaceTypes',[])):
        errors.append('M13 actual opt-in metadata is incomplete')
    for key in ['engineLog','consoleLog']:
        data=artifact(key)
        if data is not None:
            try:text=data.decode('utf-8-sig')
            except UnicodeError:errors.append('Invalid log encoding: '+key);continue
            if re.search(r'(?m)^\s*(ERROR:|SCRIPT ERROR:|WARNING:)|Object was deleted while awaiting a callback\.|CrashHandlerException:',text):errors.append('Native diagnostics present: '+key)
    if record.get('exitCode')!=0:errors.append('Native process did not complete successfully')
    if record.get('assemblyStable') is not True:errors.append('Compiled assembly changed during execution')
    if record.get('normalRestored') is not True:errors.append('Ordinary assembly restoration failed')
    return errors

def verify_campaign_matrix(matrix,root,source_sha256):
    """Derive fixed 8/40/120 coverage from retained Main saves and complete command tapes."""
    root=Path(root).resolve();errors=[]
    def bad(message):errors.append(message)
    def digest(value):return isinstance(value,str) and re.fullmatch(r'[0-9a-f]{64}',value) is not None
    def artifact(ref,label):
        try:
            if not isinstance(ref,dict) or not isinstance(ref.get('path'),str) or not digest(ref.get('sha256')):raise ValueError('Missing artifact reference')
            path=(root/ref['path']).resolve()
            if not path.is_relative_to(root) or not path.is_file() or path.stat().st_size>32*1024*1024:raise ValueError('Missing, escaping or oversized artifact')
            data=path.read_bytes()
            if sha(data)!=ref['sha256']:raise ValueError('Artifact bytes changed')
            return json.loads(data),data
        except (OSError,ValueError,TypeError,KeyError) as exc:
            bad(label+': '+str(exc));return None,None
    if not isinstance(matrix,dict) or matrix.get('format')!='OpenAntaresM13CampaignMatrix' or matrix.get('version')!=1 or matrix.get('passed') is not True:
        return ['Campaign matrix is missing, unsupported or failed']
    if matrix.get('sourceSha256')!=source_sha256 or not digest(source_sha256):bad('Campaign matrix references different source')
    campaigns=matrix.get('campaigns')
    if not isinstance(campaigns,list) or len(campaigns)!=8:return errors+['Exactly eight complete campaign profiles are required']
    expected={(species,mode):(seed,1 if mode=='Standard' else 0,('harl' if species=='peldra' else 'peldra') if mode=='Standard' else None) for species,seed in [('harl',42),('peldra',45),('sulan',43),('wenth',42)] for mode in ['Standard','Solo']}
    seen=set();main_paths=set();retained_paths=set();future_paths=set();observation_paths=set();checkpoint_count=0;future_count=0
    ui_sources={'EndTurn.Pressed','CombatOrdersDialog.ReviewedCallback','ResearchBar.CommandIssued','ColonyPanel.CommandIssued','VesselPanel.CommandIssued','FleetCommandDialog.CommandIssued','DiplomacyDialog.CommandIssued','CrisisOverviewDialog.CommandRequested','CrisisResponseDialog.CommandRequested','ScoutOverviewDialog.CommandIssued','ConflictOrdersDialog.CommandIssued','EmpireManagementDialog.CommandIssued','AssemblyRequestDialog.CommandIssued','OfficerDetailDialog.CommandIssued'}
    for campaign in campaigns:
        if not isinstance(campaign,dict):bad('Malformed campaign');continue
        key=(campaign.get('species'),campaign.get('mode'));label='/'.join(str(x) for x in key)
        if key not in expected or key in seen:bad(label+': unknown or duplicate profile');continue
        seen.add(key);seed,rivals,rival_species=expected[key]
        exact={'seed':seed,'galaxySize':'small','rivalCount':rivals,'rivalSpeciesId':rival_species,'rules':12,'writer':13,'auditBoundaries':80,'finalTurn':83,'sourceSha256':source_sha256,'decisionSource':'RequesterViewsAndPermittedPlanningQueries'}
        for field,value in exact.items():
            if field not in campaign or campaign[field]!=value:bad(label+': wrong '+field)
        for field in ['crisesEnabled','amebasEnabled','sentriesEnabled','initialMainSaveExactGenerated']:
            if campaign.get(field) is not True:bad(label+': missing required '+field)
        if campaign.get('injectedAssets') is not False:bad(label+': injected or undeclared starting assets')
        observations,_=artifact(campaign.get('observations'),label+' observations')
        observed_path=campaign.get('observations',{}).get('path') if isinstance(campaign.get('observations'),dict) else None
        if observed_path in observation_paths:bad(label+': reused observations artifact')
        observation_paths.add(observed_path)
        def count(value):return isinstance(value,int) and not isinstance(value,bool) and value>=0
        if not isinstance(observations,list) or len(observations)!=83:
            bad(label+': complete eighty audit plus three proof-boundary observations are required');observations=[]
        counters=['ownColonies','ownedVessels','stationary','travelling','noProject','crisisCases','scoutingResponses','conflictBattles','knownWars','ownProductionPaid','ownProductionIncome']
        for turn,row in enumerate(observations,1):
            if not isinstance(row,dict) or row.get('turn')!=turn or any(not count(row.get(key)) for key in counters):
                bad(label+': malformed or out-of-order observation');continue
            shortages=row.get('shortages')
            if not isinstance(shortages,list) or any(not count(value) or value==0 for value in shortages) or len(shortages)!=len(set(shortages)):
                bad(label+': observed shortage identities are missing or malformed')
            if 'researchTarget' not in row or row['researchTarget'] is not None and (not isinstance(row['researchTarget'],str) or not row['researchTarget']):bad(label+': observed research context is missing or malformed')
            if row['stationary']+row['travelling']!=row['ownedVessels'] or row['noProject']>row['ownColonies']:bad(label+': observation counts are inconsistent')
            if turn>1 and isinstance(observations[turn-2],dict):
                for counter_name in ['ownProductionPaid','ownProductionIncome']:
                    prior=observations[turn-2].get(counter_name)
                    if count(prior) and row[counter_name]<prior:bad(label+': cumulative observed production counter decreased')
        for counter_name in ['ownProductionPaid','ownProductionIncome']:
            total=campaign.get(counter_name)
            if not count(total) or total<=0 or not observations or not isinstance(observations[-1],dict) or observations[-1].get(counter_name)!=total:bad(label+': missing or unequal positive paid-action provenance total '+counter_name)
        tape,_=artifact(campaign.get('journal'),label+' command tape');by_turn={};boundary={};previous=None
        if not isinstance(tape,list) or not tape:bad(label+': empty command tape');tape=[]
        for index,entry in enumerate(tape):
            if not isinstance(entry,dict):bad(label+': malformed command');continue
            command=entry.get('command');payload=command.get('payload') if isinstance(command,dict) else None
            before=entry.get('beforeTurn');after=entry.get('afterTurn')
            if entry.get('sequence')!=index or entry.get('actualCommandCount')!=1 or entry.get('source') not in ui_sources or not entry.get('ui'):bad(label+': command lacks exact native UI provenance')
            if not isinstance(before,int) or isinstance(before,bool) or not isinstance(after,int) or isinstance(after,bool) or not isinstance(payload,dict) or payload.get('ExpectedTurn')!=before:
                bad(label+': command turn is malformed');continue
            if entry.get('accepted') is not True:bad(label+': unresolved rejected command')
            if not all(digest(entry.get(k)) for k in ['beforeSave','afterSave','afterState']):bad(label+': command state hashes missing')
            if previous is not None and entry.get('beforeSave')!=previous:bad(label+': command tape state continuity differs')
            previous=entry.get('afterSave');by_turn.setdefault(before,[]).append(entry)
            if after==before+1:
                if command.get('type')!='OpenAntares.Simulation.Commands.SetReadyCommand' or payload.get('Ready') is not True or after in boundary:bad(label+': non-Ready, duplicate or skipped boundary')
                boundary[after]=entry
            elif after!=before:bad(label+': invalid boundary increment')
        if sorted(boundary)!=list(range(1,84)):bad(label+': incomplete eighty audit plus three final proof boundaries')
        checkpoints=campaign.get('checkpoints')
        if not isinstance(checkpoints,list) or [c.get('turn') for c in checkpoints if isinstance(c,dict)]!=[0,10,25,50,80]:bad(label+': exact five ordered Main checkpoints are missing');continue
        for checkpoint in checkpoints:
            turn=checkpoint['turn'];checkpoint_count+=1;where=label+' checkpoint '+str(turn)
            main,main_bytes=artifact(checkpoint.get('mainSave'),where+' Main save')
            state=main.get('state') if isinstance(main,dict) else None
            if not isinstance(main,dict) or main.get('format')!='openantares-save' or main.get('schema_version')!=13 or main.get('rules_version')!=12 or not isinstance(state,dict):
                bad(where+': actual save has wrong format, writer or rules');state={}
            if state.get('turn')!=turn or state.get('galaxy_seed')!=seed:bad(where+': actual save has wrong turn or seed')
            actors=state.get('empires',[])
            if not isinstance(actors,list) or len(actors)!=rivals+1 or len([e for e in actors if isinstance(e,dict) and e.get('controller')=='human' and e.get('species_id')==key[0]])!=1 or len([e for e in actors if isinstance(e,dict) and e.get('controller')=='ai' and e.get('species_id')==rival_species])!=rivals:
                bad(where+': actual saved species, rival or controller mode differs')
            if not isinstance(state.get('crisis'),dict) or state['crisis'].get('enabled') is not True or not isinstance(state.get('scouting'),dict) or state['scouting'].get('enabled') is not True or not isinstance(state.get('combat'),dict) or state['combat'].get('sentries_enabled') is not True:
                bad(where+': actual saved Standard hazard switches differ')
            if checkpoint.get('canonicalEqual') is not True:bad(where+': canonical round trip not equal')
            ref=checkpoint.get('mainSave',{});retained=ref.get('path')
            if retained in retained_paths:bad(where+': duplicate retained Main copy')
            retained_paths.add(retained)
            expected_save=tape[0].get('beforeSave') if turn==0 and tape else boundary.get(turn,{}).get('afterSave')
            if main_bytes is not None and sha(main_bytes)!=expected_save:bad(where+': Main save differs from the actual boundary tape')
            try:
                actual=Path(checkpoint.get('actualMainPath',''))
                if not actual.is_absolute() or not actual.is_file() or main_bytes is None or actual.read_bytes()!=main_bytes:raise ValueError('Actual Main-written file is missing or differs')
                absolute=str(actual.resolve())
                if absolute in main_paths:raise ValueError('Actual Main save was reused')
                main_paths.add(absolute)
            except (OSError,ValueError,TypeError):bad(where+': actual Main provenance is missing or changed')
            future,_=artifact(checkpoint.get('future'),where+' three futures')
            future_ref=checkpoint.get('future',{})
            if future_ref.get('path') in future_paths:bad(where+': future proof reused')
            future_paths.add(future_ref.get('path'))
            if not isinstance(future,dict) or future.get('mainSaveSha256')!=ref.get('sha256') or future.get('equalFutureBoundaries')!=3 or not isinstance(future.get('hashes'),list) or len(future['hashes'])!=3:
                bad(where+': missing or incomplete equal future proof');continue
            for offset,item in enumerate(future['hashes'],1):
                target=turn+offset;future_count+=1
                if not isinstance(item,dict):bad(where+': malformed future');continue
                expected_boundary=boundary.get(target,{})
                if item.get('turn')!=target or not digest(item.get('save')) or not digest(item.get('state')) or item.get('save')!=item.get('loadedSave') or item.get('state')!=item.get('loadedState') or item.get('save')!=expected_boundary.get('afterSave') or item.get('state')!=expected_boundary.get('afterState'):
                    bad(where+': saved/uninterrupted future differs')
                commands=[{'command':entry['command'],'accepted':entry['accepted']} for entry in by_turn.get(target-1,[])]
                if not commands or item.get('commands')!=commands:bad(where+': future does not replay the complete actual UI command tape')
            replay,_=artifact(checkpoint.get('replay'),where+' authoritative replay')
            if not isinstance(replay,dict) or replay.get('passed') is not True or replay.get('boundaries')!=3 or replay.get('mainSaveSha256')!=ref.get('sha256') or replay.get('futureSha256')!=future_ref.get('sha256') or replay.get('method') not in {'existing-probe','native-extended-core-replay'}:
                bad(where+': authoritative replay is absent or failed')
    if seen!=set(expected) or checkpoint_count!=40 or future_count!=120:bad('Fixed eight-profile/40-checkpoint/120-future coverage is incomplete')
    return errors



REQUIRED_CASES=('Setup','Map','Selection','Colony','Navigation','Guidance','Campaigns')

def verify_suite(summary,root,require_committed=True):
    """Seven distinct native cases from one compiled source epoch; case gates remain mandatory."""
    errors=[]
    if not isinstance(summary,dict):return ['Missing suite summary']
    records=summary.get('records')
    if not isinstance(records,list):return ['Missing suite case records']
    cases=[r.get('case') if isinstance(r,dict) else None for r in records]
    if len(cases)!=len(REQUIRED_CASES) or set(cases)!=set(REQUIRED_CASES):errors.append('Exactly seven distinct required M13 cases are required')
    if summary.get('passed') is not True or summary.get('normalRestored') is not True or summary.get('error') is not None:errors.append('Suite failed or ordinary restoration is missing')
    sources=[];assemblies=[]
    for record in records:
        if not isinstance(record,dict):errors.append('Malformed case record');continue
        sources.append(record.get('source'));assemblies.append(record.get('assembly',{}).get('sha256'))
        if record.get('error') is not None:errors.append(str(record.get('case'))+': native runner error')
        errors.extend(str(record.get('case'))+': '+e for e in verify_run(record,root,require_committed=require_committed))
    if not sources or any(source!=sources[0] for source in sources) or not isinstance(sources[0],dict):errors.append('Suite does not share one exact source epoch')
    if not assemblies or not assemblies[0] or any(value!=assemblies[0] for value in assemblies):errors.append('Suite does not share one opt-in DLL')
    return errors


def main():
    p=argparse.ArgumentParser();p.add_argument('record',nargs='?');p.add_argument('--root',default='.');p.add_argument('--development',action='store_true');p.add_argument('--capture-source');p.add_argument('--assembly');p.add_argument('--suite',action='store_true');args=p.parse_args()
    root=Path(args.root)
    if args.capture_source:
        result=capture_source(root,Path(args.capture_source),Path(args.assembly),args.development);print(json.dumps(result));return 0
    if not args.record:p.error('An execution record or source-capture request is required')
    try:errors=(verify_suite if args.suite else verify_run)(json.loads(Path(args.record).read_text(encoding='utf-8-sig')),root,require_committed=not args.development)
    except (OSError,ValueError,TypeError) as e:errors=[str(e)]
    print(json.dumps({'passed':not errors,'accepted':not errors and not args.development,'scope':'Development' if args.development else ('Complete seven-case M13 automated suite; human and compatibility gates are separate' if args.suite else 'Recorded case only; full milestone coverage is separate'),'errors':errors},indent=2))
    return 1 if errors else 0

if __name__=='__main__':raise SystemExit(main())
