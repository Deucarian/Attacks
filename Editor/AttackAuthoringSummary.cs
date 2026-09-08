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
    internal static class AttackAuthoringSummary
    {
        internal static string GetCompactTypeLabel(string typeLabel)
        {
            if (string.Equals(typeLabel, "Projectile", StringComparison.OrdinalIgnoreCase))
                return "Proj";
            return typeLabel ?? "Attack";
        }

        internal static string BuildHumanSummary(AttackAuthoringState state)
        {
            return FormatFloat(state.DamageAmount) + " " + state.DamageTypeId
                + ", " + state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " tick cooldown"
                + ", " + GetTypeLabel(state).ToLowerInvariant();
        }

        internal static string BuildUsedBySummary(GameContentLibraryItem item)
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

        internal static string BuildAdvancedReport(GameContentLibraryItem item, AttackAuthoringState state)
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

        internal static string GetTypeLabel(AttackAuthoringState state)
        {
            if (state == null) return "Attack";
            if (state.IncludeStatusEffect && state.DeliveryMode == AttackRecipeDeliveryMode.Aura) return "Status";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile) return state.Homing ? "Homing" : "Projectile";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan) return "Beam";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Area) return "AOE";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Aura) return "Status";
            return state.DeliveryMode.ToString();
        }

        internal static bool HasAnyVisual(AttackAuthoringState state)
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

        internal static bool HasAnyAudio(AttackAuthoringState state)
        {
            return state != null && (state.CastAudio != null
                || state.FireAudio != null
                || state.ImpactAudio != null
                || state.TickAudio != null
                || state.ExpireAudio != null);
        }

        internal static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
