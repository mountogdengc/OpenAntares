import importlib.util,json,tempfile,hashlib,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('visual_evidence',Path(__file__).parents[2]/'tools/ConflictValidation/Verify-VisualOverhaulEvidence.py');v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
class EvidenceBoundary(unittest.TestCase):
 def setUp(self):
  d=tempfile.TemporaryDirectory();self.addCleanup(d.cleanup);self.root=Path(d.name)
 def test_missing_source_rejected(self):self.assertTrue(any('source' in e.lower() for e in v.verify_case({},self.root,True,set())))
 def test_missing_result_rejected(self):self.assertTrue(any('result' in e.lower() for e in v.verify_case({},self.root,True,set())))
 def test_missing_restoration_rejected(self):self.assertTrue(any('restor' in e.lower() for e in v.verify_case({},self.root,True,set())))
 def test_partial_frame_matrix_rejected(self):
  p=self.root/'results.json';p.write_text(json.dumps({'case':'Theme','passed':True,'checks':[{'passed':True}],'frames':[]}),encoding='utf-8');record={'case':'Theme','result':{'path':'results.json','sha256':hashlib.sha256(p.read_bytes()).hexdigest()}};self.assertTrue(any('frame' in e.lower() for e in v.verify_case(record,self.root,True,set())))
if __name__=='__main__':unittest.main()
