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
    internal sealed class EnemyProviderV2ListItem
    {
        private EnemyProviderV2ListItem(
            GameContentLibraryItem source,
            string displayName,
            string stableId,
            string roleLabel,
            string tags,
            bool hasPrefab,
            bool hasVisuals,
            bool hasAudio)
        {
            Source = source;
            DisplayName = displayName ?? string.Empty;
            StableId = stableId ?? string.Empty;
            RoleLabel = roleLabel ?? "Enemy";
            Tags = tags ?? string.Empty;
            HasPrefab = hasPrefab;
            HasVisuals = hasVisuals;
            HasAudio = hasAudio;
            ReadinessLabel = source == null ? "Missing" : source.ValidationLabel;
            ReadinessStatus = source == null
                ? DeucarianEditorStatus.Disabled
                : source.ErrorCount > 0
                    ? DeucarianEditorStatus.Error
                    : source.WarningCount > 0
                        ? DeucarianEditorStatus.Warning
                        : DeucarianEditorStatus.Success;
        }

        public GameContentLibraryItem Source { get; }
        public string DisplayName { get; }
        public string StableId { get; }
        public string RoleLabel { get; }
        public string Tags { get; }
        public bool HasPrefab { get; }
        public bool HasVisuals { get; }
        public bool HasAudio { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }

        public static IReadOnlyList<EnemyProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<EnemyProviderV2ListItem>();

            var result = new List<EnemyProviderV2ListItem>();
            for (int i = 0; i < items.Count; i++)
            {
                GameContentLibraryItem item = items[i];
                if (item == null)
                    continue;
                result.Add(FromItem(item));
            }

            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static EnemyProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            EnemyDefinitionAsset enemy = item == null ? null : item.Asset as EnemyDefinitionAsset;
            string displayName = enemy == null ? item.DisplayName : enemy.DisplayName;
            string stableId = enemy == null ? item.Id : enemy.Id;
            string tags = enemy == null ? string.Empty : JoinTags(enemy.Tags);
            string role = GetRoleLabel(enemy);
            bool hasPrefab = enemy != null && enemy.Presentation != null && enemy.Presentation.Prefab != null;
            bool hasVisuals = hasPrefab || HasPresentationAsset(enemy, false);
            bool hasAudio = HasPresentationAsset(enemy, true);
            return new EnemyProviderV2ListItem(item, displayName, stableId, role, tags, hasPrefab, hasVisuals, hasAudio);
        }

        internal static string GetRoleLabelForTests(EnemyDefinitionAsset enemy)
        {
            return GetRoleLabel(enemy);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string normalized = query.Trim().ToLowerInvariant();
            return Contains(DisplayName, normalized)
                || Contains(StableId, normalized)
                || Contains(RoleLabel, normalized)
                || Contains(Tags, normalized)
                || (Source != null && Contains(Source.Category, normalized));
        }

        private static bool Contains(string value, string normalizedQuery)
        {
            return !string.IsNullOrWhiteSpace(value) && value.ToLowerInvariant().Contains(normalizedQuery);
        }

        private static string GetRoleLabel(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return "Enemy";

            EnemyRole role = enemy.Role;
            return Enum.IsDefined(typeof(EnemyRole), role) ? role.ToString() : "Custom";
        }

        private static bool HasPresentationAsset(EnemyDefinitionAsset enemy, bool audio)
        {
            if (enemy == null || enemy.Presentation == null)
                return false;

            EnemyPresentationEventKind[] events =
            {
                EnemyPresentationEventKind.OnSpawn,
                EnemyPresentationEventKind.OnHit,
                EnemyPresentationEventKind.OnDeath
            };

            for (int i = 0; i < events.Length; i++)
            {
                if (!enemy.Presentation.TryGetEvent(events[i], out EnemyPresentationEventRecipe recipe) || recipe == null)
                    continue;
                if (audio && recipe.AudioClip != null)
                    return true;
                if (!audio && recipe.VfxPrefab != null)
                    return true;
            }

            return false;
        }

        private static string JoinTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return string.Empty;

            var values = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tags[i]))
                    values.Add(tags[i].Trim());
            }

            return string.Join(", ", values.ToArray());
        }
    }
}
