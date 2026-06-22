# Donor Findings

Donor project inspected:

`C:\Repositories\JorisHoef\Codex-Attempted-Vampire-Project\Codex-Attempted-Vampire-Project`

Findings:

- Weapon definitions and loadout ownership are project content and should stay outside Attacks.
- Donor stats include attack damage, spell damage, cooldown multiplier, crit damage, projectile count, projectile speed, contact damage, and element-specific damage.
- Donor `PlayerActor` owns Unity lifecycle, UI/resource hooks, damage callbacks, class resources, and upgrade application.
- Donor health/damage logic includes barrier absorption and armor-style reduction, which belongs in Combat rather than Attacks.
- Donor projectile, VFX, UI, and inventory concerns are coupled to Unity actors and should remain project-side or move to later packages.

Clean mappings:

- donor cooldown -> `AttackCooldownState`
- donor attack stat multiplier -> `AttackSourceSnapshot.DamageMultiplier`
- donor target enemy -> `AttackTargetCandidate`
- donor direct hit -> `AttackIntent` plus Combat `DamageResolutionRequest`

Discarded assumptions:

- Attacks should not own projectile `Update`.
- Attacks should not own weapon loadout slots.
- Attacks should not format UI text.
- Attacks should not create/destroy Unity objects.
