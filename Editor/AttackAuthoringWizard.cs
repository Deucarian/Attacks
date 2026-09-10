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
    internal static class AttackAuthoringWizard
    {
        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Behavior",
            "Delivery",
            "Presentation",
            "Balance",
            "Review"
        };

        internal static void DrawWizard(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DeucarianEditorTextGUI.LabelField("New Attack", AttackAuthoringFields.HeaderStyle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Browse", context.AuthoredItems.Count > 0, GUILayout.Width(60f), GUILayout.Height(22f)))
                {
                    state.LeaveCreate();
                    context.RequestRepaint();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);

            GameContentAuthoringValidationResult validation = AttackAuthoringSession.ValidateDraft(draft);
            context.Authoring.SetValidation(validation);

            switch (state.WizardStep)
            {
                case 1:
                    AttackAuthoringFields.DrawWizardBehavior(context, draft);
                    break;
                case 2:
                    AttackAuthoringFields.DrawWizardDelivery(context, draft);
                    break;
                case 3:
                    AttackAuthoringFields.DrawWizardPresentation(context, draft);
                    break;
                case 4:
                    AttackAuthoringFields.DrawWizardBalance(context, draft);
                    break;
                case 5:
                    DrawWizardReview(context, draft, state, validation);
                    break;
                default:
                    AttackAuthoringFields.DrawWizardIdentity(context, draft);
                    break;
            }

            DrawWizardNavigation(context, state, validation);
        }

        private static void DrawWizardReview(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state, GameContentAuthoringValidationResult validation)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorStatusChipRow.Draw(
                    new DeucarianEditorStatusChip(validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers", validation.ErrorCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                    new DeucarianEditorStatusChip(validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings", validation.WarningCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                    new DeucarianEditorStatusChip(AttackAuthoringSummary.GetTypeLabel(draft), DeucarianEditorStatus.Info));

                string[] lines = AttackRecipeAssetCreator.GetPreviewLines(draft) as string[] ?? new List<string>(AttackRecipeAssetCreator.GetPreviewLines(draft)).ToArray();
                for (int i = 0; i < lines.Length; i++)
                    DeucarianEditorTextGUI.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

                if (validation.Issues.Count > 0)
                {
                    GUILayout.Space(DeucarianEditorSpacing.Small);
                    for (int i = 0; i < validation.Issues.Count; i++)
                    {
                        GameContentAuthoringValidationIssue issue = validation.Issues[i];
                        DeucarianEditorStatus status = issue.Severity == GameContentAuthoringValidationSeverity.Error
                            ? DeucarianEditorStatus.Error
                            : issue.Severity == GameContentAuthoringValidationSeverity.Warning
                                ? DeucarianEditorStatus.Warning
                                : DeucarianEditorStatus.Info;
                        DeucarianEditorStatusBadge.Draw(status.ToString(), status, GUILayout.Width(72f));
                        DeucarianEditorTextGUI.LabelField(issue.Path + ": " + issue.Message, DeucarianEditorStyles.MutedLabel);
                    }
                }

                GUILayout.Space(DeucarianEditorSpacing.Small);
                if (context.Authoring.DrawCreateButton("Create Attack", validation.IsValid))
                {
                    AttackAuthoringSession.Create(context, draft, state);
                }

                context.Authoring.DrawCreationResult();
            });
        }

        private static void DrawWizardNavigation(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, GameContentAuthoringValidationResult validation)
        {
            GUILayout.Space(DeucarianEditorSpacing.Small);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Back", state.WizardStep > 0, GUILayout.Width(74f), GUILayout.Height(24f)))
                    state.WizardStep--;
                GUILayout.FlexibleSpace();
                if (DeucarianEditorButtons.Secondary("Next", state.WizardStep < WizardSteps.Length - 1, GUILayout.Width(74f), GUILayout.Height(24f)))
                    state.WizardStep++;
            }
        }
    }
}
