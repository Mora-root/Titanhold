# Titanhold — Unity + Codex Workflow

This document defines how Codex should work with the Titanhold Unity project through Unity MCP.

Unity MCP can read and modify Unity Editor state. Because Unity scenes, prefabs, serialized references, and assets are fragile, Codex must work carefully.

## Default Workflow

For each non-trivial task, Codex should:

1. Read relevant project documents:
   - `AGENTS.md`
   - `Docs/GDD/GDD_Current.md`
   - `Docs/Architecture/Architecture_Principles.md`
   - `Docs/Architecture/Unity_Codex_Workflow.md`
2. Analyze relevant files.
3. Explain the current flow.
4. Identify risks.
5. Propose a minimal plan.
6. List files to modify.
7. Wait for confirmation.
8. Make small changes.
9. Check Unity Console through MCP.
10. Summarize changes.
11. Suggest a git commit message.

## Safe Read-Only Actions

These are usually safe:

- reading files;
- searching project code;
- reading Unity Console logs;
- getting active scene info;
- inspecting GameObjects;
- inspecting packages;
- checking tests;
- checking git diff/status;
- reviewing markdown docs.

## Actions That Require Explicit Confirmation

Do not perform these without explicit user confirmation:

- modifying Unity scenes;
- modifying prefabs;
- modifying ScriptableObjects;
- modifying Project Settings;
- modifying package manifest;
- modifying Build Settings;
- modifying `.meta` files;
- deleting assets;
- deleting GameObjects;
- moving large folders;
- changing serialized references;
- creating or deleting scenes;
- adding Unity packages;
- saving scenes after modifications.

## Code Changes

Editing C# scripts is allowed only when the user has requested a change or approved a plan.

For code changes:

1. Keep the change small.
2. Modify the minimum number of files.
3. Preserve existing behavior unless asked otherwise.
4. Do not mix unrelated refactors.
5. After changes, check Unity Console.
6. Report errors/warnings if any.
7. Summarize changed files.

## Unity MCP Tool Safety

Read-only or low-risk tools:

- `get_scene_info`
- `get_console_logs`
- `get_gameobject`
- `get_material_info`

Use with confirmation:

- `update_gameobject`
- `update_component`
- `move_gameobject`
- `rotate_gameobject`
- `scale_gameobject`
- `set_transform`
- `reparent_gameobject`
- `select_gameobject`
- `send_console_log`
- `recompile_scripts`
- `run_tests`

High-risk tools requiring explicit confirmation:

- `delete_gameobject`
- `create_prefab`
- `add_asset_to_scene`
- `create_scene`
- `load_scene`
- `save_scene`
- `unload_scene`
- `delete_scene`
- `add_package`
- `create_material`
- `modify_material`
- `assign_material`
- `batch_execute`

## Preferred Change Size

Good tasks:

- remove noisy debug log;
- extract one command class;
- add one interface;
- refactor one state;
- add one small test;
- split one responsibility;
- add one small data model;
- rename one unclear method;
- move one piece of logic to the correct layer.

Bad tasks:

- rewrite the whole movement system;
- implement full combat at once;
- implement multiplayer;
- restructure the entire project;
- move many assets at once;
- implement inventory, loot, crafting, and equipment in one step;
- modify scenes and prefabs without a clear plan.

## Before Modifying Code

For non-trivial code changes, Codex should provide:

- problem summary;
- current implementation summary;
- proposed solution;
- files to modify;
- risk level;
- fallback plan if the change causes errors.

## After Modifying Code

After any code change:

1. Check Unity Console through MCP.
2. Report compile errors, runtime errors, and warnings.
3. If errors were introduced, propose a fix before continuing.
4. Summarize the changed files.
5. Suggest a git commit message.

## Git Workflow

Codex should not commit automatically unless explicitly asked.

Recommended user workflow:

```bash
git status
git diff
git add <changed-files>
git commit -m "<clear commit message>"
## Suggested Commit Style

Use short, clear commit messages in English.

Examples:

- `Remove noisy player state debug log`
- `Add AI workflow documentation`
- `Introduce player movement command`
- `Refactor player state transition logging`
- `Add basic Health component`

## Logging Workflow

Avoid noisy logs.

Rules:

1. Do not leave per-frame logs in gameplay code.
2. Do not log every `Update`, `Tick`, or state loop by default.
3. If state transition logs are needed, log only transitions.
4. Debug logs should be behind explicit debug flags when possible.
5. Unity Console should remain useful for real errors and warnings.

## Working With GDD

When implementing features:

1. Check `Docs/GDD/GDD_Current.md`.
2. Preserve current design direction.
3. If code and GDD conflict, ask the user before changing design intent.
4. Do not implement out-of-scope systems unless explicitly requested.
5. Prefer MVP-friendly solutions.

## Working With Architecture

When implementing or refactoring systems:

1. Check `Docs/Architecture/Architecture_Principles.md`.
2. Keep input, state, movement, combat, targeting, UI, and data responsibilities separated.
3. Do not add abstractions without a practical reason.
4. Do not introduce global mutable state as a shortcut.
5. Keep future multiplayer validation in mind, but do not add networking code yet.

## Unity Asset Safety

Scenes, prefabs, ScriptableObjects, and `.meta` files are fragile.

If a task requires changing them:

1. Explain why the change is necessary.
2. List exact assets to modify.
3. Ask for confirmation.
4. Make the smallest possible change.
5. Check Unity Console.
6. Ask the user to verify in Unity Editor.

## If Something Fails

If a tool call fails or times out:

1. Report the failure honestly.
2. Do not assume the change was applied.
3. Check Unity Console if possible.
4. Suggest a small diagnostic step.
5. Avoid repeating destructive operations.

## Final Rule

Prefer slow, safe, correct progress over large risky changes.