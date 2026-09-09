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
    internal static class WaveAuthoringEntryCommands
    {
        internal static void MoveEntry(WaveAuthoringState state, int from, int to)
        {
            if (state == null || from < 0 || to < 0 || from >= state.Entries.Count || to >= state.Entries.Count)
                return;

            WaveEntryAuthoringState entry = state.Entries[from];
            state.Entries.RemoveAt(from);
            state.Entries.Insert(to, entry);
        }

        internal static WaveEntryAuthoringState CopyEntry(WaveEntryAuthoringState entry)
        {
            return new WaveEntryAuthoringState
            {
                EntryId = WaveEntryId.CreateNew().Value,
                Enemy = entry.Enemy,
                Count = entry.Count,
                BatchSize = entry.BatchSize,
                InitialDelayTicks = entry.InitialDelayTicks,
                IntervalTicks = entry.IntervalTicks,
                SpawnChannelId = entry.SpawnChannelId,
                ScalingTier = entry.ScalingTier
            };
        }
    }
}
