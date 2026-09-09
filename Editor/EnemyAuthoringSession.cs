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
    internal static class EnemyAuthoringSession
    {
        internal static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, IReadOnlyList<EnemyProviderV2ListItem> items)
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

        internal static void EnsureEditingState(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state)
        {
            if (state.Creating || context.SelectedItem == null)
            {
                state.ClearEditingState();
                return;
            }

            EnemyDefinitionAsset selected = context.SelectedItem.Asset as EnemyDefinitionAsset;
            if (selected == null)
            {
                state.ClearEditingState();
                return;
            }

            if (state.EditingContext != null && string.Equals(state.EditingContext.Key, context.SelectedItem.Key, StringComparison.Ordinal) && state.EditingState != null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromEnemyAsset(selected);
            string fingerprint = EnemyAuthoringDraft.BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        internal static void ReloadEditingState(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyDefinitionAsset asset)
        {
            if (state == null || asset == null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
            string fingerprint = EnemyAuthoringDraft.BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.EditingContext.SetStatus("Reverted");
            state.LastEditResult = null;
            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        internal static void SaveEditedEnemy(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyDefinitionAsset asset)
        {
            if (state == null || asset == null || state.EditingState == null)
                return;

            GameContentCreationResult result = EnemyDefinitionAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            state.LastEditResult = result;
            if (result != null && result.Succeeded)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                string fingerprint = EnemyAuthoringDraft.BuildStateFingerprint(state.EditingState);
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

        internal static GameContentAuthoringValidationResult ValidateDraft(EnemyAuthoringState draft)
        {
            EnemyDefinitionAsset preview = EnemyDefinitionAssetCreator.BuildTransient(draft);
            try
            {
                return EnemyDefinitionAssetCreator.ValidateForCreation(draft, preview);
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(preview);
            }
        }

        internal static void Create(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            GameContentCreationResult result = EnemyDefinitionAssetCreator.CreateAssets(draft);
            context.Authoring.SetCreationResult(result);
            if (result != null && result.Succeeded)
            {
                context.RefreshLibrary();
                state.LeaveCreate();
            }
        }
    }
}
