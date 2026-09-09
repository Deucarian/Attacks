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
    internal static class AttackAuthoringPreview
    {
        internal static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_attack__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, () => previewController?.Stop());
        }

        internal static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);

            AttackAuthoringState source = state.Creating
                ? draft
                : state.EditingState ?? AttackGameContentPreviewSelection.ResolveAttackState(context.Preview, draft);
            if (source == null)
            {
                DeucarianEditorTextGUI.LabelField("Preview unavailable.", DeucarianEditorStyles.MutedLabel);
                EditorGUILayout.EndScrollView();
                return;
            }

            IReadOnlyList<AttackGameContentPreviewContextOption> sourceOptions = AttackGameContentPreviewContext.BuildSourceOptions(state.Creating ? null : context.SelectedItem);
            IReadOnlyList<AttackGameContentPreviewContextOption> targetOptions = AttackGameContentPreviewContext.BuildTargetOptions(state.Creating ? null : context.SelectedItem, context.AllAuthoredItems);
            state.PreviewSourceContextIndex = AttackGameContentPreviewContext.ClampIndex(state.PreviewSourceContextIndex, sourceOptions);
            state.PreviewTargetContextIndex = AttackGameContentPreviewContext.ClampIndex(state.PreviewTargetContextIndex, targetOptions);
            AttackGameContentPreviewContextOption sourceOption = sourceOptions[state.PreviewSourceContextIndex];
            AttackGameContentPreviewContextOption targetOption = targetOptions[state.PreviewTargetContextIndex];

            GameContentAuthoringActionPreview actionPreview = AttackGameContentPreviewActions.BuildActionPreview(
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
                actionPreview.SourcePrefab = sourceOption == null ? null : sourceOption.Prefab;
                actionPreview.TargetPrefab = targetOption == null ? null : targetOption.Prefab;
                actionPreview.SourceContextLabel = sourceOption == null ? string.Empty : sourceOption.Label;
                actionPreview.TargetContextLabel = targetOption == null ? string.Empty : targetOption.Label;
            }

            string contextStatus = AttackGameContentPreviewContext.BuildFallbackStatus(sourceOption, targetOption);
            if (!string.IsNullOrWhiteSpace(contextStatus))
            {
                state.PreviewStatus = contextStatus;
                context.Preview.SetStatus(state.PreviewStatus);
            }

            AttackProviderV2PreviewScope scope = AttackProviderV2PreviewModel.GetScope(
                state.Creating,
                state.EditingContext != null && state.EditingContext.IsDirty);

            GameContentPreviewLabRenderer.Draw(
                context.Preview,
                new GameContentPreviewLabModel
                {
                    Title = AttackProviderV2PreviewModel.BuildHeaderTitle(source, scope),
                    PreviewTitle = AttackProviderV2PreviewModel.BuildViewportTitle(source, scope),
                    ScopeLabel = AttackProviderV2PreviewModel.GetScopeLabel(scope),
                    PrimaryAsset = AttackGameContentPreviewSummaries.GetPrimaryAttackPreviewAsset(source),
                    EmptyText = "No visual asset assigned.",
                    PreviewOptions = new GameContentAuthoringObjectPreviewOptions
                    {
                        MinimumHeight = 220f,
                        ActionPreview = actionPreview
                    },
                    DrawControls = () => DrawPreviewControls(context, source, state, actionPreview),
                    DrawContext = () => DrawPreviewContextControls(state, sourceOptions, targetOptions),
                    DrawBody = () => DrawPreviewBody(context, source, state),
                    Chips = AttackProviderV2PreviewModel.BuildChips(source, state, scope)
                });

            string timelineAudioStatus = AttackGameContentPreviewActions.PreviewTimelineAudio(source, actionPreview, state.PreviewMuted, ref state.LastPreviewAudioPhase);
            if (!string.IsNullOrWhiteSpace(timelineAudioStatus))
            {
                state.PreviewStatus = timelineAudioStatus;
                context.Preview.SetStatus(state.PreviewStatus);
            }

            if (!string.IsNullOrWhiteSpace(state.PreviewStatus))
                DeucarianEditorTextGUI.LabelField(state.PreviewStatus, DeucarianEditorStyles.MutedLabel);

            if (state.PreviewPlaying)
                context.RequestRepaint();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, AttackAuthoringState source, AttackProviderV2State state)
        {
            context.Preview.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildAttackRows(source));
            Action<AttackPresentationEventKind> previewEvent = null;
            if (AttackProviderV2PreviewModel.EventRowsExposePreviewActions)
            {
                previewEvent = eventKind =>
                {
                    state.PreviewStatus = AttackGameContentPreviewActions.PreviewAttackEvent(source, eventKind, state.PreviewMuted);
                    context.Preview.SetStatus(state.PreviewStatus);
                };
            }

            AttackAuthoringFields.DrawPresentation(source, previewEvent, state);
        }

        private static void DrawPreviewControls(
            GameContentAuthoringSurfaceContext context,
            AttackAuthoringState source,
            AttackProviderV2State state,
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
                        state.LastPreviewAudioPhase = -1;
                        state.PreviewStatus = "Preview paused";
                    }
                    else
                    {
                        float duration = actionPreview == null ? 2.4f : actionPreview.DurationSeconds;
                        state.PreviewStartTime = EditorApplication.timeSinceStartup - (state.PausedNormalizedTime * duration / Mathf.Max(0.01f, state.PreviewSpeed));
                        state.PreviewPlaying = true;
                        state.LastPreviewAudioPhase = -1;
                        state.PreviewStatus = "Preview playing";
                    }
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    AttackEditorPreviewAudio.StopAll();
                    state.PreviewPlaying = false;
                    state.PausedNormalizedTime = 0f;
                    state.LastPreviewAudioPhase = -1;
                    state.PreviewStatus = "Preview stopped";
                }

                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;

                if (DeucarianEditorMiniToolbar.Button(state.PreviewMuted ? "Muted" : "Audio", true, GUILayout.Width(54f), GUILayout.Height(22f)))
                {
                    state.PreviewMuted = !state.PreviewMuted;
                    if (state.PreviewMuted)
                        AttackEditorPreviewAudio.StopAll();
                    state.LastPreviewAudioPhase = -1;
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
                if (DeucarianEditorMiniToolbar.Button("Full", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = true;
                    state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    state.LastPreviewAudioPhase = -1;
                    state.PreviewStatus = AttackGameContentPreviewActions.PreviewFullAttack(source, state.PreviewMuted);
                    context.Preview.SetStatus(state.PreviewStatus);
                }
            }
        }

        private static void DrawPreviewContextControls(
            AttackProviderV2State state,
            IReadOnlyList<AttackGameContentPreviewContextOption> sourceOptions,
            IReadOnlyList<AttackGameContentPreviewContextOption> targetOptions)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DeucarianEditorTextGUI.LabelField("Source", GUILayout.Width(48f));
                state.PreviewSourceContextIndex = DeucarianEditorInputGUI.Popup(
                    state.PreviewSourceContextIndex,
                    AttackGameContentPreviewContext.BuildLabels(sourceOptions),
                    GUILayout.ExpandWidth(true));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DeucarianEditorTextGUI.LabelField("Target", GUILayout.Width(48f));
                state.PreviewTargetContextIndex = DeucarianEditorInputGUI.Popup(
                    state.PreviewTargetContextIndex,
                    AttackGameContentPreviewContext.BuildLabels(targetOptions),
                    GUILayout.ExpandWidth(true));
            }
        }

        internal static IReadOnlyList<DeucarianEditorTimelineEvent> BuildTimelineEvents(AttackAuthoringState state)
        {
            return new[]
            {
                Timeline("OnCast", state.CastVfxPrefab, state.CastAudio, true),
                Timeline("OnFire", state.FireVfxPrefab, state.FireAudio, true),
                Timeline(state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan ? "Travel / Beam" : "Travel", state.BeamVfxPrefab ?? state.ProjectilePrefab, null, true),
                Timeline("OnImpact", state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab, state.ImpactAudio, true),
                Timeline("Status Tick", state.TickVfxPrefab, state.TickAudio, state.IncludeStatusEffect),
                Timeline("OnExpire", state.ExpireVfxPrefab, state.ExpireAudio, state.IncludeStatusEffect)
            };
        }

        private static DeucarianEditorTimelineEvent Timeline(string label, GameObject visual, AudioClip audio, bool enabled)
        {
            string detail = visual == null && audio == null
                ? "Optional presentation asset not assigned."
                : string.Join(", ", BuildAssignedParts(visual, audio));
            return new DeucarianEditorTimelineEvent(label, detail, visual != null, audio != null, enabled);
        }

        private static IEnumerable<string> BuildAssignedParts(GameObject visual, AudioClip audio)
        {
            if (visual != null) yield return "VFX " + visual.name;
            if (audio != null) yield return "audio " + audio.name;
        }

        internal static AttackPresentationEventKind GetEventKind(int index)
        {
            switch (index)
            {
                case 0:
                    return AttackPresentationEventKind.OnCast;
                case 1:
                    return AttackPresentationEventKind.OnFire;
                case 4:
                    return AttackPresentationEventKind.OnTick;
                case 5:
                    return AttackPresentationEventKind.OnExpire;
                default:
                    return AttackPresentationEventKind.OnImpact;
            }
        }
    }
}
