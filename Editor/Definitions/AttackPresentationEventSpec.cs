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
    public sealed class AttackPresentationEventSpec
    {
        [DefinitionField("_eventKind")] public AttackPresentationEventKind EventKind;
        [DefinitionField("_audioClip")] public AudioClip AudioClip;
        [DefinitionField("_vfxPrefab")] public GameObject VfxPrefab;
        [DefinitionField("_spawnPointRole")] public AttackPresentationSpawnPointRole SpawnPointRole = AttackPresentationSpawnPointRole.ImpactPoint;
        [DefinitionField("_attachToSpawnPoint")] public bool AttachToSpawnPoint;
    }
}
