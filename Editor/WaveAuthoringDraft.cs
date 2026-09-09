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
    internal static class WaveAuthoringDraft
    {
        public static string BuildStateFingerprint(WaveAuthoringState state)
        {
            if (state == null)
                return string.Empty;

            state.EnsureEntries();
            var builder = new StringBuilder()
                .Append(state.WaveId).Append('|')
                .Append(state.DisplayName).Append('|')
                .Append(state.TagsCsv).Append('|')
                .Append(state.OutputRoot).Append('|')
                .Append(state.StartTick.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                builder.Append('|').Append(entry.EntryId ?? string.Empty)
                    .Append(':').Append(entry.Enemy == null ? string.Empty : entry.Enemy.Id)
                    .Append(':').Append(entry.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.BatchSize.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.InitialDelayTicks.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.IntervalTicks.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(entry.SpawnChannelId ?? string.Empty)
                    .Append(':').Append(entry.ScalingTier.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}
