using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class AttackDeliveryDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttackRecipeDeliveryMode _mode = AttackRecipeDeliveryMode.Projectile;
        [SerializeField] private string _projectileDefinitionId = "projectile.example.basic";
        [SerializeField] private string _projectileSpawnableId = "projectile.example.basic";
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 8f;
        [SerializeField] private int _projectileLifetimeTicks = 120;
        [SerializeField] private bool _homing;
        [SerializeField] private float _homingTurnRate = 180f;
        [SerializeField] private int _pierceCount;
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private GameObject _beamVfxPrefab;
        [SerializeField] private GameObject _impactVfxPrefab;
        [SerializeField] private int _maxHits = 1;
        [SerializeField] private float _tickIntervalSeconds = 0.5f;

        public AttackRecipeDeliveryMode Mode => _mode;
        public string ProjectileDefinitionId => _projectileDefinitionId ?? string.Empty;
        public string ProjectileSpawnableId => _projectileSpawnableId ?? string.Empty;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public int ProjectileLifetimeTicks => _projectileLifetimeTicks;
        public bool Homing => _homing;
        public float HomingTurnRate => _homingTurnRate;
        public int PierceCount => _pierceCount;
        public int MaxImpacts => Mathf.Max(1, _pierceCount + 1);
        public float Radius => _radius;
        public GameObject BeamVfxPrefab => _beamVfxPrefab;
        public GameObject ImpactVfxPrefab => _impactVfxPrefab;
        public int MaxHits => _maxHits;
        public float TickIntervalSeconds => _tickIntervalSeconds;

        public void ConfigureProjectile(
            string projectileDefinitionId,
            string projectileSpawnableId,
            GameObject projectilePrefab,
            float speed,
            int lifetimeTicks,
            bool homing = false,
            float homingTurnRate = 180f,
            int pierceCount = 0,
            float radius = 0f)
        {
            _mode = AttackRecipeDeliveryMode.Projectile;
            _projectileDefinitionId = projectileDefinitionId ?? string.Empty;
            _projectileSpawnableId = projectileSpawnableId ?? string.Empty;
            _projectilePrefab = projectilePrefab;
            _projectileSpeed = speed;
            _projectileLifetimeTicks = lifetimeTicks;
            _homing = homing;
            _homingTurnRate = homingTurnRate;
            _pierceCount = pierceCount;
            _radius = radius;
        }

        public void ConfigureHitscan(GameObject beamVfxPrefab, GameObject impactVfxPrefab, int maxHits = 1)
        {
            _mode = AttackRecipeDeliveryMode.Hitscan;
            _beamVfxPrefab = beamVfxPrefab;
            _impactVfxPrefab = impactVfxPrefab;
            _maxHits = maxHits;
        }

        public void ConfigureArea(float radius, int maxHits = 8)
        {
            _mode = AttackRecipeDeliveryMode.Area;
            _radius = radius;
            _maxHits = maxHits;
        }

        public void ConfigureAura(float radius, float tickIntervalSeconds)
        {
            _mode = AttackRecipeDeliveryMode.Aura;
            _radius = radius;
            _tickIntervalSeconds = tickIntervalSeconds;
        }
    }
}
