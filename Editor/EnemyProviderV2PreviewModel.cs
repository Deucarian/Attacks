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
}
