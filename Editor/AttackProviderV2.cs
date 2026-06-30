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
    internal sealed class AttackProviderV2State
    {
        public string SearchText = string.Empty;
        public bool Creating;
        public int DetailPage;
        public int WizardStep;
        public Vector2 ListScroll;
        public Vector2 DetailScroll;
        public Vector2 PreviewScroll;
        public bool PreviewMuted = true;
        public bool PreviewLoop = true;
        public float PreviewSpeed = 1f;
        public bool PreviewPlaying = true;
        public GameContentAuthoringActionPreviewRenderMode PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Game;
        public double PreviewStartTime;
        public float PausedNormalizedTime = 0.5f;
        public int PreviewSourceContextIndex;
        public int PreviewTargetContextIndex;
        public int LastPreviewAudioPhase = -1;
        public string ActivePreviewKey = string.Empty;
        public string PreviewStatus = "Preview idle";
        public AttackAuthoringState EditingState;
        public GameContentAuthoringObjectEditorContext EditingContext;
        public GameContentCreationResult LastEditResult;

        public void StopPreview()
        {
            PreviewPlaying = false;
            PreviewStartTime = 0d;
            PausedNormalizedTime = 0.5f;
            LastPreviewAudioPhase = -1;
            PreviewStatus = "Preview stopped";
        }

        public void SetPreviewSource(string key, AttackGameContentPreviewController controller)
        {
            key = key ?? string.Empty;
            if (string.Equals(ActivePreviewKey, key, StringComparison.Ordinal))
                return;

            controller?.Stop();
            ActivePreviewKey = key;
            PreviewPlaying = true;
            PreviewStartTime = EditorApplication.timeSinceStartup;
            PausedNormalizedTime = 0f;
            PreviewSourceContextIndex = 0;
            PreviewTargetContextIndex = 0;
            LastPreviewAudioPhase = -1;
            PreviewStatus = "Previewing";
        }

        public void ClearEditingState()
        {
            EditingState = null;
            EditingContext = null;
            LastEditResult = null;
        }
    }

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

        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Behavior",
            "Delivery",
            "Presentation",
            "Balance",
            "Review"
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
            EnsureDefaultMode(context, state, items);
            EnsureEditingState(context, state);
            TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => DrawAttackList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => DrawPreviewLab(context, draft, state));
        }

        private static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, IReadOnlyList<AttackProviderV2ListItem> items)
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

        private static void EnsureEditingState(GameContentAuthoringSurfaceContext context, AttackProviderV2State state)
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
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        private static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_attack__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, previewController);
        }

        private static void DrawAttackList(
            GameContentAuthoringSurfaceContext context,
            AttackProviderV2State state,
            IReadOnlyList<AttackProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Attacks", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Refresh", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                    context.RefreshLibrary();
            }

            state.SearchText = DeucarianEditorSearchField.Draw(state.SearchText, "Search attacks", GUILayout.ExpandWidth(true));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Create New", true, GUILayout.Height(24f)))
                {
                    state.Creating = true;
                    state.ClearEditingState();
                    state.DetailScroll = Vector2.zero;
                    context.ClearSelection();
                    context.RequestRepaint();
                }
            }

            GUILayout.Space(DeucarianEditorSpacing.Small);
            state.ListScroll = EditorGUILayout.BeginScrollView(state.ListScroll);
            int shown = 0;
            for (int i = 0; i < items.Count; i++)
            {
                AttackProviderV2ListItem item = items[i];
                if (!item.Matches(state.SearchText))
                    continue;

                shown++;
                DrawAttackCard(context, state, item);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(items.Count == 0 ? "No authored attacks found." : "No attacks match the current search.", DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawAttackCard(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackProviderV2ListItem item)
        {
            bool selected = !state.Creating && context.IsSelected(item.Source);
            var chips = new[]
            {
                new DeucarianEditorStatusChip(GetCompactTypeLabel(item.TypeLabel), DeucarianEditorStatus.Info, item.TypeLabel),
                new DeucarianEditorStatusChip(item.ReadinessLabel, item.ReadinessStatus),
                new DeucarianEditorStatusChip(item.HasPrefab ? "Core" : "NoCore", item.HasPrefab ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, item.HasPrefab ? "Primary delivery asset assigned" : "Primary delivery asset not assigned"),
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

        private static string GetCompactTypeLabel(string typeLabel)
        {
            if (string.Equals(typeLabel, "Projectile", StringComparison.OrdinalIgnoreCase))
                return "Proj";
            return typeLabel ?? "Attack";
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);

            if (state.Creating)
                DrawWizard(context, draft, state);
            else
                DrawSelectedDetail(context, draft, state);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedDetail(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            if (context.SelectedItem == null)
            {
                EditorGUILayout.LabelField("Select an attack to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            AttackDefinitionAsset selectedAsset = context.SelectedItem.Asset as AttackDefinitionAsset;
            AttackAuthoringState selectedState = state.EditingState;
            if (selectedAsset == null || selectedState == null)
            {
                EditorGUILayout.LabelField("Selected item is not an attack asset.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            GameContentAuthoringValidationResult validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            string fingerprint = BuildStateFingerprint(selectedState);
            state.EditingContext?.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);
            bool dirty = state.EditingContext != null && state.EditingContext.IsDirty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(selectedState.DisplayName, HeaderStyle);
                    EditorGUILayout.LabelField(selectedState.AttackId, DeucarianEditorStyles.MutedLabel);
                }

                GUILayout.FlexibleSpace();
                DeucarianEditorMiniToolbar.PingButton(context.SelectedItem.Asset);
            }

            DeucarianEditorStatusChipRow.Draw(
                new DeucarianEditorStatusChip(GetCompactTypeLabel(GetTypeLabel(selectedState)), DeucarianEditorStatus.Info, GetTypeLabel(selectedState)),
                new DeucarianEditorStatusChip(validation.ErrorCount > 0 ? validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers" : validation.WarningCount > 0 ? validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings" : "Ready", validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(HasAnyVisual(selectedState) ? "VFX" : "NoVFX", HasAnyVisual(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, HasAnyVisual(selectedState) ? "Visual presentation assigned" : "No visual presentation assigned"),
                new DeucarianEditorStatusChip(HasAnyAudio(selectedState) ? "Aud" : "Mute", HasAnyAudio(selectedState) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, HasAnyAudio(selectedState) ? "Audio presentation assigned" : "No audio presentation assigned"));

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
                validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }
            else if (command == GameContentAuthoringCommand.Save)
            {
                SaveEditedAttack(context, state, selectedAsset);
                selectedState = state.EditingState;
                validation = AttackRecipeAssetCreator.ValidateForUpdate(selectedState, selectedAsset);
            }

            DrawValidationIssues(validation);

            state.DetailPage = DeucarianEditorSegmentedControl.DrawPageChips(state.DetailPage, DetailPages);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (state.DetailPage)
            {
                case 1:
                    DrawWizardBehavior(context, selectedState);
                    break;
                case 2:
                    DrawWizardDelivery(context, selectedState);
                    break;
                case 3:
                    DrawWizardPresentation(context, selectedState);
                    break;
                case 4:
                    DrawWizardBalance(context, selectedState);
                    break;
                case 5:
                    DrawReferences(context.SelectedItem);
                    break;
                case 6:
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

        private static void ReloadEditingState(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackDefinitionAsset asset)
        {
            if (state == null || asset == null)
                return;

            state.EditingState = AttackGameContentPreviewSelection.FromAttackAsset(asset);
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.EditingContext.SetStatus("Reverted");
            state.LastEditResult = null;
            GUI.FocusControl(null);
            context.RequestRepaint();
        }

        private static void SaveEditedAttack(GameContentAuthoringSurfaceContext context, AttackProviderV2State state, AttackDefinitionAsset asset)
        {
            if (state == null || asset == null || state.EditingState == null)
                return;

            GameContentCreationResult result = AttackRecipeAssetCreator.UpdateExistingAsset(asset, state.EditingState);
            state.LastEditResult = result;
            if (result != null && result.Succeeded)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromAttackAsset(asset);
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

        private static void DrawEditOverview(GameContentAuthoringSurfaceContext context, GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                state.AttackId = context.Authoring.DrawTextField("Stable ID", state.AttackId);
                state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
                state.Icon = context.Authoring.DrawObjectField("Icon", state.Icon);
                state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawValidationIssues(GameContentAuthoringValidationResult validation)
        {
            if (validation == null || validation.Issues.Count == 0)
                return;

            var messages = new List<string>();
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                GameContentAuthoringValidationIssue issue = validation.Issues[i];
                if (issue.Severity == GameContentAuthoringValidationSeverity.Info)
                    continue;
                messages.Add(issue.Path + ": " + issue.Message);
            }

            if (messages.Count == 0)
                return;

            DeucarianEditorStatusPanel.DrawValidationCard(
                validation.ErrorCount > 0
                    ? validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " edit blocker(s)."
                    : validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " edit warning(s).",
                messages,
                validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Warning);
        }

        private static void DrawOverview(GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorFieldRow.Draw("Display Name", () => EditorGUILayout.LabelField(state.DisplayName));
                DeucarianEditorFieldRow.Draw("ID", () => EditorGUILayout.LabelField(state.AttackId));
                DeucarianEditorFieldRow.Draw("Type", () => EditorGUILayout.LabelField(GetTypeLabel(state)));
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawBehavior(AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Damage", FormatFloat(state.DamageAmount) + " " + state.DamageTypeId);
                DrawValue("Cooldown", state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " ticks");
                DrawValue("Range", FormatFloat(state.Range));
                DrawValue("Targeting", state.TargetingMode.ToString());
                DrawValue("Status", state.IncludeStatusEffect ? state.StatusId : "None");
            });
        }

        private static void DrawDelivery(AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Mode", GetTypeLabel(state));
                if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
                {
                    DrawValue("Projectile ID", state.ProjectileDefinitionId);
                    DrawValue("Spawnable ID", state.ProjectileSpawnableId);
                    DrawValue("Speed", FormatFloat(state.ProjectileSpeed));
                    DrawValue("Lifetime", state.ProjectileLifetimeTicks.ToString(CultureInfo.InvariantCulture) + " ticks");
                    DrawValue("Homing", state.Homing ? "Yes, " + FormatFloat(state.HomingTurnRate) + " deg/s" : "No");
                    DrawValue("Pierce", state.PierceCount.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                {
                    DrawValue("Beam", state.BeamVfxPrefab == null ? "No beam VFX assigned" : state.BeamVfxPrefab.name);
                    DrawValue("Impact", state.ImpactVfxPrefab == null ? "No impact VFX assigned" : state.ImpactVfxPrefab.name);
                    DrawValue("Max Hits", state.MaxHits.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                if (state.DeliveryMode == AttackRecipeDeliveryMode.Area)
                {
                    DrawValue("Radius", FormatFloat(state.Radius));
                    DrawValue("Max Hits", state.MaxHits.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                DrawValue("Radius", FormatFloat(state.Radius));
                DrawValue("Tick Interval", FormatFloat(state.TickIntervalSeconds) + "s");
            });
        }

        private static void DrawPresentation(AttackAuthoringState state, Action<AttackPresentationEventKind> previewEvent, AttackProviderV2State previewState)
        {
            DeucarianEditorEventTimeline.Draw(BuildTimelineEvents(state), index =>
            {
                if (previewEvent == null)
                    return;
                previewEvent(GetEventKind(index));
            });
        }

        private static void DrawBalance(AttackAuthoringState state)
        {
            float dps = state.CooldownTicks <= 0 ? state.DamageAmount * 30f : state.DamageAmount * 30f / Mathf.Max(1, state.CooldownTicks);
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("DPS Estimate", FormatFloat(dps));
                DrawValue("Cooldown / Range", state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " ticks / " + FormatFloat(state.Range));
                DrawValue("Damage", FormatFloat(state.DamageAmount) + " " + state.DamageTypeId);
                DrawValue("Upgrade Hook", string.IsNullOrWhiteSpace(state.AttackId) ? "None" : state.AttackId);
            });
        }

        private static void DrawReferences(GameContentLibraryItem item)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                if (item.ReverseReferences == null || item.ReverseReferences.Count == 0)
                {
                    EditorGUILayout.LabelField("No known weapons, upgrades, sets, or packs reference this attack.", DeucarianEditorStyles.MutedLabel);
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

        private static void DrawAdvanced(GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorDiagnosticsDrawer.Draw("attack-v2-advanced-" + item.Key, "Raw Details", () =>
            {
                DrawValue("Asset Path", item.Path);
                DrawValue("Folder", item.Folder);
                DrawValue("Stable ID", state.AttackId);
                DrawValue("Tags", state.TagsCsv);
                if (GUILayout.Button("Copy Report", DeucarianEditorButtons.SecondaryStyle, GUILayout.Height(24f)))
                    EditorGUIUtility.systemCopyBuffer = BuildAdvancedReport(item, state);
            }, true);

            DeucarianEditorDiagnosticsDrawer.Draw("attack-v2-references-" + item.Key, "Serialized References", () =>
            {
                DrawRawReferences("Direct", item.DirectReferences);
                DrawRawReferences("Referenced By", item.ReverseReferences);
            });
        }

        private static void DrawWizard(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("New Attack", HeaderStyle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Browse", context.AuthoredItems.Count > 0, GUILayout.Width(60f), GUILayout.Height(22f)))
                {
                    state.Creating = false;
                    state.DetailScroll = Vector2.zero;
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
                    DrawWizardBehavior(context, draft);
                    break;
                case 2:
                    DrawWizardDelivery(context, draft);
                    break;
                case 3:
                    DrawWizardPresentation(context, draft);
                    break;
                case 4:
                    DrawWizardBalance(context, draft);
                    break;
                case 5:
                    DrawWizardReview(context, draft, state, validation);
                    break;
                default:
                    DrawWizardIdentity(context, draft);
                    break;
            }

            DrawWizardNavigation(context, state, validation);
        }

        private static void DrawWizardIdentity(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.AttackId = context.Authoring.DrawTextField("Stable ID", draft.AttackId);
                draft.DisplayName = context.Authoring.DrawTextField("Display Name", draft.DisplayName);
                draft.Icon = context.Authoring.DrawObjectField("Icon", draft.Icon);
                draft.TagsCsv = context.Authoring.DrawTextField("Tags", draft.TagsCsv);
                DeucarianEditorDiagnosticsDrawer.Draw("attack-v2-create-output", "Advanced Output", () =>
                {
                    draft.OutputRoot = context.Authoring.DrawOutputRootField(draft.OutputRoot);
                });
            });
        }

        private static void DrawWizardBehavior(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.DamageTypeId = context.Authoring.DrawTextField("Damage Type ID", draft.DamageTypeId);
                draft.DamageAmount = context.Authoring.DrawFloatField("Damage", draft.DamageAmount);
                draft.CooldownTicks = context.Authoring.DrawIntField("Cooldown Ticks", draft.CooldownTicks);
                draft.Range = context.Authoring.DrawFloatField("Range", draft.Range);
                draft.TargetingMode = context.Authoring.DrawEnumPopup("Targeting", draft.TargetingMode);
            });
        }

        private static void DrawWizardDelivery(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.DeliveryMode = context.Authoring.DrawEnumPopup("Mode", draft.DeliveryMode);
                if (draft.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
                {
                    draft.ProjectileDefinitionId = context.Authoring.DrawTextField("Projectile ID", draft.ProjectileDefinitionId);
                    draft.ProjectileSpawnableId = context.Authoring.DrawTextField("Spawnable ID", draft.ProjectileSpawnableId);
                    draft.ProjectilePrefab = context.Authoring.DrawObjectField("Projectile Prefab", draft.ProjectilePrefab);
                    draft.ProjectileSpeed = context.Authoring.DrawFloatField("Speed", draft.ProjectileSpeed);
                    draft.ProjectileLifetimeTicks = context.Authoring.DrawIntField("Lifetime Ticks", draft.ProjectileLifetimeTicks);
                    draft.Homing = context.Authoring.DrawToggle("Homing", draft.Homing);
                    if (draft.Homing)
                        draft.HomingTurnRate = context.Authoring.DrawFloatField("Turn Rate", draft.HomingTurnRate);
                    draft.PierceCount = context.Authoring.DrawIntField("Pierce Count", draft.PierceCount);
                    draft.Radius = context.Authoring.DrawFloatField("Radius", draft.Radius);
                    return;
                }

                if (draft.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                {
                    draft.BeamVfxPrefab = context.Authoring.DrawObjectField("Beam VFX", draft.BeamVfxPrefab);
                    draft.ImpactVfxPrefab = context.Authoring.DrawObjectField("Impact VFX", draft.ImpactVfxPrefab);
                    draft.MaxHits = context.Authoring.DrawIntField("Max Hits", draft.MaxHits);
                    return;
                }

                draft.Radius = context.Authoring.DrawFloatField("Radius", draft.Radius);
                if (draft.DeliveryMode == AttackRecipeDeliveryMode.Area)
                    draft.MaxHits = context.Authoring.DrawIntField("Max Hits", draft.MaxHits);
                if (draft.DeliveryMode == AttackRecipeDeliveryMode.Aura)
                    draft.TickIntervalSeconds = context.Authoring.DrawFloatField("Tick Interval", draft.TickIntervalSeconds);
            });
        }

        private static void DrawWizardPresentation(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawPresentation(draft, null, null);
                draft.CastAudio = context.Authoring.DrawObjectField("OnCast Audio", draft.CastAudio);
                draft.CastVfxPrefab = context.Authoring.DrawObjectField("OnCast VFX", draft.CastVfxPrefab);
                draft.FireAudio = context.Authoring.DrawObjectField("OnFire Audio", draft.FireAudio);
                draft.FireVfxPrefab = context.Authoring.DrawObjectField("OnFire VFX", draft.FireVfxPrefab);
                if (draft.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                    draft.BeamVfxPrefab = context.Authoring.DrawObjectField("Beam VFX", draft.BeamVfxPrefab);
                draft.ImpactAudio = context.Authoring.DrawObjectField("OnImpact Audio", draft.ImpactAudio);
                draft.ImpactVfxPresentationPrefab = context.Authoring.DrawObjectField("OnImpact VFX", draft.ImpactVfxPresentationPrefab);
                if (draft.IncludeStatusEffect)
                {
                    draft.TickAudio = context.Authoring.DrawObjectField("OnTick Audio", draft.TickAudio);
                    draft.TickVfxPrefab = context.Authoring.DrawObjectField("OnTick VFX", draft.TickVfxPrefab);
                    draft.ExpireAudio = context.Authoring.DrawObjectField("OnExpire Audio", draft.ExpireAudio);
                    draft.ExpireVfxPrefab = context.Authoring.DrawObjectField("OnExpire VFX", draft.ExpireVfxPrefab);
                }
            });
        }

        private static void DrawWizardBalance(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                draft.IncludeStatusEffect = context.Authoring.DrawToggle("Include Status", draft.IncludeStatusEffect);
                if (draft.IncludeStatusEffect)
                {
                    draft.StatusId = context.Authoring.DrawTextField("Status ID", draft.StatusId);
                    draft.StatusDurationTicks = context.Authoring.DrawIntField("Duration Ticks", draft.StatusDurationTicks);
                    draft.StatusTickRateTicks = context.Authoring.DrawIntField("Tick Rate Ticks", draft.StatusTickRateTicks);
                    draft.StatusStrength = context.Authoring.DrawFloatField("Strength", draft.StatusStrength);
                    draft.StatusMaxStacks = context.Authoring.DrawIntField("Max Stacks", draft.StatusMaxStacks);
                    draft.StatusStackingPolicy = context.Authoring.DrawEnumPopup("Stacking", draft.StatusStackingPolicy);
                    draft.StatusEffectNote = context.Authoring.DrawTextField("Effect Note", draft.StatusEffectNote);
                }

                DrawBalance(draft);
            });
        }

        private static void DrawWizardReview(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state, GameContentAuthoringValidationResult validation)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorStatusChipRow.Draw(
                    new DeucarianEditorStatusChip(validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blockers", validation.ErrorCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                    new DeucarianEditorStatusChip(validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warnings", validation.WarningCount == 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning),
                    new DeucarianEditorStatusChip(GetTypeLabel(draft), DeucarianEditorStatus.Info));

                string[] lines = AttackRecipeAssetCreator.GetPreviewLines(draft) as string[] ?? new List<string>(AttackRecipeAssetCreator.GetPreviewLines(draft)).ToArray();
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
                if (context.Authoring.DrawCreateButton("Create Attack", validation.IsValid))
                {
                    GameContentCreationResult result = AttackRecipeAssetCreator.CreateAssets(draft);
                    context.Authoring.SetCreationResult(result);
                    if (result != null && result.Succeeded)
                    {
                        context.RefreshLibrary();
                        state.Creating = false;
                        state.DetailScroll = Vector2.zero;
                    }
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

        private static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft, AttackProviderV2State state)
        {
            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);

            AttackAuthoringState source = state.Creating
                ? draft
                : state.EditingState ?? AttackGameContentPreviewSelection.ResolveAttackState(context.Preview, draft);
            if (source == null)
            {
                EditorGUILayout.LabelField("Preview unavailable.", DeucarianEditorStyles.MutedLabel);
                EditorGUILayout.EndScrollView();
                return;
            }

            IReadOnlyList<AttackGameContentPreviewContextOption> sourceOptions = AttackGameContentPreviewContext.BuildSourceOptions(state.Creating ? null : context.SelectedItem);
            IReadOnlyList<AttackGameContentPreviewContextOption> targetOptions = AttackGameContentPreviewContext.BuildTargetOptions(state.Creating ? null : context.SelectedItem, context.AllAuthoredItems);
            state.PreviewSourceContextIndex = AttackGameContentPreviewContext.ClampIndex(state.PreviewSourceContextIndex, sourceOptions);
            state.PreviewTargetContextIndex = AttackGameContentPreviewContext.ClampIndex(state.PreviewTargetContextIndex, targetOptions);
            AttackGameContentPreviewContextOption sourceOption = sourceOptions[state.PreviewSourceContextIndex];
            AttackGameContentPreviewContextOption targetOption = targetOptions[state.PreviewTargetContextIndex];

            GameContentAuthoringActionPreview actionPreview = AttackGameContentPreviewActions.BuildActionPreview(
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
                actionPreview.SourcePrefab = sourceOption == null ? null : sourceOption.Prefab;
                actionPreview.TargetPrefab = targetOption == null ? null : targetOption.Prefab;
                actionPreview.SourceContextLabel = sourceOption == null ? string.Empty : sourceOption.Label;
                actionPreview.TargetContextLabel = targetOption == null ? string.Empty : targetOption.Label;
            }

            GameContentPreviewLabRenderer.Draw(
                context.Preview,
                new GameContentPreviewLabModel
                {
                    Title = "Preview Lab",
                    PreviewTitle = "Attack View",
                    ScopeLabel = state.Creating ? "Draft" : state.EditingContext != null && state.EditingContext.IsDirty ? "Unsaved" : "Selected",
                    PrimaryAsset = AttackGameContentPreviewSummaries.GetPrimaryAttackPreviewAsset(source),
                    EmptyText = "No visual asset assigned.",
                    PreviewOptions = new GameContentAuthoringObjectPreviewOptions
                    {
                        MinimumHeight = 220f,
                        ActionPreview = actionPreview
                    },
                    DrawControls = () => DrawPreviewControls(context, source, state, actionPreview),
                    DrawContext = () => DrawPreviewContextControls(state, sourceOptions, targetOptions),
                    DrawBody = () => DrawPresentation(source, eventKind =>
                    {
                        state.PreviewStatus = AttackGameContentPreviewActions.PreviewAttackEvent(source, eventKind, state.PreviewMuted);
                        context.Preview.SetStatus(state.PreviewStatus);
                    }, state),
                    Chips = new[]
                    {
                        new DeucarianEditorStatusChip(HasAnyVisual(source) ? "Visual assets" : "No VFX", HasAnyVisual(source) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled),
                        new DeucarianEditorStatusChip(state.PreviewMuted ? "Muted" : HasAnyAudio(source) ? "Audio on" : "No audio", state.PreviewMuted || !HasAnyAudio(source) ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success),
                        new DeucarianEditorStatusChip(state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game ? "Game" : "Debug", state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info),
                        new DeucarianEditorStatusChip(state.PreviewLoop ? "Loop" : "Once", state.PreviewLoop ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info),
                        new DeucarianEditorStatusChip(FormatFloat(state.PreviewSpeed) + "x", DeucarianEditorStatus.Info)
                    }
                });

            string timelineAudioStatus = AttackGameContentPreviewActions.PreviewTimelineAudio(source, actionPreview, state.PreviewMuted, ref state.LastPreviewAudioPhase);
            if (!string.IsNullOrWhiteSpace(timelineAudioStatus))
            {
                state.PreviewStatus = timelineAudioStatus;
                context.Preview.SetStatus(state.PreviewStatus);
            }

            if (!string.IsNullOrWhiteSpace(state.PreviewStatus))
                EditorGUILayout.LabelField(state.PreviewStatus, DeucarianEditorStyles.MutedLabel);

            if (state.PreviewPlaying)
                context.RequestRepaint();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewControls(
            GameContentAuthoringSurfaceContext context,
            AttackAuthoringState source,
            AttackProviderV2State state,
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
                        state.LastPreviewAudioPhase = -1;
                        state.PreviewStatus = "Preview paused";
                    }
                    else
                    {
                        float duration = actionPreview == null ? 2.4f : actionPreview.DurationSeconds;
                        state.PreviewStartTime = EditorApplication.timeSinceStartup - (state.PausedNormalizedTime * duration / Mathf.Max(0.01f, state.PreviewSpeed));
                        state.PreviewPlaying = true;
                        state.LastPreviewAudioPhase = -1;
                        state.PreviewStatus = "Preview playing";
                    }
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    AttackEditorPreviewAudio.StopAll();
                    state.PreviewPlaying = false;
                    state.PausedNormalizedTime = 0f;
                    state.LastPreviewAudioPhase = -1;
                    state.PreviewStatus = "Preview stopped";
                }

                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;

                if (DeucarianEditorMiniToolbar.Button(state.PreviewMuted ? "Muted" : "Audio", true, GUILayout.Width(54f), GUILayout.Height(22f)))
                {
                    state.PreviewMuted = !state.PreviewMuted;
                    if (state.PreviewMuted)
                        AttackEditorPreviewAudio.StopAll();
                    state.LastPreviewAudioPhase = -1;
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
                if (DeucarianEditorMiniToolbar.Button("Full", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = true;
                    state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    state.LastPreviewAudioPhase = -1;
                    state.PreviewStatus = AttackGameContentPreviewActions.PreviewFullAttack(source, state.PreviewMuted);
                    context.Preview.SetStatus(state.PreviewStatus);
                }
            }
        }

        private static void DrawPreviewContextControls(
            AttackProviderV2State state,
            IReadOnlyList<AttackGameContentPreviewContextOption> sourceOptions,
            IReadOnlyList<AttackGameContentPreviewContextOption> targetOptions)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Source", GUILayout.Width(48f));
                state.PreviewSourceContextIndex = EditorGUILayout.Popup(
                    state.PreviewSourceContextIndex,
                    AttackGameContentPreviewContext.BuildLabels(sourceOptions),
                    GUILayout.ExpandWidth(true));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Target", GUILayout.Width(48f));
                state.PreviewTargetContextIndex = EditorGUILayout.Popup(
                    state.PreviewTargetContextIndex,
                    AttackGameContentPreviewContext.BuildLabels(targetOptions),
                    GUILayout.ExpandWidth(true));
            }
        }

        private static IReadOnlyList<DeucarianEditorTimelineEvent> BuildTimelineEvents(AttackAuthoringState state)
        {
            return new[]
            {
                Timeline("OnCast", state.CastVfxPrefab, state.CastAudio, true),
                Timeline("OnFire", state.FireVfxPrefab, state.FireAudio, true),
                Timeline(state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan ? "Travel / Beam" : "Travel", state.BeamVfxPrefab ?? state.ProjectilePrefab, null, true),
                Timeline("OnImpact", state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab, state.ImpactAudio, true),
                Timeline("Status Tick", state.TickVfxPrefab, state.TickAudio, state.IncludeStatusEffect),
                Timeline("OnExpire", state.ExpireVfxPrefab, state.ExpireAudio, state.IncludeStatusEffect)
            };
        }

        private static DeucarianEditorTimelineEvent Timeline(string label, GameObject visual, AudioClip audio, bool enabled)
        {
            string detail = visual == null && audio == null
                ? "Optional presentation asset not assigned."
                : string.Join(", ", BuildAssignedParts(visual, audio));
            return new DeucarianEditorTimelineEvent(label, detail, visual != null, audio != null, enabled);
        }

        private static IEnumerable<string> BuildAssignedParts(GameObject visual, AudioClip audio)
        {
            if (visual != null) yield return "VFX " + visual.name;
            if (audio != null) yield return "audio " + audio.name;
        }

        private static AttackPresentationEventKind GetEventKind(int index)
        {
            switch (index)
            {
                case 0:
                    return AttackPresentationEventKind.OnCast;
                case 1:
                    return AttackPresentationEventKind.OnFire;
                case 4:
                    return AttackPresentationEventKind.OnTick;
                case 5:
                    return AttackPresentationEventKind.OnExpire;
                default:
                    return AttackPresentationEventKind.OnImpact;
            }
        }

        private static GameContentAuthoringValidationResult ValidateDraft(AttackAuthoringState draft)
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

        private static void DrawValue(string label, string value)
        {
            DeucarianEditorFieldRow.Draw(label, () => EditorGUILayout.LabelField(value ?? string.Empty, DeucarianEditorStyles.MutedLabel));
        }

        private static void DrawRawReferences(string title, IReadOnlyList<GameContentLibraryReference> references)
        {
            EditorGUILayout.LabelField(title, DeucarianEditorStyles.SectionTitle);
            if (references == null || references.Count == 0)
            {
                EditorGUILayout.LabelField("None", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < references.Count; i++)
            {
                GameContentLibraryReference reference = references[i];
                if (reference == null || reference.Target == null)
                    continue;
                EditorGUILayout.LabelField(reference.Target.DisplayName + " - " + reference.PropertyPath, DeucarianEditorStyles.MutedLabel);
            }
        }

        private static string BuildHumanSummary(AttackAuthoringState state)
        {
            return FormatFloat(state.DamageAmount) + " " + state.DamageTypeId
                + ", " + state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " tick cooldown"
                + ", " + GetTypeLabel(state).ToLowerInvariant();
        }

        private static string BuildStateFingerprint(AttackAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            return string.Join("|", new[]
            {
                state.AttackId ?? string.Empty,
                state.DisplayName ?? string.Empty,
                AssetKey(state.Icon),
                state.TagsCsv ?? string.Empty,
                state.DamageTypeId ?? string.Empty,
                state.DamageAmount.ToString("R", CultureInfo.InvariantCulture),
                state.CooldownTicks.ToString(CultureInfo.InvariantCulture),
                state.Range.ToString("R", CultureInfo.InvariantCulture),
                state.TargetingMode.ToString(),
                state.DeliveryMode.ToString(),
                state.ProjectileDefinitionId ?? string.Empty,
                state.ProjectileSpawnableId ?? string.Empty,
                AssetKey(state.ProjectilePrefab),
                state.ProjectileSpeed.ToString("R", CultureInfo.InvariantCulture),
                state.ProjectileLifetimeTicks.ToString(CultureInfo.InvariantCulture),
                state.Homing ? "1" : "0",
                state.HomingTurnRate.ToString("R", CultureInfo.InvariantCulture),
                state.PierceCount.ToString(CultureInfo.InvariantCulture),
                state.Radius.ToString("R", CultureInfo.InvariantCulture),
                AssetKey(state.BeamVfxPrefab),
                AssetKey(state.ImpactVfxPrefab),
                state.MaxHits.ToString(CultureInfo.InvariantCulture),
                state.TickIntervalSeconds.ToString("R", CultureInfo.InvariantCulture),
                state.IncludeStatusEffect ? "1" : "0",
                state.StatusId ?? string.Empty,
                state.StatusDurationTicks.ToString(CultureInfo.InvariantCulture),
                state.StatusTickRateTicks.ToString(CultureInfo.InvariantCulture),
                state.StatusStrength.ToString("R", CultureInfo.InvariantCulture),
                state.StatusMaxStacks.ToString(CultureInfo.InvariantCulture),
                state.StatusStackingPolicy.ToString(),
                state.StatusEffectNote ?? string.Empty,
                AssetKey(state.CastAudio),
                AssetKey(state.FireAudio),
                AssetKey(state.ImpactAudio),
                AssetKey(state.TickAudio),
                AssetKey(state.ExpireAudio),
                AssetKey(state.CastVfxPrefab),
                AssetKey(state.FireVfxPrefab),
                AssetKey(state.ImpactVfxPresentationPrefab),
                AssetKey(state.TickVfxPrefab),
                AssetKey(state.ExpireVfxPrefab)
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

            int weapons = 0;
            int upgrades = 0;
            int sets = 0;
            int packs = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target == null) continue;
                if (target.Kind == GameContentLibraryKind.Weapon) weapons++;
                else if (target.Kind == GameContentLibraryKind.Upgrade) upgrades++;
                else if (target.Kind == GameContentLibraryKind.ContentSet) sets++;
                else if (target.Kind == GameContentLibraryKind.ContentPack) packs++;
            }

            return weapons.ToString(CultureInfo.InvariantCulture) + " weapon(s), "
                + upgrades.ToString(CultureInfo.InvariantCulture) + " upgrade(s), "
                + sets.ToString(CultureInfo.InvariantCulture) + " set(s), "
                + packs.ToString(CultureInfo.InvariantCulture) + " pack(s)";
        }

        private static string BuildAdvancedReport(GameContentLibraryItem item, AttackAuthoringState state)
        {
            return "Attack: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.AttackId + Environment.NewLine
                + "Path: " + item.Path + Environment.NewLine
                + "Delivery: " + GetTypeLabel(state) + Environment.NewLine
                + "Damage: " + FormatFloat(state.DamageAmount) + " " + state.DamageTypeId;
        }

        private static DeucarianEditorStatus GetStatus(GameContentLibraryItem item)
        {
            if (item == null) return DeucarianEditorStatus.Disabled;
            if (item.ErrorCount > 0) return DeucarianEditorStatus.Error;
            if (item.WarningCount > 0) return DeucarianEditorStatus.Warning;
            return DeucarianEditorStatus.Success;
        }

        private static string GetTypeLabel(AttackAuthoringState state)
        {
            if (state == null) return "Attack";
            if (state.IncludeStatusEffect && state.DeliveryMode == AttackRecipeDeliveryMode.Aura) return "Status";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile) return state.Homing ? "Homing" : "Projectile";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan) return "Beam";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Area) return "AOE";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Aura) return "Status";
            return state.DeliveryMode.ToString();
        }

        private static bool HasAnyVisual(AttackAuthoringState state)
        {
            return state != null && (state.ProjectilePrefab != null
                || state.BeamVfxPrefab != null
                || state.ImpactVfxPrefab != null
                || state.CastVfxPrefab != null
                || state.FireVfxPrefab != null
                || state.ImpactVfxPresentationPrefab != null
                || state.TickVfxPrefab != null
                || state.ExpireVfxPrefab != null);
        }

        private static bool HasAnyAudio(AttackAuthoringState state)
        {
            return state != null && (state.CastAudio != null
                || state.FireAudio != null
                || state.ImpactAudio != null
                || state.TickAudio != null
                || state.ExpireAudio != null);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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

    internal sealed class AttackProviderV2ListItem
    {
        private AttackProviderV2ListItem(
            GameContentLibraryItem source,
            string displayName,
            string stableId,
            string typeLabel,
            string tags,
            bool hasPrefab,
            bool hasVisuals,
            bool hasAudio)
        {
            Source = source;
            DisplayName = displayName ?? string.Empty;
            StableId = stableId ?? string.Empty;
            TypeLabel = typeLabel ?? "Attack";
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
        public string TypeLabel { get; }
        public string Tags { get; }
        public bool HasPrefab { get; }
        public bool HasVisuals { get; }
        public bool HasAudio { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }

        public static IReadOnlyList<AttackProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<AttackProviderV2ListItem>();

            var result = new List<AttackProviderV2ListItem>();
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

        public static AttackProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            AttackDefinitionAsset attack = item == null ? null : item.Asset as AttackDefinitionAsset;
            string displayName = attack == null ? item.DisplayName : attack.DisplayName;
            string stableId = attack == null ? item.Id : attack.Id;
            string tags = attack == null ? string.Empty : JoinTags(attack.Tags);
            string type = GetTypeLabel(attack);
            bool hasPrimaryDeliveryAsset = attack != null
                && attack.Delivery != null
                && (attack.Delivery.Mode == AttackRecipeDeliveryMode.Hitscan
                    ? attack.Delivery.BeamVfxPrefab != null
                    : attack.Delivery.ProjectilePrefab != null);
            bool hasDeliveryVfx = attack != null && attack.Delivery != null && (attack.Delivery.BeamVfxPrefab != null || attack.Delivery.ImpactVfxPrefab != null);
            bool hasPresentationVfx = HasPresentationAsset(attack, false);
            bool hasAudio = HasPresentationAsset(attack, true);
            return new AttackProviderV2ListItem(item, displayName, stableId, type, tags, hasPrimaryDeliveryAsset, hasPrimaryDeliveryAsset || hasDeliveryVfx || hasPresentationVfx, hasAudio);
        }

        internal static string GetTypeLabelForTests(AttackDefinitionAsset attack)
        {
            return GetTypeLabel(attack);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string normalized = query.Trim().ToLowerInvariant();
            return Contains(DisplayName, normalized)
                || Contains(StableId, normalized)
                || Contains(TypeLabel, normalized)
                || Contains(Tags, normalized)
                || (Source != null && Contains(Source.Category, normalized));
        }

        private static bool Contains(string value, string normalizedQuery)
        {
            return !string.IsNullOrWhiteSpace(value) && value.ToLowerInvariant().Contains(normalizedQuery);
        }

        private static string GetTypeLabel(AttackDefinitionAsset attack)
        {
            if (attack == null || attack.Delivery == null)
                return "Attack";

            AttackRecipeDeliveryMode mode = attack.Delivery.Mode;
            bool status = attack.StatusEffects != null && attack.StatusEffects.StatusEffects.Count > 0;
            if (status && mode == AttackRecipeDeliveryMode.Aura) return "Status";
            if (mode == AttackRecipeDeliveryMode.Projectile) return attack.Delivery.Homing ? "Homing" : "Projectile";
            if (mode == AttackRecipeDeliveryMode.Hitscan) return "Beam";
            if (mode == AttackRecipeDeliveryMode.Area) return "AOE";
            if (mode == AttackRecipeDeliveryMode.Aura) return "Status";
            return mode.ToString();
        }

        private static bool HasPresentationAsset(AttackDefinitionAsset attack, bool audio)
        {
            if (attack == null || attack.Presentation == null)
                return false;

            AttackPresentationEventKind[] events =
            {
                AttackPresentationEventKind.OnCast,
                AttackPresentationEventKind.OnFire,
                AttackPresentationEventKind.OnImpact,
                AttackPresentationEventKind.OnTick,
                AttackPresentationEventKind.OnExpire
            };

            for (int i = 0; i < events.Length; i++)
            {
                if (!attack.Presentation.TryGetEvent(events[i], out AttackPresentationEventRecipe recipe) || recipe == null)
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
