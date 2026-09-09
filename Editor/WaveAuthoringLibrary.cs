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
    internal static class WaveAuthoringLibrary
    {
        internal static void DrawWaveList(GameContentAuthoringSurfaceContext context, WaveProviderV2State state, IReadOnlyList<WaveProviderV2ListItem> items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DeucarianEditorTextGUI.LabelField("Waves", DeucarianEditorStyles.SectionTitle);
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
                DeucarianEditorTextGUI.LabelField(items.Count == 0 ? "No authored waves found." : "No waves match the current search.", DeucarianEditorStyles.MutedLabel);
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
    }
}
