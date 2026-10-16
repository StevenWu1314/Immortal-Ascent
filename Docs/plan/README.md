# Implementation plans

| Plan | Scope | Status |
| --- | --- | --- |
| [M1](M1/README.md) | Plains vertical slice: run structure, turn economy, awareness/LOS, Root-only statuses, materials and loot, portable crafting, minimap + HUD, plains content, simplified boss, desktop builds | not started |

## How to use these plans

- **For the executing agent:** read the plan's `README.md` first — it holds the working agreement (test
  policy, scene-wiring rules, git rules, verification ladder) and the task order. Then open exactly one task
  file, implement it, run its tests, and stop at its checkpoint. Do not start a second task in the same
  checkpoint cycle.
- **For the designer:** every task ends at a checkpoint with a short report. Reply `pass` to authorise the
  commit and the next task, or list corrections.
- Behaviour is specified in [Immortal-Ascent-GDD-v2.md](../Immortal-Ascent-GDD-v2.md), which is generated
  from the same source as the `.docx` (`Docs/gdd_build/build.py`).
