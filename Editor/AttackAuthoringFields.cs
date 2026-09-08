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
    internal static class AttackAuthoringFields
    {
        internal static void DrawEditOverview(GameContentAuthoringSurfaceContext context, GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                state.AttackId = context.Authoring.DrawTextField("Stable ID", state.AttackId);
                state.DisplayName = context.Authoring.DrawTextField("Display Name", state.DisplayName);
                state.Icon = context.Authoring.DrawObjectField("Icon", state.Icon);
                state.TagsCsv = context.Authoring.DrawTextField("Tags", state.TagsCsv);
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(AttackAuthoringSummary.BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(AttackAuthoringSummary.BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawOverview(GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DeucarianEditorFieldRow.Draw("Display Name", () => EditorGUILayout.LabelField(state.DisplayName));
                DeucarianEditorFieldRow.Draw("ID", () => EditorGUILayout.LabelField(state.AttackId));
                DeucarianEditorFieldRow.Draw("Type", () => EditorGUILayout.LabelField(AttackAuthoringSummary.GetTypeLabel(state)));
                DeucarianEditorFieldRow.Draw("Summary", () => EditorGUILayout.LabelField(AttackAuthoringSummary.BuildHumanSummary(state), DeucarianEditorStyles.MutedLabel));
                DeucarianEditorFieldRow.Draw("Used By", () => EditorGUILayout.LabelField(AttackAuthoringSummary.BuildUsedBySummary(item), DeucarianEditorStyles.MutedLabel));
            });
        }

        private static void DrawBehavior(AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Damage", AttackAuthoringSummary.FormatFloat(state.DamageAmount) + " " + state.DamageTypeId);
                DrawValue("Cooldown", state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " ticks");
                DrawValue("Range", AttackAuthoringSummary.FormatFloat(state.Range));
                DrawValue("Targeting", state.TargetingMode.ToString());
                DrawValue("Status", state.IncludeStatusEffect ? state.StatusId : "None");
            });
        }

        private static void DrawDelivery(AttackAuthoringState state)
        {
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("Mode", AttackAuthoringSummary.GetTypeLabel(state));
                if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
                {
                    DrawValue("Projectile ID", state.ProjectileDefinitionId);
                    DrawValue("Spawnable ID", state.ProjectileSpawnableId);
                    DrawValue("Speed", AttackAuthoringSummary.FormatFloat(state.ProjectileSpeed));
                    DrawValue("Lifetime", state.ProjectileLifetimeTicks.ToString(CultureInfo.InvariantCulture) + " ticks");
                    DrawValue("Homing", state.Homing ? "Yes, " + AttackAuthoringSummary.FormatFloat(state.HomingTurnRate) + " deg/s" : "No");
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
                    DrawValue("Radius", AttackAuthoringSummary.FormatFloat(state.Radius));
                    DrawValue("Max Hits", state.MaxHits.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                DrawValue("Radius", AttackAuthoringSummary.FormatFloat(state.Radius));
                DrawValue("Tick Interval", AttackAuthoringSummary.FormatFloat(state.TickIntervalSeconds) + "s");
            });
        }

        internal static void DrawPresentation(AttackAuthoringState state, Action<AttackPresentationEventKind> previewEvent, AttackProviderV2State previewState)
        {
            DeucarianEditorEventTimeline.Draw(AttackAuthoringPreview.BuildTimelineEvents(state), index =>
            {
                if (previewEvent == null)
                    return;
                previewEvent(AttackAuthoringPreview.GetEventKind(index));
            });
        }

        private static void DrawBalance(AttackAuthoringState state)
        {
            float dps = state.CooldownTicks <= 0 ? state.DamageAmount * 30f : state.DamageAmount * 30f / Mathf.Max(1, state.CooldownTicks);
            DeucarianEditorCards.DrawInlineCard(() =>
            {
                DrawValue("DPS Estimate", AttackAuthoringSummary.FormatFloat(dps));
                DrawValue("Cooldown / Range", state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " ticks / " + AttackAuthoringSummary.FormatFloat(state.Range));
                DrawValue("Damage", AttackAuthoringSummary.FormatFloat(state.DamageAmount) + " " + state.DamageTypeId);
                DrawValue("Upgrade Hook", string.IsNullOrWhiteSpace(state.AttackId) ? "None" : state.AttackId);
            });
        }

        internal static void DrawReferences(GameContentLibraryItem item)
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

        internal static void DrawAdvanced(GameContentLibraryItem item, AttackAuthoringState state)
        {
            DeucarianEditorDiagnosticsDrawer.Draw("attack-v2-advanced-" + item.Key, "Raw Details", () =>
            {
                DrawValue("Asset Path", item.Path);
                DrawValue("Folder", item.Folder);
                DrawValue("Stable ID", state.AttackId);
                DrawValue("Tags", state.TagsCsv);
                if (GUILayout.Button("Copy Report", DeucarianEditorButtons.SecondaryStyle, GUILayout.Height(24f)))
                    EditorGUIUtility.systemCopyBuffer = AttackAuthoringSummary.BuildAdvancedReport(item, state);
            }, true);

            DeucarianEditorDiagnosticsDrawer.Draw("attack-v2-references-" + item.Key, "Serialized References", () =>
            {
                GameContentAuthoringProviderGUI.DrawReferenceList("Direct", item.DirectReferences);
                GameContentAuthoringProviderGUI.DrawReferenceList("Referenced By", item.ReverseReferences);
            });
        }

        internal static void DrawWizardIdentity(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
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

        internal static void DrawWizardBehavior(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
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

        internal static void DrawWizardDelivery(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
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

        internal static void DrawWizardPresentation(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
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

        internal static void DrawWizardBalance(GameContentAuthoringSurfaceContext context, AttackAuthoringState draft)
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

        private static void DrawValue(string label, string value)
        {
            DeucarianEditorFieldRow.Draw(label, () => EditorGUILayout.LabelField(value ?? string.Empty, DeucarianEditorStyles.MutedLabel));
        }

        private static GUIStyle headerStyle;

        internal static GUIStyle HeaderStyle
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
}
