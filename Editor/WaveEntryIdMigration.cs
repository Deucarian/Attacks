using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Deucarian.Attacks.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    public sealed class WaveEntryIdMigrationIssue
    {
        public WaveEntryIdMigrationIssue(string assetPath, string message)
        {
            AssetPath = assetPath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string AssetPath { get; }
        public string Message { get; }
    }

    public sealed class WaveEntryIdMigrationReport
    {
        private readonly List<WaveEntryIdMigrationIssue> _issues = new List<WaveEntryIdMigrationIssue>();

        public int MigratedAssetCount { get; internal set; }
        public int UnchangedAssetCount { get; internal set; }
        public int ConflictAssetCount { get; internal set; }
        public IReadOnlyList<WaveEntryIdMigrationIssue> Issues => _issues;
        public bool Succeeded => ConflictAssetCount == 0;

        internal void AddConflict(string assetPath, string message)
        {
            ConflictAssetCount++;
            _issues.Add(new WaveEntryIdMigrationIssue(assetPath, message));
        }

        public string CreateSummary()
        {
            var builder = new StringBuilder();
            builder.Append("Wave entry ID migration: ")
                .Append(MigratedAssetCount.ToString(CultureInfo.InvariantCulture)).Append(" migrated, ")
                .Append(UnchangedAssetCount.ToString(CultureInfo.InvariantCulture)).Append(" unchanged, ")
                .Append(ConflictAssetCount.ToString(CultureInfo.InvariantCulture)).Append(" conflict(s).");
            for (int i = 0; i < _issues.Count; i++)
                builder.AppendLine().Append(_issues[i].AssetPath).Append(": ").Append(_issues[i].Message);
            return builder.ToString();
        }
    }

    public static class WaveEntryIdMigration
    {
        [MenuItem("Tools/Deucarian/Gameplay/Simulation/Attacks/Migrate Project Wave Entry IDs")]
        private static void MigrateProjectContentFromMenu()
        {
            WaveEntryIdMigrationReport report = MigrateProjectOwnedWaveAssets();
            EditorUtility.DisplayDialog(
                report.Succeeded ? "Wave Entry ID Migration" : "Wave Entry ID Migration Conflicts",
                report.CreateSummary(),
                "OK");
        }

        public static WaveEntryIdMigrationReport MigrateProjectOwnedWaveAssets(string assetRoot = "Assets")
        {
            string normalizedRoot = NormalizeProjectAssetRoot(assetRoot);
            string[] guids = AssetDatabase.FindAssets("t:WaveDefinitionAsset", new[] { normalizedRoot });
            Array.Sort(guids, StringComparer.Ordinal);
            var assets = new List<WaveDefinitionAsset>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                WaveDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<WaveDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null) assets.Add(asset);
            }

            return MigrateAssets(assets, true);
        }

        internal static WaveEntryIdMigrationReport MigrateAssets(IReadOnlyList<WaveDefinitionAsset> assets, bool saveAssets)
        {
            var report = new WaveEntryIdMigrationReport();
            if (assets == null) return report;

            for (int i = 0; i < assets.Count; i++)
                MigrateAsset(assets[i], report);

            if (saveAssets && report.MigratedAssetCount > 0)
                AssetDatabase.SaveAssets();
            return report;
        }

        private static void MigrateAsset(WaveDefinitionAsset wave, WaveEntryIdMigrationReport report)
        {
            string assetPath = wave == null ? "(missing wave asset)" : AssetDatabase.GetAssetPath(wave);
            if (wave == null || wave.Entries == null)
            {
                report.AddConflict(assetPath, "Wave or Entries section is missing; no identity migration was attempted.");
                return;
            }

            IReadOnlyList<WaveEntryRecipe> entries = wave.Entries.Entries;
            if (entries.Count == 0)
            {
                report.AddConflict(assetPath, "Wave has no entries; no identity migration was attempted.");
                return;
            }

            int missingCount = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                if (entry == null)
                {
                    report.AddConflict(assetPath, "Entries[" + i.ToString(CultureInfo.InvariantCulture) + "] is empty.");
                    return;
                }

                WaveEntryId entryId = entry.EntryId;
                if (entryId.IsEmpty)
                {
                    missingCount++;
                    continue;
                }

                if (!entryId.IsValid)
                {
                    report.AddConflict(assetPath, "Entries[" + i.ToString(CultureInfo.InvariantCulture) + "].EntryId is invalid: '" + entryId.Value + "'.");
                    return;
                }

                if (!seen.Add(entryId.Value))
                {
                    report.AddConflict(assetPath, "Entries contains duplicate EntryId '" + entryId.Value + "'.");
                    return;
                }
            }

            if (missingCount == 0)
            {
                report.UnchangedAssetCount++;
                return;
            }

            if (missingCount != entries.Count)
            {
                report.AddConflict(
                    assetPath,
                    "Wave has partially assigned entry IDs (" + missingCount.ToString(CultureInfo.InvariantCulture) + " of " + entries.Count.ToString(CultureInfo.InvariantCulture) + " missing); resolve the partial state explicitly before migration.");
                return;
            }

            var serialized = new SerializedObject(wave.Entries);
            SerializedProperty entriesProperty = serialized.FindProperty("_entries");
            if (entriesProperty == null || !entriesProperty.isArray || entriesProperty.arraySize != entries.Count)
            {
                report.AddConflict(assetPath, "Serialized Entries layout could not be read safely; no identity migration was attempted.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(wave.Entries, "Migrate Wave Entry IDs");
            serialized.Update();
            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty idProperty = entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_entryId");
                if (idProperty == null)
                {
                    report.AddConflict(assetPath, "Entries[" + i.ToString(CultureInfo.InvariantCulture) + "].EntryId could not be written safely.");
                    serialized.Update();
                    return;
                }

                idProperty.stringValue = i.ToString(CultureInfo.InvariantCulture);
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(wave.Entries);
            report.MigratedAssetCount++;
        }

        private static string NormalizeProjectAssetRoot(string assetRoot)
        {
            string normalized = (assetRoot ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(normalized, "Assets", StringComparison.Ordinal) &&
                !normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Wave entry ID migration only accepts project-owned roots under Assets.", nameof(assetRoot));
            return normalized;
        }
    }
}
