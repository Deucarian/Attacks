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
    internal static class EnemyAuthoringWizard
    {
        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Stats",
            "Presentation",
            "Review"
        };

        internal static void DrawWizard(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("New Enemy", EnemyAuthoringFields.HeaderStyle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Browse", context.AuthoredItems.Count > 0, GUILayout.Width(60f), GUILayout.Height(22f)))
                {
                    state.LeaveCreate();
                    context.RequestRepaint();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);

            GameContentAuthoringValidationResult validation = EnemyAuthoringSession.ValidateDraft(draft);
            context.Authoring.SetValidation(validation);

            switch (state.WizardStep)
            {
                case 1:
                    EnemyAuthoringFields.DrawWizardStats(context, draft);
                    EnemyAuthoringFields.DrawBalance(draft);
                    break;
                case 2:
                    EnemyAuthoringFields.DrawWizardPresentation(context, draft);
                    break;
                case 3:
                    DrawWizardReview(context, draft, state, validation);
                    break;
                default:
                    EnemyAuthoringFields.DrawWizardIdentity(context, draft);
                    break;
            }

            DrawWizardNavigation(state);
        }

        private static void DrawWizardReview(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state, GameContentAuthoringValidationResult validation)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorStatusChipRow.Draw(
                    new DeucarianEditorStatusChip(validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers", validation.ErrorCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                    new DeucarianEditorStatusChip(validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings", validation.WarningCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                    new DeucarianEditorStatusChip(EnemyAuthoringSummary.GetRoleLabel(draft.Role), DeucarianEditorStatus.Info));

                string[] lines = EnemyDefinitionAssetCreator.GetPreviewLines(draft) as string[] ?? new List<string>(EnemyDefinitionAssetCreator.GetPreviewLines(draft)).ToArray();
                for (int i = 0; i < lines.Length; i++)
                    EditorGUILayout.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

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
                        EditorGUILayout.LabelField(issue.Path + ": " + issue.Message, DeucarianEditorStyles.MutedLabel);
                    }
                }

                GUILayout.Space(DeucarianEditorSpacing.Small);
                if (context.Authoring.DrawCreateButton("Create Enemy", validation.IsValid))
                {
                    EnemyAuthoringSession.Create(context, draft, state);
                }

                context.Authoring.DrawCreationResult();
            });
        }

        private static void DrawWizardNavigation(EnemyProviderV2State state)
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
