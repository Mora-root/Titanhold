# Titanhold — AI Agent Instructions

## Current Scope

Titanhold is a Unity isometric ARPG/RPG prototype. Build the current solo
vertical slice so it can later support 4–8 player co-op. Do not add networking
yet; keep state, identities, and gameplay commands authority-friendly.

Active run loop:

`exploration/farming → fill run meter → manual portal → separate assault arena →
assault wave → intermission/reward → return to the same exploration location`

The vertical slice has three regular rounds. Round four still includes its full
exploration/farming phase, but its portal starts the final boss encounter instead
of a regular assault wave. Boss victory enters a final intermission for rewards;
it must not create a return portal or advance to round five.

Current assault rules:

- enemies immediately pursue an eligible player;
- targets are mutable; solo registers one player, while the roster must support
  future aggro, taunts, death, disconnects, and reselection;
- assault enemies give experience but no item loot, exploration threat, or run
  contribution; encounter rewards are intended to come from a reward chest;
- the assault reward is rolled once when the encounter completes; its optional
  one-use chest appears during intermission and emits world pickups when opened;
- the return portal appears only during intermission and completes the return to
  the saved exploration position;
- `Skelet_Assault` is independent from exploration and legacy wave prefabs.
- `Skelet_Boss_Prototype` is a temporary independent boss prefab; boss abilities,
  and telegraphs are later stages;
- boss victory opens a non-pausing completion UI. The player can collapse it to
  collect remaining drops; completing the run requires confirmation and moves
  the run to `Completed`. A separate return command then captures every session
  participant, records the victory result, and loads the Hub. It must remain
  retryable if Hub loading cannot begin.
- participant health is registered explicitly for the run scene. The run enters
  `Failed` only when no registered participant remains alive; solo currently has
  one participant, while the rule must remain suitable for a future co-op roster.
  Defeat records only fully completed rounds and returns through the same
  snapshot/session boundary as victory.
- player death clears the state machine and queued action, cancels unreleased
  attacks/abilities, stops NavMesh movement, and enters the non-looping death
  animation before the defeat UI is shown.

Current round scaling:

- round one uses base enemy values;
- each later round adds `+20%` maximum health and `+10%` damage per completed
  round;
- living exploration enemies are rescaled and restored to their new full health
  when the next exploration round begins;
- assault scaling multiplies the current round snapshot by the locked Rift
  Instability snapshot; do not compound runtime values from the previous round;
- out-of-combat enemy regeneration is a later stage and is not implemented yet.

Camp defense, towers, and the old wave flow are outside this vertical slice.
Treat them as legacy/future-activity code unless explicitly requested. Do not
delete them.

The first-build meta layer is a separate UI Hub scene, not the legacy camp in
`SampleScene`. It will own preparation, difficulty selection, and run results;
it may later grow into a physical 3D camp without changing the underlying
commands. `GameSessionService` is the scene-independent outer lifecycle around
the inner `RunFlowService`. `CharacterSnapshotService` captures and atomically
restores inventory slots, equipment instances/modifiers, level, experience, and
gold by stable item-definition ids. The item-definition catalog and persistent
session owner are connected in `HubScene`; Hub-to-run scene loading and
participant restoration are wired. A completed victory returns through the
session layer to the Hub, where the last result is shown and another run can be
started. Defeat uses the same result and return path. The local pause menu can
mark a run `Abandoned`; it records only fully completed rounds and returns over
the same retryable session boundary. Solo pause stops world time, while local
input suppression remains separate so a future co-op pause need not stop the
shared simulation.

Run participants enter exploration with full health and class resource after
their snapshot, equipment modifiers, and derived stats have been applied.

Progression and economy lifetimes are intentionally separate. Run experience,
run level, run gold, selected run abilities, temporary upgrades, and relics
belong to one participant in one active run. Character experience is awarded
only from the concluded run result. Crystals are permanent account currency;
regular crafting reagents remain stackable inventory items. Conclusion rewards
are deterministic from completed rounds, difficulty, and the victory bonus.
The first successful conclusion attempt applies character experience to every
participant snapshot and crystals once to the account wallet. Its settled result
survives a failed Hub load so retrying the transition cannot award twice.
Current `PlayerGold` remains a transitional prototype path and must not define
the durable save format. `EnemyRewardSource` is data-only; player-attributed
`CombatExecutionReport` batches award its experience through the scene's
`RunProgressionCombatAdapter`, which explicitly maps combat sources to session
participant ids and handles multi-target executions once.
World gold pickups resolve the participant's runtime progression gateway and
credit that participant's temporary run wallet; `PlayerGold` remains only as a
compatibility fallback outside the run-progression flow.
`SampleScene` presents the local participant's temporary run level, experience,
and run gold through `RunProgressionHudPresenter`; the old permanent-experience
HUD components remain present but disabled in that scene.
`GameSessionRuntime` owns the active per-participant run-progression roster and
the account crystal wallet. It creates the roster from the validated launch
participants, retains it through run/Hub transitions, and clears it only after
the session enters Hub or a launch transition is cancelled.

`Combat/Abilities/AbilityExecutionService` is a plain C# foundation for one-release
abilities, with actor-local cooldowns, immutable execution snapshots, explicit
simulation time, and execution-id-checked release, finish, and cancellation.
Resource gateways must reject spends without mutation and defer notifications
until the enclosing command returns. `AreaDamageAbilityDefinition` creates an
immutable offensive/query snapshot, and `PlayerAbilityExecutor` releases its
single area effect using scaled simulation time. `PlayerBrain` and combat reward
adapters share the explicitly selected `IPlayerSkillCommands` executor.
`SpinAbility.asset` is wired through `Player.prefab` with stable id `ability:spin`:
20 resource, 3-second cooldown, 1.5 damage multiplier, 2.5 radius, and the existing
animation's release/recovery timing. The old `PlayerSkillExecutor` component is
disabled but retained with its `SkillData` reference. Legacy animation events do
not authorize effects on the replacement path. Other ability forms and run-level
ability selection remain later stages.

## Search and Project Map

- Start in the smallest relevant project-owned folder and use exact names with
  `rg`; do not begin with broad repository exploration.
- Prefer `Assets/_Project/`. Avoid scenes, prefabs, ScriptableObjects, and large
  unrelated files for code-only work.
- Report unrelated dirty files briefly; do not inspect or modify them.
- Prefer current implementations over similarly named legacy classes.

Code routes:

- `Scripts/Run/` — current run, portal, arena, assault, registries, validators.
- `Scripts/Session/` — Hub/run lifecycle, launch parameters, participants, and
  final run results across scene boundaries.
- `Scripts/Enemies/` — AI, targeting, death, enemy reward integration.
- `Scripts/Combat/` — damage, attacks, combat identities, abilities.
- `Scripts/Player/` — player components and runtime wrappers.
- `Scripts/Inventory/`, `Equipment/`, `Loot/`, `Progression/` — named systems.
- `Scripts/Session/` — cross-scene session state, character snapshots, and stable
  item-definition resolution.
- `Scripts/UI/` — views and interaction controllers.
- `Scripts/Core/` — shared runtime utilities.
- `Scripts/Threat/`, `Camp/`, `Towers/` — outside the current slice unless asked.

Paths above are under `Assets/_Project/`. For a run/arena task, start in
`Scripts/Run/`; for enemy targeting, start from the exact state/provider in
`Scripts/Enemies/`, then inspect `EnemyBrain`. Avoid legacy
`WaveEnemyTargetProvider` unless the task targets the old flow.

Current run assets:

- `Scenes/HubScene.unity` — first-build UI Hub and persistent session root;
- `Scenes/SampleScene.unity` — exploration plus prototype assault arena;
- `Prefabs/Enemy/Skelet_Assault.prefab` — current assault enemy;
- `Prefabs/Enemy/Skelet_Boss_Prototype.prefab` — temporary round-four boss;
- `Prefabs/Run/AssaultRewardChest.prefab` — optional intermission reward chest;
- `Prefabs/Run/AssaultReturnPortal.prefab` — intermission return portal;
- `Prefabs/UI/RunCompletionUI.prefab` — final victory, confirmation, and
  completed-state UI;
- `Prefabs/UI/RunPauseUI.prefab` — solo pause, resume, and confirmed
  abandoned-run exit UI;
- `ScriptableObjects/Run/AssaultWave_Prototype.asset` — prototype wave;
- `ScriptableObjects/Run/AssaultWave_Boss_Prototype.asset` — prototype boss encounter;
- `ScriptableObjects/Run/AssaultReward_Prototype.asset` — prototype chest loot;
- `ScriptableObjects/Run/RunConclusionRewards_Prototype.asset` — deterministic
  character-experience and account-crystal rewards by outcome and difficulty;
- `ScriptableObjects/Items/ItemDefinitionCatalog.asset` — runtime lookup for
  persisted item-definition ids;
- `Prefabs/Old/` — legacy only.

Inspect Unity assets only when wiring or balance requires it and asset changes
are approved. Do not start in imported/sample folders such as
`HDRPDefaultResources`, `KayKit_Skeletons_1.1_FREE`, `ModularCastle_AssetPack`,
`RPG Tiny Hero Duo`, `TerrainSampleAssets`, `TextMesh Pro`, `TutorialInfo`, or
`Settings`.

## Architecture

- `ScriptableObject` = static definition/balance data.
- Plain C# = runtime state and core rules.
- Service = gameplay use case and mutation boundary.
- `MonoBehaviour` = Unity lifecycle, adapter, or serialized wiring.
- UI view = display and user-event emission only.

UI must not mutate inventory, equipment, run state, or other gameplay models.
Interaction controllers translate UI events into service commands. Keep domain
logic independent from UI, camera, physical input, and scene-only objects.

Prefer practical composition over large managers or speculative abstractions.
Avoid global mutable state, per-frame logs, per-enemy scene searches, and
per-enemy physics scans as authoritative targeting.

`ItemDefinitionCatalog` is the runtime resolver for persisted item ids. Treat an
invalid catalog (null entries, empty ids, or duplicate ids) as wholly unusable;
do not resolve a partially valid subset. Its build utility must include every
project-owned `ItemDefinition` under `Assets/_Project/ScriptableObjects`,
including definitions used only by loot tables outside the `Items` folder.

`GameSessionRuntime` owns the cross-scene session service and character
snapshots. `GameSessionRuntimeHost` is its persistent Unity adapter; keep it on
a dedicated root object and discover it once at scene entry instead of exposing
gameplay state through a global singleton. The Hub launch controller creates the
outer run command before loading `SampleScene`; its scene entry point restores
an existing character snapshot or captures scene defaults on the first launch,
then activates the session run. Direct `SampleScene` Play Mode remains valid.

For future multiplayer compatibility, use stable ids for static definitions,
explicit runtime ids for entities, replaceable encounter participants/targets,
services or commands for meaningful actions, and serializable runtime state only
where it provides real value.

## Staging and Safety

- Follow the current stage exactly; do not implement later stages early.
- Build replacements side-by-side. Keep legacy working until the new path is
  implemented and verified; cleanup/deletion is a separate explicit stage.
- Do not create hybrid legacy/new flows unless explicitly requested.
- Scenes, prefabs, ScriptableObjects, settings, packages, imported assets, and
  serialized references require explicit approval for the current stage.
- Do not delete assets, components, GameObjects, or serialized references unless
  explicitly requested.
- Never edit `.meta` files manually. Include Unity-generated `.meta` files for
  new scripts; report unexpected GUID, reimport, move, or `.meta` changes.
- Keep changes small and reviewable. The user normally commits after each stage;
  do not commit unless asked.

## Validation and Handoff

After code changes, recompile and run the narrowest relevant Unity validation.
Use Ability Execution Foundation validation for the shared ability lifecycle.
Use Area Damage Ability validation for offensive snapshots, deferred resource
notifications, area damage batching, and player executor selection.
Use Spin Ability Wiring validation and the Spin Ability Play Mode smoke test for
the installed Spin definition/player binding. Run the latter from saved
`SampleScene`; it checks resource cost, pause, offensive snapshot damage, one
multi-target report, attributed run experience, cooldown, and death cancellation.
Use the Console/MCP and `Tools/Titanhold/...` validators. Current relevant tools
include assault arena wiring, assault target selection, and the Run Flow Play
Mode smoke test. Use Round Enemy Scaling and its wiring validation for round
progression changes. Use Assault Enemy Scaling validation when wave multipliers
or enemy runtime combat values change. Use Player Skill Command Buffer validation
when player action sequencing changes. Use Assault Reward and Assault Reward
Vertical Slice Wiring validation when encounter rewards change. Use Boss
Encounter Wiring validation when the final-round prefab, definition, or scene
reference changes. Use Run Completion UI Wiring validation and the Run Flow Play
Mode smoke test when final-intermission UI or completion commands change.
Use Run Pause Wiring validation and the Run Flow Play Mode smoke test when pause
or voluntary run-exit behavior changes.

Report concisely: changed files/behavior, validation results, remaining
warnings/errors, unrelated dirty files, intentionally untouched systems/assets,
and any manual Unity check needed before the user's commit.
