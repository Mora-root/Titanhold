# Legacy Prototype Systems

The systems listed here belong to the old tower-defense prototype.

Do not use them as the foundation for the new camp-defense architecture.

## Old Tower-Defense Prototype

The following scripts are considered legacy/prototype code:

- `Core/WaveSpawner.cs`
- `Core/WaveConfig.cs`
- `Core/WaypointPath.cs`
- `Enemies/Old/*`
- `Enemies/Old/Enemy.cs`
- `Enemies/Old/EnemyAgentMovement.cs`
- `Enemies/Old/EnemyMovementOld.cs`
- `Towers/Tower.cs`
- `Towers/TowerConfig.cs`
- `Towers/Projectile.cs`
- `Towers/ProjectileConfig.cs`

Old tower, projectile, and enemy prefabs are also considered legacy if they still exist in the project.

## Current Camp-Defense Prototype Flow

Use the new camp-defense prototype chain instead:

`ThreatMeter -> ThreatPendingState -> CampDefenseWaveController -> CampDefenseEnemySpawner -> CampDefenseEnemyRegistry`

## Cleanup Rule

Moving these files into a `Legacy/` folder or deleting them must happen only as a separate confirmed step after checking scene and prefab references.
