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
    internal static class AttackAuthoringLibrary
    {
        internal static void DrawAttackList(
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
                    state.BeginCreate();
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
                new DeucarianEditorStatusChip(AttackAuthoringSummary.GetCompactTypeLabel(item.TypeLabel), DeucarianEditorStatus.Info, item.TypeLabel),
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
    }
}
