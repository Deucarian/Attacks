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
    public sealed class AttackDeliveryDefinitionSpec
    {
        [DefinitionField("_mode")] public AttackRecipeDeliveryMode Mode = AttackRecipeDeliveryMode.Projectile;
        [DefinitionField("_projectileDefinitionId")] public string ProjectileDefinitionId = "projectile.example.basic";
        [DefinitionField("_projectileSpawnableId")] public string ProjectileSpawnableId = "projectile.example.basic";
        [DefinitionField("_projectilePrefab")] public GameObject ProjectilePrefab;
        [DefinitionField("_projectileSpeed")] public float ProjectileSpeed = 8f;
        [DefinitionField("_projectileLifetimeTicks")] public int ProjectileLifetimeTicks = 120;
        [DefinitionField("_homing")] public bool Homing;
        [DefinitionField("_homingTurnRate")] public float HomingTurnRate = 180f;
        [DefinitionField("_pierceCount")] public int PierceCount;
        [DefinitionField("_radius")] public float Radius = 1.5f;
        [DefinitionField("_beamVfxPrefab")] public GameObject BeamVfxPrefab;
        [DefinitionField("_impactVfxPrefab")] public GameObject ImpactVfxPrefab;
        [DefinitionField("_maxHits")] public int MaxHits = 1;
        [DefinitionField("_tickIntervalSeconds")] public float TickIntervalSeconds = 0.5f;
    }
}
