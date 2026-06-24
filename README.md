# Deucarian Attacks

`com.deucarian.attacks` is a pure C# attack orchestration package for fixed-tick cooldowns, caller-supplied target candidates, deterministic target selection, attack intents, and Combat damage request creation.

Attacks answers who can attack, when they can attack, which supplied candidate should be attacked, and what Combat request should be produced. It does not query scenes, move projectiles, equip weapons, render VFX/audio/UI, grant rewards, save files, place towers, or depend on Defense Games.

## Attack Authoring

The package also includes a Unity-facing authoring layer in `Deucarian.Attacks.Authoring` and an editor-only wizard at `Deucarian/Game Content Authoring`.

An authored attack is a root `AttackDefinitionAsset` plus focused section assets:

- `AttackMechanicsDefinitionAsset` for cooldown, range, damage amount, and damage type.
- `AttackTargetingDefinitionAsset` for nearest, random, strongest, lowest-health, and forward-direction intent.
- `AttackDeliveryDefinitionAsset` for projectile, hitscan, area, and aura delivery data.
- `AttackStatusEffectsDefinitionAsset` for Combat status definitions and application requests.
- `AttackPresentationDefinitionAsset` for OnCast, OnFire, OnImpact, OnTick, and OnExpire audio/VFX hooks.

The wizard creates these sections as sub-assets under one root asset so designers work with one attack in the Project window without forcing every field into a single monolithic inspector. Runtime consumers convert the root asset to the existing pure C# `AttackDefinition` with `ToRuntimeDefinition()`, and optional presentation is invoked through `AttackPresentationRuntimeInvoker`.

Created assets use this default layout:

```text
Assets/GameContent/Attacks/{AttackId}/
└── {AttackId}_AttackDefinition.asset
    ├── {AttackId}_Mechanics
    ├── {AttackId}_Targeting
    ├── {AttackId}_Delivery
    ├── {AttackId}_StatusEffects
    └── {AttackId}_Presentation
```

The wizard validates required IDs, duplicate attack IDs, invalid numeric values, projectile prefab/model requirements, hitscan VFX requirements, duplicate status IDs, existing asset paths, and output paths before creating assets. It refuses accidental overwrites and asks before writing into an existing attack folder. Optional audio/VFX references are safe to leave empty; runtime presentation calls simply skip missing assets.

Next authoring providers should follow the same registry shape used by the wizard: Enemy, Wave, Upgrade, Tower/Weapon, VFX/Audio preset, and later shared status-effect or ability recipes.
