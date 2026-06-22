# Future Boundaries

## Projectile Package

A future projectile package can consume `AttackIntent` and own projectile spawning, movement, collision, lifetime, piercing, chaining, homing, visuals, and hit reporting.

## Weapon Systems Package

A future weapon-system package can own weapon definitions, loadout slots, equipment, ammo, reloads, inventory, upgrade drafting, and weapon-specific content. It should use Attacks for timing/intent and Combat for damage resolution.

## ECS/Burst

A future ECS package can mirror definitions, cooldown states, and candidate buffers into components/jobs. This package remains the managed reference implementation.
