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
    internal sealed class WaveProviderV2State
    {
        public string SearchText = string.Empty;
        public bool Creating;
        public int DetailPage;
        public int WizardStep;
        public Vector2 ListScroll;
        public Vector2 DetailScroll;
        public Vector2 PreviewScroll;
        public bool PreviewLoop = true;
        public bool PreviewPlaying = true;
        public float PreviewSpeed = 1f;
        public GameContentAuthoringActionPreviewRenderMode PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Game;
        public double PreviewStartTime;
        public float PausedNormalizedTime = 0.5f;
        public string ActivePreviewKey = string.Empty;
        public string PreviewStatus = "Preview idle";
        public WaveAuthoringState EditingState;
        public GameContentAuthoringObjectEditorContext EditingContext;
        public GameContentCreationResult LastEditResult;

        public void StopPreview()
        {
            PreviewPlaying = false;
            PreviewStartTime = 0d;
            PausedNormalizedTime = 0.5f;
            PreviewStatus = "Preview stopped";
        }

        public void BeginCreate()
        {
            Creating = true;
            WizardStep = 0;
            DetailPage = 0;
            DetailScroll = Vector2.zero;
            ClearEditingState();
            PreviewStatus = "Previewing draft wave";
        }

        public void ResetProviderSession()
        {
            Creating = false;
            DetailPage = 0;
            WizardStep = 0;
            ListScroll = Vector2.zero;
            DetailScroll = Vector2.zero;
            PreviewScroll = Vector2.zero;
            ActivePreviewKey = string.Empty;
            PreviewStatus = "Preview idle";
            ClearEditingState();
        }

        public void SetPreviewSource(string key, WaveGameContentPreviewController controller)
        {
            key = key ?? string.Empty;
            if (string.Equals(ActivePreviewKey, key, StringComparison.Ordinal))
                return;

            controller?.Stop();
            ActivePreviewKey = key;
            PreviewPlaying = true;
            PreviewStartTime = EditorApplication.timeSinceStartup;
            PausedNormalizedTime = 0f;
            PreviewStatus = "Previewing";
        }

        public void ClearEditingState()
        {
            EditingState = null;
            EditingContext = null;
            LastEditResult = null;
        }
    }

    internal sealed class WaveProviderV2View
    {
        private static readonly string[] DetailPages =
        {
            "Overview",
            "Entries",
            "Timing",
            "Channels",
            "Balance",
            "References",
            "Advanced"
        };

        private static readonly string[] WizardSteps =
        {
            "Identity",
            "Entries",
            "Timing",
            "Channels",
            "Balance",
            "Review"
        };

        private static readonly string[] PreferredChannels =
        {
            "perimeter-north",
            "perimeter-east",
            "perimeter-south",
            "perimeter-west",
            "entry",
            "center"
        };

        public void Draw(
            GameContentAuthoringSurfaceContext context,
            WaveAuthoringState draft,
            WaveGameContentPreviewController previewController,
            WaveProviderV2State state)
        {
            if (context == null || draft == null || state == null)
                return;

            draft.EnsureEntries();
            IReadOnlyList<WaveProviderV2ListItem> items = WaveProviderV2ListItem.Build(context.AuthoredItems);
            EnsureDefaultMode(context, state, items);
            EnsureEditingState(context, state);
            TrackPreviewSource(context, state, previewController);

            GameContentAuthoringWorkbench.Draw(
                context,
                () => DrawWaveList(context, state, items),
                () => DrawDetailOrWizard(context, draft, state),
                () => DrawPreviewLab(context, draft, state));
        }

        private static void EnsureDefaultMode(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, IReadOnlyList<WaveProviderV2ListItem> items)
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

        private static void EnsureEditingState(GameContentAuthoringSurfaceContext context, WaveProviderV2State state)
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
            string fingerprint = BuildStateFingerprint(state.EditingState);
            state.EditingContext = new GameContentAuthoringObjectEditorContext(context.SelectedItem, fingerprint);
            state.LastEditResult = null;
        }

        private static void TrackPreviewSource(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, WaveGameContentPreviewController previewController)
        {
            string key = state.Creating
                ? "__draft_wave__"
                : context.SelectedItem == null
                    ? string.Empty
                    : context.SelectedItem.Key;
            state.SetPreviewSource(key, previewController);
        }

        private static void DrawWaveList(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, IReadOnlyList<WaveProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Waves", DeucarianEditorStyles.SectionTitle);
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button("Refresh", true, GUILayout.Width(62f), GUILayout.Height(22f)))
                    context.RefreshLibrary();
            }

            state.SearchText = DeucarianEditorSearchField.Draw(state.SearchText, "Search waves", GUILayout.ExpandWidth(true));
            if (DeucarianEditorButtons.Secondary("Create New", true, GUILayout.Height(24f)))
            {
                state.BeginCreate();
                context.ClearSelection();
                context.RequestRepaint();
            }

            GUILayout.Space(DeucarianEditorSpacing.Small);
            state.ListScroll = EditorGUILayout.BeginScrollView(state.ListScroll);
            int shown = 0;
            for (int i = 0; i < items.Count; i++)
            {
                WaveProviderV2ListItem item = items[i];
                if (!item.Matches(state.SearchText))
                    continue;

                shown++;
                DrawWaveCard(context, state, item);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(items.Count == 0 ? "No authored waves found." : "No waves match the current search.", DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawWaveCard(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, WaveProviderV2ListItem item)
        {
            bool selected = !state.Creating && context.IsSelected(item.Source);
            var chips = new[]
            {
                new DeucarianEditorStatusChip(item.EnemyCountLabel, item.TotalEnemyCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(item.DurationLabel, DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(item.ReadinessLabel, item.ReadinessStatus),
                new DeucarianEditorStatusChip(item.ChannelLabel, item.HasChannels ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error, item.ChannelTooltip),
                new DeucarianEditorStatusChip(item.UsageLabel, item.UsageCount > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled, "Content set/pack usage")
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
                if (Event.current != null)
                    Event.current.Use();
            }
        }

        private static void DrawDetailOrWizard(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            state.DetailScroll = EditorGUILayout.BeginScrollView(state.DetailScroll);
            if (state.Creating)
                DrawCreateWizard(context, draft, state);
            else
                DrawSelectedWave(context, state);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelectedWave(GameContentAuthoringSurfaceContext context, WaveProviderV2State state)
        {
            WaveDefinitionAsset asset = context.SelectedItem == null ? null : context.SelectedItem.Asset as WaveDefinitionAsset;
            if (asset == null || state.EditingState == null || state.EditingContext == null)
            {
                EditorGUILayout.LabelField("Select a wave to edit.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            WaveAuthoringState edit = state.EditingState;
            edit.EnsureEntries();
            GameContentAuthoringValidationResult validation = WaveDefinitionAssetCreator.ValidateForUpdate(edit, asset);
            string fingerprint = BuildStateFingerprint(edit);
            state.EditingContext.Capture(fingerprint, validation);
            context.Authoring.SetValidation(validation);

            DrawHeader(edit.DisplayName, edit.WaveId, BuildWaveChips(edit, validation, context.SelectedItem));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(
                GameContentAuthoringWorkbenchMode.Edit,
                validation.IsValid,
                state.EditingContext.IsDirty,
                "Save",
                state.LastEditResult == null ? state.EditingContext.StatusMessage : state.LastEditResult.Message);
            HandleEditCommand(context, state, asset, command);

            state.DetailPage = DeucarianEditorSegmentedControl.DrawPageChips(state.DetailPage, DetailPages);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.DetailPage, 0, DetailPages.Length - 1))
            {
                case 1:
                    DrawEntries(context, edit);
                    break;
                case 2:
                    DrawTiming(context, edit);
                    break;
                case 3:
                    DrawChannels(edit);
                    break;
                case 4:
                    DrawBalance(edit);
                    break;
                case 5:
                    DrawReferences(context.SelectedItem);
                    break;
                case 6:
                    DrawAdvanced(context.SelectedItem, edit, asset);
                    break;
                default:
                    DrawOverview(context, edit, context.SelectedItem, false, validation);
                    break;
            }

            DrawValidationIssues(validation);
        }

        private static void HandleEditCommand(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, WaveDefinitionAsset asset, GameContentAuthoringCommand command)
        {
            if (command == GameContentAuthoringCommand.Revert)
            {
                state.EditingState = AttackGameContentPreviewSelection.FromWaveAsset(asset);
                string fingerprint = BuildStateFingerprint(state.EditingState);
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
                string fingerprint = BuildStateFingerprint(state.EditingState);
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

        private static void DrawCreateWizard(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            draft.EnsureEntries();
            GameContentAuthoringValidationResult validation = ValidateDraft(draft);
            DrawHeader("New Wave", draft.WaveId, BuildWaveChips(draft, validation, null));
            GameContentAuthoringCommand command = GameContentAuthoringCommandBar.Draw(GameContentAuthoringWorkbenchMode.Create, validation.IsValid, true, "Create");
            if (command == GameContentAuthoringCommand.Create)
            {
                GameContentCreationResult result = WaveDefinitionAssetCreator.CreateAssets(draft);
                context.Authoring.SetCreationResult(result);
                if (result != null && result.Succeeded)
                {
                    state.Creating = false;
                    context.RefreshLibrary();
                }
            }

            state.WizardStep = DeucarianEditorWizardHeader.Draw(state.WizardStep, WizardSteps);
            GUILayout.Space(DeucarianEditorSpacing.Small);
            switch (Mathf.Clamp(state.WizardStep, 0, WizardSteps.Length - 1))
            {
                case 1:
                    DrawEntries(context, draft);
                    break;
                case 2:
                    DrawTiming(context, draft);
                    break;
                case 3:
                    DrawChannels(draft);
                    break;
                case 4:
                    DrawBalance(draft);
                    break;
                case 5:
                    DrawReview(draft, validation);
                    break;
                default:
                    DrawOverview(context, draft, null, true, validation);
                    break;
            }

            DrawValidationIssues(validation);
            context.Authoring.DrawCreationResult();
        }

        private static void DrawHeader(string title, string subtitle, IReadOnlyList<DeucarianEditorStatusChip> chips)
        {
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(title) ? "Wave" : title, HeaderStyle);
            if (!string.IsNullOrWhiteSpace(subtitle))
                EditorGUILayout.LabelField(subtitle, DeucarianEditorStyles.MutedLabel);
            DeucarianEditorStatusChipRow.Draw(chips);
        }

        private static void DrawOverview(GameContentAuthoringSurfaceContext context, WaveAuthoringState state, GameContentLibraryItem item, bool creating, GameContentAuthoringValidationResult validation)
        {
            state.WaveId = context.Authoring.DrawTextField("Stable ID", state.WaveId);
            state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
            state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
            if (creating)
                state.OutputRoot = context.Authoring.DrawOutputRootField(state.OutputRoot);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Total Enemies", GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                Row("Approx Duration", GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("Enemy Mix", BuildEnemyMixSummary(state)),
                Row("Used By", item == null ? "New draft" : BuildUsageSummary(item)));
        }

        private static void DrawEntries(GameContentAuthoringSurfaceContext context, WaveAuthoringState state)
        {
            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
                DrawEntry(context, state, i);

            if (DeucarianEditorButtons.Secondary("Add Entry", true, GUILayout.Height(24f)))
                state.Entries.Add(new WaveEntryAuthoringState());
        }

        private static void DrawEntry(GameContentAuthoringSurfaceContext context, WaveAuthoringState state, int index)
        {
            WaveEntryAuthoringState entry = state.Entries[index];
            bool remove = false;
            bool duplicate = false;
            bool moveUp = false;
            bool moveDown = false;
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Entry " + (index + 1).ToString(CultureInfo.InvariantCulture), DeucarianEditorStyles.SectionTitle);
                    GUILayout.FlexibleSpace();
                    if (DeucarianEditorMiniToolbar.Button("Up", index > 0, GUILayout.Width(38f), GUILayout.Height(22f)))
                        moveUp = true;
                    if (DeucarianEditorMiniToolbar.Button("Down", index < state.Entries.Count - 1, GUILayout.Width(54f), GUILayout.Height(22f)))
                        moveDown = true;
                    if (DeucarianEditorMiniToolbar.Button("Copy", true, GUILayout.Width(50f), GUILayout.Height(22f)))
                        duplicate = true;
                    if (DeucarianEditorMiniToolbar.Button("Remove", state.Entries.Count > 1, GUILayout.Width(68f), GUILayout.Height(22f)))
                        remove = true;
                }

                if (remove)
                    return;

                entry.Enemy = context.Authoring.DrawObjectField("Enemy", entry.Enemy);
                entry.Count = context.Authoring.DrawIntField("Count", entry.Count);
                entry.BatchSize = context.Authoring.DrawIntField("Batch Size", entry.BatchSize);
                entry.InitialDelayTicks = context.Authoring.DrawIntField("Start Delay", entry.InitialDelayTicks);
                entry.IntervalTicks = context.Authoring.DrawIntField("Interval", entry.IntervalTicks);
                entry.SpawnChannelId = DrawChannelField("Lane / Channel", entry.SpawnChannelId);
                entry.ScalingTier = context.Authoring.DrawIntField("Difficulty Tier", entry.ScalingTier);

                DeucarianEditorStatusChipRow.Draw(BuildEntryChips(entry));
            });

            if (remove)
                state.Entries.RemoveAt(index);
            else if (duplicate)
                state.Entries.Insert(index + 1, CopyEntry(entry));
            else if (moveUp)
                MoveEntry(state, index, index - 1);
            else if (moveDown)
                MoveEntry(state, index, index + 1);
        }

        private static void DrawTiming(GameContentAuthoringSurfaceContext context, WaveAuthoringState state)
        {
            state.StartTick = context.Authoring.DrawIntField("Start Tick", state.StartTick);
            DrawSummaryRows(
                Row("Approx Duration", GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("First Spawn", GetFirstSpawnTick(state).ToString(CultureInfo.InvariantCulture)),
                Row("Last Spawn", GetLastSpawnTick(state).ToString(CultureInfo.InvariantCulture)),
                Row("Cadence", BuildCadenceSummary(state)));
            context.Preview.DrawTimeline(AttackGameContentPreviewSummaries.BuildWaveTimeline(state));
        }

        private static void DrawChannels(WaveAuthoringState state)
        {
            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                entry.SpawnChannelId = DrawChannelField("Entry " + (i + 1).ToString(CultureInfo.InvariantCulture), entry.SpawnChannelId);
            }

            DrawSummaryRows(
                Row("Channels", BuildChannelSummary(state)),
                Row("Groups", BuildChannelGroupSummary(state)));
        }

        private static void DrawBalance(WaveAuthoringState state)
        {
            DrawSummaryRows(
                Row("Total Enemies", GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                Row("Enemy Mix", BuildEnemyMixSummary(state)),
                Row("Pressure", BuildPressureEstimate(state)),
                Row("Pacing", BuildPacingLabel(state)),
                Row("Max Tier", GetMaxScalingTier(state).ToString(CultureInfo.InvariantCulture)));
        }

        private static void DrawReferences(GameContentLibraryItem item)
        {
            if (item == null)
            {
                EditorGUILayout.LabelField("No references for a new draft.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            if (item.ReverseReferences.Count == 0)
            {
                EditorGUILayout.LabelField("No authored content references this wave.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryReference reference = item.ReverseReferences[i];
                string label = reference.Target == null ? "Reference" : reference.Target.DisplayName;
                string detail = reference.Target == null ? reference.PropertyPath : reference.Target.Category + " - " + reference.PropertyPath;
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    EditorGUILayout.LabelField(label, DeucarianEditorStyles.SectionTitle);
                    EditorGUILayout.LabelField(detail, DeucarianEditorStyles.MutedLabel);
                });
            }
        }

        private static void DrawAdvanced(GameContentLibraryItem item, WaveAuthoringState state, WaveDefinitionAsset asset)
        {
            DrawSummaryRows(
                Row("Asset Path", item == null ? "(not created)" : item.Path),
                Row("Schedule Section", asset != null && asset.Schedule != null ? AssetDatabase.GetAssetPath(asset.Schedule) : "Missing"),
                Row("Entries Section", asset != null && asset.Entries != null ? AssetDatabase.GetAssetPath(asset.Entries) : "Missing"),
                Row("Output Root", state.OutputRoot),
                Row("Raw References", item == null ? "New draft" : item.DirectReferences.Count.ToString(CultureInfo.InvariantCulture) + " direct, " + item.ReverseReferences.Count.ToString(CultureInfo.InvariantCulture) + " reverse"));

            if (DeucarianEditorButtons.Secondary("Copy Report", true, GUILayout.Width(110f), GUILayout.Height(24f)))
                EditorGUIUtility.systemCopyBuffer = BuildAdvancedReport(item, state, asset);
        }

        private static void DrawReview(WaveAuthoringState state, GameContentAuthoringValidationResult validation)
        {
            IReadOnlyList<string> lines = WaveDefinitionAssetCreator.GetPreviewLines(state);
            for (int i = 0; i < lines.Count; i++)
                EditorGUILayout.LabelField(lines[i], DeucarianEditorStyles.MutedLabel);

            DrawSummaryRows(
                Row("Readiness", BuildValidationSummary(validation)),
                Row("Entries", state.Entries.Count.ToString(CultureInfo.InvariantCulture)),
                Row("Total Enemies", GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                Row("Channels", BuildChannelSummary(state)));
        }

        private static void DrawPreviewLab(GameContentAuthoringSurfaceContext context, WaveAuthoringState draft, WaveProviderV2State state)
        {
            WaveAuthoringState source = state.Creating ? draft : state.EditingState;
            if (source == null)
            {
                EditorGUILayout.LabelField("Select a wave to preview.", DeucarianEditorStyles.MutedLabel);
                return;
            }

            source.EnsureEntries();
            bool dirty = !state.Creating && state.EditingContext != null && state.EditingContext.IsDirty;
            GameContentPreviewLabModel model = new GameContentPreviewLabModel
            {
                Title = "Wave Preview Lab",
                PreviewTitle = string.IsNullOrWhiteSpace(source.DisplayName) ? "Wave Preview" : source.DisplayName,
                ScopeLabel = WaveProviderV2PreviewModel.GetScopeLabel(state.Creating, dirty),
                PrimaryAsset = GetPrimaryPreviewAsset(source),
                EmptyText = "No enemy prefab available for this wave.",
                PreviewOptions = BuildPreviewOptions(source, state),
                Chips = WaveProviderV2PreviewModel.BuildChips(source, state),
                DrawControls = () => DrawPreviewControls(state),
                DrawContext = () => DrawPreviewContext(context, source),
                DrawBody = () => DrawPreviewBody(context, source, state)
            };

            state.PreviewScroll = EditorGUILayout.BeginScrollView(state.PreviewScroll);
            context.Preview.SetStatus(state.PreviewStatus);
            GameContentPreviewLabRenderer.Draw(context.Preview, model);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewControls(WaveProviderV2State state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playLabel = state.PreviewPlaying ? "Pause" : "Play";
                if (DeucarianEditorMiniToolbar.Button(playLabel, true, GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    state.PreviewPlaying = !state.PreviewPlaying;
                    if (state.PreviewPlaying)
                        state.PreviewStartTime = EditorApplication.timeSinceStartup;
                    else
                        state.PausedNormalizedTime = 0.5f;
                }

                if (DeucarianEditorMiniToolbar.Button("Stop", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.StopPreview();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewLoop ? "Loop" : "Once", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewLoop = !state.PreviewLoop;
                if (DeucarianEditorMiniToolbar.Button("0.5x", true, GUILayout.Width(48f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 0.5f;
                if (DeucarianEditorMiniToolbar.Button("1x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 1f;
                if (DeucarianEditorMiniToolbar.Button("2x", true, GUILayout.Width(38f), GUILayout.Height(22f)))
                    state.PreviewSpeed = 2f;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (DeucarianEditorMiniToolbar.Button(state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game ? "Game" : "Debug", true, GUILayout.Width(58f), GUILayout.Height(22f)))
                    state.PreviewRenderMode = state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Game
                        ? GameContentAuthoringActionPreviewRenderMode.Debug
                        : GameContentAuthoringActionPreviewRenderMode.Game;
            }
        }

        private static void DrawPreviewContext(GameContentAuthoringSurfaceContext context, WaveAuthoringState source)
        {
            context.Preview.DrawSummaryRow("Enemies", GetTotalEnemyCount(source).ToString(CultureInfo.InvariantCulture));
            context.Preview.DrawSummaryRow("Duration", GetApproximateDurationTicks(source).ToString(CultureInfo.InvariantCulture) + " tick(s)");
            context.Preview.DrawSummaryRow("Channels", BuildChannelSummary(source));
        }

        private static void DrawPreviewBody(GameContentAuthoringSurfaceContext context, WaveAuthoringState source, WaveProviderV2State state)
        {
            context.Preview.DrawTimeline(AttackGameContentPreviewSummaries.BuildWaveTimeline(source));
            context.Preview.DrawSummaryRows(new[]
            {
                Row("Enemy Mix", BuildEnemyMixSummary(source)),
                Row("Cadence", BuildCadenceSummary(source)),
                Row("Pacing", BuildPacingLabel(source))
            });

            if (state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug)
            {
                context.Preview.DrawSummaryRows(BuildDebugRows(source));
                context.Preview.DrawWarnings(AttackGameContentPreviewSummaries.BuildWaveWarnings(source));
            }
        }

        private static GameContentAuthoringObjectPreviewOptions BuildPreviewOptions(WaveAuthoringState source, WaveProviderV2State state)
        {
            var preview = new GameContentAuthoringActionPreview
            {
                Mode = GameContentAuthoringActionPreviewMode.Area,
                RenderMode = state.PreviewRenderMode,
                Playing = state.PreviewPlaying,
                Loop = state.PreviewLoop,
                Speed = state.PreviewSpeed,
                StartTime = state.PreviewStartTime,
                StaticNormalizedTime = state.PausedNormalizedTime,
                Label = string.IsNullOrWhiteSpace(source.DisplayName) ? source.WaveId : source.DisplayName,
                DeliveryTypeLabel = "Wave Timeline",
                SourceContextLabel = BuildChannelSummary(source),
                TargetContextLabel = GetTotalEnemyCount(source).ToString(CultureInfo.InvariantCulture) + " enemies"
            };

            IReadOnlyList<WaveEntryAuthoringState> entries = source.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryAuthoringState entry = entries[i];
                preview.Roles.Add(new GameContentAuthoringActionPreviewRole(
                    "Entry " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    GetEnemyLabel(entry.Enemy),
                    GetEnemyPreviewAsset(entry.Enemy)));
            }

            return new GameContentAuthoringObjectPreviewOptions
            {
                MinimumHeight = 184f,
                ActionPreview = preview
            };
        }

        public static string BuildStateFingerprint(WaveAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            state.EnsureEntries();
            var builder = new StringBuilder()
                .Append(state.WaveId).Append('|')
                .Append(state.DisplayName).Append('|')
                .Append(state.TagsCsv).Append('|')
                .Append(state.OutputRoot).Append('|')
                .Append(state.StartTick.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                builder.Append('|').Append(entry.Enemy == null ? string.Empty : entry.Enemy.Id)
                    .Append(':').Append(entry.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.BatchSize.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.InitialDelayTicks.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.IntervalTicks.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.SpawnChannelId ?? string.Empty)
                    .Append(':').Append(entry.ScalingTier.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        public static int GetTotalEnemyCount(WaveAuthoringState state)
        {
            return AttackGameContentPreviewSummaries.GetWaveTotalEnemyCount(state);
        }

        public static int GetApproximateDurationTicks(WaveAuthoringState state)
        {
            return AttackGameContentPreviewSummaries.GetWaveApproximateDurationTicks(state);
        }

        public static string BuildEnemyMixSummary(WaveAuthoringState state)
        {
            if (state == null)
                return "None";

            state.EnsureEntries();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                string label = entry.Enemy == null ? "Missing enemy" : string.IsNullOrWhiteSpace(entry.Enemy.DisplayName) ? entry.Enemy.Id : entry.Enemy.DisplayName;
                counts[label] = counts.ContainsKey(label) ? counts[label] + Math.Max(0, entry.Count) : Math.Max(0, entry.Count);
            }

            if (counts.Count == 0)
                return "None";

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add(pair.Key + " x" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return string.Join(", ", parts.ToArray());
        }

        public static string BuildChannelSummary(WaveAuthoringState state)
        {
            if (state == null)
                return "None";

            state.EnsureEntries();
            var channels = new List<string>();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                string channel = state.Entries[i].SpawnChannelId;
                if (string.IsNullOrWhiteSpace(channel) || channels.Contains(channel))
                    continue;
                channels.Add(channel);
            }

            return channels.Count == 0 ? "None" : string.Join(", ", channels.ToArray());
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildWaveChips(WaveAuthoringState state, GameContentAuthoringValidationResult validation, GameContentLibraryItem item)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture) + " enemies", GetTotalEnemyCount(state) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(BuildValidationSummary(validation), validation != null && validation.ErrorCount > 0 ? DeucarianEditorStatus.Error : validation != null && validation.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(BuildChannelSummary(state) == "None" ? "NoChannel" : "Channels", BuildChannelSummary(state) == "None" ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success, BuildChannelSummary(state)),
                new DeucarianEditorStatusChip(item == null ? "Draft" : BuildUsageSummary(item), item == null || item.ReverseReferences.Count == 0 ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success)
            };
        }

        private static IReadOnlyList<DeucarianEditorStatusChip> BuildEntryChips(WaveEntryAuthoringState entry)
        {
            return new[]
            {
                new DeucarianEditorStatusChip(entry.Enemy == null ? "NoEnemy" : "Enemy", entry.Enemy == null ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(Math.Max(0, entry.Count).ToString(CultureInfo.InvariantCulture) + " total", entry.Count > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(entry.IntervalTicks.ToString(CultureInfo.InvariantCulture) + " tick", entry.IntervalTicks >= 0 ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(string.IsNullOrWhiteSpace(entry.SpawnChannelId) ? "NoChannel" : entry.SpawnChannelId, string.IsNullOrWhiteSpace(entry.SpawnChannelId) ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success)
            };
        }

        private static IReadOnlyList<GameContentAuthoringPreviewRow> BuildDebugRows(WaveAuthoringState state)
        {
            var rows = new List<GameContentAuthoringPreviewRow>();
            rows.Add(Row("Raw Wave ID", state.WaveId));
            rows.Add(Row("Start Tick", state.StartTick.ToString(CultureInfo.InvariantCulture)));
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                rows.Add(Row("Entry " + i.ToString(CultureInfo.InvariantCulture), (entry.Enemy == null ? "missing" : entry.Enemy.Id)
                    + " | count " + entry.Count.ToString(CultureInfo.InvariantCulture)
                    + " | batch " + entry.BatchSize.ToString(CultureInfo.InvariantCulture)
                    + " | delay " + entry.InitialDelayTicks.ToString(CultureInfo.InvariantCulture)
                    + " | interval " + entry.IntervalTicks.ToString(CultureInfo.InvariantCulture)
                    + " | channel " + entry.SpawnChannelId
                    + " | tier " + entry.ScalingTier.ToString(CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        private static GameContentAuthoringValidationResult ValidateDraft(WaveAuthoringState draft)
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

        private static void DrawValidationIssues(GameContentAuthoringValidationResult validation)
        {
            if (validation == null || validation.Issues.Count == 0)
                return;

            var messages = new List<string>();
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                GameContentAuthoringValidationIssue issue = validation.Issues[i];
                string prefix = string.IsNullOrWhiteSpace(issue.Path) ? string.Empty : issue.Path + ": ";
                messages.Add(prefix + issue.Message);
            }

            DeucarianEditorStatus status = validation.ErrorCount > 0
                ? DeucarianEditorStatus.Error
                : validation.WarningCount > 0
                    ? DeucarianEditorStatus.Warning
                    : DeucarianEditorStatus.Info;
            DeucarianEditorStatusPanel.DrawValidationCard(BuildValidationSummary(validation), messages, status);
        }

        private static void DrawSummaryRows(params GameContentAuthoringPreviewRow[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                GameContentAuthoringPreviewRow row = rows[i];
                DeucarianEditorFieldRow.Draw(row.Label, () => EditorGUILayout.LabelField(row.Value, EditorStyles.label));
            }
        }

        private static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        private static string DrawChannelField(string label, string value)
        {
            string current = string.IsNullOrWhiteSpace(value) ? PreferredChannels[0] : value.Trim();
            var options = new List<string>(PreferredChannels);
            if (!options.Contains(current))
                options.Add(current);

            int index = Mathf.Max(0, options.IndexOf(current));
            DeucarianEditorFieldRow.Draw(label, () => index = EditorGUILayout.Popup(index, options.ToArray()));
            return options[Mathf.Clamp(index, 0, options.Count - 1)];
        }

        private static void MoveEntry(WaveAuthoringState state, int from, int to)
        {
            if (state == null || from < 0 || to < 0 || from >= state.Entries.Count || to >= state.Entries.Count)
                return;

            WaveEntryAuthoringState entry = state.Entries[from];
            state.Entries.RemoveAt(from);
            state.Entries.Insert(to, entry);
        }

        private static WaveEntryAuthoringState CopyEntry(WaveEntryAuthoringState entry)
        {
            return new WaveEntryAuthoringState
            {
                Enemy = entry.Enemy,
                Count = entry.Count,
                BatchSize = entry.BatchSize,
                InitialDelayTicks = entry.InitialDelayTicks,
                IntervalTicks = entry.IntervalTicks,
                SpawnChannelId = entry.SpawnChannelId,
                ScalingTier = entry.ScalingTier
            };
        }

        private static string BuildValidationSummary(GameContentAuthoringValidationResult validation)
        {
            if (validation == null)
                return "Pending";
            if (validation.ErrorCount > 0)
                return validation.ErrorCount.ToString(CultureInfo.InvariantCulture) + " blocker(s)";
            if (validation.WarningCount > 0)
                return validation.WarningCount.ToString(CultureInfo.InvariantCulture) + " warning(s)";
            return "Ready";
        }

        private static string BuildUsageSummary(GameContentLibraryItem item)
        {
            if (item == null)
                return "Draft";

            int setCount = 0;
            int packCount = 0;
            for (int i = 0; i < item.ReverseReferences.Count; i++)
            {
                GameContentLibraryItem target = item.ReverseReferences[i].Target;
                if (target == null)
                    continue;
                if (target.Kind == GameContentLibraryKind.ContentSet)
                    setCount++;
                else if (target.Kind == GameContentLibraryKind.ContentPack)
                    packCount++;
            }

            return setCount.ToString(CultureInfo.InvariantCulture) + " set(s), " + packCount.ToString(CultureInfo.InvariantCulture) + " pack(s)";
        }

        private static string BuildCadenceSummary(WaveAuthoringState state)
        {
            if (state == null || state.Entries.Count == 0)
                return "No cadence";

            int min = int.MaxValue;
            int max = 0;
            for (int i = 0; i < state.Entries.Count; i++)
            {
                int interval = Math.Max(0, state.Entries[i].IntervalTicks);
                min = Math.Min(min, interval);
                max = Math.Max(max, interval);
            }

            return min == max
                ? "Every " + max.ToString(CultureInfo.InvariantCulture) + " tick(s)"
                : min.ToString(CultureInfo.InvariantCulture) + "-" + max.ToString(CultureInfo.InvariantCulture) + " tick intervals";
        }

        private static string BuildChannelGroupSummary(WaveAuthoringState state)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                string channel = string.IsNullOrWhiteSpace(state.Entries[i].SpawnChannelId) ? "Missing" : state.Entries[i].SpawnChannelId;
                counts[channel] = counts.ContainsKey(channel) ? counts[channel] + 1 : 1;
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add(pair.Key + " x" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return parts.Count == 0 ? "None" : string.Join(", ", parts.ToArray());
        }

        private static string BuildPressureEstimate(WaveAuthoringState state)
        {
            int duration = Math.Max(1, GetApproximateDurationTicks(state));
            double pressure = GetTotalEnemyCount(state) / (double)duration;
            return pressure.ToString("0.##", CultureInfo.InvariantCulture) + " enemies/tick";
        }

        private static string BuildPacingLabel(WaveAuthoringState state)
        {
            int duration = GetApproximateDurationTicks(state);
            int total = GetTotalEnemyCount(state);
            if (total <= 0)
                return "Empty";
            if (duration <= 20)
                return "Early burst";
            if (duration <= 60)
                return "Mid pressure";
            return "Long wave";
        }

        private static int GetMaxScalingTier(WaveAuthoringState state)
        {
            int max = 0;
            for (int i = 0; i < state.Entries.Count; i++)
                max = Math.Max(max, state.Entries[i].ScalingTier);
            return max;
        }

        private static int GetFirstSpawnTick(WaveAuthoringState state)
        {
            if (state == null || state.Entries.Count == 0)
                return Math.Max(0, state == null ? 0 : state.StartTick);

            int first = int.MaxValue;
            for (int i = 0; i < state.Entries.Count; i++)
                first = Math.Min(first, state.StartTick + Math.Max(0, state.Entries[i].InitialDelayTicks));
            return first == int.MaxValue ? Math.Max(0, state.StartTick) : Math.Max(0, first);
        }

        private static int GetLastSpawnTick(WaveAuthoringState state)
        {
            return Math.Max(GetFirstSpawnTick(state), state.StartTick + GetApproximateDurationTicks(state));
        }

        private static UnityEngine.Object GetPrimaryPreviewAsset(WaveAuthoringState state)
        {
            if (state == null)
                return null;

            state.EnsureEntries();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                UnityEngine.Object asset = GetEnemyPreviewAsset(state.Entries[i].Enemy);
                if (asset != null)
                    return asset;
            }

            for (int i = 0; i < state.Entries.Count; i++)
            {
                if (state.Entries[i].Enemy != null)
                    return state.Entries[i].Enemy;
            }

            return null;
        }

        private static UnityEngine.Object GetEnemyPreviewAsset(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return null;
            if (enemy.Presentation != null && enemy.Presentation.Prefab != null)
                return enemy.Presentation.Prefab;
            return enemy.Icon != null ? enemy.Icon : enemy;
        }

        private static string GetEnemyLabel(EnemyDefinitionAsset enemy)
        {
            if (enemy == null)
                return "Missing enemy";
            return string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.Id : enemy.DisplayName;
        }

        private static string BuildAdvancedReport(GameContentLibraryItem item, WaveAuthoringState state, WaveDefinitionAsset asset)
        {
            return "Wave: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.WaveId + Environment.NewLine
                + "Path: " + (item == null ? "(draft)" : item.Path) + Environment.NewLine
                + "Schedule: " + (asset == null || asset.Schedule == null ? "(missing)" : AssetDatabase.GetAssetPath(asset.Schedule)) + Environment.NewLine
                + "Entries: " + (asset == null || asset.Entries == null ? "(missing)" : AssetDatabase.GetAssetPath(asset.Entries)) + Environment.NewLine
                + "Channels: " + BuildChannelSummary(state) + Environment.NewLine
                + "Enemy Mix: " + BuildEnemyMixSummary(state);
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
                        fontSize = 14
                    };
                    headerStyle.normal.textColor = DeucarianEditorTheme.Text;
                }

                return headerStyle;
            }
        }
    }

    internal static class WaveProviderV2PreviewModel
    {
        public const bool ExposesRedundantSelectButton = false;

        public static string GetScopeLabel(bool creating, bool unsaved)
        {
            if (creating)
                return "Draft";
            return unsaved ? "Unsaved" : "Selected";
        }

        public static IReadOnlyList<DeucarianEditorStatusChip> BuildChips(WaveAuthoringState state, WaveProviderV2State previewState)
        {
            if (state == null)
                return Array.Empty<DeucarianEditorStatusChip>();

            bool debug = previewState != null && previewState.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug;
            float speed = previewState == null ? 1f : previewState.PreviewSpeed;
            return new[]
            {
                new DeucarianEditorStatusChip(debug ? "Debug" : "Game", debug ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(WaveProviderV2View.GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture) + " enemies", WaveProviderV2View.GetTotalEnemyCount(state) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(WaveProviderV2View.GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(speed.ToString("0.#", CultureInfo.InvariantCulture) + "x", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(WaveProviderV2View.BuildChannelSummary(state) == "None" ? "NoChannel" : "Channels", WaveProviderV2View.BuildChannelSummary(state) == "None" ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success)
            };
        }
    }

    internal sealed class WaveProviderV2ListItem
    {
        private WaveProviderV2ListItem(GameContentLibraryItem source, WaveDefinitionAsset asset)
        {
            Source = source;
            Asset = asset;
            StableId = asset == null ? source == null ? string.Empty : source.Id : asset.Id;
            DisplayName = asset == null ? source == null ? "Wave" : source.DisplayName : asset.DisplayName;
            Tags = asset == null ? string.Empty : string.Join(", ", asset.Tags);
            WaveAuthoringState state = asset == null ? new WaveAuthoringState() : AttackGameContentPreviewSelection.FromWaveAsset(asset);
            TotalEnemyCount = WaveProviderV2View.GetTotalEnemyCount(state);
            EnemyCountLabel = TotalEnemyCount.ToString(CultureInfo.InvariantCulture) + " enemies";
            DurationTicks = WaveProviderV2View.GetApproximateDurationTicks(state);
            DurationLabel = DurationTicks.ToString(CultureInfo.InvariantCulture) + " ticks";
            EnemyMix = WaveProviderV2View.BuildEnemyMixSummary(state);
            ChannelTooltip = WaveProviderV2View.BuildChannelSummary(state);
            HasChannels = !string.Equals(ChannelTooltip, "None", StringComparison.Ordinal);
            ChannelLabel = HasChannels ? "Channels" : "NoChannel";
            UsageCount = source == null ? 0 : source.ReverseReferences.Count;
            UsageLabel = UsageCount.ToString(CultureInfo.InvariantCulture) + " use";
            ReadinessLabel = source == null ? "Ready" : source.ValidationLabel;
            ReadinessStatus = source != null && source.ErrorCount > 0 ? DeucarianEditorStatus.Error : source != null && source.WarningCount > 0 ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Success;
        }

        public GameContentLibraryItem Source { get; }
        public WaveDefinitionAsset Asset { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public string Tags { get; }
        public int TotalEnemyCount { get; }
        public string EnemyCountLabel { get; }
        public int DurationTicks { get; }
        public string DurationLabel { get; }
        public string EnemyMix { get; }
        public bool HasChannels { get; }
        public string ChannelLabel { get; }
        public string ChannelTooltip { get; }
        public int UsageCount { get; }
        public string UsageLabel { get; }
        public string ReadinessLabel { get; }
        public DeucarianEditorStatus ReadinessStatus { get; }

        public static IReadOnlyList<WaveProviderV2ListItem> Build(IReadOnlyList<GameContentLibraryItem> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<WaveProviderV2ListItem>();

            var result = new List<WaveProviderV2ListItem>();
            for (int i = 0; i < items.Count; i++)
            {
                WaveProviderV2ListItem item = FromItem(items[i]);
                if (item != null)
                    result.Add(item);
            }

            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static WaveProviderV2ListItem FromItem(GameContentLibraryItem item)
        {
            if (item == null || item.Kind != GameContentLibraryKind.Wave)
                return null;

            return new WaveProviderV2ListItem(item, item.Asset as WaveDefinitionAsset);
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string value = query.Trim();
            return Contains(DisplayName, value)
                || Contains(StableId, value)
                || Contains(Tags, value)
                || Contains(EnemyMix, value)
                || Contains(ChannelTooltip, value)
                || Contains(DurationLabel, value);
        }

        private static bool Contains(string source, string value)
        {
            return source != null && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
