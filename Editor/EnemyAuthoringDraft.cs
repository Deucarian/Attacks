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
    internal static class EnemyAuthoringDraft
    {
        internal static string BuildStateFingerprint(EnemyAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            return string.Join("|", new[]
            {
                state.EnemyId ?? string.Empty,
                state.DisplayName ?? string.Empty,
                AssetKey(state.Icon),
                ((int)state.Role).ToString(CultureInfo.InvariantCulture),
                state.TagsCsv ?? string.Empty,
                AssetKey(state.Prefab),
                state.MaximumHealth.ToString("R", CultureInfo.InvariantCulture),
                state.MoveSpeed.ToString("R", CultureInfo.InvariantCulture),
                state.RewardValue.ToString(CultureInfo.InvariantCulture),
                state.ContactDamage.ToString("R", CultureInfo.InvariantCulture),
                state.DamageTypeId ?? string.Empty,
                state.CollisionRadius.ToString("R", CultureInfo.InvariantCulture),
                AssetKey(state.SpawnAudio),
                AssetKey(state.SpawnVfxPrefab),
                AssetKey(state.HitAudio),
                AssetKey(state.HitVfxPrefab),
                AssetKey(state.DeathAudio),
                AssetKey(state.DeathVfxPrefab)
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
