using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class EnemyStatsDefinitionAsset : ScriptableObject
    {
        [SerializeField] private float _maximumHealth = 8f;
        [SerializeField] private float _moveSpeed = 2.2f;
        [SerializeField] private int _rewardValue = 1;
        [SerializeField] private float _contactDamage = 3f;
        [SerializeField] private string _damageTypeId = "damage.template.basic";
        [SerializeField] private float _collisionRadius = 0.3f;

        public float MaximumHealth => _maximumHealth;
        public float MoveSpeed => _moveSpeed;
        public int RewardValue => _rewardValue;
        public float ContactDamage => _contactDamage;
        public string DamageTypeId => _damageTypeId ?? string.Empty;
        public float CollisionRadius => _collisionRadius;

        public void Configure(float maximumHealth, float moveSpeed, int rewardValue, float contactDamage, string damageTypeId, float collisionRadius)
        {
            _maximumHealth = maximumHealth;
            _moveSpeed = moveSpeed;
            _rewardValue = rewardValue;
            _contactDamage = contactDamage;
            _damageTypeId = damageTypeId ?? string.Empty;
            _collisionRadius = collisionRadius;
        }
    }
}
