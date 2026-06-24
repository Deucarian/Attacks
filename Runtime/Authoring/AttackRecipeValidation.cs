using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public enum AttackRecipeValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct AttackRecipeValidationOptions
    {
        public AttackRecipeValidationOptions(bool requireProjectilePrefab, bool requireHitscanVfx, bool requirePresentationAssets)
        {
            RequireProjectilePrefab = requireProjectilePrefab;
            RequireHitscanVfx = requireHitscanVfx;
            RequirePresentationAssets = requirePresentationAssets;
        }

        public bool RequireProjectilePrefab { get; }
        public bool RequireHitscanVfx { get; }
        public bool RequirePresentationAssets { get; }
        public static AttackRecipeValidationOptions RuntimeFriendly => new AttackRecipeValidationOptions(false, false, false);
        public static AttackRecipeValidationOptions AssetCreation => new AttackRecipeValidationOptions(true, true, false);
    }

    public readonly struct AttackRecipeValidationIssue
    {
        public AttackRecipeValidationIssue(AttackRecipeValidationSeverity severity, string message, string path)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public AttackRecipeValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
        public bool IsError => Severity == AttackRecipeValidationSeverity.Error;
    }

    public sealed class AttackRecipeValidationReport
    {
        private readonly AttackRecipeValidationIssue[] _issues;

        public AttackRecipeValidationReport(IReadOnlyList<AttackRecipeValidationIssue> issues)
        {
            _issues = Copy(issues);
        }

        public IReadOnlyList<AttackRecipeValidationIssue> Issues => _issues;
        public bool IsValid
        {
            get
            {
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        return false;
                return true;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        count++;
                return count;
            }
        }

        private static AttackRecipeValidationIssue[] Copy(IReadOnlyList<AttackRecipeValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0) return Array.Empty<AttackRecipeValidationIssue>();
            var copy = new AttackRecipeValidationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++) copy[i] = issues[i];
            return copy;
        }
    }

    public static class AttackRecipeValidator
    {
        public static AttackRecipeValidationReport Validate(AttackDefinitionAsset recipe)
        {
            return Validate(recipe, AttackRecipeValidationOptions.RuntimeFriendly);
        }

        public static AttackRecipeValidationReport Validate(AttackDefinitionAsset recipe, AttackRecipeValidationOptions options)
        {
            var issues = new List<AttackRecipeValidationIssue>();
            if (recipe == null)
            {
                issues.Add(Error("Recipe", "Attack recipe is missing."));
                return new AttackRecipeValidationReport(issues);
            }

            if (string.IsNullOrWhiteSpace(recipe.Id)) issues.Add(Error("Attack.Id", "Attack ID is required."));
            if (string.IsNullOrWhiteSpace(recipe.DisplayName)) issues.Add(Warning("Attack.DisplayName", "Display name is empty."));

            ValidateMechanics(recipe.Mechanics, issues);
            ValidateTargeting(recipe.Targeting, issues);
            ValidateDelivery(recipe.Delivery, options, issues);
            ValidateStatuses(recipe.StatusEffects, issues);
            ValidatePresentation(recipe.Presentation, options, issues);
            return new AttackRecipeValidationReport(issues);
        }

        private static void ValidateMechanics(AttackMechanicsDefinitionAsset mechanics, List<AttackRecipeValidationIssue> issues)
        {
            if (mechanics == null)
            {
                issues.Add(Error("Mechanics", "Mechanics section is required."));
                return;
            }

            if (mechanics.CooldownTicks < 0) issues.Add(Error("Mechanics.CooldownTicks", "Cooldown cannot be negative."));
            if (mechanics.Range < 0f || float.IsNaN(mechanics.Range) || float.IsInfinity(mechanics.Range)) issues.Add(Error("Mechanics.Range", "Range must be a finite non-negative value."));
            if (mechanics.DamageAmount <= 0f || float.IsNaN(mechanics.DamageAmount) || float.IsInfinity(mechanics.DamageAmount)) issues.Add(Error("Mechanics.DamageAmount", "Damage amount must be greater than zero."));
            if (string.IsNullOrWhiteSpace(mechanics.DamageTypeId)) issues.Add(Error("Mechanics.DamageTypeId", "Damage type ID is required."));
        }

        private static void ValidateTargeting(AttackTargetingDefinitionAsset targeting, List<AttackRecipeValidationIssue> issues)
        {
            if (targeting == null)
            {
                issues.Add(Error("Targeting", "Targeting section is required."));
                return;
            }

            if (targeting.MaxTargets <= 0) issues.Add(Error("Targeting.MaxTargets", "Max targets must be greater than zero."));
            if (targeting.Mode == AttackRecipeTargetingMode.Random) issues.Add(Warning("Targeting.Mode", "Random targeting needs caller-provided random candidate ordering or a custom selector."));
            if (targeting.Mode == AttackRecipeTargetingMode.ForwardDirection) issues.Add(Warning("Targeting.Mode", "Forward direction targeting needs caller-provided forward-cone candidates."));
        }

        private static void ValidateDelivery(AttackDeliveryDefinitionAsset delivery, AttackRecipeValidationOptions options, List<AttackRecipeValidationIssue> issues)
        {
            if (delivery == null)
            {
                issues.Add(Error("Delivery", "Delivery section is required."));
                return;
            }

            switch (delivery.Mode)
            {
                case AttackRecipeDeliveryMode.Projectile:
                    if (string.IsNullOrWhiteSpace(delivery.ProjectileDefinitionId)) issues.Add(Error("Delivery.ProjectileDefinitionId", "Projectile attacks need a projectile definition ID."));
                    if (string.IsNullOrWhiteSpace(delivery.ProjectileSpawnableId)) issues.Add(Error("Delivery.ProjectileSpawnableId", "Projectile attacks need a spawnable ID."));
                    if (delivery.ProjectileSpeed <= 0f || float.IsNaN(delivery.ProjectileSpeed) || float.IsInfinity(delivery.ProjectileSpeed)) issues.Add(Error("Delivery.ProjectileSpeed", "Projectile speed must be greater than zero."));
                    if (delivery.ProjectileLifetimeTicks <= 0) issues.Add(Error("Delivery.ProjectileLifetimeTicks", "Projectile lifetime must be greater than zero."));
                    if (delivery.PierceCount < 0) issues.Add(Error("Delivery.PierceCount", "Pierce count cannot be negative."));
                    if (delivery.Homing && (delivery.HomingTurnRate <= 0f || float.IsNaN(delivery.HomingTurnRate) || float.IsInfinity(delivery.HomingTurnRate))) issues.Add(Error("Delivery.HomingTurnRate", "Homing turn rate must be greater than zero."));
                    if (options.RequireProjectilePrefab && delivery.ProjectilePrefab == null) issues.Add(Error("Delivery.ProjectilePrefab", "Projectile attacks need a prefab or model reference before asset creation."));
                    break;
                case AttackRecipeDeliveryMode.Hitscan:
                    if (delivery.MaxHits <= 0) issues.Add(Error("Delivery.MaxHits", "Hitscan max hits must be greater than zero."));
                    if (options.RequireHitscanVfx && delivery.BeamVfxPrefab == null && delivery.ImpactVfxPrefab == null) issues.Add(Error("Delivery.HitscanVfx", "Hitscan attacks need a beam/tracer or impact VFX reference before asset creation."));
                    break;
                case AttackRecipeDeliveryMode.Area:
                    if (delivery.Radius <= 0f || float.IsNaN(delivery.Radius) || float.IsInfinity(delivery.Radius)) issues.Add(Error("Delivery.Radius", "Area attacks need a radius greater than zero."));
                    if (delivery.MaxHits <= 0) issues.Add(Error("Delivery.MaxHits", "Area attacks need a max hit count greater than zero."));
                    break;
                case AttackRecipeDeliveryMode.Aura:
                    if (delivery.Radius <= 0f || float.IsNaN(delivery.Radius) || float.IsInfinity(delivery.Radius)) issues.Add(Error("Delivery.Radius", "Aura attacks need a radius greater than zero."));
                    if (delivery.TickIntervalSeconds <= 0f || float.IsNaN(delivery.TickIntervalSeconds) || float.IsInfinity(delivery.TickIntervalSeconds)) issues.Add(Error("Delivery.TickIntervalSeconds", "Aura tick interval must be greater than zero."));
                    break;
                default:
                    issues.Add(Error("Delivery.Mode", "Unsupported delivery mode."));
                    break;
            }
        }

        private static void ValidateStatuses(AttackStatusEffectsDefinitionAsset statusEffects, List<AttackRecipeValidationIssue> issues)
        {
            if (statusEffects == null) return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<AttackStatusEffectRecipe> statuses = statusEffects.StatusEffects;
            for (int i = 0; i < statuses.Count; i++)
            {
                AttackStatusEffectRecipe status = statuses[i];
                string path = "StatusEffects[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (status == null)
                {
                    issues.Add(Error(path, "Status entry is empty."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.StatusId)) issues.Add(Error(path + ".StatusId", "Status ID is required."));
                else if (!seen.Add(status.StatusId.Trim())) issues.Add(Error(path + ".StatusId", "Duplicate status ID in attack recipe."));
                if (status.DurationTicks <= 0) issues.Add(Error(path + ".DurationTicks", "Status duration must be greater than zero."));
                if (status.TickRateTicks < 0) issues.Add(Error(path + ".TickRateTicks", "Status tick rate cannot be negative."));
                if (status.Strength < 0f || float.IsNaN(status.Strength) || float.IsInfinity(status.Strength)) issues.Add(Error(path + ".Strength", "Status strength must be finite and non-negative."));
                if (status.MaxStacks <= 0) issues.Add(Error(path + ".MaxStacks", "Status max stacks must be greater than zero."));
            }
        }

        private static void ValidatePresentation(AttackPresentationDefinitionAsset presentation, AttackRecipeValidationOptions options, List<AttackRecipeValidationIssue> issues)
        {
            if (presentation == null)
            {
                issues.Add(Warning("Presentation", "Presentation section is missing; runtime will skip audio and VFX."));
                return;
            }

            if (!options.RequirePresentationAssets) return;

            IReadOnlyList<AttackPresentationEventRecipe> events = presentation.Events;
            for (int i = 0; i < events.Count; i++)
            {
                AttackPresentationEventRecipe evt = events[i];
                if (evt == null) continue;
                if (!evt.HasAnyAsset)
                {
                    string path = "Presentation." + evt.EventKind;
                    issues.Add(Warning(path, "Presentation event has no audio or VFX reference."));
                }
            }
        }

        private static AttackRecipeValidationIssue Error(string path, string message)
        {
            return new AttackRecipeValidationIssue(AttackRecipeValidationSeverity.Error, message, path);
        }

        private static AttackRecipeValidationIssue Warning(string path, string message)
        {
            return new AttackRecipeValidationIssue(AttackRecipeValidationSeverity.Warning, message, path);
        }
    }
}
