using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
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
}
