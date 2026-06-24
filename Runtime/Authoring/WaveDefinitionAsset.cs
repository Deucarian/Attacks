using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    [CreateAssetMenu(menuName = "Deucarian/Waves/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "wave.example.basic";
        [SerializeField] private string _displayName = "Example Wave";
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private WaveScheduleDefinitionAsset _schedule;
        [SerializeField] private WaveEntriesDefinitionAsset _entries;
        [SerializeField] private string _balancingNotes = string.Empty;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public WaveScheduleDefinitionAsset Schedule => _schedule;
        public WaveEntriesDefinitionAsset Entries => _entries;
        public string BalancingNotes => _balancingNotes ?? string.Empty;

        public void Configure(
            string id,
            string displayName,
            IReadOnlyList<string> tags,
            WaveScheduleDefinitionAsset schedule,
            WaveEntriesDefinitionAsset entries,
            string balancingNotes = "")
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _tags = CopyTags(tags);
            _schedule = schedule;
            _entries = entries;
            _balancingNotes = balancingNotes ?? string.Empty;
        }

        public static WaveDefinitionAsset CreateTransient(string id, string displayName, int startTick, IReadOnlyList<WaveEntryRecipe> entries, IReadOnlyList<string> tags = null)
        {
            var schedule = CreateInstance<WaveScheduleDefinitionAsset>();
            schedule.hideFlags = HideFlags.HideAndDontSave;
            schedule.Configure(startTick);

            var entrySection = CreateInstance<WaveEntriesDefinitionAsset>();
            entrySection.hideFlags = HideFlags.HideAndDontSave;
            entrySection.Configure(entries);

            var root = CreateInstance<WaveDefinitionAsset>();
            root.hideFlags = HideFlags.HideAndDontSave;
            root.Configure(id, displayName, tags ?? Array.Empty<string>(), schedule, entrySection);
            return root;
        }

        private static string[] CopyTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0) return Array.Empty<string>();
            var copy = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) copy.Add(tag.Trim());
            }

            return copy.ToArray();
        }
    }
}
