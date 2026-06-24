using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class EnemyPresentationDefinitionAsset : ScriptableObject
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private EnemyPresentationEventRecipe[] _events =
        {
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn),
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit),
            new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath)
        };

        public GameObject Prefab => _prefab;
        public IReadOnlyList<EnemyPresentationEventRecipe> Events => _events ?? Array.Empty<EnemyPresentationEventRecipe>();

        public void Configure(GameObject prefab, IReadOnlyList<EnemyPresentationEventRecipe> events)
        {
            _prefab = prefab;
            if (events == null || events.Count == 0)
            {
                _events = Array.Empty<EnemyPresentationEventRecipe>();
                return;
            }

            _events = new EnemyPresentationEventRecipe[events.Count];
            for (int i = 0; i < events.Count; i++)
            {
                EnemyPresentationEventRecipe evt = events[i];
                _events[i] = evt == null
                    ? null
                    : new EnemyPresentationEventRecipe(evt.EventKind, evt.AudioClip, evt.VfxPrefab);
            }
        }

        public bool TryGetEvent(EnemyPresentationEventKind eventKind, out EnemyPresentationEventRecipe recipe)
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
