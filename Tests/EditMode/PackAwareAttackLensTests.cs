using System;
using Deucarian.Attacks.Editor;
using Deucarian.GameContentAuthoring.Editor;
using NUnit.Framework;

namespace Deucarian.Attacks.Tests
{
    public sealed class PackAwareAttackLensTests
    {
        [Test]
        public void Providers_ExposePackAwareLensesWithoutChangingStableProviderIds()
        {
            var attack = new AttackAuthoringProvider();
            var enemy = new EnemyAuthoringProvider();
            var encounter = new WaveAuthoringProvider();

            Assert.That(attack.ProviderId, Is.EqualTo("com.deucarian.attacks.attack"));
            Assert.That(attack.Lens.Matches(Record("attack", GameContentRecordCapabilities.Attack)), Is.True);
            Assert.That(enemy.ProviderId, Is.EqualTo("com.deucarian.attacks.enemy"));
            Assert.That(enemy.Lens.Matches(Record("enemy", GameContentRecordCapabilities.Enemy)), Is.True);
            Assert.That(encounter.ProviderId, Is.EqualTo("com.deucarian.attacks.wave"));
            Assert.That(encounter.DisplayName, Is.EqualTo("Wave / Encounter"));
            Assert.That(encounter.Lens.Matches(Record("profile", GameContentRecordCapabilities.Encounter)), Is.True);
            Assert.That(encounter.Lens.Matches(Record("wave", GameContentRecordCapabilities.Wave)), Is.True);
        }

        [Test]
        public void TypedProjections_PreserveAuthoredCommonValues()
        {
            GameContentRecordDescriptor attackRecord = Record("attack", GameContentRecordCapabilities.Attack);
            var attack = new AttackContentRecordProjection(
                attackRecord,
                17.5f,
                0.8f,
                6.25f,
                "Nearest",
                "Projectile",
                "projectile.arc",
                2,
                1.5f,
                3f,
                "Shock",
                "Arc VFX");
            var enemy = new EnemyContentRecordProjection(
                Record("enemy", GameContentRecordCapabilities.Enemy),
                "Boss",
                900f,
                2.25f,
                0.75f,
                18f,
                0.6f,
                25,
                "Persistent threat",
                true,
                "Boss bar",
                "Marker",
                "Boss presentation",
                "Leash enabled");
            var encounter = new EncounterContentRecordProjection(
                Record("profile", GameContentRecordCapabilities.Encounter),
                "RunProfile",
                300f,
                300f,
                30f,
                60f,
                120f,
                240f,
                true,
                "Compressed climax");

            Assert.That(attack.Damage, Is.EqualTo(17.5f));
            Assert.That(attack.CooldownSeconds, Is.EqualTo(0.8f));
            Assert.That(attack.Range, Is.EqualTo(6.25f));
            Assert.That(enemy.Health, Is.EqualTo(900f));
            Assert.That(enemy.MajorThreat, Is.True);
            Assert.That(enemy.LifeBarBehavior, Is.EqualTo("Boss bar"));
            Assert.That(encounter.DurationSeconds, Is.EqualTo(300f));
            Assert.That(encounter.BossSeconds, Is.EqualTo(240f));
        }

        [Test]
        public void TemplateAdapter_CanProjectWithoutDomainDependingOnTemplate()
        {
            string adapterId = "com.deucarian.attacks.tests." + Guid.NewGuid().ToString("N");
            var adapter = new FixtureAttackAdapter(adapterId);
            try
            {
                Assert.That(GameContentRecordProjectionRegistry<AttackContentRecordProjection>.Register(adapter), Is.True);
                Assert.That(GameContentRecordProjectionRegistry<AttackContentRecordProjection>.TryProject(
                    Record("attack", GameContentRecordCapabilities.Attack),
                    out AttackContentRecordProjection projection), Is.True);
                Assert.That(projection.Damage, Is.EqualTo(42f));
            }
            finally
            {
                GameContentRecordProjectionRegistry<AttackContentRecordProjection>.Unregister(adapterId);
            }
        }

        private static GameContentRecordDescriptor Record(
            string id,
            params GameContentRecordCapability[] capabilities)
        {
            return new GameContentRecordDescriptor(
                "test-pack::content::" + id,
                id,
                "content",
                null,
                id,
                string.Empty,
                string.Empty,
                Array.Empty<GameContentMetadataDescriptor>(),
                null,
                "Assets/content.json",
                "records[0]",
                Array.Empty<GameContentRecordReferenceDescriptor>(),
                Array.Empty<GameContentRecordReferenceDescriptor>(),
                GameContentAuthoringValidationResult.Valid,
                0,
                null,
                string.Empty,
                new GameContentRecordKey("com.deucarian.tests", "test-pack", id),
                capabilities);
        }

        private sealed class FixtureAttackAdapter : IGameContentRecordProjectionAdapter<AttackContentRecordProjection>
        {
            public FixtureAttackAdapter(string adapterId) { AdapterId = adapterId; }
            public string AdapterId { get; }
            public int SortOrder => 0;
            public bool TryProject(GameContentRecordDescriptor record, out AttackContentRecordProjection projection)
            {
                projection = new AttackContentRecordProjection(
                    record,
                    42f,
                    1f,
                    5f,
                    "Nearest",
                    "Direct",
                    string.Empty,
                    1,
                    0f,
                    0f,
                    string.Empty,
                    string.Empty);
                return true;
            }
        }
    }
}
