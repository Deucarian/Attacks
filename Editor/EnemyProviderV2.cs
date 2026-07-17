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
    internal sealed class EnemyProviderV2State : GameContentAuthoringProviderSessionState<EnemyAuthoringState>
    {
        public void BeginCreate()
        {
            Creating = true;
            DetailScroll = Vector2.zero;
            WizardStep = 0;
            ClearEditingState();
            PreviewStatus = "Previewing draft";
        }

        public void LeaveCreate()
        {
            Creating = false;
            DetailScroll = Vector2.zero;
            PreviewStatus = "Previewing selected enemy";
        }
    }

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

        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Stats",
            "Presentation",
            "Review"
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
            EnsureDefaultMode(context, state, items);
            EnsureEditingState(context, state);
            TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => DrawEnemyList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => DrawPreviewLab(context, draft, state));
        }

        private static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, IReadOnlyList<EnemyProviderV2ListItem> items)
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

        private static void EnsureEditingState(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state)
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
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        private static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_enemy__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, () => previewController?.Stop());
        }

        private static void DrawEnemyList(
            GameContentAuthoringSurfaceContext context,
            EnemyProviderV2State state,
            IReadOnlyList<EnemyProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Enemies", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Refresh", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                    context.RefreshLibrary();
            }

            state.SearchText = DeucarianEditorSearchField.Draw(state.SearchText, "Search enemies", GUILayout.ExpandWidth(true));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Create New", true, GUILayout.Height(24f)))
                {
                    state.BeginCreate();
                    context.RequestRepaint();
                }
            }

            GUILayout.Space(DeucarianEditorSpacing.Small);
            state.ListScroll = EditorGUILayout.BeginScrollView(state.ListScroll);
            int shown = 0;
            for (int i = 0; i < items.Count; i++)
            {
                EnemyProviderV2ListItem item = items[i];
                if (!item.Matches(state.SearchText))
                    continue;

                shown++;
                DrawEnemyCard(context, state, item);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(items.Count == 0 ? "No authored enemies found." : "No enemies match the current search.", DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawEnemyCard(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyProviderV2ListItem item)
        {
            bool selected = !state.Creating && context.IsSelected(item.Source);
            var chips = new[]
            {
                new DeucarianEditorStatusChip(item.RoleLabel, DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(item.ReadinessLabel, item.ReadinessStatus),
                new DeucarianEditorStatusChip(item.HasPrefab ? "Model" : "NoModel", item.HasPrefab ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error, item.HasPrefab ? "Enemy prefab assigned" : "Enemy prefab missing"),
                new DeucarianEditorStatusChip(item.HasVisuals ? "VFX" : "NoVFX", item.HasVisuals ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, item.HasVisuals ? "Visual presentation assigned" : "No visual presentation assigned"),
                new DeucarianEditorStatusChip(item.HasAudio ? "Audio" : "Mute", item.HasAudio ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, item.HasAudio ? "Audio presentation assigned" : "No audio presentation assigned")
            };

            bool clicked = DeucarianEditorCompactObjectCard.Draw(
                item.DisplayName,
                item.StableId,
                selected,
                chips,
                () =>
                {
                    if (DeucarianEditorMiniToolbar.PingButton(item.Source.Asset))
                        GUI.FocusControl(null);
                },
                null,
                GUILayout.ExpandWidth(true));

            if (clicked && item.Source != null)
            {
                state.Creating = false;
                state.DetailScroll = Vector2.zero;
                context.SelectItem(item.Source);
                Event.current.Use();
            }
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);

            if (state.Creating)
                DrawWizard(context, draft, state);
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
            string fingerprint = BuildStateFingerprint(selectedState);
            state.EditingContext?.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);
            bool dirty = state.EditingContext != null && state.EditingContext.IsDirty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(selectedState.DisplayName, HeaderStyle);
                    EditorGUILayout.LabelField(selectedState.EnemyId, DeucarianEditorStyles.MutedLabel);
                }

                GUILayout.FlexibleSpace();
                DeucarianEditorMiniToolbar.PingButton(context.SelectedItem.Asset);
            }

            DeucarianEditorStatusChipRow.Draw(
                new DeucarianEditorStatusChip(GetRoleLabel(selectedState.Role), DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(validation.ErrorCount > 0 ? validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers" : validation.WarningCount > 0 ? validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings" : "Ready", validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(selectedState.Prefab != null ? "Model" : "NoModel", selectedState.Prefab != null ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(HasAnyVisual(selectedState) ? "VFX" : "NoVFX", HasAnyVisual(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled),
                new DeucarianEditorStatusChip(HasAnyAudio(selectedState) ? "Aud" : "Mute", HasAnyAudio(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled));

            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                dirty,
                "Save",
                state.EditingContext == null ? string.Empty : state.EditingContext.StatusMessage);
            if (command == GameContentAuthoringCommand.Revert)
            {
                ReloadEditingState(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = EnemyDefinitionAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }
            else if (command == GameContentAuthoringCommand.Save)
            {
                SaveEditedEnemy(context, state, selectedAsset);
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
                    DrawWizardStats(context, selectedState);
                    DrawBalance(selectedState);
                    break;
                case 2:
                    DrawWizardPresentation(context, selectedState);
                    break;
                case 3:
                    DrawReferences(context.SelectedItem);
                    break;
                case 4:
                    DrawAdvanced(context.SelectedItem, selectedState);
                    break;
                default:
                    DrawEditOverview(context, context.SelectedItem, selectedState);
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

        private static void ReloadEditingState(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyDefinitionAsset asset)
        {
            if (state == null || asset == null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.EditingContext.SetStatus("Reverted");
            state.LastEditResult = null;
            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        private static void SaveEditedEnemy(GameContentAuthoringSurfaceContext context, EnemyProviderV2State state, EnemyDefinitionAsset asset)
        {
            if (state == null || asset == null || state.EditingState == null)
                return;

            GameContentCreationResult result = EnemyDefinitionAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            state.LastEditResult = result;
            if (result != null && result.Succeeded)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
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

        private static void DrawEditOverview(GameContentAuthoringSurfaceContext context, GameContentLibraryItem item, EnemyAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                state.EnemyId = context.Authoring.DrawTextField("Stable ID", state.EnemyId);
                state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
                state.Icon = context.Authoring.DrawObjectField("Icon", state.Icon);
                state.Role = context.Authoring.DrawEnumPopup("Role", state.Role);
                state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawStats(EnemyAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Health", FormatFloat(state.MaximumHealth));
                DrawValue("Move Speed", FormatFloat(state.MoveSpeed));
                DrawValue("Reward", state.RewardValue.ToString(CultureInfo.InvariantCulture));
                DrawValue("Contact Damage", FormatFloat(state.ContactDamage) + " " + state.DamageTypeId);
                DrawValue("Collision Radius", FormatFloat(state.CollisionRadius));
            });
        }

        private static void DrawPresentation(EnemyAuthoringState state)
        {
            DeucarianEditorEventTimeline.Draw(BuildTimelineEvents(state));
        }

        private static void DrawBalance(EnemyAuthoringState state)
        {
            float contactPerSecond = state.ContactDamage * Mathf.Max(0.25f, state.MoveSpeed);
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Threat Estimate", FormatFloat(contactPerSecond) + " contact pressure");
                DrawValue("Health / Reward", FormatFloat(state.MaximumHealth) + " HP / " + state.RewardValue.ToString(CultureInfo.InvariantCulture));
                DrawValue("Role", GetRoleLabel(state.Role));
            });
        }

        private static void DrawReferences(GameContentLibraryItem item)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                if (item.ReverseReferences == null || item.ReverseReferences.Count == 0)
                {
                    EditorGUILayout.LabelField("No known waves, sets, or packs reference this enemy.", DeucarianEditorStyles.MutedLabel);
                    return;
                }

                for (int i = 0; i < item.ReverseReferences.Count; i++)
                {
                    GameContentLibraryReference reference = item.ReverseReferences[i];
                    if (reference == null || reference.Target == null)
                        continue;
                    EditorGUILayout.LabelField(reference.Target.DisplayName + "  (" + reference.Target.Category + ")", DeucarianEditorStyles.MutedLabel);
                }
            });
        }

        private static void DrawAdvanced(GameContentLibraryItem item, EnemyAuthoringState state)
        {
            DeucarianEditorDiagnosticsDrawer.Draw("enemy-v2-advanced-" + item.Key, "Raw Details", () =>
            {
                DrawValue("Asset Path", item.Path);
                DrawValue("Folder", item.Folder);
                DrawValue("Stable ID", state.EnemyId);
                DrawValue("Tags", state.TagsCsv);
                if (GUILayout.Button("Copy Report", DeucarianEditorButtons.SecondaryStyle, GUILayout.Height(24f)))
                    EditorGUIUtility.systemCopyBuffer = BuildAdvancedReport(item, state);
            }, true);

            DeucarianEditorDiagnosticsDrawer.Draw("enemy-v2-references-" + item.Key, "Serialized References", () =>
            {
                GameContentAuthoringProviderGUI.DrawReferenceList("Direct", item.DirectReferences);
                GameContentAuthoringProviderGUI.DrawReferenceList("Referenced By", item.ReverseReferences);
            });
        }

        private static void DrawWizard(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("New Enemy", HeaderStyle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Browse", context.AuthoredItems.Count > 0, GUILayout.Width(60f), GUILayout.Height(22f)))
                {
                    state.LeaveCreate();
                    context.RequestRepaint();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);

            GameContentAuthoringValidationResult validation = ValidateDraft(draft);
            context.Authoring.SetValidation(validation);

            switch (state.WizardStep)
            {
                case 1:
                    DrawWizardStats(context, draft);
                    DrawBalance(draft);
                    break;
                case 2:
                    DrawWizardPresentation(context, draft);
                    break;
                case 3:
                    DrawWizardReview(context, draft, state, validation);
                    break;
                default:
                    DrawWizardIdentity(context, draft);
                    break;
            }

            DrawWizardNavigation(state);
        }

        private static void DrawWizardIdentity(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.EnemyId = context.Authoring.DrawTextField("Stable ID", draft.EnemyId);
                draft.DisplayName = context.Authoring.DrawTextField("Display Name", draft.DisplayName);
                draft.Icon = context.Authoring.DrawObjectField("Icon", draft.Icon);
                draft.Role = context.Authoring.DrawEnumPopup("Role", draft.Role);
                draft.TagsCsv = context.Authoring.DrawTextField("Tags", draft.TagsCsv);
                DeucarianEditorDiagnosticsDrawer.Draw("enemy-v2-create-output", "Advanced Output", () =>
                {
                    draft.OutputRoot = context.Authoring.DrawOutputRootField(draft.OutputRoot);
                });
            });
        }

        private static void DrawWizardStats(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.MaximumHealth = context.Authoring.DrawFloatField("Max Health", draft.MaximumHealth);
                draft.MoveSpeed = context.Authoring.DrawFloatField("Move Speed", draft.MoveSpeed);
                draft.RewardValue = context.Authoring.DrawIntField("Reward Value", draft.RewardValue);
                draft.ContactDamage = context.Authoring.DrawFloatField("Contact Damage", draft.ContactDamage);
                draft.DamageTypeId = context.Authoring.DrawTextField("Damage Type ID", draft.DamageTypeId);
                draft.CollisionRadius = context.Authoring.DrawFloatField("Collision Radius", draft.CollisionRadius);
            });
        }

        private static void DrawWizardPresentation(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawPresentation(draft);
                draft.Prefab = context.Authoring.DrawObjectField("Enemy Prefab", draft.Prefab);
                draft.SpawnAudio = context.Authoring.DrawObjectField("OnSpawn Audio", draft.SpawnAudio);
                draft.SpawnVfxPrefab = context.Authoring.DrawObjectField("OnSpawn VFX", draft.SpawnVfxPrefab);
                draft.HitAudio = context.Authoring.DrawObjectField("OnHit Audio", draft.HitAudio);
                draft.HitVfxPrefab = context.Authoring.DrawObjectField("OnHit VFX", draft.HitVfxPrefab);
                draft.DeathAudio = context.Authoring.DrawObjectField("OnDeath Audio", draft.DeathAudio);
                draft.DeathVfxPrefab = context.Authoring.DrawObjectField("OnDeath VFX", draft.DeathVfxPrefab);
            });
        }

        private static void DrawWizardReview(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state, GameContentAuthoringValidationResult validation)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorStatusChipRow.Draw(
                    new DeucarianEditorStatusChip(validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers", validation.ErrorCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                    new DeucarianEditorStatusChip(validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings", validation.WarningCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                    new DeucarianEditorStatusChip(GetRoleLabel(draft.Role), DeucarianEditorStatus.Info));

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
                    GameContentCreationResult result = EnemyDefinitionAssetCreator.CreateAssets(draft);
                    context.Authoring.SetCreationResult(result);
                    if (result != null && result.Succeeded)
                    {
                        context.RefreshLibrary();
                        state.LeaveCreate();
                    }
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

        private static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft, EnemyProviderV2State state)
        {
            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);

            EnemyAuthoringState source = state.Creating
                ? draft
                : state.EditingState ?? AttackGameContentPreviewSelection.ResolveEnemyState(context.Preview, draft);
            if (source == null)
            {
                EditorGUILayout.LabelField("Preview unavailable.", DeucarianEditorStyles.MutedLabel);
                EditorGUILayout.EndScrollView();
                return;
            }

            GameContentAuthoringActionPreview actionPreview = BuildEnemyActionPreview(
                source,
                state.PreviewPlaying,
                state.PreviewStartTime <= 0d ? EditorApplication.timeSinceStartup : state.PreviewStartTime);
            if (actionPreview != null)
            {
                actionPreview.Loop = state.PreviewLoop;
                actionPreview.Speed = state.PreviewSpeed;
                actionPreview.StaticNormalizedTime = state.PausedNormalizedTime;
                actionPreview.Muted = state.PreviewMuted;
                actionPreview.RenderMode = state.PreviewRenderMode;
            }

            EnemyProviderV2PreviewScope scope = EnemyProviderV2PreviewModel.GetScope(
                state.Creating,
                state.EditingContext != null && state.EditingContext.IsDirty);

            GameContentPreviewLabRenderer.Draw(
                context.Preview,
                new GameContentPreviewLabModel
                {
                    Title = EnemyProviderV2PreviewModel.BuildHeaderTitle(source, scope),
                    PreviewTitle = EnemyProviderV2PreviewModel.BuildViewportTitle(source, scope),
                    ScopeLabel = EnemyProviderV2PreviewModel.GetScopeLabel(scope),
                    PrimaryAsset = source.Prefab,
                    EmptyText = "Assign an enemy prefab to render the game preview.",
                    PreviewOptions = new GameContentAuthoringObjectPreviewOptions
                    {
                        MinimumHeight = 220f,
                        ActionPreview = actionPreview
                    },
                    DrawControls = () => DrawPreviewControls(context, state, actionPreview),
                    DrawContext = () => DrawPreviewContext(source, scope),
                    DrawBody = () => DrawPreviewBody(context, source),
                    Chips = EnemyProviderV2PreviewModel.BuildChips(source, state, scope)
                });

            if (!string.IsNullOrWhiteSpace(state.PreviewStatus))
                EditorGUILayout.LabelField(state.PreviewStatus, DeucarianEditorStyles.MutedLabel);

            if (state.PreviewPlaying)
                context.RequestRepaint();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, EnemyAuthoringState source)
        {
            context.Preview.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyRows(source));
            context.Preview.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyPresentationRows(source));
            DrawPresentation(source);
            context.Preview.DrawWarnings(AttackGameContentPreviewSummaries.BuildEnemyWarnings(source));
        }

        private static void DrawPreviewControls(
            GameContentAuthoringSurfaceContext context,
            EnemyProviderV2State state,
            GameContentAuthoringActionPreview actionPreview)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playLabel = state.PreviewPlaying ? "Pause" : "Play";
                if (DeucarianEditorMiniToolbar.Button(playLabel, true, GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    if (state.PreviewPlaying)
                    {
                        state.PausedNormalizedTime = actionPreview == null ? 0.5f : actionPreview.GetNormalizedTime(EditorApplication.timeSinceStartup);
                        state.PreviewPlaying = false;
                        state.PreviewStatus = "Preview paused";
                    }
                    else
                    {
                        float duration = actionPreview == null ? 2.4f : actionPreview.DurationSeconds;
                        state.PreviewStartTime = EditorApplication.timeSinceStartup - (state.PausedNormalizedTime * duration / Mathf.Max(0.01f, state.PreviewSpeed));
                        state.PreviewPlaying = true;
                        state.PreviewStatus = "Preview playing";
                    }
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    AttackEditorPreviewAudio.StopAll();
                    state.PreviewPlaying = false;
                    state.PausedNormalizedTime = 0f;
                    state.PreviewStatus = "Preview stopped";
                }

                if (DeucarianEditorMiniToolbar.Button("Restart", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = true;
                    state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    state.PausedNormalizedTime = 0f;
                    state.PreviewStatus = "Preview restarted";
                    context.Preview.SetStatus(state.PreviewStatus);
                }

                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;

                if (DeucarianEditorMiniToolbar.Button(state.PreviewMuted ? "Muted" : "Audio", true, GUILayout.Width(54f), GUILayout.Height(22f)))
                {
                    state.PreviewMuted = !state.PreviewMuted;
                    if (state.PreviewMuted)
                        AttackEditorPreviewAudio.StopAll();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] speeds = { "0.5x", "1x", "2x" };
                int selectedSpeed = state.PreviewSpeed < 0.75f ? 0 : state.PreviewSpeed > 1.5f ? 2 : 1;
                int nextSpeed = DeucarianEditorSegmentedControl.Draw(selectedSpeed, speeds, GUILayout.ExpandWidth(true));
                state.PreviewSpeed = nextSpeed == 0 ? 0.5f : nextSpeed == 2 ? 2f : 1f;
                string[] modes = { "Game", "Debug" };
                int selectedMode = state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? 1 : 0;
                int nextMode = DeucarianEditorSegmentedControl.Draw(selectedMode, modes, GUILayout.Width(132f));
                state.PreviewRenderMode = nextMode == 1 ? GameContentAuthoringActionPreviewRenderMode.Debug : GameContentAuthoringActionPreviewRenderMode.Game;
            }
        }

        private static void DrawPreviewContext(EnemyAuthoringState source, EnemyProviderV2PreviewScope scope)
        {
            DeucarianEditorFieldRow.Draw("Source", () => EditorGUILayout.LabelField(scope == EnemyProviderV2PreviewScope.Draft ? "New enemy draft" : "Selected enemy asset", DeucarianEditorStyles.MutedLabel));
            DeucarianEditorFieldRow.Draw("Target", () => EditorGUILayout.LabelField(GetRoleLabel(source.Role) + " - " + source.EnemyId, DeucarianEditorStyles.MutedLabel));
        }

        internal static GameContentAuthoringActionPreview BuildEnemyActionPreview(EnemyAuthoringState state, bool playing, double startTime)
        {
            if (state == null || state.Prefab == null)
                return null;

            var preview = new GameContentAuthoringActionPreview
            {
                PrimaryAsset = state.Prefab,
                TargetPrefab = state.Prefab,
                ImpactVfxPrefab = state.HitVfxPrefab ?? state.SpawnVfxPrefab ?? state.DeathVfxPrefab,
                TickVfxPrefab = state.SpawnVfxPrefab,
                ExpireVfxPrefab = state.DeathVfxPrefab,
                Mode = GameContentAuthoringActionPreviewMode.Static,
                Playing = playing,
                StartTime = startTime,
                StaticNormalizedTime = 0.5f,
                DurationSeconds = 2.4f,
                Label = string.IsNullOrWhiteSpace(state.DisplayName) ? state.EnemyId : state.DisplayName,
                DeliveryTypeLabel = GetRoleLabel(state.Role) + " enemy",
                AccentColor = GetRoleAccent(state.Role)
            };
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Spawn", GetObjectLabel(state.SpawnVfxPrefab, "spawn point"), state.SpawnVfxPrefab));
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Enemy", GetObjectLabel(state.Prefab, state.DisplayName), state.Prefab));
            preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Hit", GetObjectLabel(state.HitVfxPrefab, "hit response"), state.HitVfxPrefab));
            return preview;
        }

        internal static IReadOnlyList<DeucarianEditorTimelineEvent> BuildTimelineEvents(EnemyAuthoringState state)
        {
            return new[]
            {
                Timeline("OnSpawn", state.SpawnVfxPrefab, state.SpawnAudio),
                Timeline("OnHit", state.HitVfxPrefab, state.HitAudio),
                Timeline("OnDeath", state.DeathVfxPrefab, state.DeathAudio)
            };
        }

        private static DeucarianEditorTimelineEvent Timeline(string label, GameObject visual, AudioClip audio)
        {
            string detail = visual == null && audio == null
                ? "Optional presentation asset not assigned."
                : string.Join(", ", BuildAssignedParts(visual, audio));
            return new DeucarianEditorTimelineEvent(label, detail, visual != null, audio != null, true);
        }

        private static IEnumerable<string> BuildAssignedParts(GameObject visual, AudioClip audio)
        {
            if (visual != null) yield return "VFX " + visual.name;
            if (audio != null) yield return "audio " + audio.name;
        }

        private static GameContentAuthoringValidationResult ValidateDraft(EnemyAuthoringState draft)
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

        private static void DrawValue(string label, string value)
        {
            DeucarianEditorFieldRow.Draw(label, () => EditorGUILayout.LabelField(value ?? string.Empty, DeucarianEditorStyles.MutedLabel));
        }

        private static string BuildHumanSummary(EnemyAuthoringState state)
        {
            return FormatFloat(state.MaximumHealth) + " HP, "
                + FormatFloat(state.MoveSpeed) + " speed, "
                + FormatFloat(state.ContactDamage) + " " + state.DamageTypeId
                + ", " + GetRoleLabel(state.Role).ToLowerInvariant();
        }

        internal static string BuildStateFingerprint(EnemyAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            return string.Join("|", new[]
            {
                state.EnemyId ?? string.Empty,
                state.DisplayName ?? string.Empty,
                AssetKey(state.Icon),
                ((int)state.Role).ToString(CultureInfo.InvariantCulture),
                state.TagsCsv ?? string.Empty,
                AssetKey(state.Prefab),
                state.MaximumHealth.ToString("R", CultureInfo.InvariantCulture),
                state.MoveSpeed.ToString("R", CultureInfo.InvariantCulture),
                state.RewardValue.ToString(CultureInfo.InvariantCulture),
                state.ContactDamage.ToString("R", CultureInfo.InvariantCulture),
                state.DamageTypeId ?? string.Empty,
                state.CollisionRadius.ToString("R", CultureInfo.InvariantCulture),
                AssetKey(state.SpawnAudio),
                AssetKey(state.SpawnVfxPrefab),
                AssetKey(state.HitAudio),
                AssetKey(state.HitVfxPrefab),
                AssetKey(state.DeathAudio),
                AssetKey(state.DeathVfxPrefab)
            });
        }

        private static string AssetKey(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path)
                ? asset.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                : path;
        }

        private static string BuildUsedBySummary(GameContentLibraryItem item)
        {
            if (item == null || item.ReverseReferences.Count == 0)
                return "No known references";

            int waves = 0;
            int sets = 0;
            int packs = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target == null) continue;
                if (target.Kind == GameContentLibraryKind.Wave) waves++;
                else if (target.Kind == GameContentLibraryKind.ContentSet) sets++;
                else if (target.Kind == GameContentLibraryKind.ContentPack) packs++;
            }

            return waves.ToString(CultureInfo.InvariantCulture) + " wave(s), "
                + sets.ToString(CultureInfo.InvariantCulture) + " set(s), "
                + packs.ToString(CultureInfo.InvariantCulture) + " pack(s)";
        }

        private static string BuildAdvancedReport(GameContentLibraryItem item, EnemyAuthoringState state)
        {
            return "Enemy: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.EnemyId + Environment.NewLine
                + "Path: " + item.Path + Environment.NewLine
                + "Role: " + GetRoleLabel(state.Role) + Environment.NewLine
                + "Stats: " + FormatFloat(state.MaximumHealth) + " HP, " + FormatFloat(state.MoveSpeed) + " speed";
        }

        private static string GetRoleLabel(EnemyRole role)
        {
            return Enum.IsDefined(typeof(EnemyRole), role) ? role.ToString() : "Custom";
        }

        private static bool HasAnyVisual(EnemyAuthoringState state)
        {
            return state != null && (state.Prefab != null || state.SpawnVfxPrefab != null || state.HitVfxPrefab != null || state.DeathVfxPrefab != null);
        }

        private static bool HasAnyAudio(EnemyAuthoringState state)
        {
            return state != null && (state.SpawnAudio != null || state.HitAudio != null || state.DeathAudio != null);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string GetObjectLabel(UnityEngine.Object asset, string fallback)
        {
            return asset == null ? fallback ?? string.Empty : asset.name;
        }

        private static Color GetRoleAccent(EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Fast:
                    return new Color(0.2f, 0.72f, 0.95f, 1f);
                case EnemyRole.Tank:
                    return new Color(0.95f, 0.56f, 0.22f, 1f);
                case EnemyRole.Swarm:
                    return new Color(0.62f, 0.9f, 0.32f, 1f);
                case EnemyRole.Boss:
                    return new Color(0.95f, 0.32f, 0.42f, 1f);
                default:
                    return new Color(0.12f, 0.78f, 0.86f, 1f);
            }
        }

        private static GUIStyle headerStyle;

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true
                    };
                    headerStyle.normal.textColor = DeucarianEditorTheme.Text;
                }

                return headerStyle;
            }
        }
    }

    internal enum EnemyProviderV2PreviewScope
    {
        Selected,
        Draft,
        UnsavedEdit
    }

    internal static class EnemyProviderV2PreviewModel
    {
        public const bool EventRowsExposePreviewActions = false;
        public const bool ExposesRedundantSelectButton = false;

        public static EnemyProviderV2PreviewScope GetScope(bool creating, bool dirty)
        {
            if (creating)
                return EnemyProviderV2PreviewScope.Draft;
            return dirty ? EnemyProviderV2PreviewScope.UnsavedEdit : EnemyProviderV2PreviewScope.Selected;
        }

        public static string GetScopeLabel(EnemyProviderV2PreviewScope scope)
        {
            switch (scope)
            {
                case EnemyProviderV2PreviewScope.Draft:
                    return "Draft";
                case EnemyProviderV2PreviewScope.UnsavedEdit:
                    return "Unsaved";
                default:
                    return "Selected";
            }
        }

        public static string BuildHeaderTitle(EnemyAuthoringState source, EnemyProviderV2PreviewScope scope)
        {
            string sourceName = GetSourceName(source, scope);
            return string.IsNullOrWhiteSpace(sourceName)
                ? "Preview Lab"
                : "Preview Lab - " + sourceName;
        }

        public static string BuildViewportTitle(EnemyAuthoringState source, EnemyProviderV2PreviewScope scope)
        {
            return GetSourceName(source, scope);
        }

        public static IReadOnlyList<DeucarianEditorStatusChip> BuildChips(
            EnemyAuthoringState source,
            EnemyProviderV2State state,
            EnemyProviderV2PreviewScope scope)
        {
            var chips = new List<DeucarianEditorStatusChip>
            {
                BuildScopeChip(scope),
                new DeucarianEditorStatusChip(source != null && source.Prefab != null ? "Model" : "No Model", source != null && source.Prefab != null ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(HasAnyVisual(source) ? "VFX" : "No VFX", HasAnyVisual(source) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled),
                new DeucarianEditorStatusChip(state != null && state.PreviewMuted ? "Muted" : HasAnyAudio(source) ? "Audio" : "No audio", state == null || state.PreviewMuted || !HasAnyAudio(source) ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state != null && state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? "Debug" : "Game", state != null && state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state != null && state.PreviewLoop ? "Loop" : "Once", state != null && state.PreviewLoop ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(FormatFloat(state == null ? 1f : state.PreviewSpeed) + "x", DeucarianEditorStatus.Info)
            };

            return chips;
        }

        private static DeucarianEditorStatusChip BuildScopeChip(EnemyProviderV2PreviewScope scope)
        {
            switch (scope)
            {
                case EnemyProviderV2PreviewScope.Draft:
                    return new DeucarianEditorStatusChip("Draft Preview", DeucarianEditorStatus.Info);
                case EnemyProviderV2PreviewScope.UnsavedEdit:
                    return new DeucarianEditorStatusChip("Unsaved Preview", DeucarianEditorStatus.Warning);
                default:
                    return new DeucarianEditorStatusChip("Selected Preview", DeucarianEditorStatus.Success);
            }
        }

        private static string GetSourceName(EnemyAuthoringState source, EnemyProviderV2PreviewScope scope)
        {
            if (source != null)
            {
                if (scope == EnemyProviderV2PreviewScope.Draft && IsDefaultExampleDraft(source))
                    return "New Enemy Draft";
                if (!string.IsNullOrWhiteSpace(source.DisplayName))
                    return source.DisplayName.Trim();
                if (!string.IsNullOrWhiteSpace(source.EnemyId))
                    return source.EnemyId.Trim();
            }

            return scope == EnemyProviderV2PreviewScope.Draft ? "New Enemy Draft" : "Enemy View";
        }

        private static bool IsDefaultExampleDraft(EnemyAuthoringState source)
        {
            return source != null
                && string.Equals(source.DisplayName, "Basic Enemy", StringComparison.Ordinal)
                && string.Equals(source.EnemyId, "enemy.example.basic", StringComparison.Ordinal);
        }

        private static bool HasAnyVisual(EnemyAuthoringState state)
        {
            return state != null && (state.Prefab != null || state.SpawnVfxPrefab != null || state.HitVfxPrefab != null || state.DeathVfxPrefab != null);
        }

        private static bool HasAnyAudio(EnemyAuthoringState state)
        {
            return state != null && (state.SpawnAudio != null || state.HitAudio != null || state.DeathAudio != null);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    internal sealed class EnemyProviderV2ListItem
    {
        private EnemyProviderV2ListItem(
            GameContentLibraryItem source,
            string displayName,
            string stableId,
            string roleLabel,
            string tags,
            bool hasPrefab,
            bool hasVisuals,
            bool hasAudio)
        {
            Source = source;
            DisplayName = displayName ?? string.Empty;
            StableId = stableId ?? string.Empty;
            RoleLabel = roleLabel ?? "Enemy";
            Tags = tags ?? string.Empty;
            HasPrefab = hasPrefab;
            HasVisuals = hasVisuals;
            HasAudio = hasAudio;
            ReadinessLabel = source == null ? "Missing" : source.ValidationLabel;
            ReadinessStatus = source == null
                ? DeucarianEditorStatus.Disabled
                : source.ErrorCount > 0
                    ? DeucarianEditorStatus.Error
                    : source.WarningCount > 0
                        ? DeucarianEditorStatus.Warning
                        : DeucarianEditorStatus.Success;
        }

        public GameContentLibraryItem Source { get; }
        public string DisplayName { get; }
        public string StableId { get; }
        public string RoleLabel { get; }
        public string Tags { get; }
        public bool HasPrefab { get; }
        public bool HasVisuals { get; }
        public bool HasAudio { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }

        public static IReadOnlyList<EnemyProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<EnemyProviderV2ListItem>();

            var result = new List<EnemyProviderV2ListItem>();
            for (int i = 0; i < items.Count; i++)
            {
                GameContentLibraryItem item = items[i];
                if (item == null)
                    continue;
                result.Add(FromItem(item));
            }

            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static EnemyProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            EnemyDefinitionAsset enemy = item == null ? null : item.Asset as EnemyDefinitionAsset;
            string displayName = enemy == null ? item.DisplayName : enemy.DisplayName;
            string stableId = enemy == null ? item.Id : enemy.Id;
            string tags = enemy == null ? string.Empty : JoinTags(enemy.Tags);
            string role = GetRoleLabel(enemy);
            bool hasPrefab = enemy != null && enemy.Presentation != null && enemy.Presentation.Prefab != null;
            bool hasVisuals = hasPrefab || HasPresentationAsset(enemy, false);
            bool hasAudio = HasPresentationAsset(enemy, true);
            return new EnemyProviderV2ListItem(item, displayName, stableId, role, tags, hasPrefab, hasVisuals, hasAudio);
        }

        internal static string GetRoleLabelForTests(EnemyDefinitionAsset enemy)
        {
            return GetRoleLabel(enemy);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string normalized = query.Trim().ToLowerInvariant();
            return Contains(DisplayName, normalized)
                || Contains(StableId, normalized)
                || Contains(RoleLabel, normalized)
                || Contains(Tags, normalized)
                || (Source != null && Contains(Source.Category, normalized));
        }

        private static bool Contains(string value, string normalizedQuery)
        {
            return !string.IsNullOrWhiteSpace(value) && value.ToLowerInvariant().Contains(normalizedQuery);
        }

        private static string GetRoleLabel(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return "Enemy";

            EnemyRole role = enemy.Role;
            return Enum.IsDefined(typeof(EnemyRole), role) ? role.ToString() : "Custom";
        }

        private static bool HasPresentationAsset(EnemyDefinitionAsset enemy, bool audio)
        {
            if (enemy == null || enemy.Presentation == null)
                return false;

            EnemyPresentationEventKind[] events =
            {
                EnemyPresentationEventKind.OnSpawn,
                EnemyPresentationEventKind.OnHit,
                EnemyPresentationEventKind.OnDeath
            };

            for (int i = 0; i < events.Length; i++)
            {
                if (!enemy.Presentation.TryGetEvent(events[i], out EnemyPresentationEventRecipe recipe) || recipe == null)
                    continue;
                if (audio && recipe.AudioClip != null)
                    return true;
                if (!audio && recipe.VfxPrefab != null)
                    return true;
            }

            return false;
        }

        private static string JoinTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return string.Empty;

            var values = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tags[i]))
                    values.Add(tags[i].Trim());
            }

            return string.Join(", ", values.ToArray());
        }
    }
}
