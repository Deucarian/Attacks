using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal static class WaveAuthoringSummary
    {
        public static int GetTotalEnemyCount(WaveAuthoringState state)
        {
            return AttackGameContentPreviewSummaries.GetWaveTotalEnemyCount(state);
        }

        public static int GetApproximateDurationTicks(WaveAuthoringState state)
        {
            return AttackGameContentPreviewSummaries.GetWaveApproximateDurationTicks(state);
        }

        public static string BuildEnemyMixSummary(WaveAuthoringState state)
        {
            if (state == null)
                return "None";

            state.EnsureEntries();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                string label = entry.Enemy == null ? "Missing enemy" : string.IsNullOrWhiteSpace(entry.Enemy.DisplayName) ? entry.Enemy.Id : entry.Enemy.DisplayName;
                counts[label] = counts.ContainsKey(label) ? counts[label] + Math.Max(0, entry.Count) : Math.Max(0, entry.Count);
            }

            if (counts.Count == 0)
                return "None";

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add(pair.Key + " x" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return string.Join(", ", parts.ToArray());
        }

        public static string BuildChannelSummary(WaveAuthoringState state)
        {
            if (state == null)
                return "None";

            state.EnsureEntries();
            var channels = new List<string>();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                string channel = state.Entries[i].SpawnChannelId;
                if (string.IsNullOrWhiteSpace(channel) || channels.Contains(channel))
                    continue;
                channels.Add(channel);
            }

            return channels.Count == 0 ? "None" : string.Join(", ", channels.ToArray());
        }

        internal static IReadOnlyList<DeucarianEditorStatusChip> BuildWaveChips(WaveAuthoringState state, GameContentAuthoringValidationResult validation, GameContentLibraryItem item)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture) + " enemies", GetTotalEnemyCount(state) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(new GameContentAuthoringValidationSummary(validation).ReadinessLabel, validation != null && validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation != null && validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(BuildChannelSummary(state) == "None" ? "NoChannel" : "Channels", BuildChannelSummary(state) == "None" ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success, BuildChannelSummary(state)),
                new DeucarianEditorStatusChip(item == null ? "Draft" : BuildUsageSummary(item), item == null || item.ReverseReferences.Count == 0 ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success)
            };
        }

        internal static IReadOnlyList<DeucarianEditorStatusChip> BuildEntryChips(WaveEntryAuthoringState entry)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(entry.Enemy == null ? "NoEnemy" : "Enemy", entry.Enemy == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(Math.Max(0, entry.Count).ToString(CultureInfo.InvariantCulture) + " total", entry.Count > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(entry.IntervalTicks.ToString(CultureInfo.InvariantCulture) + " tick", entry.IntervalTicks >= 0 ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(string.IsNullOrWhiteSpace(entry.SpawnChannelId) ? "NoChannel" : entry.SpawnChannelId, string.IsNullOrWhiteSpace(entry.SpawnChannelId) ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success)
            };
        }

        internal static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        internal static string BuildUsageSummary(GameContentLibraryItem item)
        {
            if (item == null)
                return "Draft";

            int setCount = 0;
            int packCount = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target == null)
                    continue;
                if (target.Kind == GameContentLibraryKind.ContentSet)
                    setCount++;
                else if (target.Kind == GameContentLibraryKind.ContentPack)
                    packCount++;
            }

            return setCount.ToString(CultureInfo.InvariantCulture) + " set(s), " + packCount.ToString(CultureInfo.InvariantCulture) + " pack(s)";
        }

        internal static string BuildCadenceSummary(WaveAuthoringState state)
        {
            if (state == null || state.Entries.Count == 0)
                return "No cadence";

            int min = int.MaxValue;
            int max = 0;
            for (int i = 0; i < state.Entries.Count; i++)
            {
                int interval = Math.Max(0, state.Entries[i].IntervalTicks);
                min = Math.Min(min, interval);
                max = Math.Max(max, interval);
            }

            return min == max
                ? "Every " + max.ToString(CultureInfo.InvariantCulture) + " tick(s)"
                : min.ToString(CultureInfo.InvariantCulture) + "-" + max.ToString(CultureInfo.InvariantCulture) + " tick intervals";
        }

        internal static string BuildChannelGroupSummary(WaveAuthoringState state)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                string channel = string.IsNullOrWhiteSpace(state.Entries[i].SpawnChannelId) ? "Missing" : state.Entries[i].SpawnChannelId;
                counts[channel] = counts.ContainsKey(channel) ? counts[channel] + 1 : 1;
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add(pair.Key + " x" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return parts.Count == 0 ? "None" : string.Join(", ", parts.ToArray());
        }

        internal static string BuildPressureEstimate(WaveAuthoringState state)
        {
            int duration = Math.Max(1, GetApproximateDurationTicks(state));
            double pressure = GetTotalEnemyCount(state) / (double)duration;
            return pressure.ToString("0.##", CultureInfo.InvariantCulture) + " enemies/tick";
        }

        internal static string BuildPacingLabel(WaveAuthoringState state)
        {
            int duration = GetApproximateDurationTicks(state);
            int total = GetTotalEnemyCount(state);
            if (total <= 0)
                return "Empty";
            if (duration <= 20)
                return "Early burst";
            if (duration <= 60)
                return "Mid pressure";
            return "Long wave";
        }

        internal static int GetMaxScalingTier(WaveAuthoringState state)
        {
            int max = 0;
            for (int i = 0; i < state.Entries.Count; i++)
                max = Math.Max(max, state.Entries[i].ScalingTier);
            return max;
        }

        internal static int GetFirstSpawnTick(WaveAuthoringState state)
        {
            if (state == null || state.Entries.Count == 0)
                return Math.Max(0, state == null ? 0 : state.StartTick);

            int first = int.MaxValue;
            for (int i = 0; i < state.Entries.Count; i++)
                first = Math.Min(first, state.StartTick + Math.Max(0, state.Entries[i].InitialDelayTicks));
            return first == int.MaxValue ? Math.Max(0, state.StartTick) : Math.Max(0, first);
        }

        internal static int GetLastSpawnTick(WaveAuthoringState state)
        {
            return Math.Max(GetFirstSpawnTick(state), state.StartTick + GetApproximateDurationTicks(state));
        }

        internal static string GetEnemyLabel(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return "Missing enemy";
            return string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.Id : enemy.DisplayName;
        }

        internal static string BuildAdvancedReport(GameContentLibraryItem item, WaveAuthoringState state, WaveDefinitionAsset asset)
        {
            return "Wave: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.WaveId + Environment.NewLine
                + "Path: " + (item == null ? "(draft)" : item.Path) + Environment.NewLine
                + "Schedule: " + (asset == null || asset.Schedule == null ? "(missing)" : AssetDatabase.GetAssetPath(asset.Schedule)) + Environment.NewLine
                + "Entries: " + (asset == null || asset.Entries == null ? "(missing)" : AssetDatabase.GetAssetPath(asset.Entries)) + Environment.NewLine
                + "Channels: " + BuildChannelSummary(state) + Environment.NewLine
                + "Enemy Mix: " + BuildEnemyMixSummary(state);
        }
    }
}
