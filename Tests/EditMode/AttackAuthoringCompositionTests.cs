using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.Attacks.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Attacks.Tests
{
    public sealed class AttackAuthoringCompositionTests
    {
        [Test]
        public void DraftFingerprintDetectsDeliveryAndStatusChangesIndependentlyOfCulture()
        {
            var draft = new AttackAuthoringState { DamageAmount = 2.5f, Range = 7.25f };
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                string original = AttackAuthoringDraft.BuildStateFingerprint(draft);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
                Assert.That(AttackAuthoringDraft.BuildStateFingerprint(draft), Is.EqualTo(original));
                draft.Homing = !draft.Homing;
                string deliveryEdit = AttackAuthoringDraft.BuildStateFingerprint(draft);
                Assert.That(deliveryEdit, Is.Not.EqualTo(original));
                draft.StatusDurationTicks++;
                Assert.That(AttackAuthoringDraft.BuildStateFingerprint(draft), Is.Not.EqualTo(deliveryEdit));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void EnemyPreviewCapturesPlaybackWithoutMutatingDraft()
        {
            var model = new GameObject("enemy-composition-model");
            var draft = new EnemyAuthoringState
            {
                EnemyId = "enemy.composition", DisplayName = "Composition", Prefab = model
            };
            try
            {
                string original = EnemyAuthoringDraft.BuildStateFingerprint(draft);
                var first = EnemyAuthoringPreview.BuildEnemyActionPreview(draft, false, 10);
                var second = EnemyAuthoringPreview.BuildEnemyActionPreview(draft, true, 20);
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.Playing, Is.False);
                Assert.That(second.Playing, Is.True);
                Assert.That(EnemyAuthoringDraft.BuildStateFingerprint(draft), Is.EqualTo(original));
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test]
        public void WaveEntryCommandsPreserveOriginalIdentityAndIsolateDuplicatedRows()
        {
            var draft = new WaveAuthoringState();
            draft.Entries.Clear();
            var original = new WaveEntryAuthoringState
            {
                EntryId = "entry.original", Count = 12, BatchSize = 3,
                InitialDelayTicks = 5, IntervalTicks = 8, SpawnChannelId = "air", ScalingTier = 2
            };
            var copy = WaveAuthoringEntryCommands.CopyEntry(original);
            draft.Entries.Add(original);
            draft.Entries.Add(copy);
            string before = WaveAuthoringDraft.BuildStateFingerprint(draft);
            WaveAuthoringEntryCommands.MoveEntry(draft, 0, 1);
            Assert.That(draft.Entries[1], Is.SameAs(original));
            Assert.That(original.EntryId, Is.EqualTo("entry.original"));
            Assert.That(copy.EntryId, Is.Not.EqualTo(original.EntryId).And.Not.Empty);
            Assert.That(copy.Count, Is.EqualTo(12));
            Assert.That(copy.BatchSize, Is.EqualTo(3));
            Assert.That(copy.SpawnChannelId, Is.EqualTo("air"));
            Assert.That(WaveAuthoringDraft.BuildStateFingerprint(draft), Is.Not.EqualTo(before));
            copy.Count = 99;
            Assert.That(original.Count, Is.EqualTo(12));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void InvalidWaveMoveLeavesAuthoredRowsUnchanged(int from, int to)
        {
            var draft = new WaveAuthoringState();
            draft.EnsureEntries();
            string before = WaveAuthoringDraft.BuildStateFingerprint(draft);
            WaveAuthoringEntryCommands.MoveEntry(draft, from, to);
            Assert.That(WaveAuthoringDraft.BuildStateFingerprint(draft), Is.EqualTo(before));
        }

        [Test]
        public void AttackSessionResetDoesNotLeakPreviewContextAcrossProviders()
        {
            var first = new AttackProviderV2State
            {
                PreviewSourceContextIndex = 4, PreviewTargetContextIndex = 3, LastPreviewAudioPhase = 2
            };
            var second = new AttackProviderV2State { PreviewSourceContextIndex = 2 };
            first.ResetProviderSession();
            first.ResetProviderSession();
            first.SetPreviewSource("attack.next-selection");
            first.SetPreviewSource("attack.next-selection");
            Assert.That(first.PreviewSourceContextIndex, Is.Zero);
            Assert.That(first.PreviewTargetContextIndex, Is.Zero);
            Assert.That(first.LastPreviewAudioPhase, Is.EqualTo(-1));
            Assert.That(second.PreviewSourceContextIndex, Is.EqualTo(2));
        }
    }
}
