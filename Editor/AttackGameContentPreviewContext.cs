using System;
using System.Collections.Generic;
using System.Reflection;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed class AttackGameContentPreviewContextOption
    {
        public AttackGameContentPreviewContextOption(string label, GameObject prefab, GameContentLibraryItem item, bool fallback, int sortPriority)
        {
            Label = string.IsNullOrWhiteSpace(label) ? "Preview Context" : label.Trim();
            Prefab = prefab;
            Item = item;
            Fallback = fallback;
            SortPriority = sortPriority;
        }

        public string Label { get; }
        public GameObject Prefab { get; }
        public GameContentLibraryItem Item { get; }
        public bool Fallback { get; }
        public int SortPriority { get; }
    }

    internal static class AttackGameContentPreviewContext
    {
        public static IReadOnlyList<AttackGameContentPreviewContextOption> BuildSourceOptions(GameContentLibraryItem attackItem)
        {
            var options = new List<AttackGameContentPreviewContextOption>();
            if (attackItem != null && attackItem.ReverseReferences != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < attackItem.ReverseReferences.Count; i++)
                {
                    GameContentLibraryItem item = attackItem.ReverseReferences[i] == null ? null : attackItem.ReverseReferences[i].Target;
                    if (item == null || item.Kind != GameContentLibraryKind.Weapon)
                        continue;
                    if (!seen.Add(item.Key))
                        continue;

                    GameObject prefab = TryReadPresentationPrefab(item.Asset);
                    string suffix = prefab == null ? " - origin emitter fallback" : " - prefab source";
                    options.Add(new AttackGameContentPreviewContextOption(item.DisplayName + suffix, prefab, item, prefab == null, 0));
                }
            }

            SortOptions(options, "weapon");
            if (options.Count == 0)
                options.Add(new AttackGameContentPreviewContextOption("Origin emitter fallback", null, null, true, 100));
            return options;
        }

        public static IReadOnlyList<AttackGameContentPreviewContextOption> BuildTargetOptions(GameContentLibraryItem attackItem, IReadOnlyList<GameContentLibraryItem> authoredItems)
        {
            var options = new List<AttackGameContentPreviewContextOption>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceWeapons = CollectSourceWeaponKeys(attackItem);

            if (authoredItems != null && sourceWeapons.Count > 0)
            {
                for (int i = 0; i < authoredItems.Count; i++)
                {
                    GameContentLibraryItem contentSet = authoredItems[i];
                    if (contentSet == null || contentSet.Kind != GameContentLibraryKind.ContentSet || contentSet.DirectReferences == null)
                        continue;
                    if (!ReferencesAny(contentSet, sourceWeapons))
                        continue;

                    AddEnemyReferences(contentSet.DirectReferences, options, seen, "content set", 0);
                }
            }

            if (options.Count == 0 && authoredItems != null)
            {
                for (int i = 0; i < authoredItems.Count; i++)
                {
                    GameContentLibraryItem item = authoredItems[i];
                    if (item == null || item.Kind != GameContentLibraryKind.Enemy)
                        continue;
                    AddEnemyOption(item, options, seen, "library", 10);
                }
            }

            SortOptions(options, "enemy");
            if (options.Count == 0)
                options.Add(new AttackGameContentPreviewContextOption("Neutral target dummy fallback", null, null, true, 100));
            return options;
        }

        public static string BuildFallbackStatus(AttackGameContentPreviewContextOption source, AttackGameContentPreviewContextOption target)
        {
            bool sourceFallback = source != null && source.Fallback;
            bool targetFallback = target != null && target.Fallback;
            if (!sourceFallback && !targetFallback)
                return string.Empty;
            if (sourceFallback && targetFallback)
                return "Preview uses neutral source and target fallbacks because authored context prefabs are missing.";
            if (sourceFallback)
                return "Preview uses a neutral source fallback because no authored weapon prefab is available.";
            return "Preview uses a neutral target fallback because no authored enemy prefab is available.";
        }

        public static int ClampIndex(int index, IReadOnlyList<AttackGameContentPreviewContextOption> options)
        {
            if (options == null || options.Count == 0)
                return 0;
            return Mathf.Clamp(index, 0, options.Count - 1);
        }

        public static string[] BuildLabels(IReadOnlyList<AttackGameContentPreviewContextOption> options)
        {
            if (options == null || options.Count == 0)
                return new[] { "Fallback" };

            string[] labels = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                labels[i] = options[i] == null ? "Fallback" : options[i].Label;
            return labels;
        }

        public static bool HasResolvedPrefab(IReadOnlyList<AttackGameContentPreviewContextOption> options)
        {
            if (options == null)
                return false;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && options[i].Prefab != null)
                    return true;
            }

            return false;
        }

        private static HashSet<string> CollectSourceWeaponKeys(GameContentLibraryItem attackItem)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (attackItem == null || attackItem.ReverseReferences == null)
                return keys;

            for (int i = 0; i < attackItem.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem item = attackItem.ReverseReferences[i] == null ? null : attackItem.ReverseReferences[i].Target;
                if (item != null && item.Kind == GameContentLibraryKind.Weapon)
                    keys.Add(item.Key);
            }

            return keys;
        }

        private static bool ReferencesAny(GameContentLibraryItem contentSet, HashSet<string> keys)
        {
            if (contentSet == null || contentSet.DirectReferences == null || keys == null || keys.Count == 0)
                return false;

            for (int i = 0; i < contentSet.DirectReferences.Count; i++)
            {
                GameContentLibraryItem target = contentSet.DirectReferences[i] == null ? null : contentSet.DirectReferences[i].Target;
                if (target != null && keys.Contains(target.Key))
                    return true;
            }

            return false;
        }

        private static void AddEnemyReferences(
            IReadOnlyList<GameContentLibraryReference> references,
            List<AttackGameContentPreviewContextOption> options,
            HashSet<string> seen,
            string sourceLabel,
            int sortPriority)
        {
            if (references == null)
                return;

            for (int i = 0; i < references.Count; i++)
            {
                GameContentLibraryItem item = references[i] == null ? null : references[i].Target;
                if (item == null || item.Kind != GameContentLibraryKind.Enemy)
                    continue;
                AddEnemyOption(item, options, seen, sourceLabel, sortPriority);
            }
        }

        private static void AddEnemyOption(
            GameContentLibraryItem item,
            List<AttackGameContentPreviewContextOption> options,
            HashSet<string> seen,
            string sourceLabel,
            int sortPriority)
        {
            if (item == null || options == null || seen == null || !seen.Add(item.Key))
                return;

            GameObject prefab = TryReadPresentationPrefab(item.Asset);
            string suffix = prefab == null ? " - target dummy fallback" : " - " + sourceLabel + " target";
            options.Add(new AttackGameContentPreviewContextOption(item.DisplayName + suffix, prefab, item, prefab == null, sortPriority));
        }

        private static void SortOptions(List<AttackGameContentPreviewContextOption> options, string preferredText)
        {
            if (options == null)
                return;

            options.Sort((left, right) =>
            {
                int prefabCompare = (right.Prefab != null).CompareTo(left.Prefab != null);
                if (prefabCompare != 0)
                    return prefabCompare;

                int fallbackCompare = left.Fallback.CompareTo(right.Fallback);
                if (fallbackCompare != 0)
                    return fallbackCompare;

                int priorityCompare = left.SortPriority.CompareTo(right.SortPriority);
                if (priorityCompare != 0)
                    return priorityCompare;

                int preferredCompare = IsPreferred(right, preferredText).CompareTo(IsPreferred(left, preferredText));
                if (preferredCompare != 0)
                    return preferredCompare;

                return string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static bool IsPreferred(AttackGameContentPreviewContextOption option, string text)
        {
            if (option == null || string.IsNullOrWhiteSpace(text))
                return false;
            return option.Label.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Item != null && option.Item.DisplayName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                || (option.Item != null && option.Item.Id.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static GameObject TryReadPresentationPrefab(UnityEngine.Object asset)
        {
            object presentation = ReadMemberValue(asset, "Presentation");
            object prefab = ReadMemberValue(presentation, "Prefab");
            return prefab as GameObject;
        }

        private static object ReadMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            Type type = target.GetType();
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(target, null);
                    }
                    catch
                    {
                        return null;
                    }
                }

                FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (field != null)
                    return field.GetValue(target);

                type = type.BaseType;
            }

            return null;
        }
    }
}
