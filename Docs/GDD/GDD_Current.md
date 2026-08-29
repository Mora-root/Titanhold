# Titanhold — Current GDD Summary

This document is the current short-form design reference for implementation.

The existing CampCore defense code is an older prototype. It remains available as legacy code while the new run loop is built side-by-side, but it is not the foundation for the vertical slice.

## Game Identity

Titanhold is a solo-first isometric action RPG with run-based progression, loot, character building, exploration, and dedicated Assault encounters.

The first release is single-player. The architecture should allow future host-authoritative cooperative play for four players and later up to eight, without adding networking code now.

The intended combat rhythm is deliberate and readable rather than high-APM screen clearing. Positioning, target priority, cooldowns, build choices, and enemy composition should matter.

## Current Core Loop

```text
Run preparation
→ explore a location
→ kill enemies and collect RunXP, RunGold, and loot
→ fill the Threat Meter
→ open a portal to the Assault arena
→ optionally keep farming while Rift Instability grows
→ enter the portal
→ complete the Assault
→ receive the Assault chest and use the intermission shop
→ return to the same exploration location
→ repeat until the final encounter
→ settle the run and return to camp
```

The player protects no CampCore, crystal, tower, or other defense object in the current vertical-slice direction. A protected-object activity may be reconsidered later as separate content.

## Vertical Slice Goal

The first playable build should prove one complete cycle:

1. Explore one existing location.
2. Fight naturally respawning world enemies.
3. Gain RunXP, RunGold, and loot.
4. Fill Threat through eligible exploration kills.
5. Open a portal near the player.
6. Optionally continue farming and raise Rift Instability.
7. Enter a separate Assault arena.
8. Defeat an Assault encounter with gradual reinforcement batches.
9. Receive a simple reward.
10. Return to the same exploration location.

After this cycle works reliably, the slice expands to three exploration/Assault rounds in one biome. The third Assault ends with a mini-boss or the slice boss.

## Exploration and Threat

Threat belongs to the active run cycle, not to an enemy, scene object, camp building, or generic Health component.

Eligible naturally spawned exploration enemies contribute Threat when killed. Assault enemies and summoned enemies do not contribute Threat.

The final authoritative damage-resolution batch that fills Threat is resolved as one batch. Every eligible kill in that batch receives its ordinary rewards and contributes Threat; none of those same-batch kills increases Rift Instability.

When Threat reaches its maximum:

- Threat is locked at its maximum;
- a persistent portal appears near the player;
- exploration rewards and enemy respawning continue normally;
- subsequent eligible exploration kills add Rift Instability instead of Threat;
- the player chooses when to enter the portal.

Infinite post-portal farming is an allowed alternative strategy. It does not unlock the next difficulty or grant Assault chests and final encounter rewards. Its long-term reward rate should be balanced below successful full-run progression rather than blocked by special reward rules.

## Rift Instability

Rift Instability is a visible, run-cycle-owned accumulator.

Each eligible post-portal exploration kill grants instability points based on the enemy's authored escalation value. Ordinary enemies grant less; stronger enemies, support units, and elites may grant more.

Instability is presented in discrete levels so the player can read the danger clearly. Initial tuning target:

- 10 instability points per level;
- +10% Assault maximum health per level;
- +5% Assault damage per level.

Bonuses are additive from base values, not multiplicative per kill.

Newly respawned exploration enemies snapshot the current instability level when spawned. Enemies already alive do not silently gain health or damage during combat. Portal VFX, HUD feedback, and an enemy visual modifier should communicate the current level.

When the player enters the portal, the current instability level is snapshotted for the upcoming Assault. The snapshot affects all Assault participants, including elites and the round's mini-boss.

Rage and other class-specific resources keep their normal rules. Rage is not specially clamped at the portal and may decay naturally while the character is out of combat.

## Assault

An Assault is one encounter whose enemies appear in gradual batches. Assault enemies do not grant individual Threat and initially do not grant individual economic rewards. The completion reward is issued by the Assault chest.

The Assault completion timer starts when the encounter actually begins, not when Threat becomes full.

Completing the encounter quickly can increase its RunXP completion reward. Initial maximum speed multiplier target is approximately 1.2–1.3.

After the target completion time expires, one PressureEnrage effect begins growing without a gameplay cap. It increases enemy offensive and movement pressure so the encounter cannot be kited forever. Technical animation and navigation limits are still allowed.

Rift Instability and PressureEnrage are separate systems:

- Rift Instability is chosen through additional farming before entering the arena;
- PressureEnrage grows from taking too long inside the Assault.

## Returning to Exploration

After Assault completion and intermission, the player returns to the same exploration location instance.

The current implementation target preserves the location rather than rebuilding it as a fresh round. Threat and Rift Instability begin a new cycle. Individual enemies retain the stat snapshot with which they spawned; future respawns use the current cycle's instability state.

The exact Unity mechanism for suspending or continuing exploration simulation while the player is in the arena is an infrastructure decision and must not leak into the plain C# run model.

## Run Progression

CharacterLevel is permanent. RunLevel, RunXP, RunGold, run upgrades, run abilities, and run artifacts are temporary.

RunLevel choices pause the single-player simulation. Ability acquisition replaces an ordinary upgrade at configured milestone levels.

The active ability bar initially has five universal slots. The player begins a run by choosing one of three class starter abilities and acquires additional abilities at fixed RunLevel milestones from functional pools.

Permanent talents unlock new definitions into those pools. The selected run abilities reset when the run ends.

## Rewards and Persistence

Exploration enemies can grant RunXP, RunGold, and gear according to their reward definitions. Harder spots trade slower and riskier Threat progress for better XP and loot.

Assault completion creates a one-shot reward chest:

- RunXP is granted immediately on opening;
- RunGold is granted logically and may use visual auto-picked coins;
- rolled gear is emitted into the world;
- the chest disappears after opening.

The first save model uses one active checkpoint at an Assault/intermission boundary. Mid-round Continue Later rolls back to the last checkpoint. Reward instances use stable IDs and fixed payloads so loading cannot reroll them.

## Multiplayer-Friendly Boundaries

Do not implement networking yet. New gameplay code should nevertheless follow these boundaries:

- input and AI emit commands or intents;
- an authoritative application service validates and changes run state;
- gameplay state contains stable identifiers and serializable values where practical;
- UI, animation, VFX, scenes, and prefabs observe state but do not own rules;
- economy, rewards, Threat, Instability, combat, and transitions must be host-validatable later;
- no gameplay decision should depend on a local camera, pointer, animation event, or scene-only singleton.

## Current Implementation Priority

1. Pure C# Run Flow state and transition rules.
2. Player-attributed damage/death context and atomic kill batches.
3. Exploration kill integration for Threat and Rift Instability.
4. Portal transition and preservation of the exploration location.
5. New Assault controller and batch spawner side-by-side with legacy CampDefense.
6. Assault reward and return flow.
7. RunLevel and starter ability acquisition.
8. Save/checkpoint integration.

## Out of Scope for the First Build

- multiplayer implementation;
- the final five-to-six-round public demo structure;
- multiple biomes;
- full talent tree;
- full crafting and economy;
- procedural maps;
- gamepad controls;
- protected camp objects;
- production shop content;
- advanced boss phases;
- full save migration and account services.
