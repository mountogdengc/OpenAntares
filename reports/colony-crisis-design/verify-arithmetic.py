"""Independent design arithmetic only; does not import or execute the game."""
import json
from pathlib import Path

checks = []


def check(name, actual, expected):
    assert actual == expected, (name, actual, expected)
    checks.append({'name': name, 'actual': actual, 'expected': expected, 'passed': True})


for quarantined, expected in [(False, [20, 40, 60, 80, 100, 100]),
                               (True, [20, 25, 30, 35, 40, 45])]:
    severity = 20
    history = []
    casualties = 0
    for interval in range(1, 7):
        history.append(severity)
        if interval == 6:  # Natural recovery precedes this boundary's mortality.
            break
        casualties += int(severity >= 80)
        severity = min(100, severity + (5 if quarantined else 20))
    check('quarantine' if quarantined else 'open severity', history, expected)
    check('quarantine casualties' if quarantined else 'open casualties', casualties,
          0 if quarantined else 2)

check('grace decrement', [2 - i for i in range(3)], [2, 1, 0])
check('source N6 cure cost', (6 + 6) * 100, 1200)
check('compounded production', ((900 * 8000 // 10000) * 7500 // 10000) * 9000 // 10000, 486)
check('compounded research', ((700 * 8000 // 10000) * 7500 // 10000) * 9000 // 10000, 378)
wanted = 1000 * 50 // 100
diverted = min(wanted, 1200 - 900)
check('wanted/diverted/normal research', [wanted, diverted, 1000 - diverted], [500, 300, 700])
check('purge N5 survivors', 5 - (5 + 1) // 2, 2)
check('purge reserve901 kept', 901 // 2, 450)
check('purge N1 survivors', 1 - (1 + 1) // 2, 0)
check('civic five snapshots', list(range(10, 10 + 5)), [10, 11, 12, 13, 14])
check('civic refresh at12', [min(3, 1 + 1), 12 + 5], [2, 17])
check('civic cap', min(3, 3 + 1) * 1000, 3000)
check('cooldown schedule after26', [t for t in [40, 60, 80] if t >= 26 + 30][0], 60)
check('immunity boundary', [t < 26 + 12 for t in [26, 37, 38]], [True, True, False])

result = {'method': 'Independent proposed-rule arithmetic; no application code or gameplay executed.',
          'passed': True, 'count': len(checks), 'checks': checks}
Path(__file__).with_name('arithmetic-results.json').write_text(
    json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(f'{len(checks)}/{len(checks)} independent arithmetic checks pass; not engine tests.')
