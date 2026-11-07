"""Synthetic fixed-matrix controls. These files are never gameplay evidence."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('m13_evidence',ROOT/'tools/ConflictValidation/Verify-PlayabilityEvidence.py')
verifier=importlib.util.module_from_spec(spec);spec.loader.exec_module(verifier)

class CampaignEvidenceControls(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix='m13-campaign-synthetic-');self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name);self.source='a'*64
        self.matrix={'format':'OpenAntaresM13CampaignMatrix','version':1,'passed':True,'sourceSha256':self.source,'campaigns':[]}
        def artifact(name,data):
            p=self.root/name;p.parent.mkdir(parents=True,exist_ok=True)
            p.write_bytes(data if isinstance(data,bytes) else json.dumps(data,separators=(',',':')).encode())
            return {'path':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
        self.artifact=artifact
        for species,seed in [('harl',42),('peldra',45),('sulan',43),('wenth',42)]:
            for mode in ['Standard','Solo']:
                key=species+'-'+mode
                actors=[{'id':1,'controller':'human','species_id':species}]+([{'id':2,'controller':'ai','species_id':'harl' if species=='peldra' else 'peldra'}] if mode=='Standard' else [])
                saves={t:json.dumps({'syntheticFixture':True,'profile':key,'format':'openantares-save','schema_version':13,'rules_version':12,'state':{'turn':t,'galaxy_seed':seed,'empires':actors,'crisis':{'enabled':True},'scouting':{'enabled':True},'combat':{'sentries_enabled':True}}},separators=(',',':')).encode() for t in range(84)}
                sha=lambda b:hashlib.sha256(b).hexdigest()
                tape=[{'sequence':t,'ui':'EndTurn','source':'EndTurn.Pressed','command':{'type':'OpenAntares.Simulation.Commands.SetReadyCommand','payload':{'Issuer':{'Value':1},'ExpectedTurn':t,'Ready':True}},'accepted':True,'changed':True,'beforeTurn':t,'afterTurn':t+1,'beforeSave':sha(saves[t]),'afterSave':sha(saves[t+1]),'afterState':sha(b'state'+saves[t+1]),'actualCommandCount':1} for t in range(83)]
                journal=self.artifact('evidence/'+key+'/journal.json',tape)
                checkpoints=[]
                for turn in [0,10,25,50,80]:
                    retained=self.artifact(f'evidence/{key}/main-{turn}.json',saves[turn])
                    actual=self.root/'actual-synthetic-saves'/f'{key}-{turn}.json';actual.parent.mkdir(exist_ok=True);actual.write_bytes(saves[turn])
                    proof={'mainSaveSha256':retained['sha256'],'equalFutureBoundaries':3,'hashes':[{'turn':t,'save':sha(saves[t]),'loadedSave':sha(saves[t]),'state':sha(b'state'+saves[t]),'loadedState':sha(b'state'+saves[t]),'commands':[{'command':tape[t-1]['command'],'accepted':True}]} for t in range(turn+1,turn+4)]}
                    future=self.artifact(f'evidence/{key}/future-{turn}.json',proof)
                    replay=self.artifact(f'evidence/{key}/replay-{turn}.json',{'passed':True,'boundaries':3,'mainSaveSha256':retained['sha256'],'futureSha256':future['sha256'],'method':'existing-probe'})
                    checkpoints.append({'turn':turn,'mainSave':retained,'actualMainPath':str(actual),'canonicalEqual':True,'future':future,'replay':replay})
                observations=self.artifact('evidence/'+key+'/observations.json',[{'turn':t,'ownColonies':1,'ownedVessels':1,'stationary':1,'travelling':0,'shortages':[],'noProject':1,'researchTarget':None,'crisisCases':0,'scoutingResponses':0,'conflictBattles':0,'knownWars':0,'ownProductionPaid':t,'ownProductionIncome':100*t} for t in range(1,84)])
                self.matrix['campaigns'].append({'species':species,'seed':seed,'mode':mode,'galaxySize':'small','rivalCount':1 if mode=='Standard' else 0,'rivalSpeciesId':('harl' if species=='peldra' else 'peldra') if mode=='Standard' else None,'rules':12,'writer':13,'crisesEnabled':True,'amebasEnabled':True,'sentriesEnabled':True,'auditBoundaries':80,'finalTurn':83,'initialMainSaveExactGenerated':True,'injectedAssets':False,'sourceSha256':self.source,'decisionSource':'RequesterViewsAndPermittedPlanningQueries','journal':journal,'checkpoints':checkpoints,'observations':observations,'ownProductionPaid':83,'ownProductionIncome':8300})
    def verify(self):return verifier.verify_campaign_matrix(self.matrix,self.root,self.source)
    def changed_future(self,change):
        checkpoint=self.matrix['campaigns'][0]['checkpoints'][0]
        p=self.root/checkpoint['future']['path'];data=json.loads(p.read_text());change(data)
        checkpoint['future']=self.artifact(checkpoint['future']['path'],data)
        replay=json.loads((self.root/checkpoint['replay']['path']).read_text());replay['futureSha256']=checkpoint['future']['sha256'];checkpoint['replay']=self.artifact(checkpoint['replay']['path'],replay)
    def changed_observations(self,change):
        campaign=self.matrix['campaigns'][0];ref=campaign['observations'];data=json.loads((self.root/ref['path']).read_text());change(data)
        campaign['observations']=self.artifact(ref['path'],data)
    def test_missing_observations_fails(self):del self.matrix['campaigns'][0]['observations'];self.assertTrue(self.verify())
    def test_changed_observations_fails(self):(self.root/self.matrix['campaigns'][0]['observations']['path']).write_text('changed');self.assertTrue(self.verify())
    def test_short_observations_fails(self):self.changed_observations(lambda rows:rows.pop());self.assertTrue(self.verify())
    def test_wrong_observed_turn_fails(self):self.changed_observations(lambda rows:rows[0].update(turn=2));self.assertTrue(self.verify())
    def test_missing_observed_field_fails(self):self.changed_observations(lambda rows:rows[0].pop('shortages'));self.assertTrue(self.verify())
    def test_malformed_observed_shortages_fails(self):self.changed_observations(lambda rows:rows[0].update(shortages='none'));self.assertTrue(self.verify())
    def test_missing_paid_total_fails(self):del self.matrix['campaigns'][0]['ownProductionPaid'];self.assertTrue(self.verify())
    def test_zero_paid_total_fails(self):self.matrix['campaigns'][0]['ownProductionPaid']=0;self.assertTrue(self.verify())
    def test_paid_total_mismatch_fails(self):self.matrix['campaigns'][0]['ownProductionPaid']=84;self.assertTrue(self.verify())
    def test_decreasing_paid_observation_fails(self):self.changed_observations(lambda rows:rows[3].update(ownProductionPaid=0));self.assertTrue(self.verify())
    def test_complete_synthetic_contract(self):self.assertEqual([],self.verify())
    def test_missing_campaign_fails(self):self.matrix['campaigns'].pop();self.assertTrue(self.verify())
    def test_duplicate_campaign_fails(self):self.matrix['campaigns'][-1]=copy.deepcopy(self.matrix['campaigns'][0]);self.assertTrue(self.verify())
    def test_extra_campaign_fails(self):self.matrix['campaigns'].append(copy.deepcopy(self.matrix['campaigns'][0]));self.assertTrue(self.verify())
    def test_wrong_seed_fails(self):self.matrix['campaigns'][0]['seed']=43;self.assertTrue(self.verify())
    def test_wrong_solo_rival_fails(self):self.matrix['campaigns'][1]['rivalCount']=1;self.assertTrue(self.verify())
    def test_wrong_standard_rival_fails(self):self.matrix['campaigns'][2]['rivalSpeciesId']='wenth';self.assertTrue(self.verify())
    def test_missing_checkpoint_fails(self):self.matrix['campaigns'][0]['checkpoints'].pop();self.assertTrue(self.verify())
    def test_wrong_checkpoint_fails(self):self.matrix['campaigns'][0]['checkpoints'][2]['turn']=24;self.assertTrue(self.verify())
    def test_wrong_hazard_fails(self):self.matrix['campaigns'][0]['sentriesEnabled']=False;self.assertTrue(self.verify())
    def test_injected_assets_fail(self):self.matrix['campaigns'][0]['injectedAssets']=True;self.assertTrue(self.verify())
    def test_inexact_main_start_fails(self):self.matrix['campaigns'][0]['initialMainSaveExactGenerated']=False;self.assertTrue(self.verify())
    def test_changed_source_fails(self):self.matrix['campaigns'][0]['sourceSha256']='b'*64;self.assertTrue(self.verify())
    def test_changed_matrix_source_fails(self):self.matrix['sourceSha256']='b'*64;self.assertTrue(self.verify())
    def test_missing_future_fails(self):del self.matrix['campaigns'][0]['checkpoints'][0]['future'];self.assertTrue(self.verify())
    def test_short_future_fails(self):self.changed_future(lambda d:d['hashes'].pop());self.assertTrue(self.verify())
    def test_unequal_future_fails(self):self.changed_future(lambda d:d['hashes'][0].update(loadedSave='b'*64));self.assertTrue(self.verify())
    def test_empty_future_command_journal_fails(self):self.changed_future(lambda d:d['hashes'][0].update(commands=[]));self.assertTrue(self.verify())
    def test_missing_replay_fails(self):del self.matrix['campaigns'][0]['checkpoints'][0]['replay'];self.assertTrue(self.verify())
    def test_failed_replay_fails(self):
        c=self.matrix['campaigns'][0]['checkpoints'][0];d=json.loads((self.root/c['replay']['path']).read_text());d['passed']=False;c['replay']=self.artifact(c['replay']['path'],d);self.assertTrue(self.verify())
    def test_changed_main_copy_fails(self):c=self.matrix['campaigns'][0]['checkpoints'][0];(self.root/c['mainSave']['path']).write_text('changed');self.assertTrue(self.verify())
    def test_changed_actual_main_fails(self):Path(self.matrix['campaigns'][0]['checkpoints'][0]['actualMainPath']).write_text('changed');self.assertTrue(self.verify())
    def test_missing_boundary_journal_fails(self):
        c=self.matrix['campaigns'][0];d=json.loads((self.root/c['journal']['path']).read_text());d.pop(40);c['journal']=self.artifact(c['journal']['path'],d);self.assertTrue(self.verify())
    def test_invented_boundary_total_fails(self):self.matrix['campaigns'][0]['auditBoundaries']=79;self.assertTrue(self.verify())
    def test_non_ui_command_fails(self):
        c=self.matrix['campaigns'][0];d=json.loads((self.root/c['journal']['path']).read_text());d[0]['actualCommandCount']=0;c['journal']=self.artifact(c['journal']['path'],d);self.assertTrue(self.verify())
    def test_forged_decision_source_fails(self):self.matrix['campaigns'][0]['decisionSource']='hidden foreign state';self.assertTrue(self.verify())

if __name__=='__main__':unittest.main()
