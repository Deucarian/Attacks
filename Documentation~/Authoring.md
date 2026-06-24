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
3. Set a stable attack ID. Use a namespaced value such as `attack.project.fire-orb`; this ID is what weapons and gameplay glue should reference.
4. Choose an output root under `Assets`, usually `Assets/GameContent/Attacks`.
5. Fill in mechanics, targeting, and delivery. Projectile creation requires a prefab/model reference in the wizard so the resulting recipe is immediately usable by template spawn glue.
6. Add optional status effects and presentation references. Audio and VFX are optional at runtime.
7. Review the preview list and validation summary.
8. Press `Create Attack Asset`.

The default output is:

```text
Assets/GameContent/Attacks/{AttackId}/{AttackId}_AttackDefinition.asset
```

The wizard refuses to overwrite an existing root asset. If the target attack folder already contains assets, it asks for confirmation before adding the new root asset there. Duplicate attack IDs are blocked across existing `AttackDefinitionAsset` assets so gameplay references stay stable.

## Runtime Consumption

Use `AttackDefinitionAsset.ToRuntimeDefinition()` to create the pure C# `AttackDefinition` consumed by `AttackRuntime`. Use `CreateStatusDefinitions()` when building the `CombatCatalog`. Optional audio/VFX references are invoked with `AttackPresentationRuntimeInvoker`; missing references do not fail the attack.

```csharp
AttackDefinitionAsset recipe = /* serialized asset reference */;
CombatCatalog catalog = new CombatCatalog(
    new[] { new DamageTypeDefinition(new DamageTypeId(recipe.Mechanics.DamageTypeId)) },
    recipe.CreateStatusDefinitions());

AttackDefinition attack = recipe.ToRuntimeDefinition();
AttackRuntime runtime = new AttackRuntime(catalog, new[] { attack });
```

Projectile movement and spawning still belong to `com.deucarian.projectiles` and `com.deucarian.world-spawning`. A projectile recipe carries the IDs and tuning values those systems need, but the Attacks package does not take a runtime dependency on Projectiles.

## Current Limits

- Targeting modes are authoring intent. Runtime selection still depends on caller-supplied candidates and scores.
- Projectile, hitscan, area, and aura are data shapes here; gameplay packages decide how to move projectiles, trace beams, apply area pulses, and schedule auras.
- Status recipes currently create Combat status definitions and application requests. Rich stat modifiers and ticking effect logic are planned as shared status/effect authoring.
- Presentation is intentionally optional and best-effort. Missing clips, VFX prefabs, and model references are skipped instead of throwing.
- Next planned authoring types are Enemy, Wave, Upgrade, Tower/Weapon, and VFX/Audio preset providers.
