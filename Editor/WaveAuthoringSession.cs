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
    internal static class WaveAuthoringSession
    {
        internal static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, IReadOnlyList<WaveProviderV2ListItem> items)
        {
            if (items.Count == 0)
            {
                state.Creating = true;
                state.ClearEditingState();
                return;
            }

            if (!state.Creating && context.SelectedItem == null)
            {
                context.SelectItem(items[0].Source);
                context.RequestRepaint();
            }
        }

        internal static void EnsureEditingState(GameContentAuthoringSurfaceContext context, WaveProviderV2State state)
        {
            if (state.Creating || context.SelectedItem == null)
            {
                state.ClearEditingState();
                return;
            }

            WaveDefinitionAsset selected = context.SelectedItem.Asset as WaveDefinitionAsset;
            if (selected == null)
            {
                state.ClearEditingState();
                return;
            }

            if (state.EditingContext != null && string.Equals(state.EditingContext.Key, context.SelectedItem.Key, StringComparison.Ordinal) && state.EditingState != null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromWaveAsset(selected);
            string fingerprint = WaveAuthoringDraft.BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        internal static void HandleEditCommand(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, WaveDefinitionAsset asset, GameContentAuthoringCommand command)
        {
            if (command == GameContentAuthoringCommand.Revert)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromWaveAsset(asset);
                string fingerprint = WaveAuthoringDraft.BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Reverted");
                state.LastEditResult = null;
                GUI.FocusControl(null);
                context.RequestRepaint();
                return;
            }

            if (command != GameContentAuthoringCommand.Save)
                return;

            state.LastEditResult = WaveDefinitionAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            if (state.LastEditResult != null && state.LastEditResult.Succeeded)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromWaveAsset(asset);
                string fingerprint = WaveAuthoringDraft.BuildStateFingerprint(state.EditingState);
                state.EditingContext.Accept(fingerprint, "Saved");
                context.RefreshLibrary();
            }
            else if (state.LastEditResult != null)
            {
                state.EditingContext.SetStatus(state.LastEditResult.Message);
            }

            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        internal static GameContentAuthoringValidationResult ValidateDraft(WaveAuthoringState draft)
        {
            WaveDefinitionAsset preview = WaveDefinitionAssetCreator.BuildTransient(draft);
            try
            {
                return WaveDefinitionAssetCreator.ValidateForCreation(draft, preview);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(preview);
            }
        }

        internal static void Create(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            GameContentCreationResult result = WaveDefinitionAssetCreator.CreateAssets(draft);
            context.Authoring.SetCreationResult(result);
            if (result != null && result.Succeeded)
            {
                state.Creating = false;
                context.RefreshLibrary();
            }
        }
    }
}
