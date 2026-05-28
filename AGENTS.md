# Titanhold — AI Agent Instructions

Titanhold is a Unity isometric ARPG/RPG prototype with camp defense, Threat Meter, character progression, loot, and future multiplayer-friendly architecture.

This file defines how AI agents such as Codex should work with the project.

Before making non-trivial changes, read:

- `Docs/GDD/GDD_Current.md`
- `Docs/Architecture/Architecture_Principles.md`
- `Docs/Architecture/Unity_Codex_Workflow.md`

## Project Identity

Titanhold is primarily an ARPG/RPG. Camp defense is an important pressure layer, but it is not the main genre.

The core fantasy:

The player explores dangerous locations, fights enemies, collects loot and resources, and becomes stronger. Player actions increase the Threat Meter. When the threat becomes critical, enemies attack the active camp.

## Current Development Focus

The current focus is the first playable foundation:

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

Do not implement large RPG systems before the core loop is proven fun.

## Core Working Rules

1. Start with read-only analysis unless the user explicitly asks for changes.
2. Keep changes small, safe, and reviewable.
3. Before non-trivial changes, explain:
   - the current problem;
   - proposed solution;
   - files to modify;
   - risk level.
4. Wait for confirmation before changing important code or Unity assets.
5. After code changes, check Unity Console through MCP.
6. If the implementation conflicts with the GDD, ask before changing design intent.
7. Prefer clean, practical code over over-engineered abstractions.
8. Do not introduce large architecture rewrites in one step.
9. Do not add networking code yet, but keep gameplay logic multiplayer-friendly.
10. Do not leave noisy per-frame logs in normal gameplay code.

## Forbidden Without Explicit Confirmation

Do not modify the following without explicit user confirmation:

- Unity scenes
- Prefabs
- ScriptableObjects
- Project Settings
- Package manifest
- Build Settings
- `.meta` files
- Serialized references
- Large folder structure
- Imported assets

Do not delete assets or GameObjects unless explicitly requested.

## Unity MCP Usage

Unity MCP can inspect and modify the Unity Editor state.

Safe read-only actions:

- reading Unity Console;
- getting active scene info;
- inspecting GameObjects;
- reading project files;
- checking errors and warnings.

Potentially dangerous actions require confirmation:

- updating GameObjects;
- saving scenes;
- modifying prefabs;
- creating or deleting scenes;
- creating prefabs;
- modifying materials;
- adding packages;
- deleting objects.

## Coding Direction

Follow the project architecture principles:

- Input should produce commands or intents.
- MonoBehaviour should act as presentation/integration layer.
- Domain/application logic should not depend on Unity UI, camera, or physical input.
- StateMachine should execute behavior, not read physical input directly.
- Combat should not read input or search for targets.
- UI should react to events/state, not drive gameplay rules.
- Avoid global mutable state and large manager classes.

## When Unsure

If a task is ambiguous, prefer:

1. read-only analysis;
2. a short plan;
3. asking for confirmation.

Do not guess design intent when the GDD is unclear.
