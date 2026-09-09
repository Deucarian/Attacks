using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal static class WaveAuthoringFields
    {
        private static readonly string[] PreferredChannels =
        {
            "perimeter-north",
            "perimeter-east",
            "perimeter-south",
            "perimeter-west",
            "entry",
            "center"
        };

        internal static void DrawHeader(string title, string subtitle, IReadOnlyList<DeucarianEditorStatusChip> chips)
        {
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(title) ? "Wave" : title, HeaderStyle);
            if (!string.IsNullOrWhiteSpace(subtitle))
                EditorGUILayout.LabelField(subtitle, DeucarianEditorStyles.MutedLabel);
            DeucarianEditorStatusChipRow.Draw(chips);
        }

        internal static void DrawOverview(GameContentAuthoringSurfaceContext context, WaveAuthoringState state, GameContentLibraryItem item, bool creating, GameContentAuthoringValidationResult validation)
        {
            state.WaveId = context.Authoring.DrawTextField("Stable ID", state.WaveId);
            state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
            state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
            if (creating)
                state.OutputRoot = context.Authoring.DrawOutputRootField(state.OutputRoot);

            DrawSummaryRows(
                WaveAuthoringSummary.Row("Readiness", new GameContentAuthoringValidationSummary(validation).ReadinessLabel),
                WaveAuthoringSummary.Row("Total Enemies", WaveAuthoringSummary.GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Approx Duration", WaveAuthoringSummary.GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                WaveAuthoringSummary.Row("Enemy Mix", WaveAuthoringSummary.BuildEnemyMixSummary(state)),
                WaveAuthoringSummary.Row("Used By", item == null ? "New draft" : WaveAuthoringSummary.BuildUsageSummary(item)));
        }

        internal static void DrawEntries(GameContentAuthoringSurfaceContext context, WaveAuthoringState state)
        {
            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
                DrawEntry(context, state, i);

            if (DeucarianEditorButtons.Secondary("Add Entry", true, GUILayout.Height(24f)))
                state.Entries.Add(WaveEntryAuthoringState.CreateNew());
        }

        private static void DrawEntry(GameContentAuthoringSurfaceContext context, WaveAuthoringState state, int index)
        {
            WaveEntryAuthoringState entry = state.Entries[index];
            bool remove = false;
            bool duplicate = false;
            bool moveUp = false;
            bool moveDown = false;
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Entry " + (index + 1).ToString(CultureInfo.InvariantCulture), DeucarianEditorStyles.SectionTitle);
                    GUILayout.FlexibleSpace();
                    if (DeucarianEditorMiniToolbar.Button("Up", index > 0, GUILayout.Width(38f), GUILayout.Height(22f)))
                        moveUp = true;
                    if (DeucarianEditorMiniToolbar.Button("Down", index < state.Entries.Count - 1, GUILayout.Width(54f), GUILayout.Height(22f)))
                        moveDown = true;
                    if (DeucarianEditorMiniToolbar.Button("Copy", true, GUILayout.Width(50f), GUILayout.Height(22f)))
                        duplicate = true;
                    if (DeucarianEditorMiniToolbar.Button("Remove", state.Entries.Count > 1, GUILayout.Width(68f), GUILayout.Height(22f)))
                        remove = true;
                }

                if (remove)
                    return;

                entry.Enemy = context.Authoring.DrawObjectField("Enemy", entry.Enemy);
                entry.Count = context.Authoring.DrawIntField("Count", entry.Count);
                entry.BatchSize = context.Authoring.DrawIntField("Batch Size", entry.BatchSize);
                entry.InitialDelayTicks = context.Authoring.DrawIntField("Start Delay", entry.InitialDelayTicks);
                entry.IntervalTicks = context.Authoring.DrawIntField("Interval", entry.IntervalTicks);
                entry.SpawnChannelId = DrawChannelField("Lane / Channel", entry.SpawnChannelId);
                entry.ScalingTier = context.Authoring.DrawIntField("Difficulty Tier", entry.ScalingTier);

                DeucarianEditorStatusChipRow.Draw(WaveAuthoringSummary.BuildEntryChips(entry));
            });

            if (remove)
                state.Entries.RemoveAt(index);
            else if (duplicate)
                state.Entries.Insert(index + 1, WaveAuthoringEntryCommands.CopyEntry(entry));
            else if (moveUp)
                WaveAuthoringEntryCommands.MoveEntry(state, index, index - 1);
            else if (moveDown)
                WaveAuthoringEntryCommands.MoveEntry(state, index, index + 1);
        }

        internal static void DrawTiming(GameContentAuthoringSurfaceContext context, WaveAuthoringState state)
        {
            state.StartTick = context.Authoring.DrawIntField("Start Tick", state.StartTick);
            DrawSummaryRows(
                WaveAuthoringSummary.Row("Approx Duration", WaveAuthoringSummary.GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                WaveAuthoringSummary.Row("First Spawn", WaveAuthoringSummary.GetFirstSpawnTick(state).ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Last Spawn", WaveAuthoringSummary.GetLastSpawnTick(state).ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Cadence", WaveAuthoringSummary.BuildCadenceSummary(state)));
            context.Preview.DrawTimeline(AttackGameContentPreviewSummaries.BuildWaveTimeline(state));
        }

        internal static void DrawChannels(WaveAuthoringState state)
        {
            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                entry.SpawnChannelId = DrawChannelField("Entry " + (i + 1).ToString(CultureInfo.InvariantCulture), entry.SpawnChannelId);
            }

            DrawSummaryRows(
                WaveAuthoringSummary.Row("Channels", WaveAuthoringSummary.BuildChannelSummary(state)),
                WaveAuthoringSummary.Row("Groups", WaveAuthoringSummary.BuildChannelGroupSummary(state)));
        }

        internal static void DrawBalance(WaveAuthoringState state)
        {
            DrawSummaryRows(
                WaveAuthoringSummary.Row("Total Enemies", WaveAuthoringSummary.GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Enemy Mix", WaveAuthoringSummary.BuildEnemyMixSummary(state)),
                WaveAuthoringSummary.Row("Pressure", WaveAuthoringSummary.BuildPressureEstimate(state)),
                WaveAuthoringSummary.Row("Pacing", WaveAuthoringSummary.BuildPacingLabel(state)),
                WaveAuthoringSummary.Row("Max Tier", WaveAuthoringSummary.GetMaxScalingTier(state).ToString(CultureInfo.InvariantCulture)));
        }

        internal static void DrawReferences(GameContentLibraryItem item)
        {
            if (item == null)
            {
                EditorGUILayout.LabelField("No references for a new draft.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            if (item.ReverseReferences.Count == 0)
            {
                EditorGUILayout.LabelField("No authored content references this wave.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryReference reference = item.ReverseReferences[i];
                string label = reference.Target == null ? "Reference" : reference.Target.DisplayName;
                string detail = reference.Target == null ? reference.PropertyPath : reference.Target.Category + " - " + reference.PropertyPath;
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    EditorGUILayout.LabelField(label, DeucarianEditorStyles.SectionTitle);
                    EditorGUILayout.LabelField(detail, DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        internal static void DrawAdvanced(GameContentLibraryItem item, WaveAuthoringState state, WaveDefinitionAsset asset)
        {
            DrawSummaryRows(
                WaveAuthoringSummary.Row("Asset Path", item == null ? "(not created)" : item.Path),
                WaveAuthoringSummary.Row("Schedule Section", asset != null && asset.Schedule != null ? AssetDatabase.GetAssetPath(asset.Schedule) : "Missing"),
                WaveAuthoringSummary.Row("Entries Section", asset != null && asset.Entries != null ? AssetDatabase.GetAssetPath(asset.Entries) : "Missing"),
                WaveAuthoringSummary.Row("Output Root", state.OutputRoot),
                WaveAuthoringSummary.Row("Raw References", item == null ? "New draft" : item.DirectReferences.Count.ToString(CultureInfo.InvariantCulture) + " direct, " + item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " reverse"));

            if (DeucarianEditorButtons.Secondary("Copy Report", true, GUILayout.Width(110f), GUILayout.Height(24f)))
                EditorGUIUtility.systemCopyBuffer = WaveAuthoringSummary.BuildAdvancedReport(item, state, asset);
        }

        internal static void DrawSummaryRows(params GameContentAuthoringPreviewRow[] rows)
        {
            GameContentAuthoringProviderGUI.DrawSummaryRows(rows);
        }

        private static string DrawChannelField(string label, string value)
        {
            string current = string.IsNullOrWhiteSpace(value) ? PreferredChannels[0] : value.Trim();
            var options = new List<string>(PreferredChannels);
            if (!options.Contains(current))
                options.Add(current);

            int index = Mathf.Max(0, options.IndexOf(current));
            DeucarianEditorFieldRow.Draw(label, () => index = EditorGUILayout.Popup(index, options.ToArray()));
            return options[Mathf.Clamp(index, 0, options.Count - 1)];
        }

        private static readonly Lazy<GUIStyle> headerStyle = CreateHeaderStyleCache(() => EditorStyles.boldLabel);

        private static GUIStyle HeaderStyle => headerStyle.Value;

        internal static Lazy<GUIStyle> CreateHeaderStyleCache(Func<GUIStyle> source)
        {
            return new Lazy<GUIStyle>(() =>
            {
                var style = new GUIStyle(source())
                {
                    fontSize = 14
                };
                style.normal.textColor = DeucarianEditorTheme.Text;
                return style;
            }, LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
