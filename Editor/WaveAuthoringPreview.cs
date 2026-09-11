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
    internal static class WaveAuthoringPreview
    {
        internal static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, WaveGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_wave__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, () => previewController?.Stop());
        }

        internal static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            WaveAuthoringState source = state.Creating ? draft : state.EditingState;
            if (source == null)
            {
                DeucarianEditorTextGUI.LabelField("Select a wave to preview.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            source.EnsureEntries();
            bool dirty = !state.Creating && state.EditingContext != null && state.EditingContext.IsDirty;
            GameContentPreviewLabModel model = new GameContentPreviewLabModel
            {
                Title = "Wave Preview Lab",
                PreviewTitle = string.IsNullOrWhiteSpace(source.DisplayName) ? "Wave Preview" : source.DisplayName,
                ScopeLabel = WaveProviderV2PreviewModel.GetScopeLabel(state.Creating, dirty),
                PrimaryAsset = GetPrimaryPreviewAsset(source),
                EmptyText = "No enemy prefab available for this wave.",
                PreviewOptions = BuildPreviewOptions(source, state),
                Chips = WaveProviderV2PreviewModel.BuildChips(source, state),
                DrawControls = () => DrawPreviewControls(state),
                DrawContext = () => DrawPreviewContext(context, source),
                DrawBody = () => DrawPreviewBody(context, source, state)
            };

            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);
            context.Preview.SetStatus(state.PreviewStatus);
            GameContentPreviewLabRenderer.Draw(context.Preview, model);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewControls(WaveProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playLabel = state.PreviewPlaying ? "Pause" : "Play";
                if (DeucarianEditorMiniToolbar.Button(playLabel, true, GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = !state.PreviewPlaying;
                    if (state.PreviewPlaying)
                        state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    else
                        state.PausedNormalizedTime = 0.5f;
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.StopPreview();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;
                if (DeucarianEditorMiniToolbar.Button("0.5x", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 0.5f;
                if (DeucarianEditorMiniToolbar.Button("1x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 1f;
                if (DeucarianEditorMiniToolbar.Button("2x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 2f;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game ? "Game" : "Debug", true, GUILayout.Width(58f), GUILayout.Height(22f)))
                    state.PreviewRenderMode = state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game
                        ? GameContentAuthoringActionPreviewRenderMode.Debug
                        : GameContentAuthoringActionPreviewRenderMode.Game;
            }
        }

        private static void DrawPreviewContext(GameContentAuthoringSurfaceContext context, WaveAuthoringState source)
        {
            context.Preview.DrawSummaryRow("Enemies", WaveAuthoringSummary.GetTotalEnemyCount(source).ToString(CultureInfo.InvariantCulture));
            context.Preview.DrawSummaryRow("Duration", WaveAuthoringSummary.GetApproximateDurationTicks(source).ToString(CultureInfo.InvariantCulture) + " tick(s)");
            context.Preview.DrawSummaryRow("Channels", WaveAuthoringSummary.BuildChannelSummary(source));
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, WaveAuthoringState source, WaveProviderV2State state)
        {
            context.Preview.DrawTimeline(AttackGameContentPreviewSummaries.BuildWaveTimeline(source));
            context.Preview.DrawSummaryRows(new[]
            {
                WaveAuthoringSummary.Row("Enemy Mix", WaveAuthoringSummary.BuildEnemyMixSummary(source)),
                WaveAuthoringSummary.Row("Cadence", WaveAuthoringSummary.BuildCadenceSummary(source)),
                WaveAuthoringSummary.Row("Pacing", WaveAuthoringSummary.BuildPacingLabel(source))
            });

            if (state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug)
            {
                context.Preview.DrawSummaryRows(BuildDebugRows(source));
                context.Preview.DrawWarnings(AttackGameContentPreviewSummaries.BuildWaveWarnings(source));
            }
        }

        internal static GameContentAuthoringObjectPreviewOptions BuildPreviewOptions(WaveAuthoringState source, WaveProviderV2State state)
        {
            var preview = new GameContentAuthoringActionPreview
            {
                Mode = GameContentAuthoringActionPreviewMode.Area,
                RenderMode = state.PreviewRenderMode,
                Playing = state.PreviewPlaying,
                Loop = state.PreviewLoop,
                Speed = state.PreviewSpeed,
                StartTime = state.PreviewStartTime,
                StaticNormalizedTime = state.PausedNormalizedTime,
                Label = string.IsNullOrWhiteSpace(source.DisplayName) ? source.WaveId : source.DisplayName,
                DeliveryTypeLabel = "Wave Timeline",
                SourceContextLabel = WaveAuthoringSummary.BuildChannelSummary(source),
                TargetContextLabel = WaveAuthoringSummary.GetTotalEnemyCount(source).ToString(CultureInfo.InvariantCulture) + " enemies"
            };

            IReadOnlyList<WaveEntryAuthoringState> entries = source.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryAuthoringState entry = entries[i];
                preview.Roles.Add(new GameContentAuthoringActionPreviewRole(
                    "Entry " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    WaveAuthoringSummary.GetEnemyLabel(entry.Enemy),
                    GetEnemyPreviewAsset(entry.Enemy)));
            }

            return new GameContentAuthoringObjectPreviewOptions
            {
                MinimumHeight = 184f,
                ActionPreview = preview
            };
        }

        private static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDebugRows(WaveAuthoringState state)
        {
            var rows = new List<GameContentAuthoringPreviewRow>();
            rows.Add(WaveAuthoringSummary.Row("Raw Wave ID", state.WaveId));
            rows.Add(WaveAuthoringSummary.Row("Start Tick", state.StartTick.ToString(CultureInfo.InvariantCulture)));
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                rows.Add(WaveAuthoringSummary.Row("Entry " + i.ToString(CultureInfo.InvariantCulture), (entry.Enemy == null ? "missing" : entry.Enemy.Id)
                    + " | count " + entry.Count.ToString(CultureInfo.InvariantCulture)
                    + " | batch " + entry.BatchSize.ToString(CultureInfo.InvariantCulture)
                    + " | delay " + entry.InitialDelayTicks.ToString(CultureInfo.InvariantCulture)
                    + " | interval " + entry.IntervalTicks.ToString(CultureInfo.InvariantCulture)
                    + " | channel " + entry.SpawnChannelId
                    + " | tier " + entry.ScalingTier.ToString(CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        internal static UnityEngine.Object GetPrimaryPreviewAsset(WaveAuthoringState state)
        {
            if (state == null)
                return null;

            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                UnityEngine.Object asset = GetEnemyPreviewAsset(state.Entries[i].Enemy);
                if (asset != null)
                    return asset;
            }

            for (int i = 0; i < state.Entries.Count; i++)
            {
                if (state.Entries[i].Enemy != null)
                    return state.Entries[i].Enemy;
            }

            return null;
        }

        private static UnityEngine.Object GetEnemyPreviewAsset(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return null;
            if (enemy.Presentation != null && enemy.Presentation.Prefab != null)
                return enemy.Presentation.Prefab;
            return enemy.Icon != null ? enemy.Icon : enemy;
        }
    }
}
