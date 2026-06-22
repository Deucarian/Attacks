# Combat Integration

Attacks creates Combat requests. Combat resolves them.

Flow:

1. Game layer registers an attack source.
2. Game layer supplies target candidates.
3. `AttackRuntime.TryAttack` selects a target and emits `AttackIntent`.
4. Game layer passes `AttackIntent.ResolutionRequest` to `CombatDamageResolver.Resolve`.
5. Combat mutates `HealthState` and returns damage results.

Attacks does not duplicate Combat damage rules.
