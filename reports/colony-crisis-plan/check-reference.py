"""Reproduce plan arithmetic and check retained source bytes; no game execution."""
import hashlib
import json
from pathlib import Path

MASK = (1 << 64) - 1


class ReferencePcg:
    def __init__(self, seed, sequence):
        self.state = 0
        self.increment = ((sequence << 1) | 1) & MASK
        self.next()
        self.state = (self.state + seed) & MASK
        self.next()

    def next(self):
        old = self.state
        self.state = (old * 6364136223846793005 + self.increment) & MASK
        value = (((old >> 18) ^ old) >> 27) & 0xFFFFFFFF
        rotation = old >> 59
        return ((value >> rotation) | (value << ((-rotation) & 31))) & 0xFFFFFFFF

    def bounded(self, bound):
        threshold = ((1 << 32) - bound) % bound
        while True:
            value = self.next()
            if value >= threshold:
                return value % bound


folder = Path(__file__).parent
reference = json.loads((folder / 'reference-values.json').read_text(encoding='utf-8'))
checks = []
for vector in reference['vectors']:
    seed = vector['seed'] ^ 0x4352495349530001
    sequence = vector['empire'] ^ 0x504C414755450001
    rng = ReferencePcg(seed, sequence)
    assert rng.state == vector['initial']['state']
    assert rng.increment == vector['initial']['increment']
    assert [rng.next() for _ in range(4)] == vector['firstFourUInt32']
    rng = ReferencePcg(seed, sequence)
    roll = rng.bounded(100)
    target = rng.bounded(3) if roll < 25 else None
    assert (roll, target, rng.state) == (
        vector['probabilityRoll'], vector['targetOfThree'], vector['afterCheckState'])
    checks.append({'seed': vector['seed'], 'empire': vector['empire'], 'passed': True})

source = Path(reference['sourceRules5Save'])
assert hashlib.sha256(source.read_bytes()).hexdigest() == reference['sourceSaveSha256']
game = json.loads(source.read_text(encoding='utf-8-sig'))
assert (game['schema_version'], game['rules_version'], game['state']['turn'],
        len(game['state']['colonies'])) == (6, 5, 24, 6)
assert sum(e.get('ai_policy_id') == 'expansion_v1' for e in game['state']['empires']) == 1
result = {'method': 'Independent reference arithmetic/source inspection; not C# or game execution.',
          'vectorCases': checks, 'sourceBytesAndMetadataMatch': True,
          'genuineFutureCaptureExecuted': False, 'implementationStarted': False}
(folder / 'reference-checks.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print('3/3 independent vector cases and genuine-source bytes/metadata match; no game executed.')
