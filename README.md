# Deucarian Attacks

`com.deucarian.attacks` is a pure C# attack orchestration package for fixed-tick cooldowns, caller-supplied target candidates, deterministic target selection, attack intents, and Combat damage request creation.

Attacks answers who can attack, when they can attack, which supplied candidate should be attacked, and what Combat request should be produced. It does not query scenes, move projectiles, equip weapons, render VFX/audio/UI, grant rewards, save files, place towers, or depend on Defense Games.

## Game Content Authoring

The package also includes a Unity-facing authoring layer in `Deucarian.Attacks.Authoring` and editor-only Attack, Enemy, and Wave providers for `com.deucarian.game-content-authoring`. Install Attacks and open `Deucarian/Game Content Authoring`; the shared window comes from `com.deucarian.game-content-authoring`, while the domain-specific creation logic stays in Attacks.

An authored attack is a root `AttackDefinitionAsset` plus focused section assets:

- `AttackMechanicsDefinitionAsset` for cooldown, range, damage amount, and damage type.
- `AttackTargetingDefinitionAsset` for nearest, random, strongest, lowest-health, and forward-direction intent.
- `AttackDeliveryDefinitionAsset` for projectile, hitscan, area, and aura delivery data.
- `AttackStatusEffectsDefinitionAsset` for Combat status definitions and application requests.
- `AttackPresentationDefinitionAsset` for OnCast, OnFire, OnImpact, OnTick, and OnExpire audio/VFX hooks.

The shared window creates these sections as sub-assets under one root asset so designers work with one attack in the Project window without forcing every field into a single monolithic inspector. Runtime consumers convert the root asset to the existing pure C# `AttackDefinition` with `ToRuntimeDefinition()`, and optional presentation is invoked through `AttackPresentationRuntimeInvoker`.

Authored enemies and waves follow the same root-plus-section pattern:

- `EnemyDefinitionAsset` owns identity, role, tags, and links to `EnemyStatsDefinitionAsset` plus `EnemyPresentationDefinitionAsset`.
- `WaveDefinitionAsset` owns identity, tags, and links to `WaveScheduleDefinitionAsset` plus `WaveEntriesDefinitionAsset`.
- `EnemyPresentationRuntimeInvoker` treats prefab, audio, and VFX hooks as optional; missing optional assets are skipped.

Created assets use this default layout:

```text
Assets/GameContent/Attacks/{AttackId}/
└── {AttackId}_AttackDefinition.asset
    ├── {AttackId}_Mechanics
    ├── {AttackId}_Targeting
    ├── {AttackId}_Delivery
    ├── {AttackId}_StatusEffects
    └── {AttackId}_Presentation

Assets/GameContent/Enemies/{EnemyId}/
└── {EnemyId}_EnemyDefinition.asset
    ├── {EnemyId}_Stats
    └── {EnemyId}_Presentation

Assets/GameContent/Waves/{WaveId}/
└── {WaveId}_WaveDefinition.asset
    ├── {WaveId}_Schedule
    └── {WaveId}_Entries
```

The providers validate required IDs, duplicate IDs, invalid numeric values, missing required enemy prefabs, missing wave enemy references, duplicate status IDs, existing asset paths, and output paths before creating assets. They refuse accidental overwrites and ask before writing into an existing content folder. Optional audio/VFX references are safe to leave empty; runtime presentation calls simply skip missing assets.

Next authoring providers should follow the same registry shape used by the wizard: Upgrade, Tower/Weapon, Loot, shared Status Effect, Ability, and VFX/Audio preset providers.
