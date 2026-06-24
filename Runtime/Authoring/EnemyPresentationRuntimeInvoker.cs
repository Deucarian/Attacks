using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public readonly struct EnemyPresentationInvocationResult
    {
        public EnemyPresentationInvocationResult(bool invoked, bool audioPlayed, bool vfxSpawned)
        {
            Invoked = invoked;
            AudioPlayed = audioPlayed;
            VfxSpawned = vfxSpawned;
        }

        public bool Invoked { get; }
        public bool AudioPlayed { get; }
        public bool VfxSpawned { get; }
    }

    public static class EnemyPresentationRuntimeInvoker
    {
        public static EnemyPresentationInvocationResult Invoke(
            EnemyDefinitionAsset enemy,
            EnemyPresentationEventKind eventKind,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            AudioSource audioSource = null)
        {
            if (enemy == null || enemy.Presentation == null)
                return new EnemyPresentationInvocationResult(false, false, false);

            if (!enemy.Presentation.TryGetEvent(eventKind, out EnemyPresentationEventRecipe evt) || evt == null)
                return new EnemyPresentationInvocationResult(false, false, false);

            bool audioPlayed = TryPlayAudio(evt.AudioClip, position, audioSource);
            bool vfxSpawned = TrySpawnVfx(evt.VfxPrefab, position, rotation, parent);
            return new EnemyPresentationInvocationResult(true, audioPlayed, vfxSpawned);
        }

        private static bool TryPlayAudio(AudioClip clip, Vector3 position, AudioSource audioSource)
        {
            if (clip == null) return false;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
                return true;
            }

            AudioSource.PlayClipAtPoint(clip, position);
            return true;
        }

        private static bool TrySpawnVfx(GameObject vfxPrefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (vfxPrefab == null) return false;
            UnityEngine.Object.Instantiate(vfxPrefab, position, rotation, parent);
            return true;
        }
    }
}
