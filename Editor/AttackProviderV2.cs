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
    internal sealed class AttackProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Behavior",
            "Delivery",
            "Presentation",
            "Balance",
            "References",
            "Advanced"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            AttackAuthoringState draft,
            AttackGameContentPreviewController previewController,
            AttackProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            IReadOnlyList<AttackProviderV2ListItem> items = AttackProviderV2ListItem.Build(context.AuthoredItems);
            AttackAuthoringSession.EnsureDefaultMode(context, state, items);
            AttackAuthoringSession.EnsureEditingState(context, state);
            AttackAuthoringPreview.TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => AttackAuthoringLibrary.DrawAttackList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => AttackAuthoringPreview.DrawPreviewLab(context, draft, state));
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);

            if (state.Creating)
                AttackAuthoringWizard.DrawWizard(context, draft, state);
            else
                DrawSelectedDetail(context, draft, state);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedDetail(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            if (context.SelectedItem == null)
            {
                DeucarianEditorTextGUI.LabelField("Select an attack to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            AttackDefinitionAsset selectedAsset = context.SelectedItem.Asset as AttackDefinitionAsset;
            AttackAuthoringState selectedState = state.EditingState;
            if (selectedAsset == null || selectedState == null)
            {
                DeucarianEditorTextGUI.LabelField("Selected item is not an attack asset.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            GameContentAuthoringValidationResult validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            string fingerprint = AttackAuthoringDraft.BuildStateFingerprint(selectedState);
            state.EditingContext?.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);
            bool dirty = state.EditingContext != null && state.EditingContext.IsDirty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    DeucarianEditorTextGUI.LabelField(selectedState.DisplayName, AttackAuthoringFields.HeaderStyle);
                    DeucarianEditorTextGUI.LabelField(selectedState.AttackId, DeucarianEditorStyles.MutedLabel);
                }

                GUILayout.FlexibleSpace();
                DeucarianEditorMiniToolbar.PingButton(context.SelectedItem.Asset);
            }

            DeucarianEditorStatusChipRow.Draw(
                new DeucarianEditorStatusChip(AttackAuthoringSummary.GetCompactTypeLabel(AttackAuthoringSummary.GetTypeLabel(selectedState)), DeucarianEditorStatus.Info, AttackAuthoringSummary.GetTypeLabel(selectedState)),
                new DeucarianEditorStatusChip(validation.ErrorCount > 0 ? validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers" : validation.WarningCount > 0 ? validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings" : "Ready", validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(AttackAuthoringSummary.HasAnyVisual(selectedState) ? "VFX" : "NoVFX", AttackAuthoringSummary.HasAnyVisual(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, AttackAuthoringSummary.HasAnyVisual(selectedState) ? "Visual presentation assigned" : "No visual presentation assigned"),
                new DeucarianEditorStatusChip(AttackAuthoringSummary.HasAnyAudio(selectedState) ? "Aud" : "Mute", AttackAuthoringSummary.HasAnyAudio(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, AttackAuthoringSummary.HasAnyAudio(selectedState) ? "Audio presentation assigned" : "No audio presentation assigned"));

            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                dirty,
                "Save",
                state.EditingContext == null ? string.Empty : state.EditingContext.StatusMessage);
            if (command == GameContentAuthoringCommand.Revert)
            {
                AttackAuthoringSession.ReloadEditingState(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }
            else if (command == GameContentAuthoringCommand.Save)
            {
                AttackAuthoringSession.SaveEditedAttack(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
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
                    AttackAuthoringFields.DrawWizardBehavior(context, selectedState);
                    break;
                case 2:
                    AttackAuthoringFields.DrawWizardDelivery(context, selectedState);
                    break;
                case 3:
                    AttackAuthoringFields.DrawWizardPresentation(context, selectedState);
                    break;
                case 4:
                    AttackAuthoringFields.DrawWizardBalance(context, selectedState);
                    break;
                case 5:
                    AttackAuthoringFields.DrawReferences(context.SelectedItem);
                    break;
                case 6:
                    AttackAuthoringFields.DrawAdvanced(context.SelectedItem, selectedState);
                    break;
                default:
                    AttackAuthoringFields.DrawEditOverview(context, context.SelectedItem, selectedState);
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
    }
}
