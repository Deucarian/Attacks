using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.GameplayFoundation;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Attacks.Tests
{
    public sealed class AttacksEditModeTests
    {
        private static readonly AttackDefinitionId BasicAttack = new AttackDefinitionId("attack.basic");
        private static readonly AttackSourceId Source = new AttackSourceId("source.player");
        private static readonly DamageTypeId Physical = new DamageTypeId("damage.physical");

        [Test]
        public void AttackDefinition_ValidatesInputs()
        {
            var definition = Definition();
            Assert.AreEqual(BasicAttack, definition.Id);
            Assert.Throws<ArgumentException>(() => new AttackDefinition(new AttackDefinitionId(""), 1, Physical, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackDefinition(BasicAttack, -1, Physical, 1));
            Assert.Throws<ArgumentException>(() => new AttackDefinition(BasicAttack, 1, new DamageTypeId(""), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackDefinition(BasicAttack, 1, Physical, -1));
        }

        [Test]
        public void InitialReadinessCooldownAndZeroCooldown_Work()
        {
            AttackRuntime runtime = Runtime(Definition(cooldown: 3, ready: true));
            runtime.RegisterSource(SourceSnapshot());
            AttackResult first = runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10));
            Assert.IsTrue(first.Succeeded);
            Assert.AreEqual(3, first.Cooldown.RemainingTicks);
            Assert.AreEqual(AttackFailureReason.NotReady, runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);
            runtime.Tick(2);
            Assert.AreEqual(AttackFailureReason.NotReady, runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);
            runtime.Tick(1);
            Assert.IsTrue(runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).Succeeded);

            AttackRuntime delayed = Runtime(Definition(cooldown: 2, ready: false));
            delayed.RegisterSource(SourceSnapshot());
            Assert.AreEqual(AttackFailureReason.NotReady, delayed.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);

            AttackRuntime zero = Runtime(Definition(cooldown: 0));
            zero.RegisterSource(SourceSnapshot());
            Assert.IsTrue(zero.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).Succeeded);
            Assert.IsTrue(zero.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).Succeeded);
        }

        [Test]
        public void DisabledSourceNoCandidatesAndInvalidCandidates_ReportFailures()
        {
            AttackRuntime runtime = Runtime(Definition());
            runtime.RegisterSource(SourceSnapshot(enabled: false));
            Assert.AreEqual(AttackFailureReason.SourceDisabled, runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);
            runtime.SetSourceEnabled(Source, true);
            Assert.AreEqual(AttackFailureReason.NoCandidates, runtime.TryAttack(Source, BasicAttack, Array.Empty<AttackTargetCandidate>()).FailureReason);
            Assert.AreEqual(AttackFailureReason.InvalidCandidate, runtime.TryAttack(Source, BasicAttack, new[] { new AttackTargetCandidate(new CombatantId("combatant.bad"), null, 1) }).FailureReason);
            Assert.AreEqual(AttackFailureReason.UnknownSource, runtime.TryAttack(new AttackSourceId("source.missing"), BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);
            Assert.AreEqual(AttackFailureReason.UnknownAttack, runtime.TryAttack(Source, new AttackDefinitionId("attack.missing"), Candidates("combatant.enemy", 10)).FailureReason);
        }

        [Test]
        public void TargetSelection_IsDeterministicAndOrderIndependent()
        {
            AttackRuntime runtimeA = Runtime(Definition(cooldown: 0));
            runtimeA.RegisterSource(SourceSnapshot());
            AttackTargetCandidate high = Candidate("combatant.high", 10, 4);
            AttackTargetCandidate low = Candidate("combatant.low", 10, 2);
            Assert.AreEqual(new CombatantId("combatant.high"), runtimeA.TryAttack(Source, BasicAttack, new[] { low, high }).Intent.Selection.Target.CombatantId);

            AttackTargetCandidate tieB = Candidate("combatant.b", 10, 5);
            AttackTargetCandidate tieA = Candidate("combatant.a", 10, 5);
            Assert.AreEqual(new CombatantId("combatant.a"), runtimeA.TryAttack(Source, BasicAttack, new[] { tieB, tieA }).Intent.Selection.Target.CombatantId);
            Assert.AreEqual(new CombatantId("combatant.a"), runtimeA.TryAttack(Source, BasicAttack, new[] { tieA, tieB }).Intent.Selection.Target.CombatantId);

            AttackRuntime lowest = Runtime(Definition(cooldown: 0, policy: AttackTargetPolicy.LowestScore));
            lowest.RegisterSource(SourceSnapshot());
            Assert.AreEqual(new CombatantId("combatant.low"), lowest.TryAttack(Source, BasicAttack, new[] { high, low }).Intent.Selection.Target.CombatantId);
        }

        [Test]
        public void AttackIntentCreatesCombatRequestAndCombatResolvesIt()
        {
            AttackRuntime runtime = Runtime(Definition(baseDamage: 12));
            runtime.RegisterSource(SourceSnapshot());
            HealthState target = new HealthState(new CombatantId("combatant.enemy"), 20, 20, 5, 5);
            AttackResult attack = runtime.TryAttack(Source, BasicAttack, new[] { new AttackTargetCandidate(target.Id, target, 1) });
            DamageResolutionResult resolved = CombatDamageResolver.Resolve(attack.Intent.ResolutionRequest);
            Assert.IsTrue(attack.Succeeded);
            Assert.AreEqual(AttackIntentKind.DirectDamage, attack.Intent.Kind);
            Assert.AreEqual(target.Id, attack.Intent.DamageRequest.TargetId);
            Assert.AreEqual(5, resolved.ShieldAbsorbed);
            Assert.AreEqual(7, resolved.HealthDamage);
            Assert.AreEqual(13, target.CurrentHealth);
        }

        [Test]
        public void SnapshotReconstructionAndReplay_PreserveCooldownState()
        {
            AttackRuntime runtime = Runtime(Definition(cooldown: 4));
            runtime.RegisterSource(SourceSnapshot());
            runtime.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10));
            runtime.Tick(2);
            AttackSnapshot snapshot = runtime.CreateSnapshot();
            AttackRuntime restored = AttackRuntime.FromSnapshot(Catalog(), new[] { Definition(cooldown: 4) }, snapshot);
            Assert.AreEqual(AttackFailureReason.NotReady, restored.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).FailureReason);
            restored.Tick(2);
            Assert.IsTrue(restored.TryAttack(Source, BasicAttack, Candidates("combatant.enemy", 10)).Succeeded);

            AttackRuntime a = Runtime(Definition(cooldown: 1));
            AttackRuntime b = Runtime(Definition(cooldown: 1));
            a.RegisterSource(SourceSnapshot()); b.RegisterSource(SourceSnapshot());
            AttackTargetCandidate[] candidates = { Candidate("combatant.z", 10, 1), Candidate("combatant.a", 10, 1) };
            Assert.AreEqual(a.TryAttack(Source, BasicAttack, candidates).Intent.Selection.Target.CombatantId, b.TryAttack(Source, BasicAttack, candidates).Intent.Selection.Target.CombatantId);
        }

        [Test]
        public void SourceStatAdapter_MapsGameplayFoundationStats()
        {
            StatBlock stats = new StatBlock();
            stats.SetBaseValue(new StatId("stat.crit-chance"), 1);
            stats.SetBaseValue(new StatId("stat.crit-multiplier"), 2);
            stats.SetBaseValue(new StatId("stat.damage-multiplier"), 1.5);
            AttackSourceSnapshot source = AttackSourceStatAdapter.FromStats(
                Source,
                new CombatantId("combatant.player"),
                stats,
                new AttackSourceStatMapping(new StatId("stat.crit-chance"), new StatId("stat.crit-multiplier"), StatId.Empty, StatId.Empty, new StatId("stat.damage-multiplier")));
            AttackRuntime runtime = Runtime(Definition(baseDamage: 10));
            runtime.RegisterSource(source);
            HealthState target = new HealthState(new CombatantId("combatant.enemy"), 100, 100);
            DamageResolutionResult result = CombatDamageResolver.Resolve(runtime.TryAttack(Source, BasicAttack, new[] { new AttackTargetCandidate(target.Id, target, 1) }).Intent.ResolutionRequest);
            Assert.AreEqual(30, result.FinalDamage);
        }

        [Test]
        public void DonorProof_MapsCooldownStatsTargetAndCombatRequest()
        {
            AttackDefinition donor = Definition(new AttackDefinitionId("attack.donor.arc-bolt"), cooldown: 5, baseDamage: 8);
            StatBlock stats = new StatBlock();
            stats.SetBaseValue(new StatId("stat.attack-damage"), 1.25);
            AttackSourceSnapshot source = AttackSourceStatAdapter.FromStats(
                new AttackSourceId("source.donor.player"),
                new CombatantId("combatant.donor.player"),
                stats,
                new AttackSourceStatMapping(StatId.Empty, StatId.Empty, StatId.Empty, StatId.Empty, new StatId("stat.attack-damage")));
            AttackRuntime runtime = Runtime(donor);
            runtime.RegisterSource(source);
            HealthState target = new HealthState(new CombatantId("combatant.donor.enemy"), 40, 40);
            AttackResult attack = runtime.TryAttack(source.Id, donor.Id, new[] { new AttackTargetCandidate(target.Id, target, 7) });
            DamageResolutionResult result = CombatDamageResolver.Resolve(attack.Intent.ResolutionRequest);
            Assert.IsTrue(attack.Succeeded);
            Assert.AreEqual(5, attack.Cooldown.RemainingTicks);
            Assert.AreEqual(10, result.HealthDamage);
        }

        [Test]
        public void IdleAutoDefenseAndClassicTowerDefenseProofs_Work()
        {
            AttackRuntime idle = Runtime(Definition(new AttackDefinitionId("attack.idle.module"), cooldown: 0, baseDamage: 6));
            idle.RegisterSource(new AttackSourceSnapshot(new AttackSourceId("source.idle.tower-module"), new CombatantId("combatant.idle.module")));
            HealthState far = new HealthState(new CombatantId("combatant.enemy.far"), 20, 20);
            HealthState near = new HealthState(new CombatantId("combatant.enemy.near"), 20, 20);
            AttackResult idleAttack = idle.TryAttack(new AttackSourceId("source.idle.tower-module"), new AttackDefinitionId("attack.idle.module"), new[] { new AttackTargetCandidate(far.Id, far, 1), new AttackTargetCandidate(near.Id, near, 9) });
            Assert.AreEqual(near.Id, idleAttack.Intent.Selection.Target.CombatantId);
            CombatDamageResolver.Resolve(idleAttack.Intent.ResolutionRequest);
            Assert.AreEqual(14, near.CurrentHealth);

            AttackRuntime td = Runtime(Definition(new AttackDefinitionId("attack.td.tower"), cooldown: 0, baseDamage: 4));
            td.RegisterSource(new AttackSourceSnapshot(new AttackSourceId("source.td.tower"), new CombatantId("combatant.td.tower")));
            HealthState lead = new HealthState(new CombatantId("combatant.lane.lead"), 20, 20);
            HealthState trail = new HealthState(new CombatantId("combatant.lane.trail"), 20, 20);
            AttackResult tdAttack = td.TryAttack(new AttackSourceId("source.td.tower"), new AttackDefinitionId("attack.td.tower"), new[] { new AttackTargetCandidate(trail.Id, trail, 0.2), new AttackTargetCandidate(lead.Id, lead, 0.8) });
            Assert.AreEqual(lead.Id, tdAttack.Intent.Selection.Target.CombatantId);
            CombatDamageResolver.Resolve(tdAttack.Intent.ResolutionRequest);
            Assert.AreEqual(16, lead.CurrentHealth);
        }

        [Test]
        public void DefenseGamesCompositionProof_HasNoRuntimeDependency()
        {
            DefenseObjectiveId objective = new DefenseObjectiveId("objective.base");
            var defense = new DefenseRuntime(
                new DefenseRuntimeDefinition(new[] { new DefenseObjectiveDefinition(objective, lives: 1) }),
                new FakeSpawner(),
                new FakeNavigator(),
                new FixedRouteResolver(DefenseRouteAssignment.DestinationTo(objective, Vector3.zero)),
                null);
            defense.Start();
            DefenseAgentId agent = defense.ConsumeSpawnRequest(new SpawnRequest(new EncounterId("encounter.defense"), new WaveId("wave.one"), new SpawnGroupId("group.one"), new SpawnableId("enemy.basic"), new SpawnChannelId("entry"), 0, 1, 0, 1)).AgentId;
            HealthState attackerHealth = new HealthState(new CombatantId("combatant.enemy.basic"), 5, 5);
            AttackRuntime attacks = Runtime(Definition(baseDamage: 5));
            attacks.RegisterSource(SourceSnapshot());
            DamageResolutionResult combat = CombatDamageResolver.Resolve(attacks.TryAttack(Source, BasicAttack, new[] { new AttackTargetCandidate(attackerHealth.Id, attackerHealth, 1) }).Intent.ResolutionRequest);
            if (combat.Current.LifeState == LifeState.Dead) defense.ReportKilled(agent);
            Assert.AreEqual(0, defense.ActiveAgentCount);
        }

        [Test]
        public void DurableBenchmark_WritesAttackEvaluationMeasurements()
        {
            BenchmarkMeasurement one = Measure(1000);
            BenchmarkMeasurement five = Measure(5000);
            BenchmarkMeasurement ten = Measure(10000);
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(logDirectory);
            string path = Path.Combine(logDirectory, "attacks-benchmark-results.json");
            File.WriteAllText(path, BuildBenchmarkJson(one, five, ten), Encoding.UTF8);
            Assert.AreEqual(1000, one.OperationCount);
            Assert.AreEqual(5000, five.OperationCount);
            Assert.AreEqual(10000, ten.OperationCount);
        }

        private static BenchmarkMeasurement Measure(int count)
        {
            AttackRuntime runtime = Runtime(Definition(cooldown: 0));
            runtime.RegisterSource(SourceSnapshot());
            AttackTargetCandidate[] candidates = { Candidate("combatant.a", 1000000, 1), Candidate("combatant.b", 1000000, 2), Candidate("combatant.c", 1000000, 3) };
            runtime.TryAttack(Source, BasicAttack, candidates);
            long before = GC.GetAllocatedBytesForCurrentThread();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) runtime.TryAttack(Source, BasicAttack, candidates);
            sw.Stop();
            return new BenchmarkMeasurement(count, sw.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, candidates.Length);
        }

        private static string BuildBenchmarkJson(params BenchmarkMeasurement[] measurements)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("{");
            b.AppendLine("  \"unityVersion\": \"6000.3.5f1\",");
            b.AppendLine("  \"runtime\": \"Unity EditMode Mono\",");
            b.AppendLine("  \"configuration\": \"attacks-phase-1j-preallocated-candidates-zero-cooldown\",");
            b.AppendLine("  \"cooldownState\": \"ready zero cooldown\",");
            b.AppendLine("  \"benchmarkPath\": \"Logs/attacks-benchmark-results.json\",");
            b.AppendLine("  \"measurements\": [");
            for (int i = 0; i < measurements.Length; i++)
            {
                BenchmarkMeasurement m = measurements[i];
                b.Append("    { \"operationCount\": ").Append(m.OperationCount)
                    .Append(", \"candidateCount\": ").Append(m.CandidateCount)
                    .Append(", \"elapsedMs\": ").Append(m.ElapsedMs.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(", \"bytesAllocated\": ").Append(m.BytesAllocated).Append(" }");
                b.AppendLine(i + 1 == measurements.Length ? string.Empty : ",");
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            return b.ToString();
        }

        private static AttackRuntime Runtime(AttackDefinition definition) => new AttackRuntime(Catalog(), new[] { definition });
        private static CombatCatalog Catalog() => new CombatCatalog(new[] { new DamageTypeDefinition(Physical) });
        private static AttackDefinition Definition(int cooldown = 1, double baseDamage = 10, bool ready = true, AttackTargetPolicy policy = AttackTargetPolicy.HighestScore) => Definition(BasicAttack, cooldown, baseDamage, ready, policy);
        private static AttackDefinition Definition(AttackDefinitionId id, int cooldown = 1, double baseDamage = 10, bool ready = true, AttackTargetPolicy policy = AttackTargetPolicy.HighestScore) => new AttackDefinition(id, cooldown, Physical, baseDamage, policy, ready);
        private static AttackSourceSnapshot SourceSnapshot(bool enabled = true) => new AttackSourceSnapshot(Source, new CombatantId("combatant.player"), enabled);
        private static AttackTargetCandidate[] Candidates(string id, double health) => new[] { Candidate(id, health, 1) };
        private static AttackTargetCandidate Candidate(string id, double health, double score) { var state = new HealthState(new CombatantId(id), health, health); return new AttackTargetCandidate(state.Id, state, score); }

        private readonly struct BenchmarkMeasurement { public BenchmarkMeasurement(int operationCount, double elapsedMs, long bytesAllocated, int candidateCount) { OperationCount = operationCount; ElapsedMs = elapsedMs; BytesAllocated = bytesAllocated; CandidateCount = candidateCount; } public int OperationCount { get; } public double ElapsedMs { get; } public long BytesAllocated { get; } public int CandidateCount { get; } }

        private sealed class FakeSpawner : IDefenseWorldSpawner
        {
            public SpawnResult Spawn(SpawnRequest request) => new SpawnResult(true, SpawnFailureReason.None, new SpawnInstanceId(1), null, request);
            public DespawnResult Despawn(SpawnInstanceId instanceId, DespawnReason reason) => new DespawnResult(true, DespawnFailureReason.None, instanceId, null, reason);
        }
        private sealed class FakeNavigator : IDefenseNavigator
        {
            public bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out MovementAgentId movementAgentId) { movementAgentId = new MovementAgentId(1); return true; }
            public void Cleanup(MovementAgentId movementAgentId) { }
        }
        private sealed class FixedRouteResolver : IDefenseRouteResolver
        {
            private readonly DefenseRouteAssignment _assignment;
            public FixedRouteResolver(DefenseRouteAssignment assignment) { _assignment = assignment; }
            public bool TryResolveRoute(SpawnRequest request, GameObject spawnedObject, out DefenseRouteAssignment assignment) { assignment = _assignment; return true; }
        }
    }
}
