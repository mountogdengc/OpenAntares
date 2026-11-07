"""Synthetic negative controls; never a substitute for native M13 evidence."""
import copy,hashlib,importlib.util,json,tempfile,unittest
from pathlib import Path

HERE=Path(__file__).resolve()
ROOT=HERE.parents[2]
spec=importlib.util.spec_from_file_location('playability_evidence',ROOT/'tools/ConflictValidation/Verify-PlayabilityEvidence.py')
verifier=importlib.util.module_from_spec(spec);spec.loader.exec_module(verifier)

class EvidenceControls(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix='m13-synthetic-');self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name);self.expected={'game/Fixture.cs'}
        (self.root/'game').mkdir();(self.root/'game/Fixture.cs').write_text('original authoritative fixture')
        def artifact(name,data):
            p=self.root/name
            p.parent.mkdir(parents=True,exist_ok=True)
            p.write_bytes(data if isinstance(data,bytes) else json.dumps(data).encode())
            return {'path':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
        self.artifact=artifact
        self.record={'case':'Setup','exitCode':0,'assemblyStable':True,'normalRestored':True,
            'source':{'commit':'0'*40,'committed':True,'files':[{'path':'game/Fixture.cs','sha256':hashlib.sha256((self.root/'game/Fixture.cs').read_bytes()).hexdigest()}]},
            'assembly':artifact('evidence/validation-assembly.dll',b'synthetic PE fixture'),
            'result':artifact('evidence/results.json',{'case':'Setup','passed':True,'error':None,'checks':[{'passed':True,'name':'synthetic evidence contract'}]}),
            'normalMetadata':artifact('evidence/normal.json',{'presentScenarioTypes':[],'presentHelpers':[],'validationNamespaceTypes':[]}),
            'optInMetadata':artifact('evidence/opt-in.json',{'presentScenarioTypes':['PlayabilityGraphicalValidation'],'validationNamespaceTypes':['PlayabilityGraphicalValidation','PlayabilityCampaignChecks']}),
            'engineLog':artifact('evidence/godot.log',b'Godot synthetic fixture\n'),
            'consoleLog':artifact('evidence/console.log',b'Fixture, not gameplay evidence\n')}
    def verify(self):return verifier.verify_run(self.record,self.root,expected_paths=self.expected,require_committed=False)
    def test_valid_complete_contract(self):self.assertEqual([],self.verify())
    def test_missing_result_rejected(self):del self.record['result'];self.assertTrue(self.verify())
    def test_missing_source_rejected(self):del self.record['source'];self.assertTrue(self.verify())
    def test_subset_inventory_rejected(self):self.record['source']['files']=[];self.assertTrue(self.verify())
    def test_extra_inventory_rejected(self):self.record['source']['files'].append({'path':'game/Invented.cs','sha256':'0'*64});self.assertTrue(self.verify())
    def test_duplicate_inventory_rejected(self):self.record['source']['files']*=2;self.assertTrue(self.verify())
    def test_changed_source_rejected(self):(self.root/'game/Fixture.cs').write_text('changed');self.assertTrue(self.verify())
    def test_changed_assembly_rejected(self):(self.root/self.record['assembly']['path']).write_bytes(b'changed');self.assertTrue(self.verify())
    def test_zero_checks_rejected(self):self.record['result']=self.artifact('evidence/results.json',{'case':'Setup','passed':True,'checks':[]});self.assertTrue(self.verify())
    def test_false_check_rejected(self):self.record['result']=self.artifact('evidence/results.json',{'case':'Setup','passed':True,'checks':[{'passed':False}]});self.assertTrue(self.verify())
    def test_wrong_case_rejected(self):self.record['result']=self.artifact('evidence/results.json',{'case':'Map','passed':True,'checks':[{'passed':True}]});self.assertTrue(self.verify())
    def test_engine_diagnostic_rejected(self):self.record['engineLog']=self.artifact('evidence/godot.log',b' WARNING: actual diagnostic\n');self.assertTrue(self.verify())
    def test_deleted_callback_diagnostic_rejected(self):
        self.record['consoleLog']=self.artifact('evidence/console.log',b'Object was deleted while awaiting a callback.\n');self.assertTrue(self.verify())
    def test_native_crash_diagnostic_rejected(self):
        self.record['engineLog']=self.artifact('evidence/godot.log',b'CrashHandlerException: Program crashed with signal 11\n');self.assertTrue(self.verify())
    def test_missing_log_rejected(self):del self.record['consoleLog'];self.assertTrue(self.verify())
    def test_failed_restore_rejected(self):self.record['normalRestored']=False;self.assertTrue(self.verify())
    def test_namespace_leak_rejected(self):self.record['normalMetadata']=self.artifact('evidence/normal.json',{'presentScenarioTypes':[],'presentHelpers':[],'validationNamespaceTypes':['UnlistedHarnessHelper']});self.assertTrue(self.verify())
    def test_wrong_opt_in_rejected(self):self.record['optInMetadata']=self.artifact('evidence/opt-in.json',{'presentScenarioTypes':['GraphicalValidation']});self.assertTrue(self.verify())
    def test_failed_exit_rejected(self):self.record['exitCode']=1;self.assertTrue(self.verify())
    def test_unstable_assembly_rejected(self):self.record['assemblyStable']=False;self.assertTrue(self.verify())
    def test_path_escape_rejected(self):self.record['result']={'path':'../outside.json','sha256':'0'*64};self.assertTrue(self.verify())
    def test_development_source_never_final_acceptance(self):
        self.record['source']['committed']=False
        self.assertTrue(verifier.verify_run(self.record,self.root,expected_paths=self.expected,require_committed=True))

if __name__=='__main__':unittest.main()
