using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal static class AttackAuthoringDraft
    {
        internal static string BuildStateFingerprint(AttackAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            return string.Join("|", new[]
            {
                state.AttackId ?? string.Empty,
                state.DisplayName ?? string.Empty,
                AssetKey(state.Icon),
                state.TagsCsv ?? string.Empty,
                state.DamageTypeId ?? string.Empty,
                state.DamageAmount.ToString("R", CultureInfo.InvariantCulture),
                state.CooldownTicks.ToString(CultureInfo.InvariantCulture),
                state.Range.ToString("R", CultureInfo.InvariantCulture),
                state.TargetingMode.ToString(),
                state.DeliveryMode.ToString(),
                state.ProjectileDefinitionId ?? string.Empty,
                state.ProjectileSpawnableId ?? string.Empty,
                AssetKey(state.ProjectilePrefab),
                state.ProjectileSpeed.ToString("R", CultureInfo.InvariantCulture),
                state.ProjectileLifetimeTicks.ToString(CultureInfo.InvariantCulture),
                state.Homing ? "1" : "0",
                state.HomingTurnRate.ToString("R", CultureInfo.InvariantCulture),
                state.PierceCount.ToString(CultureInfo.InvariantCulture),
                state.Radius.ToString("R", CultureInfo.InvariantCulture),
                AssetKey(state.BeamVfxPrefab),
                AssetKey(state.ImpactVfxPrefab),
                state.MaxHits.ToString(CultureInfo.InvariantCulture),
                state.TickIntervalSeconds.ToString("R", CultureInfo.InvariantCulture),
                state.IncludeStatusEffect ? "1" : "0",
                state.StatusId ?? string.Empty,
                state.StatusDurationTicks.ToString(CultureInfo.InvariantCulture),
                state.StatusTickRateTicks.ToString(CultureInfo.InvariantCulture),
                state.StatusStrength.ToString("R", CultureInfo.InvariantCulture),
                state.StatusMaxStacks.ToString(CultureInfo.InvariantCulture),
                state.StatusStackingPolicy.ToString(),
                state.StatusEffectNote ?? string.Empty,
                AssetKey(state.CastAudio),
                AssetKey(state.FireAudio),
                AssetKey(state.ImpactAudio),
                AssetKey(state.TickAudio),
                AssetKey(state.ExpireAudio),
                AssetKey(state.CastVfxPrefab),
                AssetKey(state.FireVfxPrefab),
                AssetKey(state.ImpactVfxPresentationPrefab),
                AssetKey(state.TickVfxPrefab),
                AssetKey(state.ExpireVfxPrefab)
            });
        }

        private static string AssetKey(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path)
                ? asset.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                : path;
        }
    }
}
