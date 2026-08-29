# Prototype Status

This document summarizes the current prototype state for daily development.

> Direction note: the CampCore defense flow below describes the currently implemented legacy prototype. The active vertical-slice direction is now a run-based exploration → portal → separate Assault arena → return-to-location loop defined in `Docs/GDD/GDD_Current.md`. New run work must be built side-by-side and must not expand the legacy CampDefense controllers into the new foundation.

## Working Prototype Loops

- Player movement.
- Targeting.
- Basic combat.
- Health.
- Enemy death.
- Enemy death -> `EnemyThreatSource` on enemy prefab -> active Main Camp threat.
- Enemy death -> `EnemyRewardSource` on enemy prefab -> `PlayerExperience`.
- Threat -> pending wave.
- Start wave -> spawn enemies.
- Registry tracks alive enemies.
- Victory/defeat.
- Resolution/recovery.
- Camp Crystal UI for player-facing start/recovery actions.

Threat belongs to the currently active Main Camp.

Threat does not belong to the current scene, location, or act.

Threat Meter represents accumulated Camp Crystal resonance / charge for the active Main Camp.

## Camp Defense Loop

Current prototype chain:

```text
enemy death
-> active Main Camp threat
-> pending
-> start wave
-> spawn
-> registry
-> victory/defeat
-> resolution/recovery
```

Main components in the current loop:

- `EnemyDeathNotifier`
- `EnemyThreatSource`
- `EnemyRewardSource`
- `PlayerExperience`
- `PlayerExperienceHUD`
- `ThreatMeter`
- `ThreatPendingState`
- `CampDefenseWaveController`
- `CampDefenseEnemySpawner`
- `CampDefenseEnemyRegistry`
- `CampDefenseResultState`
- `CampDefenseResolutionController`
- `CampBrokenState`
- `CampCore`
- `CampCommandInteractable`
- `CampCrystalUIController`

Camp Crystal / CampCore is the primary camp-defense command object. In the prototype scene, this object may be named Camp Crystal.

Camp Crystal concentrates monster death energy / resonance, powers the camp, and is the main target of camp-defense enemies.

Camp Crystal / CampCore is now the primary player-facing camp command object.

The player interacts with Camp Crystal to open Camp Crystal UI.

Camp Crystal UI can:

- show threat current/max;
- show pending state;
- show wave state;
- show broken state;
- show CampCore health;
- start a pending camp-defense wave;
- restore a broken camp.

`CampCommandInteractable` now opens Camp Crystal UI instead of directly starting a wave or restoring the camp.

Player gameplay input is ignored while the pointer is over UI, so UI button clicks should not create movement commands.

Long-term, Camp Crystal UI can also open camp overview / management UI.

If Camp Crystal is destroyed, many camp buildings/functions stop working until recovery.

`CampCommandInteractable` may be used on the Camp Crystal object for the current MVP. In the prototype scene, Camp Crystal / CampCore is the intended player-facing start/recovery interactable.

Altar / Resonance Altar is future secondary building for wave modifiers/resources, not the primary start/restore object.

`EnemyThreatSource` is the current MVP path for enemy threat gain. It lives on enemy prefabs that should grant threat when they die.

MVP: any death of an enemy with `EnemyThreatSource` grants threat.

Future: threat gain should require player-attributed `DeathContext` / `DamageContext`.

World enemies may have `EnemyThreatSource`. Future wave enemies may use a separate prefab without `EnemyThreatSource` if wave enemies should not generate threat.

`EnemyDeathThreatListener` is an older temporary scene-level adapter. It should not be used as the foundation for respawn/spawn enemies because it only tracks notifiers found in the scene during its initialization.

`PlayerExperience` is the current MVP runtime XP component.

It currently:

- stores `CurrentLevel`;
- stores `CurrentExperience`;
- exposes `ExperienceToNextLevel`;
- exposes `AddExperience(int amount)`;
- can level up through `AddExperience(int amount)`;
- invokes `OnExperienceChanged` when XP changes;
- invokes `OnLevelChanged` when level changes.

MVP level progression:

- Level 1 starts at 0 XP;
- base XP to next level is 100;
- each next level requirement increases by 50;
- `CurrentExperience` is XP within the current level, not lifetime XP.

`EnemyRewardSource` is the current enemy-side XP reward adapter.

It currently:

- listens to `EnemyDeathNotifier.Died`;
- adds XP to `PlayerExperience`;
- uses an `experienceAmount` value on the enemy component.

MVP: any death of an enemy with `EnemyRewardSource` grants XP.

Future: XP gain should require player-attributed `DeathContext` / `DamageContext` before granting XP.

World enemies and wave enemies can both give XP through `EnemyRewardSource`.

`PlayerExperienceHUD` shows the current level and XP value in the HUD through TMP text.

Current XP HUD scope:

- text-only XP display;
- displays `XP: current / required`;
- does not duplicate level text in the bottom XP HUD;
- no XP bar;
- no level-up popup;
- no save/load.

Current MVP progression does not include:

- stat points;
- talent points;
- class rewards;
- level-up popup;
- XP bar;
- save/load.

`PlayerExperience` is the runtime source of XP and level.

`PlayerInfo` remains profile/static display data, such as player name.

`PlayerHUDController` composes both sources for player HUD display:

- player name comes from `PlayerInfo`;
- player level comes from `PlayerExperience.CurrentLevel`.

`PlayerInfo.Level` remains legacy/fallback only and should not be treated as the main runtime level source.

Threat and XP are separate systems:

- world enemies can give Threat through `EnemyThreatSource`;
- wave enemies usually should not give Threat;
- both world enemies and wave enemies can give XP through `EnemyRewardSource`.

Current tested behavior:

- world enemy death -> XP text grows and Threat grows;
- wave enemy death -> XP text grows, Threat does not grow.

Current enemy death flow:

- `Health.OnDeath` is raised.
- `EnemyBrain` stops the state machine, movement, and sensor logic.
- `EnemyBrain` disables the root gameplay collider.
- `EnemyAnimator` triggers the `Death` animation.
- `EnemyDeathNotifier` notifies external systems.
- `EnemyDeathDespawn` removes the corpse after a delay.

Current animator setup appears valid:

- `Death` trigger exists.
- `Any State -> Death_A` transition exists.
- The death transition has no exit time.
- `Death_A` uses a non-looping death clip.
- Current `despawnDelay` gives enough time for the death animation to play.

World enemy prefabs and camp-defense wave enemy prefabs should be separate long-term.

World enemies can use `EnemyThreatSource` and normal `EnemySensor` targeting.

For MVP location areas, world enemies should now be spawned through `WorldEnemySpawnZone`.

`WorldEnemySpawnZone` supports:

- one `enemyPrefab` per zone;
- `maxAlive` enemies per zone;
- random spawn positions inside `spawnRadius`;
- `NavMesh.SamplePosition` validation for spawn positions;
- per-death respawn timers.

Per-death respawn behavior:

- enemies killed together respawn together after the delay;
- enemies killed over time respawn with matching staggered intervals.

`WorldEnemyRespawnPoint` is now a simple/older point prototype and should not be the main world spawning model.

Camp-defense wave enemies should usually not have `EnemyThreatSource`, because the wave is already the result of accumulated threat.

Wave enemies use `WaveEnemyTargetProvider`.

Wave enemies target CampCore through `CampCoreTarget` as their primary objective.

Wave enemies may temporarily switch to nearby local aggro targets through `EnemySensor`.

Wave enemies need separate behavior focused on CampCore / camp attack logic.

`CampDefenseEnemySpawner` should use wave-specific enemy prefabs long-term, not generic world enemy prefabs.

Wave enemies still use `CampDefenseEnemySpawner` and are separate from world spawn zones.

For the current MVP, shared prefab usage is acceptable temporarily only for testing.

Current MVP still uses `AimPoint` for CampCore targeting. Large target approach points / closest-point targeting are future scope.

## Temporary Debug Helpers

These components are prototype helpers and should be replaced before production-facing gameplay:

- `CampDefenseDebugStarter` - starts a pending wave from a debug key.
- `CampCoreDebugDamage` - damages/kills CampCore from a debug key.
- `CampBrokenDebugRestorer` - calls camp recovery from a debug key.
- `CampDefenseDebugHUD` - shows current prototype state through TMP text.

Debug helpers are acceptable only for prototype scenes and local testing. Do not build player-facing gameplay on top of debug key input.

They still exist as temporary fallback/local testing tools only. Player-facing camp defense flow should go through Camp Crystal interaction and Camp Crystal UI.

## Legacy Tower-Defense Systems

The old tower-defense prototype should not be used as the foundation for the new camp-defense architecture.

Do not build new systems on top of:

- `WaveSpawner.cs`
- `WaveConfig.cs`
- `WaypointPath.cs`
- `Enemies/Old/*`
- `Enemy.cs`
- `EnemyAgentMovement.cs`
- `EnemyMovementOld.cs`
- `Tower.cs`
- `TowerConfig.cs`
- `Projectile.cs`
- `ProjectileConfig.cs`
- old tower/projectile/enemy prefabs, if they still exist

## Next Direction

Replace debug keys with player-facing flows:

- interactable Camp Crystal / CampCore command object;
- player-facing start-wave action;
- player-facing camp recovery action;
- minimal UI feedback for threat, pending wave, camp broken state, and camp defense result;
- next implementation direction should replace prototype input with interaction-driven gameplay, not expand debug helpers.

Next intended enemy work:

- visually verify and polish `Death_A` animation in Play Mode;
- later add loot;
- consider pooling only after enemy lifecycle is stable;
- keep wave enemies and world enemies separate.

Future progression work:

- Character Info panel can show level/details;
- eventually remove or replace old `PlayerInfo.Level` if no longer needed;
- XP bar;
- level-up thresholds;
- level-up rewards;
- stat/talent points;
- player-attributed reward routing;
- save/load.
