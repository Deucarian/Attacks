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
    internal sealed class AttackProviderV2ListItem
    {
        private AttackProviderV2ListItem(
            GameContentLibraryItem source,
            string displayName,
            string stableId,
            string typeLabel,
            string tags,
            bool hasPrefab,
            bool hasVisuals,
            bool hasAudio)
        {
            Source = source;
            DisplayName = displayName ?? string.Empty;
            StableId = stableId ?? string.Empty;
            TypeLabel = typeLabel ?? "Attack";
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
        public string TypeLabel { get; }
        public string Tags { get; }
        public bool HasPrefab { get; }
        public bool HasVisuals { get; }
        public bool HasAudio { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }

        public static IReadOnlyList<AttackProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<AttackProviderV2ListItem>();

            var result = new List<AttackProviderV2ListItem>();
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

        public static AttackProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            AttackDefinitionAsset attack = item == null ? null : item.Asset as AttackDefinitionAsset;
            string displayName = attack == null ? item.DisplayName : attack.DisplayName;
            string stableId = attack == null ? item.Id : attack.Id;
            string tags = attack == null ? string.Empty : JoinTags(attack.Tags);
            string type = GetTypeLabel(attack);
            bool hasPrimaryDeliveryAsset = attack != null
                && attack.Delivery != null
                && (attack.Delivery.Mode == AttackRecipeDeliveryMode.Hitscan
                    ? attack.Delivery.BeamVfxPrefab != null
                    : attack.Delivery.ProjectilePrefab != null);
            bool hasDeliveryVfx = attack != null && attack.Delivery != null && (attack.Delivery.BeamVfxPrefab != null || attack.Delivery.ImpactVfxPrefab != null);
            bool hasPresentationVfx = HasPresentationAsset(attack, false);
            bool hasAudio = HasPresentationAsset(attack, true);
            return new AttackProviderV2ListItem(item, displayName, stableId, type, tags, hasPrimaryDeliveryAsset, hasPrimaryDeliveryAsset || hasDeliveryVfx || hasPresentationVfx, hasAudio);
        }

        internal static string GetTypeLabelForTests(AttackDefinitionAsset attack)
        {
            return GetTypeLabel(attack);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string normalized = query.Trim().ToLowerInvariant();
            return Contains(DisplayName, normalized)
                || Contains(StableId, normalized)
                || Contains(TypeLabel, normalized)
                || Contains(Tags, normalized)
                || (Source != null && Contains(Source.Category, normalized));
        }

        private static bool Contains(string value, string normalizedQuery)
        {
            return !string.IsNullOrWhiteSpace(value) && value.ToLowerInvariant().Contains(normalizedQuery);
        }

        private static string GetTypeLabel(AttackDefinitionAsset attack)
        {
            if (attack == null || attack.Delivery == null)
                return "Attack";

            AttackRecipeDeliveryMode mode = attack.Delivery.Mode;
            bool status = attack.StatusEffects != null && attack.StatusEffects.StatusEffects.Count > 0;
            if (status && mode == AttackRecipeDeliveryMode.Aura) return "Status";
            if (mode == AttackRecipeDeliveryMode.Projectile) return attack.Delivery.Homing ? "Homing" : "Projectile";
            if (mode == AttackRecipeDeliveryMode.Hitscan) return "Beam";
            if (mode == AttackRecipeDeliveryMode.Area) return "AOE";
            if (mode == AttackRecipeDeliveryMode.Aura) return "Status";
            return mode.ToString();
        }

        private static bool HasPresentationAsset(AttackDefinitionAsset attack, bool audio)
        {
            if (attack == null || attack.Presentation == null)
                return false;

            AttackPresentationEventKind[] events =
            {
                AttackPresentationEventKind.OnCast,
                AttackPresentationEventKind.OnFire,
                AttackPresentationEventKind.OnImpact,
                AttackPresentationEventKind.OnTick,
                AttackPresentationEventKind.OnExpire
            };

            for (int i = 0; i < events.Length; i++)
            {
                if (!attack.Presentation.TryGetEvent(events[i], out AttackPresentationEventRecipe recipe) || recipe == null)
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
