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
    internal static class AttackProviderV2PreviewModel
    {
        public const bool EventRowsExposePreviewActions = false;

        public static AttackProviderV2PreviewScope GetScope(bool creating, bool dirty)
        {
            if (creating)
                return AttackProviderV2PreviewScope.Draft;
            return dirty ? AttackProviderV2PreviewScope.UnsavedEdit : AttackProviderV2PreviewScope.Selected;
        }

        public static string GetScopeLabel(AttackProviderV2PreviewScope scope)
        {
            switch (scope)
            {
                case AttackProviderV2PreviewScope.Draft:
                    return "Draft";
                case AttackProviderV2PreviewScope.UnsavedEdit:
                    return "Unsaved";
                default:
                    return "Selected";
            }
        }

        public static string BuildHeaderTitle(AttackAuthoringState source, AttackProviderV2PreviewScope scope)
        {
            string sourceName = GetSourceName(source, scope);
            return string.IsNullOrWhiteSpace(sourceName)
                ? "Preview Lab"
                : "Preview Lab - " + sourceName;
        }

        public static string BuildViewportTitle(AttackAuthoringState source, AttackProviderV2PreviewScope scope)
        {
            return GetSourceName(source, scope);
        }

        public static IReadOnlyList<DeucarianEditorStatusChip> BuildChips(
            AttackAuthoringState source,
            AttackProviderV2State state,
            AttackProviderV2PreviewScope scope)
        {
            var chips = new List<DeucarianEditorStatusChip>
            {
                BuildScopeChip(scope),
                new DeucarianEditorStatusChip(HasAnyVisual(source) ? "VFX" : "No VFX", HasAnyVisual(source) ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Disabled),
                new DeucarianEditorStatusChip(state != null && state.PreviewMuted ? "Muted" : HasAnyAudio(source) ? "Audio" : "No audio", state == null || state.PreviewMuted || !HasAnyAudio(source) ? DeucarianEditorStatus.Disabled : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state != null && state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? "Debug" : "Game", state != null && state.PreviewRenderMode == GameContentAuthoringActionPreviewRenderMode.Debug ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Success),
                new DeucarianEditorStatusChip(state != null && state.PreviewLoop ? "Loop" : "Once", state != null && state.PreviewLoop ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(FormatFloat(state == null ? 1f : state.PreviewSpeed) + "x", DeucarianEditorStatus.Info)
            };

            return chips;
        }

        private static DeucarianEditorStatusChip BuildScopeChip(AttackProviderV2PreviewScope scope)
        {
            switch (scope)
            {
                case AttackProviderV2PreviewScope.Draft:
                    return new DeucarianEditorStatusChip("Draft Preview", DeucarianEditorStatus.Info);
                case AttackProviderV2PreviewScope.UnsavedEdit:
                    return new DeucarianEditorStatusChip("Unsaved Preview", DeucarianEditorStatus.Warning);
                default:
                    return new DeucarianEditorStatusChip("Selected Preview", DeucarianEditorStatus.Success);
            }
        }

        private static string GetSourceName(AttackAuthoringState source, AttackProviderV2PreviewScope scope)
        {
            if (source != null)
            {
                if (scope == AttackProviderV2PreviewScope.Draft && IsDefaultExampleDraft(source))
                    return "New Attack Draft";
                if (!string.IsNullOrWhiteSpace(source.DisplayName))
                    return source.DisplayName.Trim();
                if (!string.IsNullOrWhiteSpace(source.AttackId))
                    return source.AttackId.Trim();
            }

            return scope == AttackProviderV2PreviewScope.Draft ? "New Attack Draft" : "Attack View";
        }

        private static bool IsDefaultExampleDraft(AttackAuthoringState source)
        {
            return source != null
                && string.Equals(source.DisplayName, "Fire Orb", StringComparison.Ordinal)
                && string.Equals(source.AttackId, "attack.example.fire-orb", StringComparison.Ordinal);
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
    }
}
