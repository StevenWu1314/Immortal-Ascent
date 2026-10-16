# T11 — M1 integration, full-run playtest, desktop builds

**Depends on:** every other M1 task, in particular T09 and T10.
**GDD:** section 17 (M1 definition), 16.4 (performance and platform), Appendix B (gap tracking).

## Goal

Prove M1 as a whole: one complete plains run, no console errors, all tests green, and macOS + Windows
builds that launch.

## Work

1. **Full-run pass.** Play start → plains floor 1 → floor 2 → Tusk Sovereign → victory; then a second run
   ending in death. Fix whatever breaks. Every fix gets a test if it is a rule, or an explicit note if it is
   feel.
2. **Console hygiene.** A complete run must produce **zero errors and zero exceptions**. Warnings are
   acceptable only if each one is listed in the checkpoint with a reason. Generation logging
   (`IslandManager` island/passage counts, reachability results) is currently noisy every floor — either
   silence it behind a debug flag or list it as accepted noise, but decide explicitly.
3. **Existing diagnostics must all still pass** — this is the regression gate:
   `MapgenDiagnostics.ChestWalkability`, `OpenRealChest`, `ConsumableBuff`, `TurnPipeline`,
   `RoomBuilderStress`, `PlayDriver.Status`, `StepTowardNearestEnemy`, `OpenAdjacentChest`,
   `DumpEntityPositions`, `Audit Bridge Tiles`, `Audit Enemy Rooms`.
4. **Performance.** 60 fps at 1080p on the designer's machine during a boss fight (worst case: many
   entities, lighting, minimap). If it misses, profile before optimising and report the actual cost centre;
   the GDD flags per-tile fog/lighting work as the likely risk.
5. **Builds.** Run `BuildMenu.SwitchToDesktop`, then `BuildMac` and `BuildWindows`. Launch each build and
   confirm: menu loads, a run starts, input works, and quitting returns cleanly. Note honestly that these
   are unsigned local builds — code signing and notarisation are out of scope for M1.
6. **Update the docs.**
   - Tick the status board in `Docs/plan/M1/README.md`.
   - Update `Docs/Immortal-Ascent-GDD-v2.md` (+ regenerate the `.docx` via `Docs/gdd_build/build.py`) where
     M1 deliberately diverged: single-tile boss, Root-only statuses, no blueprints, no save.
   - Correct two GDD appendix B entries that are already wrong today: **B19** (it claims six correctness
     bugs are unfixed; four were fixed before M1 started — only the mapgen range/overlap bug and combat
     inside `EntityManager.MoveEntity` remained, and both are closed by T01/T03) and **B23** (it claims
     macOS has never shipped; `Library/PlayerDataCache/OSXUniversal` shows a macOS build exists, so the
     wording should be "verify Apple Silicon behaviour").
7. **Write the M1 report** at `Docs/plan/M1/M1-REPORT.md`: what shipped, what was deferred to M2 and why,
   test counts, the M1 acceptance checklist below, and any number the designer should revisit.

## M1 acceptance checklist

- [ ] A full plains run is playable start to finish in one sitting.
- [ ] Out of combat, movement is free; in combat, exactly one action per turn.
- [ ] Bumping a wall never costs a turn.
- [ ] Aim mode is free to enter; only firing costs a turn and one arrow.
- [ ] No enemy ever shoots through a wall.
- [ ] Unaware enemies roam; aware enemies commit.
- [ ] Walking into a web roots the player for 2 rounds, then the web is gone.
- [ ] Materials are obtainable from veins and corpses; arrows are no longer capped at 10.
- [ ] No chest or enemy ever drops a weapon or armour; crafting is the only source.
- [ ] Crafting works anywhere out of combat and never during a fight.
- [ ] Minimap reveals as you explore, shows discovered stairs, and never shows enemies.
- [ ] The Tusk Sovereign takes exactly one action per round and its charge is dodgeable.
- [ ] Death returns to the menu; killing the boss ends the run with a placeholder summary.
- [ ] EditMode and PlayMode test suites are green.
- [ ] macOS and Windows builds launch and play.
- [ ] Console is free of errors and exceptions across a full run.

## Verification

1. All tests green (report counts).
2. The full diagnostics list above, run through the bridge, all passing.
3. Designer plays a complete run and signs off — this is the only acceptance test that matters for M1.
4. Both builds launched by the designer.

## Checkpoint

This is the M1 delivery checkpoint. Report the acceptance checklist with each line marked pass/fail/waived,
the honest list of what is still rough, and a proposed M2 scope based on what M1 revealed. Do not commit
until the designer accepts the checklist.

## Out of scope

M2 content: desert and tundra, the other four statuses, resistances, rune on-hit effects and slot scaling,
blueprints and the journal, multi-tile bosses. M3: save/resume, localization, the full ending, balance
across all three realms.
