using System;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Attacks.Editor
{
    internal static class WaveToolkitAuthoring
    {
        internal static VisualElement Create(GameContentAuthoringSurfaceContext context)
        {
            var asset = context.SelectedItem?.Asset as WaveDefinitionAsset;
            return GameContentToolkitDraftEditor.Create(context,
                () => asset == null ? new WaveAuthoringState() : AttackGameContentPreviewSelection.FromWaveAsset(asset),
                WaveAuthoringDraft.BuildStateFingerprint,
                state => asset == null ? WaveAuthoringSession.ValidateDraft(state) : WaveDefinitionAssetCreator.ValidateForUpdate(state, asset),
                state => asset == null ? WaveDefinitionAssetCreator.CreateAssets(state) : WaveDefinitionAssetCreator.UpdateExistingAsset(asset, state),
                (root, state) => { Fields(root, state, context); AttackToolkitPreview.AddWave(root, state); });
        }
        private static void Fields(VisualElement root, WaveAuthoringState state, GameContentAuthoringSurfaceContext context)
        {
            var form = new DeucarianEditorWorkspaceForm(root);
            form.Text("DisplayName", "Name", () => state.DisplayName, value => state.DisplayName = value);
            form.Integer("StartTick", "Start (ticks)", () => state.StartTick, value => state.StartTick = value);
            var advanced = form.Section("Advanced data", true);
            advanced.Text("WaveId", "Wave Id", () => state.WaveId, value => state.WaveId = value);
            advanced.Text("TagsCsv", "Tags Csv", () => state.TagsCsv, value => state.TagsCsv = value);
            advanced.Text("OutputRoot", "Output Root", () => state.OutputRoot, value => state.OutputRoot = value);
            Entries(root, state, context);
        }
        private static void Entries(VisualElement root, WaveAuthoringState state, GameContentAuthoringSurfaceContext context)
        {
            var section = new DeucarianEditorWorkspaceForm(root).Section("Entries", true);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                int index = i; var item = state.Entries[i];
                var form = section.Section("Entry " + (i + 1), true);
                form.ReadOnly("EntryId", "Stable ID", () => item.EntryId);
                form.Asset("Enemy", "Enemy", typeof(EnemyDefinitionAsset), () => item.Enemy, value => item.Enemy = (EnemyDefinitionAsset)value);
                form.Integer("Count", "Count", () => item.Count, value => item.Count = value);
                form.Integer("BatchSize", "Batch Size", () => item.BatchSize, value => item.BatchSize = value);
                form.Integer("InitialDelayTicks", "Initial Delay Ticks", () => item.InitialDelayTicks, value => item.InitialDelayTicks = value);
                form.Integer("IntervalTicks", "Interval Ticks", () => item.IntervalTicks, value => item.IntervalTicks = value);
                form.Text("SpawnChannelId", "Spawn Channel Id", () => item.SpawnChannelId, value => item.SpawnChannelId = value);
                form.Integer("ScalingTier", "Scaling Tier", () => item.ScalingTier, value => item.ScalingTier = value);
                var up = DeucarianEditorWorkspaceControls.Button("Up", () =>
                {
                    state.Entries.RemoveAt(index); state.Entries.Insert(index - 1, item); context.RequestRepaint();
                });
                var down = DeucarianEditorWorkspaceControls.Button("Down", () =>
                {
                    state.Entries.RemoveAt(index); state.Entries.Insert(index + 1, item); context.RequestRepaint();
                });
                up.SetEnabled(index > 0); down.SetEnabled(index < state.Entries.Count - 1);
                form.Root.Add(DeucarianEditorWorkspaceControls.EndActions(up, down,
                    DeucarianEditorWorkspaceControls.Button("Copy", () => { state.Entries.Insert(index + 1, WaveAuthoringEntryCommands.CopyEntry(item)); context.RequestRepaint(); }),
                    DeucarianEditorWorkspaceControls.Button("Remove", () => { state.Entries.Remove(item); context.RequestRepaint(); })));
            }
            section.Action("content-add-entries", "Add entry", () =>
            {
                state.Entries.Add(WaveEntryAuthoringState.CreateNew()); context.RequestRepaint();
            });
        }
    }
}
