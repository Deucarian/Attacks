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
    internal static class WaveAuthoringWizard
    {
        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Entries",
            "Timing",
            "Channels",
            "Balance",
            "Review"
        };

        internal static void DrawCreateWizard(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            draft.EnsureEntries();
            GameContentAuthoringValidationResult validation = WaveAuthoringSession.ValidateDraft(draft);
            WaveAuthoringFields.DrawHeader("New Wave", draft.WaveId, WaveAuthoringSummary.BuildWaveChips(draft, validation, null));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(GameContentAuthoringWorkbenchMode.Create, validation.IsValid, true, "Create");
            if (command == GameContentAuthoringCommand.Create)
            {
                WaveAuthoringSession.Create(context, draft, state);
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.WizardStep, 0, WizardSteps.Length - 1))
            {
                case 1:
                    WaveAuthoringFields.DrawEntries(context, draft);
                    break;
                case 2:
                    WaveAuthoringFields.DrawTiming(context, draft);
                    break;
                case 3:
                    WaveAuthoringFields.DrawChannels(draft);
                    break;
                case 4:
                    WaveAuthoringFields.DrawBalance(draft);
                    break;
                case 5:
                    DrawReview(draft, validation);
                    break;
                default:
                    WaveAuthoringFields.DrawOverview(context, draft, null, true, validation);
                    break;
            }

            GameContentAuthoringProviderGUI.DrawValidationIssues(validation);
            context.Authoring.DrawCreationResult();
        }

        private static void DrawReview(WaveAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            IReadOnlyList<string> lines = WaveDefinitionAssetCreator.GetPreviewLines(state);
            for (int i = 0; i < lines.Count; i++)
                EditorGUILayout.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

            WaveAuthoringFields.DrawSummaryRows(
                WaveAuthoringSummary.Row("Readiness", new GameContentAuthoringValidationSummary(validation).ReadinessLabel),
                WaveAuthoringSummary.Row("Entries", state.Entries.Count.ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Total Enemies", WaveAuthoringSummary.GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                WaveAuthoringSummary.Row("Channels", WaveAuthoringSummary.BuildChannelSummary(state)));
        }
    }
}
