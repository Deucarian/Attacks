# Deucarian Attacks

`com.deucarian.attacks` is a pure C# attack orchestration package for fixed-tick cooldowns, caller-supplied target candidates, deterministic target selection, attack intents, and Combat damage request creation.

Attacks answers who can attack, when they can attack, which supplied candidate should be attacked, and what Combat request should be produced. It does not query scenes, move projectiles, equip weapons, render VFX/audio/UI, grant rewards, save files, place towers, or depend on Defense Games.

## Game Content Authoring

The package also includes a Unity-facing authoring layer in `Deucarian.Attacks.Authoring` and editor-only Attack, Enemy, and Wave providers for `com.deucarian.game-content-authoring`. Install Attacks and open `Tools/Deucarian/Tools & Quality/Game Content Authoring`; the shared window comes from `com.deucarian.game-content-authoring`, while the domain-specific creation logic stays in Attacks.

Those providers are now reusable pack-aware lenses. `Attacks`, `Enemies`, and `Wave / Encounter` inspect matching records from the globally selected pack through immutable typed projection contracts. They retain the stable provider IDs `com.deucarian.attacks.attack`, `com.deucarian.attacks.enemy`, and `com.deucarian.attacks.wave`; the broadened encounter label does not break old Wave registration. External JSON-backed records are clearly read-only and show their owner, source, references, validation, authored common values, and preview fallback. Selecting `Project Content` preserves the existing ScriptableObject create/edit forms and rich asset previews under `Assets/GameContent`.

Game/template editor packages provide `IGameContentRecordProjectionAdapter<AttackContentRecordProjection>`, `EnemyContentRecordProjection`, or `EncounterContentRecordProjection` adapters. Attacks does not depend on those games or parse their source formats. A record can be visible in several lenses while retaining the same canonical pack-scoped key.

An authored attack is a root `AttackDefinitionAsset` plus focused section assets:

- `AttackMechanicsDefinitionAsset` for cooldown, range, damage amount, and damage type.
- `AttackTargetingDefinitionAsset` for nearest, random, strongest, lowest-health, and forward-direction intent.
- `AttackDeliveryDefinitionAsset` for projectile, hitscan, area, and aura delivery data.
- `AttackStatusEffectsDefinitionAsset` for Combat status definitions and application requests.
- `AttackPresentationDefinitionAsset` for OnCast, OnFire, OnImpact, OnTick, and OnExpire audio/VFX hooks.

The shared window creates these sections as sub-assets under one root asset so designers work with one attack in the Project window without forcing every field into a single monolithic inspector. Runtime consumers convert the root asset to the existing pure C# `AttackDefinition` with `ToRuntimeDefinition()`, and optional presentation is invoked through `AttackPresentationRuntimeInvoker`.

The authoring window's rich preview panel is editor-only. Attack previews summarize damage, targeting, delivery, status effects, expected dummy-target results, and OnCast/OnFire/OnImpact/OnTick/OnExpire audio/VFX hooks. Enemy previews show prefab/model thumbnails, stats, role, reward, and spawn/hit/death presentation hooks. Wave previews show total enemy count, approximate duration, channels, difficulty tier, missing references, and a spawn timeline. Preview thumbnails use Unity editor asset previews, and preview audio is best-effort; missing optional audio, VFX, and model references are reported instead of throwing or dirtying scenes.

Authored enemies and waves follow the same root-plus-section pattern:

- `EnemyDefinitionAsset` owns identity, role, tags, and links to `EnemyStatsDefinitionAsset` plus `EnemyPresentationDefinitionAsset`.
- `WaveDefinitionAsset` owns identity, tags, and links to `WaveScheduleDefinitionAsset` plus `WaveEntriesDefinitionAsset`.
- `EnemyPresentationRuntimeInvoker` treats prefab, audio, and VFX hooks as optional; missing optional assets are skipped.

Created assets use this default layout:

```text
Assets/GameContent/Attacks/{AttackId}/
`-- {AttackId}_AttackDefinition.asset
    |-- {AttackId}_Mechanics
    |-- {AttackId}_Targeting
    |-- {AttackId}_Delivery
    |-- {AttackId}_StatusEffects
    `-- {AttackId}_Presentation

Assets/GameContent/Enemies/{EnemyId}/
`-- {EnemyId}_EnemyDefinition.asset
    |-- {EnemyId}_Stats
    `-- {EnemyId}_Presentation

Assets/GameContent/Waves/{WaveId}/
`-- {WaveId}_WaveDefinition.asset
    |-- {WaveId}_Schedule
    `-- {WaveId}_Entries
```

The providers validate required IDs, duplicate IDs, invalid numeric values, missing required enemy prefabs, missing wave enemy references, duplicate status IDs, existing asset paths, and output paths before creating assets. They refuse accidental overwrites and ask before writing into an existing content folder. Optional audio/VFX references are safe to leave empty; runtime presentation calls simply skip missing assets.

Additional authoring providers should follow the same pack-aware registry and projection shape without creating parallel content stores.

## Install

Stable:

```json
"com.deucarian.attacks": "https://github.com/Deucarian/Attacks.git#main"
```

Development:

```json
"com.deucarian.attacks": "https://github.com/Deucarian/Attacks.git#develop"
```

Use `#main` for stable package consumption and `#develop` when testing active package work.

## When To Use This

Use this package when you need Pure C# attack orchestration plus Unity game content authoring assets and authoring providers.

Do not use this package to take ownership of capabilities outside its `AGENTS.md` boundary. Reusable behavior should stay with the package that owns that capability in the Package Registry governance docs.

## Quick Start

1. Install the package through Deucarian Package Installer or Unity Package Manager using the URL above.
2. Let Unity finish resolving packages and compiling assemblies.
3. Import the `Attacks Minimal` sample if you want a working reference scene or setup.
4. Start from the package README sections above and the public runtime/editor APIs in this repository.

## Integrations

Direct Deucarian package dependencies:

- `com.deucarian.gameplay-foundation`
- `com.deucarian.combat`
- `com.deucarian.editor`
- `com.deucarian.game-content-authoring`

Install optional companion packages only when their owned capability is needed by production code, samples, or tests.

## Validation

Run the shared package validator from this repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Troubleshooting

- Package does not resolve: confirm the stable or development Git URL matches the Package Registry entry and that required Deucarian dependencies are installed.
- Unity compile errors after install: let Package Manager finish resolving dependencies, then check asmdef references against `package.json` dependencies.
- Behavior appears to belong in another package: consult `AGENTS.md` and the Package Registry governance docs before moving or duplicating code.

## License

MIT. See `LICENSE.md`.
