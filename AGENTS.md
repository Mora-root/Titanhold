# Titanhold — AI Agent Instructions

## Mission and Current Stage

Titanhold is a Unity isometric ARPG/RPG prototype. Build the solo vertical slice
while keeping gameplay state and commands suitable for later host-authoritative
4–8 player co-op. Do not implement networking yet.

Active replacement loop:

`continuous exploration/activities → chapter progress → Rift Collapse and manual
boss portal → direct boss encounter → reward/completion`

The existing nine-round exploration/assault flow must remain operational until the
replacement has its portal, presentation, boss transition, and reward path and the
user explicitly approves cutover. Do not create a hybrid flow.

Private design documents outside the repository hold game-design decisions. Do not
copy them into the repository or Git without explicit permission. `AGENTS.md`
contains only technical state and working rules.

## Active Chapter Flow

- `RunChapterFlowService` is the plain-C# authority for chapter progress,
  escalation, Rift Collapse time, instability stacks, boss scaling snapshots, and
  the direct boss/reward/completion phases. `RunChapterFlowRuntime` is its parallel
  Unity adapter.
- `RunChapterProgressApplicationService` accepts immutable atomic progress batches.
  Stable event, source, and participant ids provide replay protection and future
  co-op attribution.
- `RunChapterCombatProgressAdapter` converts participant-attributed multi-target
  combat reports into one progress event. It temporarily reads
  `EnemyRunContributionSource.ThreatAmount` as chapter progress, is not scene-wired,
  and must not run beside the legacy meter before cutover.
- `RunChapterFlowPresentationProjection` exposes an immutable polling-friendly
  snapshot containing chapter, progress, escalation, phase, portal availability,
  collapse timing, instability, authoritative boss scaling, and forced-transition
  state. Reading it never advances simulation time. UI must poll fresh snapshots
  for countdown display and must not infer game values from phase or recalculate
  boss scaling.
- `RunChapterFlowRuntime` owns the replay-safe chapter boss-transition application,
  advances collapse time, resolves registered scene participants, and publishes/
  stores one immutable manual or forced request with authoritative boss scaling.
  `SampleScene` has a side-by-side chapter portal and polling prototype chapter HUD.
  `RunChapterBossEncounterCoordinator` moves the solo participant into the existing
  arena, directly spawns `Skelet_Boss_Prototype` with the frozen chapter scaling,
  and advances boss death to `Reward`; it does not invoke legacy waves. The reward
  path and combat-progress adapter are not connected yet.
- Prototype configuration: progress `100`; thresholds `0/30/60/85%`; collapse
  `120s`; one instability stack per `20s`, capped at six; each stack adds `10%`
  boss health and `5%` boss damage.
- At full progress, ordinary rewards continue but progress stops. Manual entry and
  collapse expiry share the same transition path and freeze the current boss
  scaling snapshot. There is no assault wave before the chapter boss.
- In the Editor or a Development Build, `F7` fills the active chapter progress
  through `RunChapterProgressApplicationService` for portal/collapse testing.

## Legacy Boundaries — Preserve Until Cutover

- Preserve the old Threat meter, nine regular rounds, boss round, assault arena,
  reward chest, return portal, checkpoint/settlement path, camp defense, and towers.
- Exploration enemies target explicitly registered participants through
  `ExplorationTargetRegistry`; targets remain mutable and are reselected when
  invalid or out of range. `EnemySensor` is only a bounded non-allocating fallback.
- Assault enemies immediately pursue eligible participants. They grant experience
  but no item loot, exploration threat, or run-meter contribution. Encounter loot
  comes from the reward chest.
- `Skelet_Assault` and `Skelet_Boss_Prototype` remain independent from exploration
  and legacy-wave prefabs.
- Defeat depends on explicitly registered participant health and occurs only when
  no participant remains alive. Victory settlement is retry-safe; failed Hub loads
  must not duplicate rewards.
- Player death clears state and queued actions, cancels unreleased combat actions,
  stops NavMesh movement, and completes the death presentation before defeat UI.

## Balance and Spawning

- Enemies do not autolevel directly from the player. Difficulty comes from authored
  chapter, region, world stage, enemy role, and explicit difficulty modifiers.
- `RunRoundBalanceSnapshot` and its resolver remain the legacy per-round boundary.
  `RunRoundBalanceDefinition` builds an all-or-nothing authored table; isolated
  tests may use the compatible fallback.
- `EnemyDefinitionCatalog` is the strict all-or-nothing enemy-balance registry.
  Resolve a stable id before mutation. Authored and dynamically spawned enemies
  must initialize base stats before round or instability scaling; missing bindings
  or definitions fail the spawn.
- `ExplorationSpawnBalanceDefinition` is the strict boundary for spawn profiles.
  `RunFlowRuntime` owns the built table and `WorldEnemySpawnZone` resolves the
  active stage. Missing data keeps the legacy single-prefab fallback; assigned
  invalid data fails explicitly.
- Scaling always starts from immutable authored base values; never compound prior
  runtime values. Out-of-combat enemy regeneration is not implemented.
- `Tools/Titanhold/Balance Overview` is read-only and its validator must not mutate
  authored assets.

## Session, Progression, and UI Boundaries

- `HubScene` is the first-build UI-only meta layer. `GameSessionService` owns the
  scene-independent lifecycle; `GameSessionRuntimeHost` is the persistent Unity
  adapter and is discovered once at scene entry, not exposed as a global singleton.
- `CharacterSnapshotService` atomically captures and restores inventory, equipment,
  character progression, and gold through stable definition ids. Participants enter
  exploration with full health and primary resource after derived stats apply.
- Lifetimes remain separate:
  - run/participant: RunXP, RunLevel, RunGold, run abilities, upgrades, relics, and
    temporary state;
  - character: character experience, level, talents, abilities, and equipment;
  - account: crystals and account-wide unlocks;
  - ordinary crafting reagents: stackable inventory items.
- `GameSessionRuntime` owns participant progression, combat-resource rosters,
  account wallet, five-slot run loadouts, choices, upgrades, and readiness. Run
  state survives retryable run/Hub transitions and clears only at the defined
  session boundary.
- Ability and upgrade offers are deterministic, participant-scoped, replay-safe,
  and stable-id based. Catalogs and unlock schedules are all-or-nothing.
  `RunLevelRewardSelectionService` serializes crossed milestones so only one choice
  is pending for a participant.
- `RunUpgradeStatApplicationService` reconciles authoritative stacks onto
  replaceable gateways while preserving other modifier sources. Repeated
  `Increased` modifiers add; repeated `More` modifiers multiply. Max-health changes
  preserve absolute current health and clamp only when necessary.
- The Hub does not seed a starter ability. The run scene gates solo simulation and
  local input until the deterministic starter choice is accepted and readiness is
  sealed.
- Run HUDs and choice views are passive projections. They emit slot indices or
  stable option ids; controllers and domain services validate and mutate state.
  Solo pause and local input suppression remain separate for future co-op.
- `EnemyRewardSource` is data-only. Player-attributed combat reports award RunXP
  through `RunProgressionCombatAdapter`; world gold credits the participant run
  wallet. `PlayerGold` is compatibility-only.
- Conclusion rewards are deterministic and settle once for character experience
  and account crystals, even when the Hub load must be retried.
- `ItemDefinitionCatalog` must include every project-owned `ItemDefinition` under
  `Assets/_Project/ScriptableObjects`, including loot-table-only definitions.
  Invalid item or ability catalogs never expose partial subsets.

## Combat and Ability Contracts

- `AbilityExecutionService` is the plain-C# authority for one-release abilities:
  actor-local cooldowns, immutable commit snapshots, explicit simulation time, and
  execution-id checked release, finish, and cancellation.
- Availability preflight checks cooldown and all required resources before
  approach. Commit atomically rechecks and spends resources and starts cooldown;
  animation events never authorize replacement-path effects.
- Player skill commands capture the explicitly selected target when issued,
  including while buffered. One next action is retained. Invalid commands do not
  mutate movement; accepted skill or world-action commands own movement and clear
  stale manual destinations.
- Runtime abilities use the shared `IRuntimeAbilityDefinition` and
  `IRuntimeAbilitySnapshot` contracts. Targeted and cone attacks may request basic
  attack continuation after recovery; area/self abilities do not. Newer buffered
  skills or movement take precedence.
- Targeted execution validates target, range, facing, and optional obstruction at
  commit, then target, obstruction, and grace range at release. Facing is not
  rechecked at release. Targeted movement exposes an immutable movement directive
  consumed by local player movement.
- Timed effects group stacks by effect and combat source, refresh one shared expiry,
  enforce caps, and replace one sourced stat modifier atomically. Expiry uses
  explicit simulation time.
- Current warrior run abilities and their balance live in the ability definitions
  and unlock schedule assets. Do not duplicate their values in executors or UI.
  `SpinAbility.asset` remains a direct-scene fallback and Spin is not a starter.
- Rage uses the generic participant-owned bounded combat-resource foundation.
  Decay and external generation are not implemented. Health and primary-resource
  flasks are independent from the five run slots and never restore secondary
  resources.
- A universal forward Dash is planned outside the five run slots. Its future
  execution contract must support an authored evade window; do not implement later
  Dash stages early.

## Project Map and Search

Start in the smallest relevant project-owned folder and use exact names with `rg`.
Prefer `Assets/_Project/`; do not begin in scenes, large assets, or imported/sample
packages. Report unrelated dirty files without inspecting or modifying them.

- `Scripts/Run/`: chapter/legacy run flow, portals, arena, registries, validators.
- `Scripts/Session/`: Hub/run lifecycle, participants, snapshots, results.
- `Scripts/Combat/`: damage, identities, abilities, resources, effects.
- `Scripts/Enemies/`: AI, mutable targeting, death, rewards.
- `Scripts/Player/`: input-facing components, states, runtime adapters.
- `Scripts/UI/`: passive views and interaction controllers.
- `Scripts/Inventory/`, `Equipment/`, `Loot/`, `Progression/`: named systems.
- `Scripts/Core/`: shared runtime utilities and stats.
- `Scripts/Threat/`, `Camp/`, `Towers/`: legacy/out of scope unless requested.

For run/arena work start in `Scripts/Run/`. For enemy targeting start from the
exact provider/state in `Scripts/Enemies/`, then inspect `EnemyBrain`; avoid
`WaveEnemyTargetProvider` unless working on legacy flow.

Primary scenes are `Scenes/HubScene.unity` and `Scenes/SampleScene.unity`. Active
project assets live under `Assets/_Project/ScriptableObjects` and
`Assets/_Project/Prefabs`; `Prefabs/Old` is legacy. Never start in imported folders
such as `HDRPDefaultResources`, asset packs, `TerrainSampleAssets`, `TextMesh Pro`,
`TutorialInfo`, or `Settings`.

## Architecture Rules

- `ScriptableObject`: static definitions and authored balance.
- Plain C#: runtime state and core rules.
- Service: use-case and mutation boundary.
- `MonoBehaviour`: Unity lifecycle, adapter, or serialized wiring.
- UI view: rendering and user-event emission only; controllers translate events
  into commands. UI never mutates gameplay models directly.
- Keep domain rules independent from UI, camera, physical input, animation events,
  and scene-only objects.
- Prefer practical composition over broad managers and speculative abstractions.
- Avoid global mutable state, per-frame logs, per-enemy scene searches, and
  per-enemy physics scans as authoritative targeting.
- For future co-op use stable definition ids, explicit participant/runtime entity
  ids, replaceable rosters, validated mutation commands, deterministic offers, and
  serializable state only where it has current value.

## Agent Orchestration

- The primary chat implements small and medium sequential stages directly.
- Use a subagent only for large independent work or parallel research when the
  separate context materially saves time.
- After subagent work, review its diff and architectural boundaries. Do not
  automatically repeat the entire execution cycle; repeat Unity compilation and
  validators only when the change carries relevant risk or the result is uncertain.
- Prefer one long agent wait over repeated short polling, use direct Unity MCP
  tools, and avoid broad tool-catalog dumps.
- Do not estimate usage percentages without available usage statistics.

## Staging and Safety

- Follow the current stage; do not implement later stages early.
- Build replacements side-by-side and keep legacy working until explicit cutover
  approval. Cleanup or deletion requires a later explicit stage.
- Scenes, prefabs, ScriptableObjects, settings, packages, imported assets, and
  serialized references require explicit approval for the current stage.
- Do not delete assets, components, GameObjects, or serialized references without
  explicit approval.
- Never edit `.meta` files manually. Include Unity-generated `.meta` files for new
  scripts and report unexpected GUID, reimport, move, or `.meta` changes.
- Preserve unrelated user changes. Keep stages small and reviewable.
- The user normally commits after each stage. Never commit unless asked.

## Validation and Handoff

After code changes, recompile and run only the narrowest relevant
`Tools/Titanhold/...` validators. Expand checks only when the integration boundary
changed or results are uncertain.

- Chapter flow: Chapter Flow, Progress Application, Combat Progress Adapter, and
  Chapter Presentation validators as applicable.
- Ability foundations: Ability Execution, Combat Resources, damage-shape, timed
  effect, catalog/loadout, and command-buffer validators as applicable.
- Session/UI wiring: starting choice/readiness, run-level rewards, combat HUD,
  completion, pause, and relevant wiring validators.
- Legacy run integration: target selection, enemy scaling, assault reward/boss,
  and Run Flow Play Mode smoke tests only when those boundaries change.

Use direct Unity MCP tools and inspect Console errors. Restore `HubScene` after
Play Mode checks and remove automatically generated TMP fallback-cache diffs.

Handoff concisely: changed behavior/files, validation results, remaining warnings
or errors, unrelated dirty files, intentionally untouched systems/assets, required
manual Unity check, and a proposed commit title.
