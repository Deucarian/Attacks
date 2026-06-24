using System;
using System.Collections.Generic;
using Deucarian.Combat;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
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
}
