using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public enum EnemyRole
    {
        Basic = 0,
        Fast = 1,
        Tank = 2,
        Swarm = 3,
        Boss = 4
    }

    public enum EnemyPresentationEventKind
    {
        OnSpawn = 0,
        OnHit = 1,
        OnDeath = 2
    }

    [Serializable]
    public sealed class EnemyPresentationEventRecipe
    {
        [SerializeField] private EnemyPresentationEventKind _eventKind;
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private GameObject _vfxPrefab;

        public EnemyPresentationEventRecipe()
        {
        }

        public EnemyPresentationEventRecipe(EnemyPresentationEventKind eventKind, AudioClip audioClip = null, GameObject vfxPrefab = null)
        {
            _eventKind = eventKind;
            _audioClip = audioClip;
            _vfxPrefab = vfxPrefab;
        }

        public EnemyPresentationEventKind EventKind => _eventKind;
        public AudioClip AudioClip => _audioClip;
        public GameObject VfxPrefab => _vfxPrefab;
        public bool HasAnyAsset => _audioClip != null || _vfxPrefab != null;
    }

    public readonly struct EnemyDefinitionValidationOptions
    {
        public EnemyDefinitionValidationOptions(bool requirePrefab)
        {
            RequirePrefab = requirePrefab;
        }

        public bool RequirePrefab { get; }
        public static EnemyDefinitionValidationOptions RuntimeFriendly => new EnemyDefinitionValidationOptions(false);
        public static EnemyDefinitionValidationOptions AssetCreation => new EnemyDefinitionValidationOptions(true);
    }

    public static class EnemyDefinitionValidator
    {
        public static ContentAuthoringValidationReport Validate(EnemyDefinitionAsset recipe)
        {
            return Validate(recipe, EnemyDefinitionValidationOptions.RuntimeFriendly);
        }

        public static ContentAuthoringValidationReport Validate(EnemyDefinitionAsset recipe, EnemyDefinitionValidationOptions options)
        {
            var issues = new List<ContentAuthoringValidationIssue>();
            if (recipe == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Enemy", "Enemy definition is missing."));
                return new ContentAuthoringValidationReport(issues);
            }

            if (string.IsNullOrWhiteSpace(recipe.Id)) issues.Add(ContentAuthoringValidationIssue.Error("Enemy.Id", "Enemy ID is required."));
            if (string.IsNullOrWhiteSpace(recipe.DisplayName)) issues.Add(ContentAuthoringValidationIssue.Warning("Enemy.DisplayName", "Display name is empty."));
            ValidateStats(recipe.Stats, issues);
            ValidatePresentation(recipe.Presentation, options, issues);
            return new ContentAuthoringValidationReport(issues);
        }

        private static void ValidateStats(EnemyStatsDefinitionAsset stats, List<ContentAuthoringValidationIssue> issues)
        {
            if (stats == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Stats", "Stats section is required."));
                return;
            }

            if (stats.MaximumHealth <= 0f || float.IsNaN(stats.MaximumHealth) || float.IsInfinity(stats.MaximumHealth)) issues.Add(ContentAuthoringValidationIssue.Error("Stats.MaximumHealth", "Maximum health must be greater than zero."));
            if (stats.MoveSpeed <= 0f || float.IsNaN(stats.MoveSpeed) || float.IsInfinity(stats.MoveSpeed)) issues.Add(ContentAuthoringValidationIssue.Error("Stats.MoveSpeed", "Move speed must be greater than zero."));
            if (stats.RewardValue < 0) issues.Add(ContentAuthoringValidationIssue.Error("Stats.RewardValue", "Reward value cannot be negative."));
            if (stats.ContactDamage < 0f || float.IsNaN(stats.ContactDamage) || float.IsInfinity(stats.ContactDamage)) issues.Add(ContentAuthoringValidationIssue.Error("Stats.ContactDamage", "Contact damage must be finite and non-negative."));
            if (string.IsNullOrWhiteSpace(stats.DamageTypeId)) issues.Add(ContentAuthoringValidationIssue.Error("Stats.DamageTypeId", "Damage type ID is required."));
            if (stats.CollisionRadius <= 0f || float.IsNaN(stats.CollisionRadius) || float.IsInfinity(stats.CollisionRadius)) issues.Add(ContentAuthoringValidationIssue.Error("Stats.CollisionRadius", "Collision radius must be greater than zero."));
        }

        private static void ValidatePresentation(EnemyPresentationDefinitionAsset presentation, EnemyDefinitionValidationOptions options, List<ContentAuthoringValidationIssue> issues)
        {
            if (presentation == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Presentation", "Presentation section is required."));
                return;
            }

            if (options.RequirePrefab && presentation.Prefab == null)
                issues.Add(ContentAuthoringValidationIssue.Error("Presentation.Prefab", "Choose an enemy prefab or model reference before creating this asset."));
        }
    }
}
