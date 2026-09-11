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
    public sealed class AttackStatusEffectsDefinitionSpec
    {
        [DefinitionField("_statusEffects")] public AttackStatusEffectSpec[] StatusEffects = Array.Empty<AttackStatusEffectSpec>();
    }
}
