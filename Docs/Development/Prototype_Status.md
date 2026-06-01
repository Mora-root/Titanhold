# Prototype Status

This document summarizes the current prototype state for daily development.

## Working Prototype Loops

- Player movement.
- Targeting.
- Basic combat.
- Health.
- Enemy death.
- Enemy death -> `EnemyThreatSource` on enemy prefab -> active Main Camp threat.
- Threat -> pending wave.
- Start wave -> spawn enemies.
- Registry tracks alive enemies.
- Victory/defeat.
- Resolution/recovery.

Threat belongs to the currently active Main Camp.

Threat does not belong to the current scene, location, or act.

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
- `ThreatMeter`
- `ThreatPendingState`
- `CampDefenseWaveController`
- `CampDefenseEnemySpawner`
- `CampDefenseEnemyRegistry`
- `CampDefenseResultState`
- `CampDefenseResolutionController`
- `CampBrokenState`
- `CampCore`

`EnemyThreatSource` is the current MVP path for enemy threat gain. It lives on enemy prefabs that should grant threat when they die.

MVP: any death of an enemy with `EnemyThreatSource` grants threat.

Future: threat gain should require player-attributed `DeathContext` / `DamageContext`.

World enemies may have `EnemyThreatSource`. Future wave enemies may use a separate prefab without `EnemyThreatSource` if wave enemies should not generate threat.

`EnemyDeathThreatListener` is an older temporary scene-level adapter. It should not be used as the foundation for respawn/spawn enemies because it only tracks notifiers found in the scene during its initialization.

World enemy prefabs and camp-defense wave enemy prefabs should be separate long-term.

World enemies may have `EnemyThreatSource` and generate active Main Camp threat.

Camp-defense wave enemies should usually not have `EnemyThreatSource`, because the wave is already the result of accumulated threat.

Wave enemies need separate behavior focused on CampCore / camp attack logic.

`CampDefenseEnemySpawner` should use wave-specific enemy prefabs long-term, not generic world enemy prefabs.

For the current MVP, shared prefab usage is acceptable temporarily only for testing.

## Temporary Debug Helpers

These components are prototype helpers and should be replaced before production-facing gameplay:

- `CampDefenseDebugStarter` - starts a pending wave from a debug key.
- `CampCoreDebugDamage` - damages/kills CampCore from a debug key.
- `CampBrokenDebugRestorer` - calls camp recovery from a debug key.
- `CampDefenseDebugHUD` - shows current prototype state through TMP text.

Debug helpers are acceptable only for prototype scenes and local testing. Do not build player-facing gameplay on top of debug key input.

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

- interactable camp altar or camp command object;
- player-facing start-wave action;
- player-facing camp recovery action;
- minimal UI feedback for threat, pending wave, camp broken state, and camp defense result;
- next implementation direction should replace prototype input with interaction-driven gameplay, not expand debug helpers.
