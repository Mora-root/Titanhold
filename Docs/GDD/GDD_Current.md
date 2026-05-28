# Titanhold — Current GDD Summary

This document is the current short-form design reference for daily development.

The full GDD may contain more details, but this file defines the current practical direction for implementation.

## Game Identity

Titanhold is a Unity isometric ARPG/RPG with camp defense.

The game is not a pure tower defense and not a tower defense game with a hero. The main activity is ARPG gameplay:

- exploring locations;
- fighting enemies;
- gaining XP;
- collecting loot;
- gathering resources;
- completing quests;
- improving the hero;
- improving the camp;
- progressing through acts.

Camp defense is a pressure layer that periodically interrupts or complements exploration.

## Core Fantasy

The player explores dangerous locations and provokes danger through their actions.

The more active the player is, the more the Threat Meter grows. When the threat becomes critical, enemies attack the active camp.

The player must decide:

- continue exploring and risk being far from camp;
- return to prepare;
- manually start the wave;
- delay the wave if the camp has the required resources.

## Core Gameplay Loop

Camp preparation  
→ exploration  
→ combat  
→ XP / loot / resources  
→ Threat Meter growth  
→ pending wave warning  
→ return to camp or continue risking  
→ camp defense  
→ rewards or consequences  
→ upgrade hero and camp  
→ progress through act  
→ defeat act boss  
→ move Main Camp to next act

## Design Pillars

### 1. Conscious ARPG Combat

Titanhold should not be balanced around pure high-speed zoom-zoom farming.

The preferred combat and farming rhythm is more deliberate:

- the player explores the location;
- evaluates enemy groups;
- may gather a larger pack;
- uses positioning, cooldowns, and skill timing;
- kills the pack through prepared execution;
- or plays a build focused on quickly eliminating priority or single targets.

Both AoE pack-clearing and priority-target / single-target playstyles should be viable.

The optimal strategy should not become infinite full-map pulling. Enemy types, ranged units, elites, aggro rules, cooldowns, and Threat Meter should limit mindless mass pulling.

### 2. Threat Through Activity

Threat Meter grows because of player activity, not only because of time.

Threat can grow from:

- killing regular enemies;
- killing elite enemies;
- killing mini-bosses;
- completing events;
- progressing story actions;
- possibly slow passive growth during active gameplay.

This makes the world feel reactive.

### 3. Camp as Long-Term Progression

The Main Camp is not disposable.

The player develops long-term camp progress. When moving to a new act, the active camp moves forward, while previous camps become outposts.

Camp progression should be separated from physical camp layout:

- `CampProgressData` — what is unlocked and upgraded.
- `CampLayoutData` — how the current camp is physically placed in the current location.

### 4. Multiplayer-Friendly Foundation

The project is single-player for now.

However, input, combat, skills, stats, and camp systems should be designed so they do not block future server-authoritative multiplayer.

Do not add multiplayer code yet.

### 5. Practical Scope

The MVP must stay narrow.

Do not build full RPG systems before the core loop is proven fun.

The main question for the prototype:

Is the loop “fight enemies → grow threat → defend camp → improve hero/camp” fun?

## Current MVP Focus

The current development focus is:

- Player Movement
- Input abstraction
- StateMachine
- Targeting
- Health
- Basic Combat
- Basic Skill execution
- Threat Meter prototype
- CampCore prototype
- Simple Wave prototype

## MVP Vertical Slice Goal

The first playable vertical slice should prove this flow:

1. Player appears in a small test location.
2. Player moves around.
3. Player selects or attacks enemies.
4. Enemies take damage and die.
5. Threat Meter grows from enemy kills.
6. At 100%, wave becomes pending.
7. Player returns to camp.
8. Wave starts.
9. Enemies move toward CampCore.
10. Player defends CampCore.
11. Wave succeeds or fails.
12. Player receives a simple reward.

## Current Out of Scope

Do not implement these unless explicitly requested:

- multiplayer implementation;
- full talent tree;
- full inventory system;
- full crafting system;
- full quest system;
- full dialogue system;
- gamepad mode;
- risk instances;
- endgame systems;
- large procedural content;
- complex economy;
- full loot affix system;
- full class/subclass system.

## Input and Control Direction

The game should eventually support:

- Mouse Mode;
- WASD Mode;
- Gamepad Mode.

Core systems should not depend on specific physical buttons.

Physical input should be converted into abstract commands or intents, such as:

- `MoveCommand`
- `ActionCommand`
- `SelectionCommand`
- `ClearTargetCommand`
- `SkillSlotCommand`
- `InteractCommand`
- `CancelCommand`

Mouse Mode can support click-to-move and auto-approach.

WASD Mode should keep movement under direct player control. In WASD Mode, target skills should not automatically move the player to the target unless explicitly designed.

Gamepad Mode is out of scope for now.

## Targeting Direction

The design separates several target concepts:

- `InspectTarget` — the target shown in UI or inspected by the player.
- `CombatTarget` — the current combat target.
- `CastTarget` — the target locked when a cast begins.

These concepts may differ.

For example, the player may attack enemy A while inspecting enemy B.

Combat visuals and UI should make this distinction clear.

## Combat Direction

Combat should feel deliberate and readable.

Important principles:

- positioning matters;
- cooldown timing matters;
- target priority matters;
- enemy composition matters;
- not every skill should be spammed without cooldown;
- AoE builds and single-target builds should both have a purpose.

Basic enemy groups should be satisfying to clear, but the game should avoid becoming pure screen-clearing speed farming.

## Threat Meter Direction

Threat Meter belongs to the currently active Main Camp, not to a scene, location, act, enemy, or wave spawner.

Player actions anywhere in the world can increase threat for the currently active Main Camp as long as that camp has not been transferred.

The player may travel to previous act locations, future act locations, optional dungeons, event zones, or other exploration areas. Enemy kills and other threat sources still increase threat for the active Main Camp.

Changing location, scene, or act does not reset threat by itself.

Threat resets only when the player confirms the story/gameplay transfer of the Main Camp to a new active camp.

When camp transfer is confirmed:

- the previous Main Camp becomes inactive or becomes an outpost;
- the new Main Camp becomes the active Main Camp;
- current threat is reset;
- pending wave state is cleared;
- a new active camp threat cycle begins.

Threat can grow from:

- killing regular enemies;
- killing elite enemies;
- killing mini-bosses;
- completing events;
- progressing story actions;
- gathering important resources;
- activating special artifacts;
- possibly slow passive growth during active gameplay.

When Threat Meter reaches 100%:

- the wave becomes pending;
- the player receives a warning;
- the wave does not have to start instantly;
- the player may manually start it;
- camp resources or altar systems may delay it later.

Threat should create pressure, not constant annoyance.

For MVP, `ThreatMeter` may exist as a scene component in the prototype scene. Long-term, it should represent the active Main Camp threat and be saved/restored as part of game state.

## Camp Defense Direction

Waves are not strictly lane-based tower defense.

Enemies may spawn from:

- portals;
- rifts;
- lairs;
- invasion routes.

Enemies move toward the active camp, especially CampCore.

The player may help defend the camp directly.

Camp defenses may help, but the player character remains important.

## Failure Direction

If CampCore is destroyed:

- the wave fails;
- wave rewards are not granted;
- Threat Meter may be frozen;
- camp enters a broken state;
- the player must restore the camp.

Failure should be meaningful but not permanently destructive.

Do not punish offline camp destruction in future multiplayer scenarios.

## Development Priority

When choosing between systems, prioritize:

1. Core movement and control feel.
2. Clean input-to-intent pipeline.
3. StateMachine clarity.
4. Basic combat loop.
5. Targeting clarity.
6. Health/damage reliability.
7. Threat Meter prototype.
8. Camp defense prototype.

Do not prioritize advanced RPG features before the basic loop feels good.
