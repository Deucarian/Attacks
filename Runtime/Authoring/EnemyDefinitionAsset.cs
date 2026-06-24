using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    [CreateAssetMenu(menuName = "Deucarian/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "enemy.example.basic";
        [SerializeField] private string _displayName = "Example Enemy";
        [SerializeField] private Sprite _icon;
        [SerializeField] private EnemyRole _role = EnemyRole.Basic;
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private EnemyStatsDefinitionAsset _stats;
        [SerializeField] private EnemyPresentationDefinitionAsset _presentation;
        [SerializeField] private string _balancingNotes = string.Empty;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public Sprite Icon => _icon;
        public EnemyRole Role => _role;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public EnemyStatsDefinitionAsset Stats => _stats;
        public EnemyPresentationDefinitionAsset Presentation => _presentation;
        public string BalancingNotes => _balancingNotes ?? string.Empty;

        public void Configure(
            string id,
            string displayName,
            Sprite icon,
            EnemyRole role,
            IReadOnlyList<string> tags,
            EnemyStatsDefinitionAsset stats,
            EnemyPresentationDefinitionAsset presentation,
            string balancingNotes = "")
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _icon = icon;
            _role = role;
            _tags = CopyTags(tags);
            _stats = stats;
            _presentation = presentation;
            _balancingNotes = balancingNotes ?? string.Empty;
        }

        public static EnemyDefinitionAsset CreateTransient(
            string id,
            string displayName,
            EnemyRole role,
            float maximumHealth,
            float moveSpeed,
            int rewardValue,
            float contactDamage,
            string damageTypeId,
            float collisionRadius = 0.3f,
            GameObject prefab = null,
            IReadOnlyList<string> tags = null)
        {
            var stats = CreateInstance<EnemyStatsDefinitionAsset>();
            stats.hideFlags = HideFlags.HideAndDontSave;
            stats.Configure(maximumHealth, moveSpeed, rewardValue, contactDamage, damageTypeId, collisionRadius);

            var presentation = CreateInstance<EnemyPresentationDefinitionAsset>();
            presentation.hideFlags = HideFlags.HideAndDontSave;
            presentation.Configure(prefab, new[]
            {
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath)
            });

            var root = CreateInstance<EnemyDefinitionAsset>();
            root.hideFlags = HideFlags.HideAndDontSave;
            root.Configure(id, displayName, null, role, tags ?? Array.Empty<string>(), stats, presentation);
            return root;
        }

        private static string[] CopyTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0) return Array.Empty<string>();
            var copy = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) copy.Add(tag.Trim());
            }

            return copy.ToArray();
        }
    }
}
