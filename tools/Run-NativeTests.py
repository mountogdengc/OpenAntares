"""Foreground Windows checks, invoked only by the shared queue's exec command."""
from pathlib import Path
import argparse,datetime as dt,hashlib,json,re,shutil,subprocess,sys,uuid,xml.etree.ElementTree as ET

def sha(data): return hashlib.sha256(data).hexdigest()
def write(path,value): path.write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8")
def capture(root,config):
    paths=set()
    for pattern in config["include"]:
        if Path(pattern).is_absolute() or ".." in Path(pattern).parts: raise ValueError("Unsafe source pattern")
        matches=[p for p in root.glob(pattern) if p.is_file() and not any(s.lower() in {".git",".godot","bin","obj",".aws"} for s in p.relative_to(root).parts)]
        if not any(c in pattern for c in "*?[") and not matches: raise ValueError("Missing required source: "+pattern)
        for p in matches:
            if not p.resolve().is_relative_to(root): raise ValueError("Source escapes checkout")
            paths.add(p)
    if not paths: raise ValueError("No sources captured")
    if sum(p.stat().st_size for p in paths)>config["max_source_mb"]*1024*1024: raise ValueError("Source limit exceeded")
    return {p.relative_to(root).as_posix():sha(p.read_bytes()) for p in sorted(paths)}

def main():
    parser=argparse.ArgumentParser();parser.add_argument("--source-root",required=True);parser.add_argument("--config",required=True);parser.add_argument("--suite",default="default");args=parser.parse_args()
    root=Path(args.source_root).resolve(strict=True);config=json.loads(Path(args.config).read_text(encoding="utf-8-sig"))
    if config.get("execution")!="native": raise ValueError("Native configuration required")
    suite=config["suites"][args.suite];started=dt.datetime.now(dt.timezone.utc)
    out=root/"reports/native-tests"/(started.strftime("%Y%m%d-%H%M%S")+"-"+uuid.uuid4().hex[:8]);out.mkdir(parents=True)
    result={"passed":False,"execution":"native","suite":args.suite,"sourceRoot":str(root),"output":str(out)}
    pointers={};existing=set();folder=root/"reports/scout-execution"
    if folder.exists(): existing={p.name for p in folder.iterdir() if p.is_file()}
    for kind in ("generated","parity"):
        p=folder/("task14-"+kind+"-latest.json")
        if p.is_file(): pointers[p]=p.read_bytes()
    exit_code=1
    try:
        before=capture(root,config);write(out/"source-before.json",{"sourceRoot":str(root),"files":before})
        command=[str(out) if arg=="{output}" else arg for arg in suite["command"]]
        command[0]=shutil.which(command[0]) or command[0];write(out/"request.json",{"sourceRoot":str(root),"configPath":str(Path(args.config).resolve()),"suite":args.suite,"command":command,"execution":"native","head":subprocess.check_output(["git","-C",str(root),"rev-parse","HEAD"]).decode().strip()})
        with (out/"command.log").open("wb") as log: completed=subprocess.run(command,cwd=root,stdout=log,stderr=subprocess.STDOUT)
        result["commandExitCode"]=completed.returncode
        after=capture(root,config);write(out/"source-after.json",{"sourceRoot":str(root),"files":after});result["sourceFiles"]=len(before)
        if before!=after: raise RuntimeError("Sources changed during the run; evidence does not describe final bytes")
        if completed.returncode: raise RuntimeError("Native command failed; see command.log")
        exit_code=0
        if suite["kind"]=="trx":
            report=out/suite["report"]
            if not report.is_file(): raise RuntimeError("Missing TRX evidence")
            counters=ET.parse(report).find(".//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters")
            if counters is None or int(counters.get("executed","0"))==0: raise RuntimeError("Empty TRX evidence")
            result["counters"]=dict(counters.attrib)
            if any(int(counters.get(k,"0")) for k in ("failed","error","timeout","aborted")): raise RuntimeError("TRX records unsuccessful tests")
            if int(counters.get("notExecuted","0")): exit_code=5;result["coverage"]="Passed command with skipped tests; not a complete pass"
        result["passed"]=exit_code==0
    except Exception as error:
        result["error"]=str(error);exit_code=1;result["passed"]=False
    finally:
        restored=[]
        try:
            for pointer,original in pointers.items():
                current=pointer.read_bytes()
                if current==original: continue
                item=json.loads(current);name=item["artifact"]
                match=re.fullmatch(r"task14-(?:generated|parity)-(\d{8}-\d{6}-\d{7})\.json",name)
                artifact=folder/name
                if not match or name in existing or artifact.resolve().parent!=folder.resolve() or sha(artifact.read_bytes())!=item["sha256"]: raise RuntimeError("Evidence pointer changed outside this test run: "+str(pointer))
                artifact_time=dt.datetime.strptime(match.group(1)[:22],"%Y%m%d-%H%M%S-%f").replace(tzinfo=dt.timezone.utc)
                if artifact_time<started-dt.timedelta(seconds=1): raise RuntimeError("Evidence artifact predates this test run")
                retained=out/"scout-evidence";retained.mkdir(exist_ok=True)
                (retained/name).write_bytes(artifact.read_bytes());(retained/pointer.name).write_bytes(current)
                if pointer.read_bytes()!=current: raise RuntimeError("Evidence pointer changed concurrently")
                pointer.write_bytes(original);restored.append({"pointer":str(pointer),"restoredSha256":sha(original),"newArtifactRetained":str(artifact)})
            result["restoredEvidencePointers"]=restored
        except Exception as error: result["preservationError"]=str(error);result["passed"]=False;exit_code=1
        result["exitCode"]=exit_code;write(out/"result.json",result)
        log=out/"command.log"
        if log.exists(): print("\n".join(log.read_text(encoding="utf-8-sig",errors="replace").splitlines()[-10:]))
        print(json.dumps(result,indent=2))
    return exit_code
if __name__=="__main__": sys.exit(main())
