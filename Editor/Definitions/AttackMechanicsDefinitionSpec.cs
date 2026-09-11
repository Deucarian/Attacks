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
    public sealed class AttackMechanicsDefinitionSpec
    {
        [DefinitionField("_cooldownTicks")] public int CooldownTicks = 30;
        [DefinitionField("_range")] public float Range = 6f;
        [DefinitionField("_damageAmount")] public float DamageAmount = 8f;
        [DefinitionField("_damageTypeId")] public string DamageTypeId = "damage.physical";
    }
}
