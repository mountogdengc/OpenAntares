"""Synthetic suite coverage controls; individual artifact contracts have separate tests."""
import copy,importlib.util,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('m13',Path(__file__).resolve().parents[2]/'tools/ConflictValidation/Verify-PlayabilityEvidence.py')
v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
class SuiteCoverage(unittest.TestCase):
    def setUp(self):
        self.summary={'passed':True,'normalRestored':True,'error':None,'records':[{'case':case,'error':None,'source':{'files':['same fixture']},'assembly':{'sha256':'a'*64}} for case in ['Setup','Map','Selection','Colony','Navigation','Guidance','Campaigns']]}
    def verify(self):
        original=v.verify_run;calls=[]
        def individual(*args,**kwargs):calls.append((args,kwargs));return []
        v.verify_run=individual
        try:
            errors=v.verify_suite(self.summary,Path('.'),require_committed=False)
            self.assertEqual(len(self.summary['records']),len(calls));return errors
        finally:v.verify_run=original
    def test_complete_seven_case_contract(self):self.assertEqual([],self.verify())
    def test_subset(self):self.summary['records'].pop();self.assertTrue(self.verify())
    def test_duplicate(self):self.summary['records'][-1]=copy.deepcopy(self.summary['records'][0]);self.assertTrue(self.verify())
    def test_extra(self):self.summary['records'].append(copy.deepcopy(self.summary['records'][0]));self.assertTrue(self.verify())
    def test_source_epoch(self):self.summary['records'][-1]['source']={'files':['changed']};self.assertTrue(self.verify())
    def test_dll_epoch(self):self.summary['records'][-1]['assembly']['sha256']='b'*64;self.assertTrue(self.verify())
    def test_runner_failure(self):self.summary['records'][0]['error']='failed';self.assertTrue(self.verify())
    def test_restoration(self):self.summary['normalRestored']=False;self.assertTrue(self.verify())
    def test_case_verifier_still_mandatory(self):
        original=v.verify_run;calls=[]
        def individual(*args,**kwargs):calls.append((args,kwargs));return ['failed artifact']
        v.verify_run=individual
        try:
            self.assertTrue(v.verify_suite(self.summary,Path('.'),require_committed=False));self.assertEqual(7,len(calls))
        finally:v.verify_run=original
if __name__=='__main__':unittest.main()
