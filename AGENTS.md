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
`GameSessionRuntime` also owns a separate per-run ability loadout roster with five
slots per participant. Ability ownership and slot assignment use stable ability
ids and explicit commands; slot replacement and movement are atomic, while an
unassigned granted ability remains owned for the current run. The roster has the
same retryable transition lifetime as run progression and is cleared on Hub entry
or launch cancellation. The Hub launch currently grants `ability:spin` into slot
zero. On session-backed run entry, the participant's live slot state is bound to
`PlayerAbilityExecutor` through the persistent ability-definition catalog. Direct
`SampleScene` Play Mode keeps the serialized Spin fallback. Ability pools,
level-based choices, and selection UI are later stages.
`GameSessionRuntime` also owns a per-run ability-choice service over the live
loadout. Explicit offer commands carry a participant id, choice id, target slot,
candidate ids, option count, and roll seed. Offers are deterministic, exclude
already-owned abilities, require the requested number of eligible options, and
allow one pending choice per participant. Successful selection atomically grants
and assigns one offered ability; resolved choice ids cannot be replayed. The
service survives retryable run/Hub transitions and is cleared with the other run
state. Milestone levels, class/universal pools, presentation, and pause behavior
are not connected yet.
`GameSessionRuntime` owns run-start readiness over the live ability loadout. Each
participant must confirm an ability currently assigned to slot zero; changing
that slot revokes the participant's confirmation until the roster is sealed.
The current Hub-provided Spin seed auto-confirms and seals the solo roster, so
existing play remains unchanged. A later starting-choice UI will confirm each
participant explicitly and gate exploration on the sealed roster; run activation
is not gated by readiness yet.
`RunStartingAbilitySelectionService` composes the general run-choice service and
start readiness. A starter offer contains exactly three unique stable ability
ids, always targets slot zero, and a successful selection grants the ability,
assigns it, confirms that participant, and seals readiness after the final
participant chooses. The runtime owns this coordinator with the other per-run
ability state. Concrete class pools and their UI presentation are not wired yet.
Starting pools use a separate stable character-archetype id rather than a
character-instance id. `RunStartingAbilityPoolRegistry` rejects the complete
configuration when pool/archetype ids are malformed or duplicated, a pool does
not contain exactly three unique abilities, or an ability is absent from the
main ability-definition resolver. Abilities may intentionally be shared between
archetypes for universal starter options. Concrete ScriptableObject pool assets
and their Hub/run-scene wiring are later stages. Inspector authoring is provided
by `RunStartingAbilityPoolDefinition` and `RunStartingAbilityPoolCatalog`; the
catalog cross-validates every referenced ability against the main
`AbilityDefinitionCatalog` and never exposes a partially valid pool set.
`RunParticipantSelection` carries an optional stable character-archetype id in
addition to player and character-instance ids. When a participant enters a new
session without a seeded ability but with an archetype, `GameSessionRuntime`
resolves that archetype and creates the deterministic `choice:starting` offer;
the UI must only present the resulting pending choice. The current Hub still
sends both `archetype:warrior` and the transitional Spin seed, so it produces no
pending choice and existing gameplay remains unchanged. The persistent host is
ready to accept an optional starting-pool catalog, rejects a mismatched or
invalid catalog, and passes a valid resolver into the session runtime. The Hub
scene does not reference a concrete starting-pool catalog yet.
`HubStartingAbilitySelectionPresenter` is the UI boundary for a pending starter
choice. It accepts only `choice:starting` targeting slot zero with exactly three
resolvable options, preserves their rolled order, and maps optional
`IAbilityPresentationDefinition` metadata into immutable view data. Missing
presentation metadata falls back to the stable ability id; UI code must not
reconstruct, reroll, or mutate the choice.
`HubStartingAbilitySelectionCoordinator` retains only the currently presented
local choice, rejects invalid or unoffered UI submissions before mutation, and
delegates the accepted stable id to `RunStartingAbilitySelectionService`.
Successful selection clears its presentation state; the domain service remains
the authority for loadout assignment and readiness sealing.
`HubStartingAbilitySelectionView` is a passive three-option Unity view. It
renders immutable presentation data, emits only the selected option index, and
owns no ability ids, choice rules, or session mutations.
`HubRunLaunchController` creates the run session first, then loads the run scene
only after that session's start-readiness roster is sealed. Seeded Spin keeps
the current immediate path; an unseeded participant raises a starting-choice
request and waits without polling. `HubStartingAbilitySelectionController`
builds the local coordinator from that session's services, presents its pending
choice, and submits the chosen option. `HubScene` contains the wired dormant
three-card overlay; the current seeded-Spin path leaves it hidden. `Install
Starting Ability Selection UI` is the targeted idempotent editor command for
that wiring and has a separate read-only validator.

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
Ability definitions expose their stable ids through `IAbilityDefinition`.
`AbilityDefinitionRegistry` rejects the entire definition set when any entry is
missing, malformed, or duplicated. `AbilitySlotDefinitionResolver` joins a live
participant slot source to that registry, so replacing a run slot is visible to
an already-bound executor without rebuilding its state. `PlayerAbilityExecutor`
can use this binding while retaining its current serialized Spin fallback for
direct scene testing. `AbilityDefinitionCatalog.asset` is the project resolver;
the persistent session host validates it before a Hub launch can begin.

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
- `ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset` — runtime lookup
  for stable ability ids used by participant run slots;
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
Use Ability Definition Resolution validation when stable-id definition lookup or
live run-slot resolution changes.
Use Ability Definition Catalog Wiring validation when the persistent catalog,
Hub starting ability, or run-entry executor binding changes.
Use Run Ability Choices validation when deterministic ability offers, selection,
or their session lifetime changes.
Use Run Start Ability Readiness validation when starter confirmation, the
future co-op ready barrier, or its session lifetime changes.
Use Starting Ability Selection validation when three-option starter offers or
their loadout/readiness coordination changes.
Use Starting Ability Pools validation when archetype resolution or pool/catalog
integrity changes.
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
