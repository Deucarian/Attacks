using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class WaveEntriesDefinitionAsset : ScriptableObject
    {
        [SerializeField] private WaveEntryRecipe[] _entries = Array.Empty<WaveEntryRecipe>();

        public IReadOnlyList<WaveEntryRecipe> Entries => _entries ?? Array.Empty<WaveEntryRecipe>();

        public void Configure(IReadOnlyList<WaveEntryRecipe> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<WaveEntryRecipe>();
                return;
            }

            _entries = new WaveEntryRecipe[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                _entries[i] = entry == null
                    ? null
                    : new WaveEntryRecipe(entry.EntryId.Value, entry.Enemy, entry.Count, entry.BatchSize, entry.InitialDelayTicks, entry.IntervalTicks, entry.SpawnChannelId, entry.ScalingTier);
            }
        }
    }
}
