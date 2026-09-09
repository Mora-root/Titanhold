# Titanhold — AI Agent Instructions

## Mission and Scope

Titanhold is a Unity isometric ARPG/RPG prototype. Build the solo vertical slice
while keeping gameplay state and commands suitable for later host-authoritative
4–8 player co-op. Do not implement networking yet.

Current loop:

`exploration/farming → fill run meter → manual portal → separate assault arena →
assault encounter → intermission/reward → return to the same exploration location`

There are three regular rounds. Round four includes exploration, then its portal
starts the final boss. Boss victory enters final intermission; it must not create
a return portal or advance to round five.

Camp defense, towers, and the legacy wave flow are outside this slice. Preserve
them as legacy/future activity code unless explicitly requested.

## Gameplay Invariants

### Run and Assault

- Filling the meter locks it at maximum and creates a persistent manual portal.
  Further eligible exploration kills keep their ordinary rewards and add Rift
  Instability; the portal snapshots that instability when the player enters.
- Assault enemies immediately pursue an eligible participant. Their target is
  mutable for future aggro, taunts, death, disconnects, and reselection.
- Assault enemies grant experience but no item loot, exploration threat, or run
  meter contribution. Encounter rewards come from the reward chest.
- The encounter reward is rolled once. Its optional one-use chest appears during
  intermission and emits world pickups when opened.
- The return portal exists only during regular intermission and restores the saved
  exploration position.
- `Skelet_Assault` and `Skelet_Boss_Prototype` are independent from exploration
  and legacy wave prefabs. Boss abilities and telegraphs are later work.
- Boss victory opens non-pausing completion UI. The player may collapse it to
  collect drops. Confirmation moves the run to `Completed`; a separate retryable
  command captures participants, settles the result, and loads Hub.
- Explicitly registered participant health determines defeat. The run becomes
  `Failed` only when no registered participant remains alive. Defeat and abandon
  record only fully completed rounds and use the same session boundary as victory.
- Player death clears the state machine and queued action, cancels unreleased
  attacks/abilities, stops NavMesh movement, and plays the non-looping death
  animation before defeat UI appears.

### Scaling

- Round one uses authored base values. Each completed round adds `+20%` maximum
  health and `+10%` damage to later-round enemies.
- Living exploration enemies are rescaled and restored to their new full health
  when the next exploration round begins.
- Assault scaling multiplies the current round snapshot by the locked Rift
  Instability snapshot; never compound previous runtime values.
- Out-of-combat enemy regeneration is not implemented yet.
- `EnemyDefinition`/`EnemyDefinitionCatalog` provide the all-or-nothing central
  enemy-balance registry and immutable base-stat snapshots. The application
  service and optional `EnemyBaseStatsReceiver` can feed CharacterStats, combat,
  movement, and detection without changing legacy fallbacks.
  `EnemyDefinitionInitializationService` resolves a strict stable id before any
  mutation; `EnemyDefinitionBinding` is the local prefab adapter. No definition
  assets, prefab bindings, or runtime spawn integration are connected yet.

## Session, Progression, and Economy

- The first-build meta layer is the UI-only `HubScene`, not the legacy camp in
  `SampleScene`. It owns preparation, difficulty selection, and run results.
- `GameSessionService` is the outer scene-independent lifecycle around
  `RunFlowService`. `GameSessionRuntimeHost` is the persistent Unity adapter;
  discover it once at scene entry rather than exposing a global singleton.
- `CharacterSnapshotService` atomically captures/restores inventory, equipment
  instances and modifiers, character level/experience, and gold using stable
  item-definition ids. Participants enter exploration with full health and class
  resource after restoration and derived-stat application.
- Solo pause stops world time. Local input suppression remains separate so future
  co-op pause need not stop shared simulation.

Lifetimes are separate:

- per run and participant: RunXP, RunLevel, RunGold, run abilities, upgrades,
  relics, and temporary state;
- per character: character experience, level, talents, abilities, and equipment;
- per account: crystals and account-wide unlocks;
- inventory items: ordinary crafting reagents remain stackable items.

Conclusion rewards are deterministic from outcome, completed rounds, difficulty,
and victory bonus. The first successful settlement awards character experience to
each participant and account crystals once. Settlement survives a failed Hub load,
so retry cannot duplicate rewards.

`EnemyRewardSource` is data-only. Player-attributed `CombatExecutionReport`
batches award RunXP through `RunProgressionCombatAdapter`, which maps combat actors
to participant ids and handles multi-target executions once. World gold pickups
credit that participant's run wallet through its progression gateway. `PlayerGold`
is compatibility-only and must not define durable save data.

`GameSessionRuntime` owns the active per-participant progression and combat-resource
rosters, account wallet, five-slot run ability loadouts, ability-choice service,
and start-readiness roster. These survive retryable run/Hub transitions and clear
only after entering Hub or cancelling launch. Ability ownership and slot commands
use stable ids; replacement and movement are atomic.

General ability offers are deterministic, exclude already owned definitions, allow
one pending choice per participant, and cannot replay a resolved choice id.

Starter selection rules:

- a participant without a seeded ability receives one deterministic
  `choice:starting` offer for slot zero;
- the offer contains exactly three unique definitions resolved from the stable
  character-archetype pool;
- successful selection grants/assigns the ability and confirms readiness;
- changing confirmed slot zero revokes readiness until it is confirmed again;
- Hub creates the run transition immediately; the run scene activates only after
  its readiness roster is sealed;
- presentation preserves rolled order and never rerolls or mutates the offer;
- the view emits only an option index; coordinator/domain services own validation.

The Hub does not seed an ability. It loads `SampleScene` in the transition phase;
the in-run start gate pauses solo simulation and suppresses local gameplay input
while the wired three-card overlay is open. Selecting one option seals readiness,
activates the run, closes the overlay, and starts the first round. The warrior
starter pool and catalogs are connected through the persistent host in `HubScene`.

## Combat and Abilities

`AbilityExecutionService` is plain C# for one-release abilities: actor-local
cooldowns, immutable commit snapshots, explicit simulation time, and execution-id
checked release/finish/cancellation. Resource gateways reject unaffordable spends
without mutation and defer notifications until the enclosing command returns.
Resource and cooldown commit only when execution actually starts. Animation events
never authorize effects on the replacement path.

Player skill commands capture the explicitly selected target when input is issued,
including while buffered behind another action. Runtime definitions implement the
shared `IRuntimeAbilityDefinition`/`IRuntimeAbilitySnapshot` contract.

An accepted skill command owns movement: it clears the previously stored manual
destination, while an invalid command leaves movement untouched. Runtime ability
snapshots carry a semantic post-action policy. Targeted and cone attacks continue
basic attacks against their surviving primary target after recovery; a newer
buffered skill or manual movement command takes precedence. Area attacks do not
request this follow-up.

- `AreaDamageAbilityDefinition`: self-centred multi-target release.
- `TargetedDamageAbilityDefinition`: requires a live non-self target; commit checks
  range, horizontal facing, and optional obstruction. Release rechecks target,
  obstruction, and authored grace range, but not facing.
- Targeted preflight returns ready/repositionable/invalid without spending. The
  approach state paths for range/obstruction, rotates at normal speed for facing,
  commits only when valid, and is cancelled by manual movement.
- `ConeDamageAbilityDefinition`: uses the selected target for approach/facing, then
  releases one report over unique targets in the forward sector. The primary target
  takes full damage; secondary targets currently use an authored `30%` multiplier.
- Targeted and cone damage may author an optional timed stat effect. It applies
  after successful non-lethal damage, so the triggering hit uses existing defense.
  Effect expiry receives explicit simulation time.

`TimedStackingStatEffectService` aggregates stacks by effect and combat source,
refreshes one shared expiry, enforces the cap, and replaces one sourced stat
modifier atomically. `TimedStackingStatEffectReceiver` is connected with an
explicit `CharacterStats` reference on the four active enemy prefabs. Their stat
configs remain unset, so authored health continues using `Health` fallback values
and base Armor remains zero. Cross-player/global co-op cap rules are not defined.

Warrior starter set:

- Heavy Strike (`ability:heavy-strike`): one target, `1.5x` damage, generates one
  Rage;
- Crushing Strike (`ability:crushing-strike`): `1.0x` damage, generates one Rage,
  and applies five possible stacks of `-5%` armor with an eight-second shared
  duration;
- Cleave: full damage to the selected target, 30% to other forward-sector targets,
  and no Rage generation.

Spin is not a starter. Generic bounded combat-resource state, per-run participant
ownership/binding, and idempotent once-per-release successful-damage generation
exist. The warrior starts each run with `0/8` Rage through its archetype resource
loadout. Rage decay, external generation (such as taking damage), and UI are not
implemented yet.

`SpinAbility.asset` remains the direct-scene fallback: stable id `ability:spin`,
20 resource, 3-second cooldown, 1.5 damage multiplier, 2.5 radius. The disabled
legacy `PlayerSkillExecutor` and its `SkillData` reference remain intact.

Definition catalogs are all-or-nothing: null entries, malformed/duplicate ids, or
unresolved pool abilities invalidate the whole catalog. Live ability slot binding
must reflect replacement without rebuilding the player executor.

## Project Map and Search

Start in the smallest relevant project-owned folder and use exact names with `rg`.
Prefer `Assets/_Project/`; do not begin in scenes, large assets, or imported/sample
packages. Report unrelated dirty files without inspecting or modifying them.

- `Scripts/Run/`: run flow, portals, arena, assault, registries, validators.
- `Scripts/Session/`: Hub/run lifecycle, participants, snapshots, results.
- `Scripts/Combat/`: damage, identities, attacks, abilities, effects.
- `Scripts/Enemies/`: AI, mutable targeting, death, reward integration.
- `Scripts/Player/`: input-facing components, states, runtime adapters.
- `Scripts/UI/`: passive views and interaction controllers.
- `Scripts/Inventory/`, `Equipment/`, `Loot/`, `Progression/`: named systems.
- `Scripts/Core/`: shared runtime utilities and stats.
- `Scripts/Threat/`, `Camp/`, `Towers/`: legacy/out of current scope unless asked.

For run/arena work start in `Scripts/Run/`. For targeting, start from the exact
provider/state in `Scripts/Enemies/`, then inspect `EnemyBrain`; avoid legacy
`WaveEnemyTargetProvider` unless targeting the old flow.

Current assets:

- scenes: `Scenes/HubScene.unity`, `Scenes/SampleScene.unity`;
- enemies: `Prefabs/Enemy/Skelet_Assault.prefab`,
  `Prefabs/Enemy/Skelet_Boss_Prototype.prefab`;
- run prefabs: `Prefabs/Run/AssaultRewardChest.prefab`,
  `Prefabs/Run/AssaultReturnPortal.prefab`;
- UI: `Prefabs/UI/RunCompletionUI.prefab`, `Prefabs/UI/RunPauseUI.prefab`;
- definitions: `ScriptableObjects/Run/AssaultWave_Prototype.asset`,
  `AssaultWave_Boss_Prototype.asset`, `AssaultReward_Prototype.asset`,
  `RunConclusionRewards_Prototype.asset`;
- catalogs: `ScriptableObjects/Items/ItemDefinitionCatalog.asset`,
  `ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset`;
- `Prefabs/Old/`: legacy only.

Never start in imported folders including `HDRPDefaultResources`, asset packs,
`TerrainSampleAssets`, `TextMesh Pro`, `TutorialInfo`, or `Settings`.

## Architecture Rules

- `ScriptableObject`: static definitions and balance data.
- Plain C#: runtime state and core rules.
- Service: use case and mutation boundary.
- `MonoBehaviour`: Unity lifecycle, adapter, or serialized wiring.
- UI view: rendering and user-event emission only; controllers translate events
  into commands. UI never mutates gameplay models directly.
- Keep domain rules independent from UI, camera, physical input, animation events,
  and scene-only objects.
- Prefer practical composition over broad managers/speculative abstractions.
- Avoid global mutable state, per-frame logs, per-enemy scene searches, and
  per-enemy physics scans as authoritative targeting.
- For future co-op, use stable definition ids, explicit runtime entity ids,
  replaceable participant/target rosters, commands for mutations, and serializable
  state only where it has current value.

`ItemDefinitionCatalog` must include every project-owned `ItemDefinition` under
`Assets/_Project/ScriptableObjects`, including loot-table-only definitions. Invalid
item or ability catalogs must never expose a partially valid subset.

## Staging and Safety

- Follow the current stage; do not implement later stages early.
- Build replacements side-by-side. Keep legacy working until separately approved
  cleanup; do not create hybrid legacy/new flows unless requested.
- Scenes, prefabs, ScriptableObjects, settings, packages, imported assets, and
  serialized references require explicit approval for the current stage.
- Do not delete assets, components, GameObjects, or serialized references without
  explicit approval.
- Never edit `.meta` files manually. Include Unity-generated `.meta` files for new
  scripts and report unexpected GUID, reimport, move, or `.meta` changes.
- Preserve unrelated user changes. Keep stages small and reviewable.
- The user normally commits after each stage. Never commit unless asked.

## Validation and Handoff

After code changes, recompile and run the narrowest applicable Unity checks:

- catalogs/loadouts: Ability Definition Resolution, Ability Definition Catalog
  Wiring, Run Ability Choices;
- starting choice: Run Start Ability Readiness, Starting Ability Selection,
  Starting Ability Pools, and their UI/wiring validators;
- execution: Ability Execution Foundation, Combat Resources, Run Combat Resources,
  Area Damage Ability, Targeted Damage Ability, Cone Damage Ability, Timed Stacking
  Stat Effects;
- sequencing/wiring: Player Skill Command Buffer, Spin Ability Wiring, and the
  Spin Ability Play Mode smoke test from saved `SampleScene`;
- run: assault arena/target selection, Round Enemy Scaling, Assault Enemy Scaling,
  Assault Reward and wiring, Boss Encounter Wiring, Run Completion UI Wiring, Run
  Pause Wiring, and the Run Flow Play Mode smoke test as relevant.

Use the Console/MCP and `Tools/Titanhold/...` validators. Run broader smoke tests
only when their integration boundary changed. Restore `HubScene` after Play Mode
checks and remove automatically generated TMP fallback-cache diffs.

Handoff concisely: changed behavior/files, validation results, remaining warnings
or errors, unrelated dirty files, intentionally untouched systems/assets, manual
Unity check required, and proposed commit title.
