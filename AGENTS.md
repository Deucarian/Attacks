# Deucarian Attacks Agent Notes

Package ID: `com.deucarian.attacks`
Repository: `Deucarian/Attacks`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Attack definitions, cooldown/readiness state, caller-supplied target candidate evaluation, attack intents, Combat damage request creation, Unity attack authoring assets, and Game Content Authoring providers for attack, enemy, and wave definitions.

Registered capabilities:
- None.

This package must not own:

- Combat damage resolution, health/shields/status effect simulation, projectile lifecycle, weapon slots/fire cadence, world spawning, movement/pathing, encounter scheduling, progression/rewards, persistence, UI, VFX/audio playback, or product-specific combat rules.

## Dependencies

Allowed dependency shape:

- Runtime attack orchestration may depend on Gameplay Foundation and Combat.
- Authoring/editor surfaces may depend on Editor and Game Content Authoring.

Required dependencies and why:

- `com.deucarian.gameplay-foundation`: shared gameplay IDs, validation reports, and deterministic primitives.
- `com.deucarian.combat`: damage request and combat-facing attack result types.
- `com.deucarian.editor`: shared editor shell/resources for authoring surfaces.
- `com.deucarian.game-content-authoring`: provider registration and validation UI for attack content.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- None.

## Policies

- Keep runtime attack orchestration pure and independent from GameObject spawning, movement, projectiles, weapons, templates, and defense-game frameworks.
- Keep Unity authoring assets in the authoring assembly and editor providers in the editor assembly.
- Do not add hard dependencies on Projectiles, Weapon Systems, Defense Games, Auto Defense, Progression, Persistence, UI, or template packages.
- Logging: Do not introduce direct Unity Debug calls.
- Unity object lifetime: Use Common only if production code directly owns transient Unity object cleanup.
- Testing: Test fixture teardown may use Unity `DestroyImmediate` directly.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, Package Installer fallback, and Bootstrap fallback together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.
