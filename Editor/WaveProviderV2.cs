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
    internal sealed class WaveProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Entries",
            "Timing",
            "Channels",
            "Balance",
            "References",
            "Advanced"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            WaveAuthoringState draft,
            WaveGameContentPreviewController previewController,
            WaveProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            draft.EnsureEntries();
            IReadOnlyList<WaveProviderV2ListItem> items = WaveProviderV2ListItem.Build(context.AuthoredItems);
            WaveAuthoringSession.EnsureDefaultMode(context, state, items);
            WaveAuthoringSession.EnsureEditingState(context, state);
            WaveAuthoringPreview.TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => WaveAuthoringLibrary.DrawWaveList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => WaveAuthoringPreview.DrawPreviewLab(context, draft, state));
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);
            if (state.Creating)
                WaveAuthoringWizard.DrawCreateWizard(context, draft, state);
            else
                DrawSelectedWave(context, state);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedWave(GameContentAuthoringSurfaceContext context, WaveProviderV2State state)
        {
            WaveDefinitionAsset asset = context.SelectedItem == null ? null : context.SelectedItem.Asset as WaveDefinitionAsset;
            if (asset == null || state.EditingState == null || state.EditingContext == null)
            {
                DeucarianEditorTextGUI.LabelField("Select a wave to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            WaveAuthoringState edit = state.EditingState;
            edit.EnsureEntries();
            GameContentAuthoringValidationResult validation = WaveDefinitionAssetCreator.ValidateForUpdate(edit, asset);
            string fingerprint = WaveAuthoringDraft.BuildStateFingerprint(edit);
            state.EditingContext.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);

            WaveAuthoringFields.DrawHeader(edit.DisplayName, edit.WaveId, WaveAuthoringSummary.BuildWaveChips(edit, validation, context.SelectedItem));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                state.EditingContext.IsDirty,
                "Save",
                state.LastEditResult == null ? state.EditingContext.StatusMessage : state.LastEditResult.Message);
            WaveAuthoringSession.HandleEditCommand(context, state, asset, command);

            state.DetailPage = DeucarianEditorSegmentedControl.DrawPageChips(state.DetailPage, DetailPages);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.DetailPage, 0, DetailPages.Length - 1))
            {
                case 1:
                    WaveAuthoringFields.DrawEntries(context, edit);
                    break;
                case 2:
                    WaveAuthoringFields.DrawTiming(context, edit);
                    break;
                case 3:
                    WaveAuthoringFields.DrawChannels(edit);
                    break;
                case 4:
                    WaveAuthoringFields.DrawBalance(edit);
                    break;
                case 5:
                    WaveAuthoringFields.DrawReferences(context.SelectedItem);
                    break;
                case 6:
                    WaveAuthoringFields.DrawAdvanced(context.SelectedItem, edit, asset);
                    break;
                default:
                    WaveAuthoringFields.DrawOverview(context, edit, context.SelectedItem, false, validation);
                    break;
            }

            GameContentAuthoringProviderGUI.DrawValidationIssues(validation);
        }

        public static string BuildStateFingerprint(WaveAuthoringState state)
        {
            return WaveAuthoringDraft.BuildStateFingerprint(state);
        }

        public static int GetTotalEnemyCount(WaveAuthoringState state)
        {
            return WaveAuthoringSummary.GetTotalEnemyCount(state);
        }

        public static int GetApproximateDurationTicks(WaveAuthoringState state)
        {
            return WaveAuthoringSummary.GetApproximateDurationTicks(state);
        }

        public static string BuildEnemyMixSummary(WaveAuthoringState state)
        {
            return WaveAuthoringSummary.BuildEnemyMixSummary(state);
        }

        public static string BuildChannelSummary(WaveAuthoringState state)
        {
            return WaveAuthoringSummary.BuildChannelSummary(state);
        }

        internal static void MoveEntry(WaveAuthoringState state, int from, int to)
        {
            WaveAuthoringEntryCommands.MoveEntry(state, from, to);
        }

        internal static WaveEntryAuthoringState CopyEntry(WaveEntryAuthoringState entry)
        {
            return WaveAuthoringEntryCommands.CopyEntry(entry);
        }
    }
}
