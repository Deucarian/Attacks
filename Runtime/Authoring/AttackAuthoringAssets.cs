using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Combat;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public enum AttackRecipeTargetingMode
    {
        Nearest = 0,
        Random = 1,
        Strongest = 2,
        LowestHealth = 3,
        ForwardDirection = 4
    }

    public enum AttackRecipeDeliveryMode
    {
        Projectile = 0,
        Hitscan = 1,
        Area = 2,
        Aura = 3
    }

    public enum AttackPresentationEventKind
    {
        OnCast = 0,
        OnFire = 1,
        OnImpact = 2,
        OnTick = 3,
        OnExpire = 4
    }

    public enum AttackPresentationSpawnPointRole
    {
        Caster = 0,
        Muzzle = 1,
        Target = 2,
        ImpactPoint = 3
    }

    [Serializable]
    public sealed class AttackStatusEffectRecipe
    {
        [SerializeField] private string _statusId = "status.example.slow";
        [SerializeField] private int _durationTicks = 60;
        [SerializeField] private int _tickRateTicks;
        [SerializeField] private float _strength = 1f;
        [SerializeField] private int _maxStacks = 1;
        [SerializeField] private StatusStackingPolicy _stackingPolicy = StatusStackingPolicy.UniqueRefresh;
        [SerializeField] private string _modifierStatId = string.Empty;
        [SerializeField] private string _effectNote = string.Empty;

        public AttackStatusEffectRecipe()
        {
        }

        public AttackStatusEffectRecipe(
            string statusId,
            int durationTicks,
            int tickRateTicks = 0,
            float strength = 1f,
            int maxStacks = 1,
            StatusStackingPolicy stackingPolicy = StatusStackingPolicy.UniqueRefresh,
            string modifierStatId = "",
            string effectNote = "")
        {
            _statusId = statusId ?? string.Empty;
            _durationTicks = durationTicks;
            _tickRateTicks = tickRateTicks;
            _strength = strength;
            _maxStacks = maxStacks;
            _stackingPolicy = stackingPolicy;
            _modifierStatId = modifierStatId ?? string.Empty;
            _effectNote = effectNote ?? string.Empty;
        }

        public string StatusId => _statusId ?? string.Empty;
        public int DurationTicks => _durationTicks;
        public int TickRateTicks => _tickRateTicks;
        public float Strength => _strength;
        public int MaxStacks => _maxStacks;
        public StatusStackingPolicy StackingPolicy => _stackingPolicy;
        public string ModifierStatId => _modifierStatId ?? string.Empty;
        public string EffectNote => _effectNote ?? string.Empty;

        public StatusEffectDefinition ToRuntimeDefinition()
        {
            return new StatusEffectDefinition(
                new StatusEffectId(StatusId),
                DurationTicks,
                MaxStacks,
                StackingPolicy,
                Strength);
        }

        public StatusApplicationRequest ToApplicationRequest()
        {
            return new StatusApplicationRequest(new StatusEffectId(StatusId));
        }
    }

    [Serializable]
    public sealed class AttackPresentationEventRecipe
    {
        [SerializeField] private AttackPresentationEventKind _eventKind;
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private GameObject _vfxPrefab;
        [SerializeField] private AttackPresentationSpawnPointRole _spawnPointRole = AttackPresentationSpawnPointRole.ImpactPoint;
        [SerializeField] private bool _attachToSpawnPoint;

        public AttackPresentationEventRecipe()
        {
        }

        public AttackPresentationEventRecipe(
            AttackPresentationEventKind eventKind,
            AudioClip audioClip = null,
            GameObject vfxPrefab = null,
            AttackPresentationSpawnPointRole spawnPointRole = AttackPresentationSpawnPointRole.ImpactPoint,
            bool attachToSpawnPoint = false)
        {
            _eventKind = eventKind;
            _audioClip = audioClip;
            _vfxPrefab = vfxPrefab;
            _spawnPointRole = spawnPointRole;
            _attachToSpawnPoint = attachToSpawnPoint;
        }

        public AttackPresentationEventKind EventKind => _eventKind;
        public AudioClip AudioClip => _audioClip;
        public GameObject VfxPrefab => _vfxPrefab;
        public AttackPresentationSpawnPointRole SpawnPointRole => _spawnPointRole;
        public bool AttachToSpawnPoint => _attachToSpawnPoint;
        public bool HasAnyAsset => _audioClip != null || _vfxPrefab != null;
    }

    public sealed class AttackMechanicsDefinitionAsset : ScriptableObject
    {
        [SerializeField] private int _cooldownTicks = 30;
        [SerializeField] private float _range = 6f;
        [SerializeField] private float _damageAmount = 8f;
        [SerializeField] private string _damageTypeId = "damage.physical";

        public int CooldownTicks => _cooldownTicks;
        public float Range => _range;
        public float DamageAmount => _damageAmount;
        public string DamageTypeId => _damageTypeId ?? string.Empty;

        public void Configure(int cooldownTicks, float range, float damageAmount, string damageTypeId)
        {
            _cooldownTicks = cooldownTicks;
            _range = range;
            _damageAmount = damageAmount;
            _damageTypeId = damageTypeId ?? string.Empty;
        }
    }

    public sealed class AttackTargetingDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttackRecipeTargetingMode _mode = AttackRecipeTargetingMode.Nearest;
        [SerializeField] private int _maxTargets = 1;
        [SerializeField] private bool _requiresLineOfSight;

        public AttackRecipeTargetingMode Mode => _mode;
        public int MaxTargets => _maxTargets;
        public bool RequiresLineOfSight => _requiresLineOfSight;

        public void Configure(AttackRecipeTargetingMode mode, int maxTargets = 1, bool requiresLineOfSight = false)
        {
            _mode = mode;
            _maxTargets = maxTargets;
            _requiresLineOfSight = requiresLineOfSight;
        }

        public AttackTargetPolicy ToRuntimePolicy()
        {
            return _mode == AttackRecipeTargetingMode.LowestHealth
                ? AttackTargetPolicy.LowestScore
                : AttackTargetPolicy.HighestScore;
        }
    }

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

    public sealed class AttackStatusEffectsDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttackStatusEffectRecipe[] _statusEffects = Array.Empty<AttackStatusEffectRecipe>();

        public IReadOnlyList<AttackStatusEffectRecipe> StatusEffects => _statusEffects ?? Array.Empty<AttackStatusEffectRecipe>();

        public void Configure(IReadOnlyList<AttackStatusEffectRecipe> statusEffects)
        {
            if (statusEffects == null || statusEffects.Count == 0)
            {
                _statusEffects = Array.Empty<AttackStatusEffectRecipe>();
                return;
            }

            _statusEffects = new AttackStatusEffectRecipe[statusEffects.Count];
            for (int i = 0; i < statusEffects.Count; i++)
            {
                AttackStatusEffectRecipe status = statusEffects[i];
                _statusEffects[i] = status == null
                    ? null
                    : new AttackStatusEffectRecipe(
                        status.StatusId,
                        status.DurationTicks,
                        status.TickRateTicks,
                        status.Strength,
                        status.MaxStacks,
                        status.StackingPolicy,
                        status.ModifierStatId,
                        status.EffectNote);
            }
        }

        public StatusApplicationRequest[] CreateApplicationRequests()
        {
            if (_statusEffects == null || _statusEffects.Length == 0) return Array.Empty<StatusApplicationRequest>();
            var requests = new List<StatusApplicationRequest>();
            for (int i = 0; i < _statusEffects.Length; i++)
            {
                AttackStatusEffectRecipe status = _statusEffects[i];
                if (status == null || string.IsNullOrWhiteSpace(status.StatusId)) continue;
                requests.Add(status.ToApplicationRequest());
            }

            return requests.ToArray();
        }

        public StatusEffectDefinition[] CreateRuntimeDefinitions()
        {
            if (_statusEffects == null || _statusEffects.Length == 0) return Array.Empty<StatusEffectDefinition>();
            var definitions = new List<StatusEffectDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _statusEffects.Length; i++)
            {
                AttackStatusEffectRecipe status = _statusEffects[i];
                if (status == null || string.IsNullOrWhiteSpace(status.StatusId)) continue;
                if (!seen.Add(status.StatusId.Trim())) continue;
                definitions.Add(status.ToRuntimeDefinition());
            }

            return definitions.ToArray();
        }
    }

    public sealed class AttackPresentationDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttackPresentationEventRecipe[] _events =
        {
            new AttackPresentationEventRecipe(AttackPresentationEventKind.OnCast, spawnPointRole: AttackPresentationSpawnPointRole.Caster),
            new AttackPresentationEventRecipe(AttackPresentationEventKind.OnFire, spawnPointRole: AttackPresentationSpawnPointRole.Muzzle),
            new AttackPresentationEventRecipe(AttackPresentationEventKind.OnImpact, spawnPointRole: AttackPresentationSpawnPointRole.ImpactPoint),
            new AttackPresentationEventRecipe(AttackPresentationEventKind.OnTick, spawnPointRole: AttackPresentationSpawnPointRole.Target),
            new AttackPresentationEventRecipe(AttackPresentationEventKind.OnExpire, spawnPointRole: AttackPresentationSpawnPointRole.ImpactPoint)
        };

        public IReadOnlyList<AttackPresentationEventRecipe> Events => _events ?? Array.Empty<AttackPresentationEventRecipe>();

        public void Configure(IReadOnlyList<AttackPresentationEventRecipe> events)
        {
            if (events == null || events.Count == 0)
            {
                _events = Array.Empty<AttackPresentationEventRecipe>();
                return;
            }

            _events = new AttackPresentationEventRecipe[events.Count];
            for (int i = 0; i < events.Count; i++)
            {
                AttackPresentationEventRecipe evt = events[i];
                _events[i] = evt == null
                    ? null
                    : new AttackPresentationEventRecipe(
                        evt.EventKind,
                        evt.AudioClip,
                        evt.VfxPrefab,
                        evt.SpawnPointRole,
                        evt.AttachToSpawnPoint);
            }
        }

        public bool TryGetEvent(AttackPresentationEventKind eventKind, out AttackPresentationEventRecipe recipe)
        {
            if (_events != null)
            {
                for (int i = 0; i < _events.Length; i++)
                {
                    if (_events[i] != null && _events[i].EventKind == eventKind)
                    {
                        recipe = _events[i];
                        return true;
                    }
                }
            }

            recipe = null;
            return false;
        }
    }

    [CreateAssetMenu(menuName = "Deucarian/Attacks/Attack Definition", fileName = "AttackDefinition")]
    public sealed class AttackDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "attack.example.basic";
        [SerializeField] private string _displayName = "Example Attack";
        [SerializeField] private Sprite _icon;
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private AttackMechanicsDefinitionAsset _mechanics;
        [SerializeField] private AttackTargetingDefinitionAsset _targeting;
        [SerializeField] private AttackDeliveryDefinitionAsset _delivery;
        [SerializeField] private AttackStatusEffectsDefinitionAsset _statusEffects;
        [SerializeField] private AttackPresentationDefinitionAsset _presentation;
        [SerializeField] private string _upgradeHookId = string.Empty;
        [SerializeField] private string _balancingNotes = string.Empty;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public Sprite Icon => _icon;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public AttackMechanicsDefinitionAsset Mechanics => _mechanics;
        public AttackTargetingDefinitionAsset Targeting => _targeting;
        public AttackDeliveryDefinitionAsset Delivery => _delivery;
        public AttackStatusEffectsDefinitionAsset StatusEffects => _statusEffects;
        public AttackPresentationDefinitionAsset Presentation => _presentation;
        public string UpgradeHookId => _upgradeHookId ?? string.Empty;
        public string BalancingNotes => _balancingNotes ?? string.Empty;

        public void Configure(
            string id,
            string displayName,
            Sprite icon,
            IReadOnlyList<string> tags,
            AttackMechanicsDefinitionAsset mechanics,
            AttackTargetingDefinitionAsset targeting,
            AttackDeliveryDefinitionAsset delivery,
            AttackStatusEffectsDefinitionAsset statusEffects,
            AttackPresentationDefinitionAsset presentation,
            string upgradeHookId = "",
            string balancingNotes = "")
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _icon = icon;
            _tags = CopyTags(tags);
            _mechanics = mechanics;
            _targeting = targeting;
            _delivery = delivery;
            _statusEffects = statusEffects;
            _presentation = presentation;
            _upgradeHookId = upgradeHookId ?? string.Empty;
            _balancingNotes = balancingNotes ?? string.Empty;
        }

        public AttackDefinition ToRuntimeDefinition()
        {
            if (_mechanics == null) throw new InvalidOperationException("Attack recipe has no mechanics section.");
            StatusApplicationRequest[] statuses = _statusEffects == null
                ? Array.Empty<StatusApplicationRequest>()
                : _statusEffects.CreateApplicationRequests();
            AttackTargetPolicy targetPolicy = _targeting == null
                ? AttackTargetPolicy.HighestScore
                : _targeting.ToRuntimePolicy();
            return new AttackDefinition(
                new AttackDefinitionId(Id),
                _mechanics.CooldownTicks,
                new DamageTypeId(_mechanics.DamageTypeId),
                _mechanics.DamageAmount,
                targetPolicy,
                true,
                statuses: statuses);
        }

        public StatusEffectDefinition[] CreateStatusDefinitions()
        {
            return _statusEffects == null
                ? Array.Empty<StatusEffectDefinition>()
                : _statusEffects.CreateRuntimeDefinitions();
        }

        public static AttackDefinitionAsset CreateTransient(
            string id,
            string displayName,
            AttackRecipeDeliveryMode deliveryMode,
            string damageTypeId,
            float damageAmount,
            int cooldownTicks,
            float range,
            AttackRecipeTargetingMode targetingMode,
            IReadOnlyList<AttackStatusEffectRecipe> statuses = null,
            string projectileDefinitionId = "",
            string projectileSpawnableId = "",
            float projectileSpeed = 8f,
            int projectileLifetimeTicks = 120,
            bool homing = false,
            int pierceCount = 0)
        {
            var mechanics = CreateInstance<AttackMechanicsDefinitionAsset>();
            mechanics.hideFlags = HideFlags.HideAndDontSave;
            mechanics.Configure(cooldownTicks, range, damageAmount, damageTypeId);

            var targeting = CreateInstance<AttackTargetingDefinitionAsset>();
            targeting.hideFlags = HideFlags.HideAndDontSave;
            targeting.Configure(targetingMode);

            var delivery = CreateInstance<AttackDeliveryDefinitionAsset>();
            delivery.hideFlags = HideFlags.HideAndDontSave;
            if (deliveryMode == AttackRecipeDeliveryMode.Projectile)
            {
                delivery.ConfigureProjectile(
                    projectileDefinitionId,
                    projectileSpawnableId,
                    null,
                    projectileSpeed,
                    projectileLifetimeTicks,
                    homing,
                    pierceCount: pierceCount);
            }
            else if (deliveryMode == AttackRecipeDeliveryMode.Hitscan)
            {
                delivery.ConfigureHitscan(null, null);
            }
            else if (deliveryMode == AttackRecipeDeliveryMode.Area)
            {
                delivery.ConfigureArea(2f);
            }
            else
            {
                delivery.ConfigureAura(2f, 0.5f);
            }

            var statusSection = CreateInstance<AttackStatusEffectsDefinitionAsset>();
            statusSection.hideFlags = HideFlags.HideAndDontSave;
            statusSection.Configure(statuses);

            var presentation = CreateInstance<AttackPresentationDefinitionAsset>();
            presentation.hideFlags = HideFlags.HideAndDontSave;

            var root = CreateInstance<AttackDefinitionAsset>();
            root.hideFlags = HideFlags.HideAndDontSave;
            root.Configure(id, displayName, null, Array.Empty<string>(), mechanics, targeting, delivery, statusSection, presentation);
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
