# Titanhold — Architecture Principles

This document defines the architecture direction for Titanhold.

The goal is not academic purity. The goal is clean, practical, testable Unity code that can grow into a larger ARPG/RPG and remain friendly to future multiplayer.

## Architecture Direction

Use a practical Clean Architecture / Hexagonal Architecture approach where it provides value.

Keep the code:

- readable;
- simple;
- testable;
- modular;
- multiplayer-friendly;
- not over-engineered.

Avoid creating abstractions that do not solve a real current or near-future problem.

## Layers

### Domain

Domain contains pure gameplay rules and models.

Examples:

- health rules;
- damage rules;
- stat formulas;
- skill rules;
- threat rules;
- wave rules;
- item data models;
- camp progression rules.

Domain should avoid `UnityEngine` dependencies where possible.

Domain should not know about:

- UI;
- camera;
- physical input;
- Unity scenes;
- prefabs;
- animations;
- VFX;
- audio;
- MonoBehaviour lifecycle.

### Application

Application coordinates use cases, commands, and gameplay flow.

Examples:

- player action command handling;
- skill execution flow;
- combat orchestration;
- threat meter updates;
- wave start logic;
- inventory use cases;
- camp upgrade use cases.

Application may depend on domain abstractions.

Application should not depend directly on UI or scene objects unless wrapped by interfaces.

### Infrastructure

Infrastructure contains Unity-specific or external implementation details.

Examples:

- Unity Input System;
- legacy Unity input adapters;
- NavMeshAgent movement implementation;
- save/load;
- scene loading;
- Addressables;
- analytics;
- audio service;
- external SDKs;
- file system;
- MCP/editor tooling.

Infrastructure implements interfaces required by application/domain.

### Presentation

Presentation contains Unity scene-facing objects.

Examples:

- MonoBehaviour components;
- UI views;
- camera controllers;
- VFX;
- animation controllers;
- target markers;
- selection visuals;
- scene-only wiring.

Presentation may call application layer, but should avoid owning core gameplay rules.

## Core Rules

1. MonoBehaviour should not contain business logic unless the behavior is purely presentation or integration.
2. Input should produce commands or intents.
3. StateMachine should execute behavior, not read physical input directly.
4. Combat should not read input.
5. Combat should not search for targets.
6. Targeting should find/select targets, not apply combat logic.
7. UI should react to events/state, not drive gameplay rules.
8. Camera should not be required by domain or application logic.
9. Avoid God objects and large Manager classes.
10. Avoid global mutable singletons.
11. Prefer explicit dependencies.
12. Prefer composition over inheritance-heavy designs.
13. Use design patterns only when they simplify the code.
14. Keep methods short and focused.
15. Keep responsibilities clear.

## Input Architecture

Physical input should be converted into commands or intents.

Good flow:

Physical input  
→ input adapter  
→ command / intent  
→ PlayerBrain or application use case  
→ StateMachine / gameplay system  
→ movement, combat, skill, interaction

Bad flow:

Physical input  
→ combat/skill/stat system directly

Input systems should support future control modes:

- Mouse Mode;
- WASD Mode;
- Gamepad Mode later.

Control modes should not require rewriting combat, stats, skills, or targeting.

## Player State Machine

StateMachine should coordinate player behavior.

Possible states:

- `IdleState`
- `MoveState`
- `ApproachState`
- `AttackState`
- `SkillState`
- `CastState`
- `InteractState`
- `LootState`

State classes should not read keyboard or mouse input directly.

States may consume commands or intent data prepared by input/application layer.

State transitions should be explicit and easy to debug.

Avoid per-frame state transition spam in Unity Console.

## Movement

Movement should be separated from input.

Good:

- input creates movement intent;
- state machine decides whether movement is allowed;
- movement component executes movement.

Bad:

- input directly moves the character;
- combat directly moves the character;
- UI directly moves the character.

Unity-specific movement, such as `NavMeshAgent`, belongs to infrastructure/presentation implementation, not pure domain rules.

## Targeting

Targeting should be responsible for target detection and target state.

Targeting should not apply damage or execute combat.

Important target concepts:

- `InspectTarget`
- `CombatTarget`
- `CastTarget`

Combat and skills should receive targets from targeting/application flow, not search for them on their own.

## Combat

Combat should be responsible for combat execution, not input or UI.

Combat may handle:

- cooldown checks;
- attack start;
- animation event hit timing;
- damage application request;
- attack finish;
- explicit attack cancellation.

Combat should not:

- read physical input;
- raycast from camera;
- update UI directly;
- decide player movement;
- search for targets.

## Skills

Skill execution should be data-driven where practical.

Skill data may be stored in ScriptableObjects.

Skill logic should avoid being tied directly to UI, physical input, or scene-specific objects.

Target skills should use target selection rules defined by the application layer.

Cast-time skills should lock `CastTarget` when casting begins.

## Health and Damage

Health should be generic enough for:

- player;
- enemies;
- walls;
- towers;
- CampCore;
- destructible objects.

Health should handle:

- current health;
- max health;
- damage;
- healing;
- death;
- events.

Health should not directly handle:

- UI;
- loot;
- AI;
- animation;
- audio.

Other systems should react to Health events.

## Run Flow, Threat, and Rift Instability

The current vertical-slice direction owns Threat and Rift Instability inside the active run cycle.

They must not be tied directly to:

- Unity scenes;
- individual enemy objects;
- generic Health;
- UI;
- portals or VFX;
- legacy CampDefense or WaveSpawner components.

Generic Health must not know about rewards, Threat, or Rift Instability.

An enemy-specific adapter may translate an attributed, eligible death into a run application command. The authoritative run service decides whether that kill contributes Threat, contributes Rift Instability, or has no run-flow effect.

Expected flow:

```text
Attributed exploration kill batch
→ run application service
→ Threat while exploring
→ Rift Instability after the portal opens
→ immutable Assault scaling snapshot on portal entry
→ presentation and Unity adapters react to state changes
```

The final damage-resolution batch that fills Threat must be atomic. Kills from the same authoritative attack batch must not be split between Threat and Rift Instability because of listener order.

The plain C# run model owns phases and transitions. Scene loading, teleportation, portal visuals, encounter spawning, and preservation of the exploration location are infrastructure/presentation concerns driven by that model.

## ScriptableObject Usage

ScriptableObjects are useful for:

- character configs;
- enemy configs;
- skill data;
- item data;
- wave data;
- camp upgrade data.

Do not use ScriptableObjects as uncontrolled global mutable state.

Runtime state should be stored in runtime objects or save data, not directly in shared asset data unless explicitly designed.

## Logging Rules

1. Do not leave per-frame `Debug.Log` in normal gameplay code.
2. Transition logs are acceptable only behind debug flags or development-only tools.
3. Logs should help diagnose problems, not spam Unity Console.
4. Repeated logs from `Update`, `Tick`, or state loops should be avoided.
5. After removing logs, verify Unity Console remains useful and clean.

## Future Multiplayer-Friendly Rules

The game is single-player now, but architecture should not block future multiplayer.

Rules:

1. Treat player input as intent, not final truth.
2. Separate intent, validation, and execution.
3. Critical gameplay actions should be possible to validate server-side later.
4. Do not put important game rules only in UI or MonoBehaviour.
5. Do not assume there will only ever be one local player.
6. Do not hardcode gameplay systems to `Camera.main`, local input, or local player.
7. Do not add networking code before needed.
8. Avoid offline camp punishment logic.
9. Do not rely on client-only checks for future economy, inventory, damage, rewards, cooldowns, or camp state.
10. Keep data and visual representation separated.

## Design Patterns

Use patterns only when they solve a real problem.

Potentially useful patterns:

- Command — player actions, future server validation.
- State Machine — player/enemy/camp states.
- Strategy — interchangeable behavior.
- Factory — controlled object creation.
- Adapter — Unity API, input, save, external SDK isolation.
- Observer/Event — UI and systems reacting to gameplay events.
- Repository — save/data access when needed.

Avoid using patterns just to make the code look architectural.

## Refactoring Rules

When refactoring:

1. Preserve existing behavior unless the user asks to change it.
2. Change one responsibility at a time.
3. Keep the project compiling after each step.
4. Prefer small, reviewable commits.
5. Avoid large file moves unless necessary.
6. Do not modify scenes/prefabs to fix code architecture unless explicitly approved.
