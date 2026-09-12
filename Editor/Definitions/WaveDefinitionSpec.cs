using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor.Definitions
{
    [Serializable]
    public sealed class WaveDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("_tags")] public string[] Tags = Array.Empty<string>();
        [DefinitionSection("_schedule", typeof(WaveScheduleDefinitionAsset))] public WaveScheduleDefinitionSpec Schedule = new WaveScheduleDefinitionSpec();
        [DefinitionSection("_entries", typeof(WaveEntriesDefinitionAsset))] public WaveEntriesDefinitionSpec Entries = new WaveEntriesDefinitionSpec();
        [DefinitionField("_balancingNotes")] public string BalancingNotes = string.Empty;
    }
}
