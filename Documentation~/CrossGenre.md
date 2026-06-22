# Cross-Genre Reuse

## Idle Auto Defense

A tower module can be an `AttackSourceId`. The game supplies nearby enemies as candidates with a nearest/highest-priority score. Attacks selects deterministically and emits a direct damage request.

Idle income, central-core state, radial spawning, upgrades, and offline progression remain outside Attacks.

## Classic Tower Defense

A tower can be an attack source. The game supplies lane enemies with path-progress or priority scores. Attacks emits a direct attack intent or, later, an intent consumed by a projectile package.

Placement, build/sell, grids, pathfinding, economy, and projectile movement stay outside Attacks.
