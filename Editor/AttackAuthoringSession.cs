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
    internal static class AttackAuthoringSession
    {
        internal static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, IReadOnlyList<AttackProviderV2ListItem> items)
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

        internal static void EnsureEditingState(GameContentAuthoringSurfaceContext context, AttackProviderV2State state)
        {
            if (context == null || state == null)
                return;

            if (state.Creating || context.SelectedItem == null)
            {
                state.ClearEditingState();
                return;
            }

            AttackDefinitionAsset selected = context.SelectedItem.Asset as AttackDefinitionAsset;
            if (selected == null)
            {
                state.ClearEditingState();
                return;
            }

            if (state.EditingContext != null && string.Equals(state.EditingContext.Key, context.SelectedItem.Key, StringComparison.Ordinal) && state.EditingState != null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromAttackAsset(selected);
            string fingerprint = AttackAuthoringDraft.BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        internal static void ReloadEditingState(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackDefinitionAsset asset)
        {
            if (state == null || asset == null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromAttackAsset(asset);
            string fingerprint = AttackAuthoringDraft.BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.EditingContext.SetStatus("Reverted");
            state.LastEditResult = null;
            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        internal static void SaveEditedAttack(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackDefinitionAsset asset)
        {
            if (state == null || asset == null || state.EditingState == null)
                return;

            GameContentCreationResult result = AttackRecipeAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            state.LastEditResult = result;
            if (result != null && result.Succeeded)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromAttackAsset(asset);
                string fingerprint = AttackAuthoringDraft.BuildStateFingerprint(state.EditingState);
                if (state.EditingContext == null || context.SelectedItem == null)
                    state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
                state.EditingContext.Accept(fingerprint, "Saved");
                context.RefreshLibrary();
            }
            else if (state.EditingContext != null && result != null)
            {
                state.EditingContext.SetStatus(result.Message);
            }

            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        internal static GameContentAuthoringValidationResult ValidateDraft(AttackAuthoringState draft)
        {
            AttackDefinitionAsset preview = AttackRecipeAssetCreator.BuildTransient(draft);
            try
            {
                return AttackRecipeAssetCreator.ValidateForCreation(draft, preview);
            }
            finally
            {
                AttackRecipeAssetCreator.DestroyTransient(preview);
            }
        }

        internal static void Create(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            GameContentCreationResult result = AttackRecipeAssetCreator.CreateAssets(draft);
            context.Authoring.SetCreationResult(result);
            if (result != null && result.Succeeded)
            {
                context.RefreshLibrary();
                state.LeaveCreate();
            }
        }
    }
}
