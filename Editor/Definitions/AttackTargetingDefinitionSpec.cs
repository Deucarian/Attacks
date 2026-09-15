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
    public sealed class AttackTargetingDefinitionSpec
    {
        [DefinitionField("_mode")] public AttackRecipeTargetingMode Mode = AttackRecipeTargetingMode.Nearest;
        [DefinitionField("_maxTargets")] public int MaxTargets = 1;
        [DefinitionField("_requiresLineOfSight")] public bool RequiresLineOfSight;
    }
}
