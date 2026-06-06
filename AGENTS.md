# Titanhold — AI Agent Instructions

Titanhold is a Unity isometric ARPG/RPG prototype with camp defense, Threat Meter, character progression, loot, and future multiplayer-friendly architecture.

This file defines how AI agents such as Codex should work with the project.

Token-Efficient Search Rules

Use the project map before broad exploration.

Prefer narrow search first:

search exact class names;
search exact method names;
search known folders from the map below;
avoid reading large unrelated files;
avoid inspecting scenes/prefabs unless the task involves serialized references or Unity wiring.

If the task is code-only, do not open Unity assets unless necessary.

If unrelated dirty files exist, report them briefly but do not inspect or modify them unless they affect the task.

Project Folder Map / Where To Look First

Use this folder map before broad search. Start from the smallest relevant folder, then search exact class or method names inside it.

Core Project Folder
Assets/_Project/ — main project-owned content and code. Prefer this over third-party/sample folders.
Code
Assets/_Project/Scripts/Core/ — shared core gameplay utilities, common runtime logic, base systems.
Assets/_Project/Scripts/Player/ — player-facing components and player-owned runtime wrappers.
Assets/_Project/Scripts/Inventory/ — inventory runtime model, item containers, item stacks, player inventory logic.
Assets/_Project/Scripts/Equipment/ — equipment runtime model, equipment services, equipment wrappers.
Assets/_Project/Scripts/Loot/ — pickups, loot rewards, loot drop flow, dropper logic.
Assets/_Project/Scripts/Enemies/ — enemy logic, enemy death/drop integration.
Assets/_Project/Scripts/Combat/ — combat, damage, attacks, skills interaction if present.
Assets/_Project/Scripts/Threat/ — Threat Meter and camp attack pressure systems.
Assets/_Project/Scripts/Camp/ — camp core, camp defense, camp-related gameplay.
Assets/_Project/Scripts/Towers/ — tower/building gameplay systems.
Assets/_Project/Scripts/Progression/ — leveling, experience, progression systems.
Assets/_Project/Scripts/UI/ — UI scripts. Search specific subfolders first when available.
Data / Assets
Assets/_Project/ScriptableObjects/ — project-owned ScriptableObject data such as item definitions, configs, drop tables, skills, or balance assets.
Assets/_Project/Prefabs/ — project-owned prefabs.
Prefabs/UI/ — UI prefabs.
Prefabs/Loot/ — loot/pickup prefabs.
Prefabs/Enemy/ — enemy prefabs.
Prefabs/Old/ — legacy prefabs. Do not use or modify unless explicitly requested.
Assets/_Project/Scenes/ — project-owned scenes.
Assets/_Project/Materials/, Art/, Audio/ — project-owned presentation assets.
Third-Party / Samples / Do Not Start Here

Do not inspect or modify these unless the task explicitly involves them:

Assets/HDRPDefaultResources/
Assets/KayKit_Skeletons_1.1_FREE/
Assets/ModularCastle_AssetPack/
Assets/RPG Tiny Hero Duo/
Assets/TerrainSampleAssets/
Assets/TextMesh Pro/
Assets/TutorialInfo/
Assets/Settings/
Search Strategy By Task
Inventory task: start in Assets/_Project/Scripts/Inventory/, then Assets/_Project/Scripts/UI/.
Equipment task: start in Assets/_Project/Scripts/Equipment/, then Assets/_Project/Scripts/Inventory/, then Assets/_Project/Scripts/UI/.
Loot/pickup task: start in Assets/_Project/Scripts/Loot/, then check Assets/_Project/Prefabs/Loot/ only if prefab wiring is part of the task.
Enemy drop task: start in Assets/_Project/Scripts/Enemies/ and Assets/_Project/Scripts/Loot/.
UI task: start in Assets/_Project/Scripts/UI/ and Assets/_Project/Prefabs/UI/ only if prefab wiring is explicitly requested.
Scene wiring task: inspect Assets/_Project/Scenes/ only after confirming scene changes are allowed.
Data/balance task: start in Assets/_Project/ScriptableObjects/.

Prefer targeted rg searches inside the relevant folder before reading entire files or folders.

Architecture Rules

Use this separation:

ScriptableObject = static data/config.
Plain C# model = runtime state and core rules.
MonoBehaviour = Unity lifecycle, scene wiring, inspector references.
Service = gameplay use case/application logic.
UI = display and user command emission.

Examples:

ItemDefinition is static item data.
ItemContainer is a plain C# inventory model.
PlayerInventory is a MonoBehaviour wrapper over ItemContainer.
CharacterEquipment is a plain C# equipment state model.
PlayerEquipmentRuntime is a MonoBehaviour wrapper for equipment runtime objects.
EquipmentService owns equip/unequip gameplay rules.
UI views must not directly mutate gameplay state.
UI Rules

UI should not own gameplay rules.

Slot views should:

display icon/name/amount;
emit events such as click/right-click/drag;
not call gameplay services directly;
not mutate ItemSlot, ItemContainer, or CharacterEquipment.

Interaction controllers may translate UI events into service calls.

Services mutate models.

Legacy Replacement Rules

When replacing legacy systems:

build the new system side-by-side first;
keep the old flow working until the new alternative is implemented and verified;
do not create hybrid legacy/new code unless explicitly requested;
cleanup/removal must be a separate explicit stage.

Do not delete legacy classes, components, prefabs, or serialized references unless the user explicitly asks.

Staged Refactor Rules

Follow the current stage boundary exactly.

Do not implement future stages early.

Examples:

if a stage says no UI, do not touch UI;
if a stage says no scenes/prefabs/assets, do not touch them;
if a stage says code-only, do not modify Unity assets;
if a stage says read-only analysis, do not change code.

Keep changes small, safe, and reviewable.

Unity Asset Safety

Do not modify these without explicit confirmation:

Unity scenes;
prefabs;
ScriptableObjects;
Project Settings;
Package manifest;
Build Settings;
imported assets;
serialized references.

Do not delete assets or GameObjects unless explicitly requested.

Do not manually edit .meta files.

Unity-generated .meta files for newly created scripts are expected and should be included with those scripts.

Unexpected .meta changes, asset reimports, GUID changes, or asset moves must be reported.

Coding Rules

Prefer clean, practical code over over-engineered abstractions.

Keep domain/application logic independent from:

Unity UI;
camera;
physical input;
scene-only objects.

Avoid global mutable state and large manager classes.

Do not leave noisy per-frame logs in normal gameplay code.

Do not add networking code yet, but keep gameplay logic multiplayer-friendly:

runtime state should be serializable where practical;
use stable ids for static definitions;
use runtime instance ids for unique items;
express gameplay actions through services/commands where possible.
Validation

After code changes, run relevant compile/validation checks when available.

Use Unity Console/MCP for errors when appropriate.

For Unity Editor validation runners, prefer menu tools under:

Tools/Titanhold/...
Response Format After Changes

Keep the report short.

Include:

changed files;
what changed;
validation result;
unrelated dirty files, if any;
what was intentionally not changed when important.
