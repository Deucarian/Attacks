using System;
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
}
