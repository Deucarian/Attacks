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
    public sealed class EnemyStatsDefinitionSpec
    {
        [DefinitionField("_maximumHealth")] public float MaximumHealth = 8f;
        [DefinitionField("_moveSpeed")] public float MoveSpeed = 2.2f;
        [DefinitionField("_rewardValue")] public int RewardValue = 1;
        [DefinitionField("_contactDamage")] public float ContactDamage = 3f;
        [DefinitionField("_damageTypeId")] public string DamageTypeId = "damage.template.basic";
        [DefinitionField("_collisionRadius")] public float CollisionRadius = 0.3f;
    }
}
