# ADR-028: One additional M13 report-layout repair

Date:2026-11-07. Status:Accepted for bounded scope.

Owner answer to the explicit five-to-six amendment question: **“Extend to six fixes (recommended)”**. This authorizes one sixth unique post-audit product repair, M13-POL-06: the reproduced Wenth/Solo turn7 required-report rendering crash, its regression and acceptance reruns. The original reviewed spec/ten-task-plan bodies remain unchanged; their five-fix wording is superseded solely for this issue by the cap of six.

Constrain grouped report/nested wrapped text to available report width, preserve every material explanation/action and saved acknowledgement gate, reproduce and repair with native evidence at all four sizes, then rerun affected/full required gates. Keep simulation/content/schema/balance, engine version, theme/ANGLE/60FPS/VSync unchanged. No seventh product fix, commit/merge/push/release/cleanup or human acceptance is authorized by this answer. Commit authority remains separate under the approved plan.

See reports/M13_SCOPE_AMENDMENT_2026-11-07.md and reports/playability-execution/task09-triage/blocking-sixth-finding.json. Prior failed runs/logs/saves/source/DLL and all frozen evidence remain preserved.

## Controlled diagnosis update

The same approved POL06 crash was isolated to the console: hiding only EmpireConsole after accepted turn7 lets the still-visible required report render quietly. Constraining report text corrected provisional heights but did not stop the loop, so all those candidate changes were reverted to exact pre-sixth canonical hashes. Captured console content is1001px in a998px viewport and automatic scrollbar appearance changes wrapping width. The repair under test is solely ConsoleScroll.VerticalScrollMode=Reserve; installed Godot4.7.2 documentation says this keeps content width stable while showing the scrollbar only when necessary. No report content or UI is hidden by the proposed product fix. This refines implementation of the same authorized crash repair, not a seventh issue or scope expansion.
