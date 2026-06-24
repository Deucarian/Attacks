# Attack Authoring

Attack authoring uses a root asset plus focused sections instead of one large asset with unrelated fields. The root `AttackDefinitionAsset` is the designer-facing object. Its section assets keep mechanics, targeting, delivery, status effects, and presentation independent enough to understand and validate.

## Why Sub-Assets

The wizard creates section objects as sub-assets of `{AttackId}_AttackDefinition.asset`. That keeps the Project window workflow clean: designers select one attack, but Unity still serializes clear focused objects. Separate files can be introduced later for sections that benefit from reuse across many attacks, such as audio/VFX presets or shared status effects.

## Fields

- Identity: stable string ID, display name, icon, tags, upgrade hook metadata, and notes.
- Mechanics: cooldown ticks, range, damage amount, and damage type ID.
- Targeting: nearest, random, strongest, lowest-health, or forward-direction intent. The runtime still receives caller-supplied candidates and scores.
- Delivery: projectile, hitscan, area, or aura. Projectile recipes include projectile ID, spawnable ID, prefab/model, speed, lifetime, homing, turn rate, pierce, and radius.
- Status effects: Combat status IDs, duration, tick rate metadata, strength, stacks, stacking policy, and placeholder effect notes.
- Presentation: OnCast, OnFire, OnImpact, OnTick, and OnExpire event entries with optional audio clips, VFX prefabs, and spawn point roles.

## Creating An Attack

1. Open `Deucarian/Game Content Authoring`.
2. Select `Attack`.
3. Fill in identity, mechanics, targeting, delivery, optional status effects, and presentation references.
4. Review the preview list and validation messages.
5. Press `Create Attack`.

The default output is:

```text
Assets/GameContent/Attacks/{AttackId}/{AttackId}_AttackDefinition.asset
```

## Runtime Consumption

Use `AttackDefinitionAsset.ToRuntimeDefinition()` to create the pure C# `AttackDefinition` consumed by `AttackRuntime`. Use `CreateStatusDefinitions()` when building the `CombatCatalog`. Optional audio/VFX references are invoked with `AttackPresentationRuntimeInvoker`; missing references do not fail the attack.

Projectile movement and spawning still belong to `com.deucarian.projectiles` and `com.deucarian.world-spawning`. A projectile recipe carries the IDs and tuning values those systems need, but the Attacks package does not take a runtime dependency on Projectiles.
