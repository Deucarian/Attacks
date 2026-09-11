using System;
using System.Linq;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine.UIElements;

namespace Deucarian.Attacks.Editor
{
    internal static class AttackToolkitPreview
    {
        internal static void Add(VisualElement root, AttackAuthoringState state, GameContentAuthoringSurfaceContext context)
        {
            int lastPhase = -1, sourceIndex = 0, targetIndex = 0;
            var sources = AttackGameContentPreviewContext.BuildSourceOptions(context.SelectedItem);
            var targets = AttackGameContentPreviewContext.BuildTargetOptions(context.SelectedItem, context.AllAuthoredItems);
            string status = string.Empty;
            var preview = GameContentToolkitPreview.Add(root,
                () => AttackGameContentPreviewSummaries.GetPrimaryAttackPreviewAsset(state), playback =>
                {
                    var model = AttackGameContentPreviewActions.BuildActionPreview(state, playback.Playing, playback.Started);
                    model.SourcePrefab = sources[sourceIndex].Prefab; model.SourceContextLabel = sources[sourceIndex].Label;
                    model.TargetPrefab = targets[targetIndex].Prefab; model.TargetContextLabel = targets[targetIndex].Label;
                    return model;
                }, () => AttackGameContentPreviewSummaries.BuildAttackRows(state)
                    .Concat(AttackGameContentPreviewSummaries.BuildAttackExpectedRows(state)).ToArray(),
                model => AttackGameContentPreviewActions.PreviewTimelineAudio(state, model, model.Muted, ref lastPhase),
                () => { lastPhase = -1; AttackEditorPreviewAudio.StopAll(); });
            preview.Choice("preview-source", "Source", sources.Select(value => value.Label).ToArray(), () => sourceIndex, value => sourceIndex = value);
            preview.Choice("preview-target", "Target", targets.Select(value => value.Label).ToArray(), () => targetIndex, value => targetIndex = value);
            var events = preview.Section("Audition events", true);
            foreach (AttackPresentationEventKind kind in Enum.GetValues(typeof(AttackPresentationEventKind)))
                events.Action(null, "Preview " + kind, () => { status = AttackGameContentPreviewActions.PreviewAttackEvent(state, kind); events.Refresh(); });
            events.Note(() => status);
        }

        internal static void AddEnemy(VisualElement root, EnemyAuthoringState state)
        {
            var preview = GameContentToolkitPreview.Add(root, () => state.Prefab,
                playback => EnemyAuthoringPreview.BuildEnemyActionPreview(state, playback.Playing, playback.Started),
                () => AttackGameContentPreviewSummaries.BuildEnemyRows(state), stop: AttackEditorPreviewAudio.StopAll);
            string status = string.Empty;
            var events = preview.Section("Audition events", true);
            foreach (EnemyPresentationEventKind kind in Enum.GetValues(typeof(EnemyPresentationEventKind)))
                events.Action(null, "Preview " + kind, () => { status = AttackGameContentPreviewActions.PreviewEnemyEvent(state, kind); events.Refresh(); });
            events.Note(() => status);
        }

        internal static void AddWave(VisualElement root, WaveAuthoringState state)
        {
            var previewState = new WaveProviderV2State();
            GameContentToolkitPreview.Add(root, () => WaveAuthoringPreview.GetPrimaryPreviewAsset(state),
                playback => WaveAuthoringPreview.BuildPreviewOptions(state, previewState).ActionPreview,
                () => AttackGameContentPreviewSummaries.BuildWaveRows(state).Concat(
                    AttackGameContentPreviewSummaries.BuildWaveTimeline(state).Select(item => new GameContentAuthoringPreviewRow(item.Label, item.TimeLabel + " · " + item.Detail))).ToArray());
        }
    }
}
