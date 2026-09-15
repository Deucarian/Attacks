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
    public sealed class WaveEntrySpec
    {
        [DefinitionField("_entryId")] public string EntryId = string.Empty;
        [DefinitionField("_enemy")] public EnemyDefinitionAsset Enemy;
        [DefinitionField("_count")] public int Count = 4;
        [DefinitionField("_batchSize")] public int BatchSize = 1;
        [DefinitionField("_initialDelayTicks")] public int InitialDelayTicks;
        [DefinitionField("_intervalTicks")] public int IntervalTicks = 12;
        [DefinitionField("_spawnChannelId")] public string SpawnChannelId = "perimeter-north";
        [DefinitionField("_scalingTier")] public int ScalingTier;
    }
}
