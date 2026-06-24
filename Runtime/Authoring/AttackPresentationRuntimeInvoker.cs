using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public readonly struct AttackPresentationInvocationResult
    {
        public AttackPresentationInvocationResult(bool invoked, bool audioPlayed, bool vfxSpawned)
        {
            Invoked = invoked;
            AudioPlayed = audioPlayed;
            VfxSpawned = vfxSpawned;
        }

        public bool Invoked { get; }
        public bool AudioPlayed { get; }
        public bool VfxSpawned { get; }
    }

    public static class AttackPresentationRuntimeInvoker
    {
        public static AttackPresentationInvocationResult Invoke(
            AttackDefinitionAsset attack,
            AttackPresentationEventKind eventKind,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            AudioSource audioSource = null)
        {
            if (attack == null || attack.Presentation == null)
                return new AttackPresentationInvocationResult(false, false, false);

            if (!attack.Presentation.TryGetEvent(eventKind, out AttackPresentationEventRecipe evt) || evt == null)
                return new AttackPresentationInvocationResult(false, false, false);

            bool audioPlayed = TryPlayAudio(evt.AudioClip, position, audioSource);
            bool vfxSpawned = TrySpawnVfx(evt, position, rotation, parent);
            return new AttackPresentationInvocationResult(true, audioPlayed, vfxSpawned);
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

        private static bool TrySpawnVfx(AttackPresentationEventRecipe evt, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (evt.VfxPrefab == null) return false;
            UnityEngine.Object.Instantiate(evt.VfxPrefab, position, rotation, evt.AttachToSpawnPoint ? parent : null);
            return true;
        }
    }
}
