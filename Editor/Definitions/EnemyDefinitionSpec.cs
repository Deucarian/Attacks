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
    public sealed class EnemyDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("_icon")] public Sprite Icon;
        [DefinitionField("_role")] public EnemyRole Role = EnemyRole.Basic;
        [DefinitionField("_tags")] public string[] Tags = Array.Empty<string>();
        [DefinitionSection("_stats", typeof(EnemyStatsDefinitionAsset))] public EnemyStatsDefinitionSpec Stats = new EnemyStatsDefinitionSpec();
        [DefinitionSection("_presentation", typeof(EnemyPresentationDefinitionAsset))] public EnemyPresentationDefinitionSpec Presentation = new EnemyPresentationDefinitionSpec();
        [DefinitionField("_balancingNotes")] public string BalancingNotes = string.Empty;
    }
}
