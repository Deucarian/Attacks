# ADR-0001: Attacks Boundary

## Status

Accepted for 0.1.0.

## Decision

`com.deucarian.attacks` owns attack orchestration: attack source identity, attack definition identity, cooldown state, readiness checks, deterministic selection from caller-supplied target candidates, direct attack intents, and Combat damage request creation.

The package is named `attacks`, not `weapon-systems`, because the reusable concept needed before projectiles is attack intent and timing. Weapons, loadouts, inventory, ammo, reloads, equipment slots, and upgrades are higher-level ownership concepts and should compose attacks later.

## Dependencies

Runtime depends only on:

- `com.deucarian.gameplay-foundation`
- `com.deucarian.combat`

Runtime does not depend on Defense Games, Encounters, World Spawning, World Navigation, Progression, Persistence, UI packages, Unity.Entities, GameObjects, MonoBehaviours, scenes, physics, or networking.

## Definitions And State

`AttackDefinition` is authored/static data: cooldown ticks, first readiness policy, target policy, damage type, base damage, optional Combat source/defense snapshots, and optional status applications.

`AttackRuntime` owns mutable source and cooldown state. Runtime state can be snapshotted and reconstructed for deterministic replay.

## Cooldown Model

Cooldowns are fixed-tick integers. `Tick` advances registered attack states. A zero-cooldown attack remains ready after each use. First-attack readiness is explicit on the definition so content can choose immediate or delayed first use.

## Target Candidate Boundary

Attack targets are supplied by the caller. The package does not discover targets, query physics, inspect GameObjects, search scenes, understand lanes, know towers, or know enemies.

## Selection And Tie-Breaking

Target selection uses caller-provided candidate scores. Invalid candidates and non-finite scores are ignored. Higher score wins by default. Equal scores are resolved by stable `CombatantId` ordering so insertion order does not affect selection.

## Combat Boundary

Attacks creates `DamageRequest` and `DamageResolutionRequest` values. Combat remains responsible for damage type validation, critical resolution, armor, penetration, resistance, shield absorption, health damage, exact-zero death, overkill, status application, and mutation of `HealthState`.

## Direct, Hitscan, And Projectiles

0.1.0 emits direct/hitscan-style attack intents. A future projectile package can consume the same attack intent and create projectile simulation without moving projectile behavior into Attacks.

## Source Stats

Gameplay Foundation stats can be adapted into `AttackSourceSnapshot` and Combat source data. This package provides the data shapes and examples; project-specific stat naming remains outside runtime.

## Exclusions

Inventory, weapons, equipment, upgrades, projectile movement, projectile spawning, VFX, audio, UI, rewards, persistence, tower placement, pathfinding, and Defense Games lifecycles stay outside this package.

## Future Usage

Idle Auto Defense can register tower modules as attack sources and supply scored enemy candidates gathered by the game layer.

Classic Tower Defense can register towers as attack sources and supply lane enemies scored by game-authored path progress, distance, or priority.

Defense Games can supply active attackers or objectives to Attacks from the game layer, then interpret Combat results as killed, leaked, or damaged signals without a runtime dependency.

## Future ECS/Burst Boundary

An ECS/Burst package can mirror attack definitions and cooldown states into components/jobs later. This managed package remains the pure C# authoring and deterministic reference runtime.
