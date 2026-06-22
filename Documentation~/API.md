# Attacks API

`com.deucarian.attacks` is a pure C# orchestration layer for fixed-tick attack readiness, deterministic selection from caller-supplied targets, direct attack intents, and Combat request creation.

## Runtime

- `AttackRuntime` owns registered attack sources and cooldown state.
- `RegisterSource` adds or replaces a source and initializes cooldown state for known definitions.
- `Tick` advances all cooldowns by fixed ticks.
- `TryAttack` checks source state, attack definition, cooldown readiness, supplied candidates, and emits an `AttackIntent`.
- `CreateSnapshot` and `FromSnapshot` preserve source and cooldown state for deterministic replay.

## Definitions And Sources

- `AttackDefinitionId` identifies authored attack definitions.
- `AttackSourceId` identifies a runtime source such as player, tower, minion, trap, or module.
- `AttackDefinition` contains cooldown ticks, first readiness policy, target policy, damage type, base damage, optional defense/status data, and direct damage request data.
- `AttackSourceSnapshot` contains source identity, Combat source combatant ID, enabled state, Combat source snapshot, and damage multiplier.

## Targeting

Targets are supplied by callers as `AttackTargetCandidate` values. Each candidate contains a Combatant ID, Combat `HealthState`, score, validity flag, and optional Combat defense snapshot.

`DeterministicAttackTargetSelector` ignores invalid candidates, prefers highest score by default, and breaks equal-score ties by stable `CombatantId` ordering. `AttackTargetPolicy.LowestScore` is available for path-progress, nearest-distance, or other caller-defined scoring schemes.

## Combat Boundary

`DirectDamageRequestFactory` creates:

- `DamageRequest`
- `DamageResolutionRequest`

Combat remains responsible for resolution through `CombatDamageResolver.Resolve`. Attacks does not apply damage, mutate health, calculate armor/resistance, absorb shields, decide death, or apply status rules.

## Extension Points

- `IAttackTargetSelector`
- `IAttackDamageRequestFactory`

Use these when game-specific target priority or request construction is needed. They should still consume supplied data and avoid world queries.
