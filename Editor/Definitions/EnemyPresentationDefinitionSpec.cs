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
    public sealed class EnemyPresentationDefinitionSpec
    {
        [DefinitionField("_prefab")] public GameObject Prefab;
        [DefinitionField("_events")] public EnemyPresentationEventRecipe[] Events = {
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn),
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit),
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath)
        };
    }
}
