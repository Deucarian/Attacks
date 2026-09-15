using System;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Attacks.Editor
{
    internal static class AttackToolkitAuthoring
    {
        internal static VisualElement Create(GameContentAuthoringSurfaceContext context)
        {
            var asset = context.SelectedItem?.Asset as AttackDefinitionAsset;
            return GameContentToolkitDraftEditor.Create(context,
                () => asset == null ? new AttackAuthoringState() : AttackGameContentPreviewSelection.FromAttackAsset(asset),
                AttackAuthoringDraft.BuildStateFingerprint,
                state => asset == null ? AttackAuthoringSession.ValidateDraft(state) : AttackRecipeAssetCreator.ValidateForUpdate(state, asset),
                state => asset == null ? AttackRecipeAssetCreator.CreateAssets(state) : AttackRecipeAssetCreator.UpdateExistingAsset(asset, state),
                (root, state) => { Fields(root, state, context); AttackToolkitPreview.Add(root, state, context); });
        }
        private static void Fields(VisualElement root, AttackAuthoringState state, GameContentAuthoringSurfaceContext context)
        {
            var form = new DeucarianEditorWorkspaceForm(root);
            form.Number("DamageAmount", "Damage", () => state.DamageAmount, value => state.DamageAmount = value);
            form.IntegerWithSlider("CooldownTicks", "Cooldown (ticks)", 0, 120, () => state.CooldownTicks, value => state.CooldownTicks = value);
            form.NumberWithSlider("Range", "Range", 0, 20, () => state.Range, value => state.Range = value);
            form.Enum("TargetingMode", "Target", () => state.TargetingMode, value => state.TargetingMode = value);
            var advanced = form.Section("Advanced data", true);
            advanced.Text("AttackId", "Attack Id", () => state.AttackId, value => state.AttackId = value);
            advanced.Text("DisplayName", "Name", () => state.DisplayName, value => state.DisplayName = value);
            advanced.Asset("Icon", "Icon", typeof(Sprite), () => state.Icon, value => state.Icon = (Sprite)value);
            advanced.Text("TagsCsv", "Tags Csv", () => state.TagsCsv, value => state.TagsCsv = value);
            advanced.Text("OutputRoot", "Output Root", () => state.OutputRoot, value => state.OutputRoot = value);
            advanced.Text("DamageTypeId", "Damage Type Id", () => state.DamageTypeId, value => state.DamageTypeId = value);
            advanced.Enum("DeliveryMode", "Delivery Mode", () => state.DeliveryMode, value => { state.DeliveryMode = value; advanced.Refresh(); });
            advanced.Text("ProjectileDefinitionId", "Projectile Definition Id", () => state.ProjectileDefinitionId, value => state.ProjectileDefinitionId = value);
            advanced.Text("ProjectileSpawnableId", "Projectile Spawnable Id", () => state.ProjectileSpawnableId, value => state.ProjectileSpawnableId = value);
            advanced.Asset("ProjectilePrefab", "Projectile Prefab", typeof(GameObject), () => state.ProjectilePrefab, value => state.ProjectilePrefab = (GameObject)value);
            advanced.Number("ProjectileSpeed", "Projectile Speed", () => state.ProjectileSpeed, value => state.ProjectileSpeed = value);
            advanced.Integer("ProjectileLifetimeTicks", "Projectile Lifetime Ticks", () => state.ProjectileLifetimeTicks, value => state.ProjectileLifetimeTicks = value);
            advanced.Toggle("Homing", "Homing", () => state.Homing, value => { state.Homing = value; advanced.Refresh(); });
            advanced.Number("HomingTurnRate", "Homing Turn Rate", () => state.HomingTurnRate, value => state.HomingTurnRate = value);
            advanced.Integer("PierceCount", "Pierce Count", () => state.PierceCount, value => state.PierceCount = value);
            advanced.Number("Radius", "Radius", () => state.Radius, value => state.Radius = value);
            advanced.Asset("BeamVfxPrefab", "Beam Vfx Prefab", typeof(GameObject), () => state.BeamVfxPrefab, value => state.BeamVfxPrefab = (GameObject)value);
            advanced.Asset("ImpactVfxPrefab", "Impact Vfx Prefab", typeof(GameObject), () => state.ImpactVfxPrefab, value => state.ImpactVfxPrefab = (GameObject)value);
            advanced.Integer("MaxHits", "Max Hits", () => state.MaxHits, value => state.MaxHits = value);
            advanced.Number("TickIntervalSeconds", "Tick Interval Seconds", () => state.TickIntervalSeconds, value => state.TickIntervalSeconds = value);
            advanced.Toggle("IncludeStatusEffect", "Include Status Effect", () => state.IncludeStatusEffect, value => { state.IncludeStatusEffect = value; advanced.Refresh(); });
            advanced.Text("StatusId", "Status Id", () => state.StatusId, value => state.StatusId = value);
            advanced.Integer("StatusDurationTicks", "Status Duration Ticks", () => state.StatusDurationTicks, value => state.StatusDurationTicks = value);
            advanced.Integer("StatusTickRateTicks", "Status Tick Rate Ticks", () => state.StatusTickRateTicks, value => state.StatusTickRateTicks = value);
            advanced.Number("StatusStrength", "Status Strength", () => state.StatusStrength, value => state.StatusStrength = value);
            advanced.Integer("StatusMaxStacks", "Status Max Stacks", () => state.StatusMaxStacks, value => state.StatusMaxStacks = value);
            advanced.Enum("StatusStackingPolicy", "Status Stacking Policy", () => state.StatusStackingPolicy, value => state.StatusStackingPolicy = value);
            advanced.Text("StatusEffectNote", "Status Effect Note", () => state.StatusEffectNote, value => state.StatusEffectNote = value);
            advanced.Asset("CastAudio", "Cast Audio", typeof(AudioClip), () => state.CastAudio, value => state.CastAudio = (AudioClip)value);
            advanced.Asset("FireAudio", "Fire Audio", typeof(AudioClip), () => state.FireAudio, value => state.FireAudio = (AudioClip)value);
            advanced.Asset("ImpactAudio", "Impact Audio", typeof(AudioClip), () => state.ImpactAudio, value => state.ImpactAudio = (AudioClip)value);
            advanced.Asset("TickAudio", "Tick Audio", typeof(AudioClip), () => state.TickAudio, value => state.TickAudio = (AudioClip)value);
            advanced.Asset("ExpireAudio", "Expire Audio", typeof(AudioClip), () => state.ExpireAudio, value => state.ExpireAudio = (AudioClip)value);
            advanced.Asset("CastVfxPrefab", "Cast Vfx Prefab", typeof(GameObject), () => state.CastVfxPrefab, value => state.CastVfxPrefab = (GameObject)value);
            advanced.Asset("FireVfxPrefab", "Fire Vfx Prefab", typeof(GameObject), () => state.FireVfxPrefab, value => state.FireVfxPrefab = (GameObject)value);
            advanced.Asset("ImpactVfxPresentationPrefab", "Impact Vfx Presentation Prefab", typeof(GameObject), () => state.ImpactVfxPresentationPrefab, value => state.ImpactVfxPresentationPrefab = (GameObject)value);
            advanced.Asset("TickVfxPrefab", "Tick Vfx Prefab", typeof(GameObject), () => state.TickVfxPrefab, value => state.TickVfxPrefab = (GameObject)value);
            advanced.Asset("ExpireVfxPrefab", "Expire Vfx Prefab", typeof(GameObject), () => state.ExpireVfxPrefab, value => state.ExpireVfxPrefab = (GameObject)value);
            foreach (string id in new[] { "ProjectileDefinitionId", "ProjectileSpawnableId", "ProjectilePrefab", "ProjectileSpeed", "ProjectileLifetimeTicks", "Homing", "PierceCount" })
                advanced.VisibleWhen(root.Q(id), () => state.DeliveryMode == AttackRecipeDeliveryMode.Projectile);
            advanced.VisibleWhen(root.Q("HomingTurnRate"), () => state.DeliveryMode == AttackRecipeDeliveryMode.Projectile && state.Homing);
            foreach (string id in new[] { "BeamVfxPrefab", "ImpactVfxPrefab" }) advanced.VisibleWhen(root.Q(id), () => state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan);
            advanced.VisibleWhen(root.Q("Radius"), () => state.DeliveryMode != AttackRecipeDeliveryMode.Hitscan);
            advanced.VisibleWhen(root.Q("MaxHits"), () => state.DeliveryMode != AttackRecipeDeliveryMode.Projectile);
            advanced.VisibleWhen(root.Q("TickIntervalSeconds"), () => state.DeliveryMode == AttackRecipeDeliveryMode.Aura);
            foreach (string id in new[] { "StatusId", "StatusDurationTicks", "StatusTickRateTicks", "StatusStrength", "StatusMaxStacks", "StatusStackingPolicy", "StatusEffectNote" })
                advanced.VisibleWhen(root.Q(id), () => state.IncludeStatusEffect);
            advanced.Refresh();
        }
    }
}
