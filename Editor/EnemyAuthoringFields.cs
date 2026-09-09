using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal static class EnemyAuthoringFields
    {
        internal static void DrawEditOverview(GameContentAuthoringSurfaceContext context, GameContentLibraryItem item, EnemyAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                state.EnemyId = context.Authoring.DrawTextField("Stable ID", state.EnemyId);
                state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
                state.Icon = context.Authoring.DrawObjectField("Icon", state.Icon);
                state.Role = context.Authoring.DrawEnumPopup("Role", state.Role);
                state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(EnemyAuthoringSummary.BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(EnemyAuthoringSummary.BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawStats(EnemyAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Health", EnemyAuthoringSummary.FormatFloat(state.MaximumHealth));
                DrawValue("Move Speed", EnemyAuthoringSummary.FormatFloat(state.MoveSpeed));
                DrawValue("Reward", state.RewardValue.ToString(CultureInfo.InvariantCulture));
                DrawValue("Contact Damage", EnemyAuthoringSummary.FormatFloat(state.ContactDamage) + " " + state.DamageTypeId);
                DrawValue("Collision Radius", EnemyAuthoringSummary.FormatFloat(state.CollisionRadius));
            });
        }

        internal static void DrawPresentation(EnemyAuthoringState state)
        {
            DeucarianEditorEventTimeline.Draw(EnemyAuthoringPreview.BuildTimelineEvents(state));
        }

        internal static void DrawBalance(EnemyAuthoringState state)
        {
            float contactPerSecond = state.ContactDamage * Mathf.Max(0.25f, state.MoveSpeed);
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Threat Estimate", EnemyAuthoringSummary.FormatFloat(contactPerSecond) + " contact pressure");
                DrawValue("Health / Reward", EnemyAuthoringSummary.FormatFloat(state.MaximumHealth) + " HP / " + state.RewardValue.ToString(CultureInfo.InvariantCulture));
                DrawValue("Role", EnemyAuthoringSummary.GetRoleLabel(state.Role));
            });
        }

        internal static void DrawReferences(GameContentLibraryItem item)
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

        internal static void DrawAdvanced(GameContentLibraryItem item, EnemyAuthoringState state)
        {
            DeucarianEditorDiagnosticsDrawer.Draw("enemy-v2-advanced-" + item.Key, "Raw Details", () =>
            {
                DrawValue("Asset Path", item.Path);
                DrawValue("Folder", item.Folder);
                DrawValue("Stable ID", state.EnemyId);
                DrawValue("Tags", state.TagsCsv);
                if (GUILayout.Button("Copy Report", DeucarianEditorButtons.SecondaryStyle, GUILayout.Height(24f)))
                    EditorGUIUtility.systemCopyBuffer = EnemyAuthoringSummary.BuildAdvancedReport(item, state);
            }, true);

            DeucarianEditorDiagnosticsDrawer.Draw("enemy-v2-references-" + item.Key, "Serialized References", () =>
            {
                GameContentAuthoringProviderGUI.DrawReferenceList("Direct", item.DirectReferences);
                GameContentAuthoringProviderGUI.DrawReferenceList("Referenced By", item.ReverseReferences);
            });
        }

        internal static void DrawWizardIdentity(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
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

        internal static void DrawWizardStats(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
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

        internal static void DrawWizardPresentation(GameContentAuthoringSurfaceContext context, EnemyAuthoringState draft)
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

        private static void DrawValue(string label, string value)
        {
            DeucarianEditorFieldRow.Draw(label, () => EditorGUILayout.LabelField(value ?? string.Empty, DeucarianEditorStyles.MutedLabel));
        }

        private static readonly Lazy<GUIStyle> headerStyle = CreateHeaderStyleCache(() => EditorStyles.boldLabel);

        internal static GUIStyle HeaderStyle => headerStyle.Value;

        internal static Lazy<GUIStyle> CreateHeaderStyleCache(Func<GUIStyle> source)
        {
            return new Lazy<GUIStyle>(() =>
            {
                var style = new GUIStyle(source())
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true
                };
                style.normal.textColor = DeucarianEditorTheme.Text;
                return style;
            }, LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
