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
    internal sealed class EnemyProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Stats",
            "Presentation",
            "References",
            "Advanced"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            EnemyAuthoringState draft,
            EnemyGameContentPreviewController previewController,
            EnemyProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            IReadOnlyList<EnemyProviderV2ListItem> items = EnemyProviderV2ListItem.Build(context.AuthoredItems);
            EnemyAuthoringSession.EnsureDefaultMode(context, state, items);
            EnemyAuthoringSession.EnsureEditingState(context, state);
            EnemyAuthoringPreview.TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => EnemyAuthoringLibrary.DrawEnemyList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => EnemyAuthoringPreview.DrawPreviewLab(context, draft, state));
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);

            if (state.Creating)
                EnemyAuthoringWizard.DrawWizard(context, draft, state);
            else
                DrawSelectedDetail(context, state);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedDetail(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state)
        {
            if (context.SelectedItem == null)
            {
                EditorGUILayout.LabelField("Select an enemy to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            EnemyDefinitionAsset selectedAsset = context.SelectedItem.Asset as EnemyDefinitionAsset;
            EnemyAuthoringState selectedState = state.EditingState;
            if (selectedAsset == null || selectedState == null)
            {
                EditorGUILayout.LabelField("Selected item is not an enemy asset.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            GameContentAuthoringValidationResult validation = EnemyDefinitionAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            string fingerprint = EnemyAuthoringDraft.BuildStateFingerprint(selectedState);
            state.EditingContext?.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);
            bool dirty = state.EditingContext != null && state.EditingContext.IsDirty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(selectedState.DisplayName, EnemyAuthoringFields.HeaderStyle);
                    EditorGUILayout.LabelField(selectedState.EnemyId, DeucarianEditorStyles.MutedLabel);
                }

                GUILayout.FlexibleSpace();
                DeucarianEditorMiniToolbar.PingButton(context.SelectedItem.Asset);
            }

            DeucarianEditorStatusChipRow.Draw(
                new DeucarianEditorStatusChip(EnemyAuthoringSummary.GetRoleLabel(selectedState.Role), DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(validation.ErrorCount > 0 ? validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers" : validation.WarningCount > 0 ? validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings" : "Ready", validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(selectedState.Prefab != null ? "Model" : "NoModel", selectedState.Prefab != null ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(EnemyAuthoringSummary.HasAnyVisual(selectedState) ? "VFX" : "NoVFX", EnemyAuthoringSummary.HasAnyVisual(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled),
                new DeucarianEditorStatusChip(EnemyAuthoringSummary.HasAnyAudio(selectedState) ? "Aud" : "Mute", EnemyAuthoringSummary.HasAnyAudio(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled));

            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                dirty,
                "Save",
                state.EditingContext == null ? string.Empty : state.EditingContext.StatusMessage);
            if (command == GameContentAuthoringCommand.Revert)
            {
                EnemyAuthoringSession.ReloadEditingState(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = EnemyDefinitionAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }
            else if (command == GameContentAuthoringCommand.Save)
            {
                EnemyAuthoringSession.SaveEditedEnemy(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = EnemyDefinitionAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }

            GameContentAuthoringProviderGUI.DrawValidationIssues(
                validation,
                GameContentAuthoringValidationSummaryStyle.Edit,
                false);

            state.DetailPage = DeucarianEditorSegmentedControl.DrawPageChips(state.DetailPage, DetailPages);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (state.DetailPage)
            {
                case 1:
                    EnemyAuthoringFields.DrawWizardStats(context, selectedState);
                    EnemyAuthoringFields.DrawBalance(selectedState);
                    break;
                case 2:
                    EnemyAuthoringFields.DrawWizardPresentation(context, selectedState);
                    break;
                case 3:
                    EnemyAuthoringFields.DrawReferences(context.SelectedItem);
                    break;
                case 4:
                    EnemyAuthoringFields.DrawAdvanced(context.SelectedItem, selectedState);
                    break;
                default:
                    EnemyAuthoringFields.DrawEditOverview(context, context.SelectedItem, selectedState);
                    break;
            }

            if (state.LastEditResult != null)
            {
                GUILayout.Space(DeucarianEditorSpacing.Small);
                DeucarianEditorStatusPanel.DrawStatusCard(
                    state.LastEditResult.Message,
                    state.LastEditResult.Succeeded ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error);
            }
        }

        internal static GameContentAuthoringActionPreview BuildEnemyActionPreview(EnemyAuthoringState state, bool playing, double startTime)
        {
            return EnemyAuthoringPreview.BuildEnemyActionPreview(state, playing, startTime);
        }

        internal static IReadOnlyList<DeucarianEditorTimelineEvent> BuildTimelineEvents(EnemyAuthoringState state)
        {
            return EnemyAuthoringPreview.BuildTimelineEvents(state);
        }

        internal static string BuildStateFingerprint(EnemyAuthoringState state)
        {
            return EnemyAuthoringDraft.BuildStateFingerprint(state);
        }
    }
}
