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
                new DeucarianEditorStatusChip(WaveAuthoringSummary.GetTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture) + " enemies", WaveAuthoringSummary.GetTotalEnemyCount(state) > 0 ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Error),
                new DeucarianEditorStatusChip(WaveAuthoringSummary.GetApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " ticks", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(speed.ToString("0.#", CultureInfo.InvariantCulture) + "x", DeucarianEditorStatus.Info),
                new DeucarianEditorStatusChip(WaveAuthoringSummary.BuildChannelSummary(state) == "None" ? "NoChannel" : "Channels", WaveAuthoringSummary.BuildChannelSummary(state) == "None" ? DeucarianEditorStatus.Error : DeucarianEditorStatus.Success)
            };
        }
    }
}
