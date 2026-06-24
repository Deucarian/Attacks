# Game Content Authoring

Game content authoring uses a root asset plus focused sections instead of one large asset with unrelated fields. The root asset is the designer-facing object. Its section assets keep gameplay data, presentation data, and scheduling data independent enough to understand and validate.

## Why Sub-Assets

The wizard creates section objects as sub-assets of the root asset. That keeps the Project window workflow clean: designers select one attack, enemy, or wave, but Unity still serializes clear focused objects. Separate files can be introduced later for sections that benefit from reuse across many assets, such as audio/VFX presets or shared status effects.

Package samples may store the same sections as sibling `.asset` files because `Samples~` content is copied into a project by Unity. The authoring wizard creates true sub-assets for normal project content under `Assets/...`.

## Attack Fields

- Identity: stable string ID, display name, icon, tags, upgrade hook metadata, and notes.
- Mechanics: cooldown ticks, range, damage amount, and damage type ID.
- Targeting: nearest, random, strongest, lowest-health, or forward-direction intent. The runtime still receives caller-supplied candidates and scores.
- Delivery: projectile, hitscan, area, or aura. Projectile recipes include projectile ID, spawnable ID, prefab/model, speed, lifetime, homing, turn rate, pierce, and radius.
- Status effects: Combat status IDs, duration, tick rate metadata, strength, stacks, stacking policy, and placeholder effect notes.
- Presentation: OnCast, OnFire, OnImpact, OnTick, and OnExpire event entries with optional audio clips, VFX prefabs, and spawn point roles.

## Enemy Fields

- Identity: stable string ID, display name, icon, tags, and role (`Basic`, `Fast`, `Tank`, `Swarm`, or `Boss`).
- Stats: maximum health, move speed, reward value, contact damage, damage type ID, and collision radius.
- Presentation: enemy prefab/model plus optional OnSpawn, OnHit, and OnDeath audio/VFX hooks.

## Wave Fields

- Identity: stable string ID, display name, and tags.
- Schedule: start tick.
- Entries: enemy definition reference, count, batch size, initial delay ticks, interval ticks, spawn channel/lane, and difficulty tier.

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

## Creating An Enemy

1. Open `Deucarian/Game Content Authoring`.
2. Select `Enemy`.
3. Set a stable enemy ID such as `enemy.project.fast`.
4. Choose an output root under `Assets`, usually `Assets/GameContent/Enemies`.
5. Assign a prefab or model reference, then fill in health, speed, reward, contact damage, and collision radius.
6. Add optional spawn, hit, and death audio/VFX references.
7. Review the preview list and validation summary.
8. Press `Create Enemy Asset`.

The default output is:

```text
Assets/GameContent/Enemies/{EnemyId}/{EnemyId}_EnemyDefinition.asset
```

The wizard blocks duplicate enemy IDs, invalid stats, missing required prefabs, invalid paths, and existing root assets. It asks before writing into a non-empty enemy folder.

## Creating A Wave

1. Open `Deucarian/Game Content Authoring`.
2. Select `Wave`.
3. Set a stable wave ID such as `wave.project.early`.
4. Choose an output root under `Assets`, usually `Assets/GameContent/Waves`.
5. Add one or more entries and assign an `EnemyDefinitionAsset` to each entry.
6. Set count, batch size, initial delay, interval, spawn channel/lane, and difficulty tier.
7. Review the preview list and validation summary.
8. Press `Create Wave Asset`.

The default output is:

```text
Assets/GameContent/Waves/{WaveId}/{WaveId}_WaveDefinition.asset
```

The wizard blocks missing enemy references, empty entry lists, invalid counts or timings, duplicate wave IDs, invalid paths, and existing root assets. It asks before writing into a non-empty wave folder.

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

Enemy and wave assets are intentionally package-neutral data. Templates or gameplay glue should validate them, convert them into the owning gameplay package definitions, and keep fallbacks for incomplete assigned content. The Idle Auto Defense template demonstrates this by converting `EnemyDefinitionAsset` into auto-defense enemy definitions and `WaveDefinitionAsset` into Encounter waves/spawn groups.

## Current Limits

- Targeting modes are authoring intent. Runtime selection still depends on caller-supplied candidates and scores.
- Projectile, hitscan, area, and aura are data shapes here; gameplay packages decide how to move projectiles, trace beams, apply area pulses, and schedule auras.
- Status recipes currently create Combat status definitions and application requests. Rich stat modifiers and ticking effect logic are planned as shared status/effect authoring.
- Presentation is intentionally optional and best-effort. Missing clips, VFX prefabs, and model references are skipped instead of throwing.
- Wave entries currently reference authored enemies directly and assume the consuming template knows how to map spawn channels.
- Enemy armor/resistance is not exposed until a shared runtime package owns that behavior.
- Sample audio is intentionally empty; audio fields are ready for projects that provide clips.

## Content Authoring Roadmap

Next planned authoring providers are Upgrade, Tower/Weapon, Loot, shared Status Effect, Ability, and VFX/Audio preset providers.
