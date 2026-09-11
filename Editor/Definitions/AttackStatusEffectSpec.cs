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
    public sealed class AttackStatusEffectSpec
    {
        [DefinitionField("_statusId")] public string StatusId = "status.example.slow";
        [DefinitionField("_durationTicks")] public int DurationTicks = 60;
        [DefinitionField("_tickRateTicks")] public int TickRateTicks;
        [DefinitionField("_strength")] public float Strength = 1f;
        [DefinitionField("_maxStacks")] public int MaxStacks = 1;
        [DefinitionField("_stackingPolicy")] public StatusStackingPolicy StackingPolicy = StatusStackingPolicy.UniqueRefresh;
        [DefinitionField("_modifierStatId")] public string ModifierStatId = string.Empty;
        [DefinitionField("_effectNote")] public string EffectNote = string.Empty;
    }
}
