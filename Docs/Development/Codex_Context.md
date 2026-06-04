# Codex Working Context

Short context for small implementation tasks.

For small tasks, read `AGENTS.md`, this file, and directly relevant source files.

Read the full GDD / Architecture docs only for design or architecture changes.

## Camp Defense

Camp Crystal / CampCore is the primary camp-defense command object and protected object.

Threat belongs to the active Main Camp / Camp Crystal resonance, not to a scene, location, or act.

Current camp-defense loop:

```text
enemy deaths
-> threat
-> pending
-> Camp Crystal UI
-> start wave
-> spawn wave enemies
-> victory / defeat
-> resolution / recovery
```

## Enemies

World enemies and wave enemies are separate prefabs.

World enemies:

- spawned by `WorldEnemySpawnZone`;
- can have `EnemyThreatSource`;
- can have `EnemyRewardSource`.

Wave enemies:

- spawned by `CampDefenseEnemySpawner`;
- use `WaveEnemyTargetProvider`;
- target `CampCoreTarget` with local aggro override;
- usually do not have `EnemyThreatSource`;
- can have `EnemyRewardSource`.

Enemy death flow:

```text
Health.OnDeath
-> EnemyBrain stops AI / movement / sensor and disables root collider
-> EnemyAnimator plays Death
-> EnemyDeathNotifier notifies systems
-> EnemyDeathDespawn destroys corpse after delay
```

## Progression

`PlayerExperience` owns runtime XP and level.

`PlayerInfo` owns profile/static display data, such as player name.

`PlayerHUDController` shows:

- player name from `PlayerInfo`;
- player level from `PlayerExperience`.

Bottom XP HUD shows XP current / required only, without level text.

Loot uses independent `EnemyLootDropper` entries, optional `LootDropMotion`, universal `LootPickup`, and reward components.

Gold uses `PlayerGold`; Materials/Trophies use slot-based `PlayerLootInventory` with `LootItemDefinition`.

Inventory UI is a visible 7-column slot grid with insertion-order fill, hover tooltip, and temporary I-key debug opener.

Input TODO: future input contexts / command routing should let UI capture pointer input during drag/drop and block gameplay move/target intents until release.

## Temporary / Legacy

Debug helpers are temporary and should not become player-facing gameplay foundations.

Legacy `WaveSpawner`, `Tower`, and `Projectile` systems are not the basis for the current architecture.
