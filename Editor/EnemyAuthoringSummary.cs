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
    internal static class EnemyAuthoringSummary
    {
        internal static string BuildHumanSummary(EnemyAuthoringState state)
        {
            return FormatFloat(state.MaximumHealth) + " HP, "
                + FormatFloat(state.MoveSpeed) + " speed, "
                + FormatFloat(state.ContactDamage) + " " + state.DamageTypeId
                + ", " + GetRoleLabel(state.Role).ToLowerInvariant();
        }

        internal static string BuildUsedBySummary(GameContentLibraryItem item)
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

        internal static string BuildAdvancedReport(GameContentLibraryItem item, EnemyAuthoringState state)
        {
            return "Enemy: " + state.DisplayName + Environment.NewLine
                + "ID: " + state.EnemyId + Environment.NewLine
                + "Path: " + item.Path + Environment.NewLine
                + "Role: " + GetRoleLabel(state.Role) + Environment.NewLine
                + "Stats: " + FormatFloat(state.MaximumHealth) + " HP, " + FormatFloat(state.MoveSpeed) + " speed";
        }

        internal static string GetRoleLabel(EnemyRole role)
        {
            return Enum.IsDefined(typeof(EnemyRole), role) ? role.ToString() : "Custom";
        }

        internal static bool HasAnyVisual(EnemyAuthoringState state)
        {
            return state != null && (state.Prefab != null || state.SpawnVfxPrefab != null || state.HitVfxPrefab != null || state.DeathVfxPrefab != null);
        }

        internal static bool HasAnyAudio(EnemyAuthoringState state)
        {
            return state != null && (state.SpawnAudio != null || state.HitAudio != null || state.DeathAudio != null);
        }

        internal static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        internal static string GetObjectLabel(UnityEngine.Object asset, string fallback)
        {
            return asset == null ? fallback ?? string.Empty : asset.name;
        }

        internal static Color GetRoleAccent(EnemyRole role)
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
    }
}
