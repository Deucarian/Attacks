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
            TotalEnemyCount = WaveAuthoringSummary.GetTotalEnemyCount(state);
            EnemyCountLabel = TotalEnemyCount.ToString(CultureInfo.InvariantCulture) + " enemies";
            DurationTicks = WaveAuthoringSummary.GetApproximateDurationTicks(state);
            DurationLabel = DurationTicks.ToString(CultureInfo.InvariantCulture) + " ticks";
            EnemyMix = WaveAuthoringSummary.BuildEnemyMixSummary(state);
            ChannelTooltip = WaveAuthoringSummary.BuildChannelSummary(state);
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
