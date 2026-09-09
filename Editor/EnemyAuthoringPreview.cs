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
    internal static class EnemyAuthoringPreview
    {
        internal static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_enemy__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, () => previewController?.Stop());
        }

        internal static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);

            EnemyAuthoringState source = state.Creating
                ? draft
                : state.EditingState ?? AttackGameContentPreviewSelection.ResolveEnemyState(context.Preview, draft);
            if (source == null)
            {
                EditorGUILayout.LabelField("Preview unavailable.", DeucarianEditorStyles.MutedLabel);
                EditorGUILayout.EndScrollView();
                return;
            }

            GameContentAuthoringActionPreview actionPreview = BuildEnemyActionPreview(
                source,
                state.PreviewPlaying,
                state.PreviewStartTime <= 0d ? EditorApplication.timeSinceStartup : state.PreviewStartTime);
            if (actionPreview != null)
            {
                actionPreview.Loop = state.PreviewLoop;
                actionPreview.Speed = state.PreviewSpeed;
                actionPreview.StaticNormalizedTime = state.PausedNormalizedTime;
                actionPreview.Muted = state.PreviewMuted;
                actionPreview.RenderMode = state.PreviewRenderMode;
            }

            EnemyProviderV2PreviewScope scope = EnemyProviderV2PreviewModel.GetScope(
                state.Creating,
                state.EditingContext != null && state.EditingContext.IsDirty);

            GameContentPreviewLabRenderer.Draw(
                context.Preview,
                new GameContentPreviewLabModel
                {
                    Title = EnemyProviderV2PreviewModel.BuildHeaderTitle(source, scope),
                    PreviewTitle = EnemyProviderV2PreviewModel.BuildViewportTitle(source, scope),
                    ScopeLabel = EnemyProviderV2PreviewModel.GetScopeLabel(scope),
                    PrimaryAsset = source.Prefab,
                    EmptyText = "Assign an enemy prefab to render the game preview.",
                    PreviewOptions = new GameContentAuthoringObjectPreviewOptions
                    {
                        MinimumHeight = 220f,
                        ActionPreview = actionPreview
                    },
                    DrawControls = () => DrawPreviewControls(context, state, actionPreview),
                    DrawContext = () => DrawPreviewContext(source, scope),
                    DrawBody = () => DrawPreviewBody(context, source),
                    Chips = EnemyProviderV2PreviewModel.BuildChips(source, state, scope)
                });

            if (!string.IsNullOrWhiteSpace(state.PreviewStatus))
                EditorGUILayout.LabelField(state.PreviewStatus, DeucarianEditorStyles.MutedLabel);

            if (state.PreviewPlaying)
                context.RequestRepaint();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, EnemyAuthoringState source)
        {
            context.Preview.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyRows(source));
            context.Preview.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyPresentationRows(source));
            EnemyAuthoringFields.DrawPresentation(source);
            context.Preview.DrawWarnings(AttackGameContentPreviewSummaries.BuildEnemyWarnings(source));
        }

        private static void DrawPreviewControls(
            GameContentAuthoringSurfaceContext context,
            EnemyProviderV2State state,
            GameContentAuthoringActionPreview actionPreview)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playLabel = state.PreviewPlaying ? "Pause" : "Play";
                if (DeucarianEditorMiniToolbar.Button(playLabel, true, GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    if (state.PreviewPlaying)
                    {
                        state.PausedNormalizedTime = actionPreview == null ? 0.5f : actionPreview.GetNormalizedTime(EditorApplication.timeSinceStartup);
                        state.PreviewPlaying = false;
                        state.PreviewStatus = "Preview paused";
                    }
                    else
                    {
                        float duration = actionPreview == null ? 2.4f : actionPreview.DurationSeconds;
                        state.PreviewStartTime = EditorApplication.timeSinceStartup - (state.PausedNormalizedTime * duration / Mathf.Max(0.01f, state.PreviewSpeed));
                        state.PreviewPlaying = true;
                        state.PreviewStatus = "Preview playing";
                    }
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    AttackEditorPreviewAudio.StopAll();
                    state.PreviewPlaying = false;
                    state.PausedNormalizedTime = 0f;
                    state.PreviewStatus = "Preview stopped";
                }

                if (DeucarianEditorMiniToolbar.Button("Restart", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = true;
                    state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    state.PausedNormalizedTime = 0f;
                    state.PreviewStatus = "Preview restarted";
                    context.Preview.SetStatus(state.PreviewStatus);
                }

                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;

                if (DeucarianEditorMiniToolbar.Button(state.PreviewMuted ? "Muted" : "Audio", true, GUILayout.Width(54f), GUILayout.Height(22f)))
                {
                    state.PreviewMuted = !state.PreviewMuted;
                    if (state.PreviewMuted)
                        AttackEditorPreviewAudio.StopAll();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] speeds = { "0.5x", "1x", "2x" };
                int selectedSpeed = state.PreviewSpeed < 0.75f ? 0 : state.PreviewSpeed > 1.5f ? 2 : 1;
                int nextSpeed = DeucarianEditorSegmentedControl.Draw(selectedSpeed, speeds, GUILayout.ExpandWidth(true));
                state.PreviewSpeed = nextSpeed == 0 ? 0.5f : nextSpeed == 2 ? 2f : 1f;
                string[] modes = { "Game", "Debug" };
                int selectedMode = state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? 1 : 0;
                int nextMode = DeucarianEditorSegmentedControl.Draw(selectedMode, modes, GUILayout.Width(132f));
                state.PreviewRenderMode = nextMode == 1 ? GameContentAuthoringActionPreviewRenderMode.Debug : GameContentAuthoringActionPreviewRenderMode.Game;
            }
        }

        private static void DrawPreviewContext(EnemyAuthoringState source, EnemyProviderV2PreviewScope scope)
        {
            DeucarianEditorFieldRow.Draw("Source", () => EditorGUILayout.LabelField(scope == EnemyProviderV2PreviewScope.Draft ? "New enemy draft" : "Selected enemy asset", DeucarianEditorStyles.MutedLabel));
            DeucarianEditorFieldRow.Draw("Target", () => EditorGUILayout.LabelField(EnemyAuthoringSummary.GetRoleLabel(source.Role) + " - " + source.EnemyId, DeucarianEditorStyles.MutedLabel));
        }

        internal static GameContentAuthoringActionPreview BuildEnemyActionPreview(EnemyAuthoringState state, bool playing, double startTime)
        {
            if (state == null || state.Prefab == null)
                return null;

            var preview = new GameContentAuthoringActionPreview
            {
                PrimaryAsset = state.Prefab,
                TargetPrefab = state.Prefab,
                ImpactVfxPrefab = state.HitVfxPrefab ?? state.SpawnVfxPrefab ?? state.DeathVfxPrefab,
                TickVfxPrefab = state.SpawnVfxPrefab,
                ExpireVfxPrefab = state.DeathVfxPrefab,
                Mode = GameContentAuthoringActionPreviewMode.Static,
                Playing = playing,
                StartTime = startTime,
                StaticNormalizedTime = 0.5f,
                DurationSeconds = 2.4f,
                Label = string.IsNullOrWhiteSpace(state.DisplayName) ? state.EnemyId : state.DisplayName,
                DeliveryTypeLabel = EnemyAuthoringSummary.GetRoleLabel(state.Role) + " enemy",
                AccentColor = EnemyAuthoringSummary.GetRoleAccent(state.Role)
            };
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Spawn", EnemyAuthoringSummary.GetObjectLabel(state.SpawnVfxPrefab, "spawn point"), state.SpawnVfxPrefab));
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Enemy", EnemyAuthoringSummary.GetObjectLabel(state.Prefab, state.DisplayName), state.Prefab));
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Hit", EnemyAuthoringSummary.GetObjectLabel(state.HitVfxPrefab, "hit response"), state.HitVfxPrefab));
            return preview;
        }

        internal static IReadOnlyList<DeucarianEditorTimelineEvent> BuildTimelineEvents(EnemyAuthoringState state)
        {
            return new[]
            {
                Timeline("OnSpawn", state.SpawnVfxPrefab, state.SpawnAudio),
                Timeline("OnHit", state.HitVfxPrefab, state.HitAudio),
                Timeline("OnDeath", state.DeathVfxPrefab, state.DeathAudio)
            };
        }

        private static DeucarianEditorTimelineEvent Timeline(string label, GameObject visual, AudioClip audio)
        {
            string detail = visual == null && audio == null
                ? "Optional presentation asset not assigned."
                : string.Join(", ", BuildAssignedParts(visual, audio));
            return new DeucarianEditorTimelineEvent(label, detail, visual != null, audio != null, true);
        }

        private static IEnumerable<string> BuildAssignedParts(GameObject visual, AudioClip audio)
        {
            if (visual != null) yield return "VFX " + visual.name;
            if (audio != null) yield return "audio " + audio.name;
        }
    }
}
