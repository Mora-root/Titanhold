# Prototype Status

This document summarizes the current prototype state for daily development.

## Working Prototype Loops

- Player movement.
- Targeting.
- Basic combat.
- Health.
- Enemy death.
- Enemy death -> active Main Camp threat.
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
- `EnemyDeathThreatListener`
- `ThreatMeter`
- `ThreatPendingState`
- `CampDefenseWaveController`
- `CampDefenseEnemySpawner`
- `CampDefenseEnemyRegistry`
- `CampDefenseResultState`
- `CampDefenseResolutionController`
- `CampBrokenState`
- `CampCore`

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
