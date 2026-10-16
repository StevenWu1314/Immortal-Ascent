# Unity Game Dev — dsh agent preset

A DeepSeek Harness agent preset tuned for coding *this* game. It pairs a gameplay
engineer persona with a project skill that records the codebase's real contracts,
conventions, and verification commands.

## Using it

In the Harness Web GUI, create a session and pick **Unity Game Dev** as the agent
preset. Then just ask for the change you want.

The preset carries:

- **Persona** — Unity 2022.3.62f2 context, plus the project's hard rules: don't
  hand-edit `.unity`/`.prefab`/`.asset`/`.meta` files, don't rename serialized
  fields, read the shared contracts before changing them, verify before claiming
  success.
- **Skill `unity-rogue-asian-fantasy`** — the grid and turn contracts, terrain
  codes, singleton and static-event conventions, which directories are vendored
  third-party code, and the exact Unity batchmode commands that compile-check and
  exercise the generation pipeline.
- **Tools** — filesystem, search, shell, background jobs, skills, subagents,
  workflows, todos, web, plus `tool-cordis` and `tool-plugin-manager` so the agent
  can inspect and adjust the harness from inside a session.

## Files

| File | Purpose |
| --- | --- |
| `cordis.patch.yml` | The preset declaration: persona text and the plugin list. |
| `package.json` | Bundle manifest; `dsh.bundle.patch` points at the patch. |
| `skills/unity-rogue-asian-fantasy/SKILL.md` | The project knowledge the agent loads. |

## Editing it

- **Change how the agent behaves** → edit the `persona` prefix in `cordis.patch.yml`.
- **Change what the agent knows about the game** → edit
  `skills/unity-rogue-asian-fantasy/SKILL.md`. This is where new conventions,
  subsystems, and verification commands belong; prefer it over growing the persona.
- **Change which plugins mount** → edit the `plugins` list in `cordis.patch.yml`.

This bundle is installed into the dsh profile as a `link:` dependency, so edits
here take effect without reinstalling. The profile is "live", so the Host reloads
the patch automatically; existing sessions keep the plugin revision they started
with, so **start a new session** to pick up changes.

If the profile ever fails to load the patch, reinstall from the repo root:

```bash
dsh plugin install-bundle "/Users/stevenwu/Documents/GitHub/Rogue Asian Fantasy/Tools/dsh-unity-preset"
```

Uninstall with `dsh plugin remove-bundle @local/dsh-unity-rogue-preset`.

## One fragile line

`customSkillDirs` in `cordis.patch.yml` is an **absolute path**, because a
relative path in a preset config resolves against the dsh profile directory, not
against this bundle. If the project directory moves, re-point that single line:

```yaml
customSkillDirs:
  - /Users/stevenwu/Documents/GitHub/Rogue Asian Fantasy/Tools/dsh-unity-preset/skills
```

## Suggested next steps

`AGENTS.md` does not exist yet. Adding one at the project root with the build
commands and the hard "don't touch" rules would make a plain session — one not
using this preset — safer too, since `dsh-agent-instructions` loads it
automatically.
