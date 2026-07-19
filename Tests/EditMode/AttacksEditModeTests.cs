using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Deucarian.Attacks.Editor;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Editor;
using Deucarian.Encounters;
using Deucarian.GameContentAuthoring.Editor;
using Deucarian.GameplayFoundation;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public void AttackDefinitionAsset_ConvertsToRuntimeDefinitionAndStatusHooks()
        {
            AttackDefinitionAsset recipe = AttackDefinitionAsset.CreateTransient(
                "attack.authoring.slow-bolt",
                "Slow Bolt",
                AttackRecipeDeliveryMode.Projectile,
                Physical.Value,
                9,
                2,
                7,
                AttackRecipeTargetingMode.Nearest,
                new[] { new AttackStatusEffectRecipe("status.authoring.slow", 60, 15, 0.35f) },
                "projectile.authoring.slow-bolt",
                "projectile.authoring.slow-bolt",
                8,
                120,
                homing: true);

            AttackRecipeValidationReport report = AttackRecipeValidator.Validate(recipe);
            Assert.IsTrue(report.IsValid);

            AttackDefinition definition = recipe.ToRuntimeDefinition();
            Assert.AreEqual(new AttackDefinitionId("attack.authoring.slow-bolt"), definition.Id);
            Assert.AreEqual(2, definition.CooldownTicks);
            Assert.AreEqual(9, definition.BaseDamage);
            Assert.AreEqual(1, definition.Statuses.Count);

            var catalog = new CombatCatalog(new[] { new DamageTypeDefinition(Physical) }, recipe.CreateStatusDefinitions());
            var runtime = new AttackRuntime(catalog, new[] { definition });
            runtime.RegisterSource(SourceSnapshot());

            HealthState target = new HealthState(new CombatantId("combatant.authoring.enemy"), 20, 20);
            AttackResult attack = runtime.TryAttack(Source, definition.Id, new[] { new AttackTargetCandidate(target.Id, target, 1) });
            var statusState = new StatusState();
            DamageResolutionResult resolved = CombatDamageResolver.Resolve(catalog, target, statusState, attack.Intent.DamageRequest);

            Assert.IsTrue(attack.Succeeded);
            Assert.IsTrue(resolved.Succeeded);
            Assert.IsTrue(statusState.Contains(new StatusEffectId("status.authoring.slow")));
        }

        [Test]
        public void AttackRecipeValidation_RequiresProjectilePrefabForAssetCreation()
        {
            AttackDefinitionAsset recipe = AttackDefinitionAsset.CreateTransient(
                "attack.authoring.prefab-check",
                "Prefab Check",
                AttackRecipeDeliveryMode.Projectile,
                Physical.Value,
                5,
                1,
                5,
                AttackRecipeTargetingMode.Nearest,
                projectileDefinitionId: "projectile.authoring.prefab-check",
                projectileSpawnableId: "projectile.authoring.prefab-check");

            AttackRecipeValidationReport missingPrefab = AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.AssetCreation);
            Assert.IsFalse(missingPrefab.IsValid);

            GameObject projectilePrefab = new GameObject("AuthoringProjectilePrefab");
            try
            {
                recipe.Delivery.ConfigureProjectile(
                    "projectile.authoring.prefab-check",
                    "projectile.authoring.prefab-check",
                    projectilePrefab,
                    8,
                    120);
                AttackRecipeValidationReport valid = AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.AssetCreation);
                Assert.IsTrue(valid.IsValid);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
            }
        }

        [Test]
        public void PresentationInvoker_GracefullyHandlesMissingOptionalAssets()
        {
            AttackDefinitionAsset recipe = AttackDefinitionAsset.CreateTransient(
                "attack.authoring.presentation",
                "Presentation",
                AttackRecipeDeliveryMode.Hitscan,
                Physical.Value,
                4,
                0,
                5,
                AttackRecipeTargetingMode.ForwardDirection);

            AttackPresentationInvocationResult result = AttackPresentationRuntimeInvoker.Invoke(
                recipe,
                AttackPresentationEventKind.OnFire,
                Vector3.zero,
                Quaternion.identity);

            Assert.IsTrue(result.Invoked);
            Assert.IsFalse(result.AudioPlayed);
            Assert.IsFalse(result.VfxSpawned);
        }

        [Test]
        public void SharedGameContentAuthoringSurface_ExposesMenuAndAttackProviders()
        {
            Assert.AreEqual("Tools/Deucarian/Tools & Quality/Game Content Authoring", GameContentAuthoringWindow.MenuPath);

            var providerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (IGameContentAuthoringProvider provider in GameContentAuthoringProviderRegistry.Providers)
                providerIds.Add(provider.ProviderId);

            Assert.IsTrue(providerIds.Contains("com.deucarian.attacks.attack"));
            Assert.IsTrue(providerIds.Contains("com.deucarian.attacks.enemy"));
            Assert.IsTrue(providerIds.Contains("com.deucarian.attacks.wave"));
        }

        [Test]
        public void AttackProvider_UsesCustomAuthoringSurface()
        {
            var provider = new AttackAuthoringProvider();

            Assert.That(provider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
        }

        [Test]
        public void SharedGameContentAuthoringSurface_ProvidersExposeSafePreviewLifecycle()
        {
            foreach (IGameContentAuthoringProvider provider in GameContentAuthoringProviderRegistry.Providers)
            {
                if (!provider.ProviderId.StartsWith("com.deucarian.attacks.", StringComparison.Ordinal))
                    continue;

                Assert.DoesNotThrow(provider.StopPreview);
            }
        }

        [Test]
        public void AttackPreviewSummaries_IncludeCombatPresentationAndExpectedResult()
        {
            var state = new AttackAuthoringState
            {
                IncludeStatusEffect = true,
                StatusId = "status.preview.burning",
                DeliveryMode = AttackRecipeDeliveryMode.Hitscan,
                BeamVfxPrefab = null,
                DamageAmount = 12.5f,
                DamageTypeId = "damage.preview.fire"
            };

            IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildAttackRows(state);
            IReadOnlyList<GameContentAuthoringPreviewRow> presentation = AttackGameContentPreviewSummaries.BuildAttackPresentationRows(state);
            IReadOnlyList<GameContentAuthoringPreviewRow> expected = AttackGameContentPreviewSummaries.BuildAttackExpectedRows(state);

            AssertRowContains(rows, "Damage", "12.5 damage.preview.fire");
            AssertRowContains(rows, "Delivery", "Hitscan");
            AssertRowContains(rows, "Status", "status.preview.burning");
            AssertRowContains(presentation, "OnTick", "no audio");
            AssertRowContains(expected, "Expected", "status.preview.burning");
        }

        [Test]
        public void SelectedAttackPreviewState_UsesSelectedAssetData()
        {
            AttackDefinitionAsset selected = AttackDefinitionAsset.CreateTransient(
                "attack.moss.cursor-ray",
                "Cursor Ray",
                AttackRecipeDeliveryMode.Hitscan,
                "damage.moss.focus",
                6.5f,
                18,
                8.5f,
                AttackRecipeTargetingMode.ForwardDirection);

            var provider = new AttackAuthoringProvider();
            var selection = new GameContentAuthoringPreviewSelection(
                provider.ProviderId,
                selected.DisplayName,
                selected.Id,
                "Attacks",
                "Assets/GameContent/Attacks/attack.moss.cursor-ray.asset",
                selected);
            var context = new GameContentAuthoringPreviewContext(null, provider, selectedExistingItem: selection);

            AttackAuthoringState state = AttackGameContentPreviewSelection.ResolveAttackState(context, new AttackAuthoringState());
            IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildAttackRows(state);
            string flattened = FlattenRows(rows);

            Assert.AreEqual("attack.moss.cursor-ray", state.AttackId);
            Assert.AreEqual("Cursor Ray", state.DisplayName);
            Assert.AreEqual(AttackRecipeDeliveryMode.Hitscan, state.DeliveryMode);
            AssertRowContains(rows, "ID", "attack.moss.cursor-ray");
            AssertRowContains(rows, "Name", "Cursor Ray");
            Assert.IsFalse(flattened.Contains("attack.example"));
            Assert.IsFalse(flattened.Contains("fire-orb"));
        }

        [Test]
        public void SelectedProjectilePreviewState_ReplacesCreateFormExampleValues()
        {
            AttackDefinitionAsset selected = AttackDefinitionAsset.CreateTransient(
                "attack.moss.spore-pop",
                "Spore Pop",
                AttackRecipeDeliveryMode.Projectile,
                "damage.moss.spore",
                9f,
                24,
                7f,
                AttackRecipeTargetingMode.Nearest,
                projectileDefinitionId: "projectile.moss.spore-pop",
                projectileSpawnableId: "projectile.moss.spore-pop",
                projectileSpeed: 10f,
                projectileLifetimeTicks: 96);

            var provider = new AttackAuthoringProvider();
            var selection = new GameContentAuthoringPreviewSelection(
                provider.ProviderId,
                selected.DisplayName,
                selected.Id,
                "Attacks",
                "Assets/GameContent/Attacks/attack.moss.spore-pop.asset",
                selected);
            var context = new GameContentAuthoringPreviewContext(null, provider, selectedExistingItem: selection);

            AttackAuthoringState state = AttackGameContentPreviewSelection.ResolveAttackState(context, new AttackAuthoringState());
            IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildAttackRows(state);
            string flattened = FlattenRows(rows);

            Assert.AreEqual("attack.moss.spore-pop", state.AttackId);
            Assert.AreEqual("projectile.moss.spore-pop", state.ProjectileDefinitionId);
            AssertRowContains(rows, "Delivery", "projectile.moss.spore-pop");
            Assert.IsFalse(flattened.Contains("attack.example"));
            Assert.IsFalse(flattened.Contains("fire-orb"));
        }

        [Test]
        public void PreviewContext_IgnoresSelectionFromDifferentProvider()
        {
            AttackDefinitionAsset selectedAttack = AttackDefinitionAsset.CreateTransient(
                "attack.moss.moss-seeker",
                "Moss Seeker",
                AttackRecipeDeliveryMode.Projectile,
                "damage.moss.seek",
                7f,
                30,
                9f,
                AttackRecipeTargetingMode.Nearest,
                projectileDefinitionId: "projectile.moss.seeker",
                projectileSpawnableId: "projectile.moss.seeker",
                homing: true);
            var enemyProvider = new EnemyAuthoringProvider();
            var staleSelection = new GameContentAuthoringPreviewSelection(
                "com.deucarian.attacks.attack",
                selectedAttack.DisplayName,
                selectedAttack.Id,
                "Attacks",
                "Assets/GameContent/Attacks/attack.moss.moss-seeker.asset",
                selectedAttack);
            var context = new GameContentAuthoringPreviewContext(null, enemyProvider, selectedExistingItem: staleSelection);
            var createState = new EnemyAuthoringState { EnemyId = "enemy.create.current", DisplayName = "Current Enemy Form" };

            EnemyAuthoringState state = AttackGameContentPreviewSelection.ResolveEnemyState(context, createState);

            Assert.AreSame(createState, state);
            Assert.IsFalse(context.HasSelectedExistingItem);
            Assert.AreEqual("enemy.create.current", state.EnemyId);
        }

        [Test]
        public void PreviewActions_HandleMissingOptionalAssetsWithoutThrowing()
        {
            var attack = new AttackAuthoringState { IncludeStatusEffect = true };
            var enemy = new EnemyAuthoringState();
            var wave = new WaveAuthoringState();

            Assert.DoesNotThrow(() => AttackGameContentPreviewActions.PreviewAttackEvent(attack, AttackPresentationEventKind.OnImpact));
            Assert.DoesNotThrow(() => AttackGameContentPreviewActions.PreviewAttackEvent(attack, AttackPresentationEventKind.OnTick));
            Assert.DoesNotThrow(() => AttackGameContentPreviewActions.PreviewEnemyEvent(enemy, EnemyPresentationEventKind.OnSpawn));
            Assert.DoesNotThrow(() => AttackGameContentPreviewActions.PreviewWaveTimeline(wave));

            StringAssert.Contains("no audio clip assigned", AttackGameContentPreviewActions.PreviewAttackEvent(attack, AttackPresentationEventKind.OnImpact));
            StringAssert.Contains("no audio clip assigned", AttackGameContentPreviewActions.PreviewEnemyEvent(enemy, EnemyPresentationEventKind.OnSpawn));
        }

        [Test]
        public void PreviewActions_MuteSuppressesAttackAudioPreview()
        {
            AudioClip clip = AudioClip.Create("MutedPreviewClip", 128, 1, 44100, false);
            try
            {
                var attack = new AttackAuthoringState { CastAudio = clip };

                string status = AttackGameContentPreviewActions.PreviewAttackEvent(attack, AttackPresentationEventKind.OnCast, true);
                string full = AttackGameContentPreviewActions.PreviewFullAttack(attack, true);

                StringAssert.Contains("audio muted", status);
                StringAssert.Contains("Audio muted", full);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AttackProviderV2ListModel_ClassifiesDeliveryModes()
        {
            AttackDefinitionAsset homing = AttackDefinitionAsset.CreateTransient(
                "attack.test.homing",
                "Homing",
                AttackRecipeDeliveryMode.Projectile,
                Physical.Value,
                1f,
                1,
                4f,
                AttackRecipeTargetingMode.Nearest,
                projectileDefinitionId: "projectile.test.homing",
                projectileSpawnableId: "projectile.test.homing",
                homing: true);
            AttackDefinitionAsset beam = AttackDefinitionAsset.CreateTransient(
                "attack.test.beam",
                "Beam",
                AttackRecipeDeliveryMode.Hitscan,
                Physical.Value,
                1f,
                1,
                4f,
                AttackRecipeTargetingMode.ForwardDirection);
            AttackDefinitionAsset area = AttackDefinitionAsset.CreateTransient(
                "attack.test.area",
                "Area",
                AttackRecipeDeliveryMode.Area,
                Physical.Value,
                1f,
                1,
                4f,
                AttackRecipeTargetingMode.Nearest);
            AttackDefinitionAsset status = AttackDefinitionAsset.CreateTransient(
                "attack.test.status",
                "Status",
                AttackRecipeDeliveryMode.Aura,
                Physical.Value,
                1f,
                1,
                4f,
                AttackRecipeTargetingMode.Nearest,
                new[] { new AttackStatusEffectRecipe("status.test.slow", 30, 10, 1f) });

            try
            {
                Assert.That(AttackProviderV2ListItem.GetTypeLabelForTests(homing), Is.EqualTo("Homing"));
                Assert.That(AttackProviderV2ListItem.GetTypeLabelForTests(beam), Is.EqualTo("Beam"));
                Assert.That(AttackProviderV2ListItem.GetTypeLabelForTests(area), Is.EqualTo("AOE"));
                Assert.That(AttackProviderV2ListItem.GetTypeLabelForTests(status), Is.EqualTo("Status"));
            }
            finally
            {
                AttackRecipeAssetCreator.DestroyTransient(homing);
                AttackRecipeAssetCreator.DestroyTransient(beam);
                AttackRecipeAssetCreator.DestroyTransient(area);
                AttackRecipeAssetCreator.DestroyTransient(status);
            }
        }

        [Test]
        public void AttackRecipeAssetCreator_UpdateExistingAssetSavesSelectedAttackSections()
        {
            string rootFolder = "Assets/__AttackGcaV3EditTests_" + Guid.NewGuid().ToString("N");
            try
            {
                var createState = new AttackAuthoringState
                {
                    AttackId = "attack.test.v3-edit." + Guid.NewGuid().ToString("N"),
                    DisplayName = "Before Edit",
                    TagsCsv = "before",
                    OutputRoot = rootFolder,
                    DamageTypeId = Physical.Value,
                    DamageAmount = 5f,
                    CooldownTicks = 12,
                    Range = 6f,
                    DeliveryMode = AttackRecipeDeliveryMode.Area,
                    Radius = 2f,
                    MaxHits = 2
                };

                GameContentCreationResult created = AttackRecipeAssetCreator.CreateAssets(createState);
                Assert.That(created.Succeeded, Is.True, created.Message);
                var asset = (AttackDefinitionAsset)created.CreatedRoot;
                Assert.That(asset, Is.Not.Null);

                AttackAuthoringState editState = AttackGameContentPreviewSelection.FromAttackAsset(asset);
                editState.DisplayName = "After Edit";
                editState.TagsCsv = "edited, v3";
                editState.DamageAmount = 12f;
                editState.CooldownTicks = 20;
                editState.Range = 9f;
                editState.DeliveryMode = AttackRecipeDeliveryMode.Projectile;
                editState.ProjectileDefinitionId = "projectile.test.edited";
                editState.ProjectileSpawnableId = "projectile.test.edited";
                editState.ProjectileSpeed = 9.5f;
                editState.ProjectileLifetimeTicks = 90;
                editState.ProjectilePrefab = null;
                editState.IncludeStatusEffect = true;
                editState.StatusId = "status.test.edited";
                editState.StatusDurationTicks = 30;
                editState.StatusTickRateTicks = 10;
                editState.StatusStrength = 2f;
                editState.StatusMaxStacks = 2;

                GameContentCreationResult saved = AttackRecipeAssetCreator.UpdateExistingAsset(asset, editState);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.DisplayName, Is.EqualTo("After Edit"));
                Assert.That(asset.Tags, Does.Contain("edited"));
                Assert.That(asset.Mechanics.DamageAmount, Is.EqualTo(12f));
                Assert.That(asset.Mechanics.CooldownTicks, Is.EqualTo(20));
                Assert.That(asset.Mechanics.Range, Is.EqualTo(9f));
                Assert.That(asset.Delivery.Mode, Is.EqualTo(AttackRecipeDeliveryMode.Projectile));
                Assert.That(asset.Delivery.ProjectileDefinitionId, Is.EqualTo("projectile.test.edited"));
                Assert.That(asset.Delivery.ProjectileSpawnableId, Is.EqualTo("projectile.test.edited"));
                Assert.That(asset.Delivery.ProjectilePrefab, Is.Null);
                Assert.That(asset.StatusEffects.StatusEffects.Count, Is.EqualTo(1));
                Assert.That(asset.StatusEffects.StatusEffects[0].StatusId, Is.EqualTo("status.test.edited"));
                Assert.That(AttackRecipeAssetCreator.ValidateForUpdate(editState, asset).IsValid, Is.True);
                IReadOnlyList<GameContentAuthoringPreviewRow> savedRows = AttackGameContentPreviewSummaries.BuildAttackRows(AttackGameContentPreviewSelection.FromAttackAsset(asset));
                AssertRowContains(savedRows, "Damage", "12");
                AssertRowContains(savedRows, "Delivery", "projectile.test.edited");
            }
            finally
            {
                if (AssetDatabase.IsValidFolder(rootFolder))
                    AssetDatabase.DeleteAsset(rootFolder);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void FullAttackPreview_BuildsViewportActionFromDeliveryAndPresentation()
        {
            var projectile = new GameObject("ProjectilePreviewPrefab");
            var impact = new GameObject("ImpactPreviewPrefab");
            try
            {
                var attack = new AttackAuthoringState
                {
                    DisplayName = "Preview Bolt",
                    DeliveryMode = AttackRecipeDeliveryMode.Projectile,
                    ProjectilePrefab = projectile,
                    ImpactVfxPrefab = impact,
                    IncludeStatusEffect = true
                };

                GameContentAuthoringActionPreview preview = AttackGameContentPreviewActions.BuildActionPreview(attack, true, 5d);

                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.Mode, Is.EqualTo(GameContentAuthoringActionPreviewMode.Projectile));
                Assert.That(preview.PrimaryAsset, Is.SameAs(projectile));
                Assert.That(preview.ProjectilePrefab, Is.SameAs(projectile));
                Assert.That(preview.ImpactVfxPrefab, Is.SameAs(impact));
                Assert.That(preview.RenderMode, Is.EqualTo(GameContentAuthoringActionPreviewRenderMode.Game));
                Assert.That(preview.Playing, Is.True);
                Assert.That(preview.DeliveryTypeLabel, Is.EqualTo("Projectile"));
                AssertPreviewRoles(preview, "Source", "Projectile", "Target");
                Assert.That(GameContentAuthoringObjectPreviewUtility.RequestsRoleLabels(preview), Is.False);
                preview.RenderMode = GameContentAuthoringActionPreviewRenderMode.Debug;
                Assert.That(GameContentAuthoringObjectPreviewUtility.RequestsRoleLabels(preview), Is.True);
                Assert.That(PreviewRole(preview, "Projectile").Label, Is.EqualTo("ProjectilePreviewPrefab"));
                Assert.That(GameContentAuthoringObjectPreviewUtility.BuildRoleLabelContent(PreviewRole(preview, "Projectile")).text, Is.EqualTo("Projectile: ProjectilePreviewPrefab"));
                Assert.That(PreviewRole(preview, "Projectile").Asset, Is.SameAs(projectile));
                Assert.That(preview.GetPhaseLabel(6.5d), Is.EqualTo("Projectile travel"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectile);
                UnityEngine.Object.DestroyImmediate(impact);
            }
        }

        [Test]
        public void AttackProviderV2Preview_EventRowsUseGlobalControlsOnly()
        {
            Assert.That(AttackProviderV2PreviewModel.EventRowsExposePreviewActions, Is.False);
        }

        [Test]
        public void AttackProviderV2Preview_DraftAndUnsavedScopesExposeCompactChips()
        {
            var draft = new AttackAuthoringState();
            var previewState = new AttackProviderV2State
            {
                PreviewMuted = true,
                PreviewLoop = true,
                PreviewSpeed = 1f,
                PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Game
            };

            AttackProviderV2PreviewScope draftScope = AttackProviderV2PreviewModel.GetScope(true, false);
            IReadOnlyList<DeucarianEditorStatusChip> draftChips = AttackProviderV2PreviewModel.BuildChips(draft, previewState, draftScope);

            Assert.That(AttackProviderV2PreviewModel.GetScopeLabel(draftScope), Is.EqualTo("Draft"));
            Assert.That(AttackProviderV2PreviewModel.BuildHeaderTitle(draft, draftScope), Is.EqualTo("Preview Lab - New Attack Draft"));
            AssertChip(draftChips, "Draft Preview", DeucarianEditorStatus.Info);
            AssertChip(draftChips, "Muted", DeucarianEditorStatus.Disabled);
            AssertChip(draftChips, "Game", DeucarianEditorStatus.Success);
            AssertChip(draftChips, "Loop", DeucarianEditorStatus.Success);

            draft.DisplayName = "Live Draft Bolt";
            Assert.That(AttackProviderV2PreviewModel.BuildHeaderTitle(draft, draftScope), Is.EqualTo("Preview Lab - Live Draft Bolt"));

            AttackProviderV2PreviewScope unsavedScope = AttackProviderV2PreviewModel.GetScope(false, true);
            IReadOnlyList<DeucarianEditorStatusChip> unsavedChips = AttackProviderV2PreviewModel.BuildChips(draft, previewState, unsavedScope);

            Assert.That(AttackProviderV2PreviewModel.GetScopeLabel(unsavedScope), Is.EqualTo("Unsaved"));
            AssertChip(unsavedChips, "Unsaved Preview", DeucarianEditorStatus.Warning);
        }

        [Test]
        public void AttackProviderV2Preview_CreateAndProviderSelectionClearTransientPreviewState()
        {
            var state = new AttackProviderV2State
            {
                EditingState = new AttackAuthoringState { DisplayName = "Dirty Edit" },
                ActivePreviewKey = "selected.attack"
            };

            state.BeginCreate();

            Assert.That(state.Creating, Is.True);
            Assert.That(state.WizardStep, Is.EqualTo(0));
            Assert.That(state.EditingState, Is.Null);
            Assert.That(state.PreviewStatus, Is.EqualTo("Previewing draft"));

            state.LeaveCreate();

            Assert.That(state.Creating, Is.False);
            Assert.That(state.PreviewStatus, Is.EqualTo("Previewing selected attack"));

            var provider = new AttackAuthoringProvider();
            AttackProviderV2State providerState = GetProviderV2State(provider);
            providerState.BeginCreate();
            providerState.ActivePreviewKey = "__draft_attack__";
            providerState.EditingState = new AttackAuthoringState { DisplayName = "Unsaved Draft" };

            provider.OnSelected();

            Assert.That(providerState.Creating, Is.False);
            Assert.That(providerState.ActivePreviewKey, Is.Empty);
            Assert.That(providerState.EditingState, Is.Null);
            Assert.That(providerState.PreviewStatus, Is.EqualTo("Preview idle"));
        }

        [Test]
        public void AttackProviderV2Preview_DeliverySwitchUpdatesPreviewModeAndClearsStaleAssets()
        {
            var projectile = new GameObject("projectile.live-draft");
            var beam = new GameObject("beam.live-draft");
            try
            {
                var draft = new AttackAuthoringState
                {
                    DeliveryMode = AttackRecipeDeliveryMode.Projectile,
                    ProjectilePrefab = projectile,
                    BeamVfxPrefab = beam
                };

                GameContentAuthoringActionPreview projectilePreview = AttackGameContentPreviewActions.BuildActionPreview(draft, true, 0d);

                Assert.That(projectilePreview.Mode, Is.EqualTo(GameContentAuthoringActionPreviewMode.Projectile));
                Assert.That(projectilePreview.ProjectilePrefab, Is.SameAs(projectile));
                Assert.That(projectilePreview.BeamVfxPrefab, Is.Null);
                Assert.That(projectilePreview.PrimaryAsset, Is.SameAs(projectile));

                draft.DeliveryMode = AttackRecipeDeliveryMode.Hitscan;
                GameContentAuthoringActionPreview beamPreview = AttackGameContentPreviewActions.BuildActionPreview(draft, true, 0d);

                Assert.That(beamPreview.Mode, Is.EqualTo(GameContentAuthoringActionPreviewMode.Hitscan));
                Assert.That(beamPreview.ProjectilePrefab, Is.Null);
                Assert.That(beamPreview.BeamVfxPrefab, Is.SameAs(beam));
                Assert.That(beamPreview.PrimaryAsset, Is.SameAs(beam));
                AssertPreviewRoles(beamPreview, "Source", "Beam", "Impact");

                draft.DeliveryMode = AttackRecipeDeliveryMode.Projectile;
                GameContentAuthoringActionPreview projectileAgain = AttackGameContentPreviewActions.BuildActionPreview(draft, true, 0d);

                Assert.That(projectileAgain.ProjectilePrefab, Is.SameAs(projectile));
                Assert.That(projectileAgain.BeamVfxPrefab, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectile);
                UnityEngine.Object.DestroyImmediate(beam);
            }
        }

        [Test]
        public void AttackProviderV2Preview_DraftPresentationChangesUpdatePreviewData()
        {
            var projectileA = new GameObject("projectile.draft.a");
            var projectileB = new GameObject("projectile.draft.b");
            var impactA = new GameObject("impact.draft.a");
            var impactB = new GameObject("impact.draft.b");
            try
            {
                var draft = new AttackAuthoringState
                {
                    DisplayName = "Draft A",
                    AttackId = "attack.draft.a",
                    DeliveryMode = AttackRecipeDeliveryMode.Projectile,
                    ProjectilePrefab = projectileA,
                    ImpactVfxPresentationPrefab = impactA,
                    DamageAmount = 4f,
                    DamageTypeId = Physical.Value
                };

                GameContentAuthoringActionPreview previewA = AttackGameContentPreviewActions.BuildActionPreview(draft, true, 0d);
                Assert.That(previewA.PrimaryAsset, Is.SameAs(projectileA));
                Assert.That(previewA.ImpactVfxPrefab, Is.SameAs(impactA));

                draft.DisplayName = "Draft B";
                draft.ProjectilePrefab = projectileB;
                draft.ImpactVfxPresentationPrefab = impactB;
                draft.DamageAmount = 17f;

                GameContentAuthoringActionPreview previewB = AttackGameContentPreviewActions.BuildActionPreview(draft, true, 0d);
                IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildAttackRows(draft);

                Assert.That(previewB.Label, Is.EqualTo("Draft B"));
                Assert.That(previewB.PrimaryAsset, Is.SameAs(projectileB));
                Assert.That(previewB.ImpactVfxPrefab, Is.SameAs(impactB));
                AssertRowContains(rows, "Damage", "17");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileA);
                UnityEngine.Object.DestroyImmediate(projectileB);
                UnityEngine.Object.DestroyImmediate(impactA);
                UnityEngine.Object.DestroyImmediate(impactB);
            }
        }

        [Test]
        public void AttackProviderV2Preview_UnsavedEditUpdatesPreviewAndRevertRestoresSavedData()
        {
            AttackDefinitionAsset asset = AttackDefinitionAsset.CreateTransient(
                "attack.preview.unsaved",
                "Saved Preview",
                AttackRecipeDeliveryMode.Hitscan,
                Physical.Value,
                6f,
                12,
                5f,
                AttackRecipeTargetingMode.ForwardDirection);
            try
            {
                AttackAuthoringState edit = AttackGameContentPreviewSelection.FromAttackAsset(asset);
                edit.DisplayName = "Unsaved Preview";
                edit.DamageAmount = 11f;
                edit.Range = 9f;

                IReadOnlyList<GameContentAuthoringPreviewRow> unsavedRows = AttackGameContentPreviewSummaries.BuildAttackRows(edit);
                AttackProviderV2PreviewScope scope = AttackProviderV2PreviewModel.GetScope(false, true);

                Assert.That(asset.Mechanics.DamageAmount, Is.EqualTo(6f));
                Assert.That(AttackProviderV2PreviewModel.GetScopeLabel(scope), Is.EqualTo("Unsaved"));
                AssertChip(AttackProviderV2PreviewModel.BuildChips(edit, new AttackProviderV2State(), scope), "Unsaved Preview", DeucarianEditorStatus.Warning);
                AssertRowContains(unsavedRows, "Name", "Unsaved Preview");
                AssertRowContains(unsavedRows, "Damage", "11");
                AssertRowContains(unsavedRows, "Range", "9");

                AttackAuthoringState reverted = AttackGameContentPreviewSelection.FromAttackAsset(asset);
                IReadOnlyList<GameContentAuthoringPreviewRow> revertedRows = AttackGameContentPreviewSummaries.BuildAttackRows(reverted);

                AssertRowContains(revertedRows, "Name", "Saved Preview");
                AssertRowContains(revertedRows, "Damage", "6");
                AssertRowContains(revertedRows, "Range", "5");
            }
            finally
            {
                AttackRecipeAssetCreator.DestroyTransient(asset);
            }
        }

        [Test]
        public void EnemyProvider_AndWaveProvider_UseCustomAuthoringSurfaces()
        {
            var enemyProvider = new EnemyAuthoringProvider();
            var waveProvider = new WaveAuthoringProvider();

            Assert.That(enemyProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(waveProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
        }

        [Test]
        public void EnemyProviderV2ListModel_ClassifiesRoles()
        {
            EnemyDefinitionAsset basic = EnemyDefinitionAsset.CreateTransient("enemy.test.basic", "Basic", EnemyRole.Basic, 8f, 2f, 1, 3f, Physical.Value);
            EnemyDefinitionAsset fast = EnemyDefinitionAsset.CreateTransient("enemy.test.fast", "Fast", EnemyRole.Fast, 8f, 3f, 1, 3f, Physical.Value);
            EnemyDefinitionAsset tank = EnemyDefinitionAsset.CreateTransient("enemy.test.tank", "Tank", EnemyRole.Tank, 20f, 1.2f, 2, 5f, Physical.Value);
            EnemyDefinitionAsset swarm = EnemyDefinitionAsset.CreateTransient("enemy.test.swarm", "Swarm", EnemyRole.Swarm, 4f, 2.8f, 1, 1f, Physical.Value);
            EnemyDefinitionAsset boss = EnemyDefinitionAsset.CreateTransient("enemy.test.boss", "Boss", EnemyRole.Boss, 60f, 1.4f, 8, 9f, Physical.Value);
            EnemyDefinitionAsset custom = EnemyDefinitionAsset.CreateTransient("enemy.test.custom", "Custom", (EnemyRole)999, 12f, 2f, 1, 3f, Physical.Value);
            try
            {
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(basic), Is.EqualTo("Basic"));
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(fast), Is.EqualTo("Fast"));
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(tank), Is.EqualTo("Tank"));
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(swarm), Is.EqualTo("Swarm"));
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(boss), Is.EqualTo("Boss"));
                Assert.That(EnemyProviderV2ListItem.GetRoleLabelForTests(custom), Is.EqualTo("Custom"));
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(basic);
                EnemyDefinitionAssetCreator.DestroyTransient(fast);
                EnemyDefinitionAssetCreator.DestroyTransient(tank);
                EnemyDefinitionAssetCreator.DestroyTransient(swarm);
                EnemyDefinitionAssetCreator.DestroyTransient(boss);
                EnemyDefinitionAssetCreator.DestroyTransient(custom);
            }
        }

        [Test]
        public void EnemyProviderV2Preview_EventRowsUseGlobalControlsOnly()
        {
            Assert.That(EnemyProviderV2PreviewModel.EventRowsExposePreviewActions, Is.False);
            Assert.That(EnemyProviderV2PreviewModel.ExposesRedundantSelectButton, Is.False);
        }

        [Test]
        public void EnemyProviderV2Preview_DraftAndUnsavedScopesExposeCompactChips()
        {
            var draft = new EnemyAuthoringState();
            var previewState = new EnemyProviderV2State
            {
                PreviewMuted = true,
                PreviewLoop = true,
                PreviewSpeed = 1f,
                PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Game
            };

            EnemyProviderV2PreviewScope draftScope = EnemyProviderV2PreviewModel.GetScope(true, false);
            IReadOnlyList<DeucarianEditorStatusChip> draftChips = EnemyProviderV2PreviewModel.BuildChips(draft, previewState, draftScope);

            Assert.That(EnemyProviderV2PreviewModel.GetScopeLabel(draftScope), Is.EqualTo("Draft"));
            Assert.That(EnemyProviderV2PreviewModel.BuildHeaderTitle(draft, draftScope), Is.EqualTo("Preview Lab - New Enemy Draft"));
            AssertChip(draftChips, "Draft Preview", DeucarianEditorStatus.Info);
            AssertChip(draftChips, "No Model", DeucarianEditorStatus.Error);
            AssertChip(draftChips, "Muted", DeucarianEditorStatus.Disabled);
            AssertChip(draftChips, "Game", DeucarianEditorStatus.Success);

            draft.DisplayName = "Live Draft Enemy";
            Assert.That(EnemyProviderV2PreviewModel.BuildHeaderTitle(draft, draftScope), Is.EqualTo("Preview Lab - Live Draft Enemy"));

            EnemyProviderV2PreviewScope unsavedScope = EnemyProviderV2PreviewModel.GetScope(false, true);
            IReadOnlyList<DeucarianEditorStatusChip> unsavedChips = EnemyProviderV2PreviewModel.BuildChips(draft, previewState, unsavedScope);

            Assert.That(EnemyProviderV2PreviewModel.GetScopeLabel(unsavedScope), Is.EqualTo("Unsaved"));
            AssertChip(unsavedChips, "Unsaved Preview", DeucarianEditorStatus.Warning);
        }

        [Test]
        public void EnemyProviderV2Preview_CreateAndProviderSelectionClearTransientPreviewState()
        {
            var state = new EnemyProviderV2State
            {
                EditingState = new EnemyAuthoringState { DisplayName = "Dirty Enemy Edit" },
                ActivePreviewKey = "selected.enemy"
            };

            state.BeginCreate();

            Assert.That(state.Creating, Is.True);
            Assert.That(state.WizardStep, Is.EqualTo(0));
            Assert.That(state.EditingState, Is.Null);
            Assert.That(state.PreviewStatus, Is.EqualTo("Previewing draft"));

            state.LeaveCreate();

            Assert.That(state.Creating, Is.False);
            Assert.That(state.PreviewStatus, Is.EqualTo("Previewing selected enemy"));

            var provider = new EnemyAuthoringProvider();
            EnemyProviderV2State providerState = GetEnemyProviderV2State(provider);
            providerState.BeginCreate();
            providerState.ActivePreviewKey = "__draft_enemy__";
            providerState.EditingState = new EnemyAuthoringState { DisplayName = "Unsaved Draft" };

            provider.OnSelected();

            Assert.That(providerState.Creating, Is.False);
            Assert.That(providerState.ActivePreviewKey, Is.Empty);
            Assert.That(providerState.EditingState, Is.Null);
            Assert.That(providerState.PreviewStatus, Is.EqualTo("Preview idle"));
        }

        [Test]
        public void SelectedEnemyPreviewState_UsesSelectedAssetData()
        {
            var prefab = new GameObject("DustMitePreviewPrefab");
            EnemyDefinitionAsset selected = EnemyDefinitionAsset.CreateTransient(
                "enemy.moss.dust-mite",
                "Dust Mite",
                EnemyRole.Fast,
                6f,
                3.5f,
                1,
                2f,
                "damage.moss.dust",
                prefab: prefab);
            try
            {
                var provider = new EnemyAuthoringProvider();
                var selection = new GameContentAuthoringPreviewSelection(
                    provider.ProviderId,
                    selected.DisplayName,
                    selected.Id,
                    "Enemies",
                    "Assets/GameContent/Enemies/enemy.moss.dust-mite.asset",
                    selected);
                var context = new GameContentAuthoringPreviewContext(null, provider, selectedExistingItem: selection);

                EnemyAuthoringState state = AttackGameContentPreviewSelection.ResolveEnemyState(context, new EnemyAuthoringState());
                IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildEnemyRows(state);
                string flattened = FlattenRows(rows);

                Assert.AreEqual("enemy.moss.dust-mite", state.EnemyId);
                Assert.AreEqual("Dust Mite", state.DisplayName);
                Assert.AreEqual(EnemyRole.Fast, state.Role);
                Assert.AreSame(prefab, state.Prefab);
                AssertRowContains(rows, "ID", "enemy.moss.dust-mite");
                AssertRowContains(rows, "Name", "Dust Mite");
                AssertRowContains(rows, "Speed", "3.5");
                Assert.IsFalse(flattened.Contains("enemy.example"));
                Assert.IsFalse(flattened.Contains("Basic Enemy"));
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(selected);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void EnemyProviderV2Preview_BuildsGameAndDebugPreviewFromSelectedPrefab()
        {
            var prefab = new GameObject("CableBeetlePreviewPrefab");
            try
            {
                var enemy = new EnemyAuthoringState
                {
                    DisplayName = "Cable Beetle",
                    EnemyId = "enemy.moss.cable-beetle",
                    Role = EnemyRole.Tank,
                    Prefab = prefab
                };

                GameContentAuthoringActionPreview preview = EnemyProviderV2View.BuildEnemyActionPreview(enemy, true, 5d);

                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.Mode, Is.EqualTo(GameContentAuthoringActionPreviewMode.Static));
                Assert.That(preview.PrimaryAsset, Is.SameAs(prefab));
                Assert.That(preview.TargetPrefab, Is.SameAs(prefab));
                Assert.That(preview.DeliveryTypeLabel, Is.EqualTo("Tank enemy"));
                AssertPreviewRoles(preview, "Spawn", "Enemy", "Hit");
                Assert.That(GameContentAuthoringObjectPreviewUtility.RequestsRoleLabels(preview), Is.False);
                preview.RenderMode = GameContentAuthoringActionPreviewRenderMode.Debug;
                Assert.That(GameContentAuthoringObjectPreviewUtility.RequestsRoleLabels(preview), Is.True);
                Assert.That(PreviewRole(preview, "Enemy").Asset, Is.SameAs(prefab));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void EnemyProviderV2Preview_UnsavedEditUpdatesPreviewAndRevertRestoresSavedData()
        {
            var prefab = new GameObject("EnemyUnsavedPreviewPrefab");
            EnemyDefinitionAsset asset = EnemyDefinitionAsset.CreateTransient(
                "enemy.preview.unsaved",
                "Saved Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value,
                prefab: prefab);
            try
            {
                EnemyAuthoringState edit = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                edit.DisplayName = "Unsaved Enemy";
                edit.MaximumHealth = 18f;
                edit.MoveSpeed = 4f;

                IReadOnlyList<GameContentAuthoringPreviewRow> unsavedRows = AttackGameContentPreviewSummaries.BuildEnemyRows(edit);
                EnemyProviderV2PreviewScope scope = EnemyProviderV2PreviewModel.GetScope(false, true);

                Assert.That(asset.Stats.MaximumHealth, Is.EqualTo(8f));
                Assert.That(EnemyProviderV2PreviewModel.GetScopeLabel(scope), Is.EqualTo("Unsaved"));
                AssertChip(EnemyProviderV2PreviewModel.BuildChips(edit, new EnemyProviderV2State(), scope), "Unsaved Preview", DeucarianEditorStatus.Warning);
                AssertRowContains(unsavedRows, "Name", "Unsaved Enemy");
                AssertRowContains(unsavedRows, "Health", "18");
                AssertRowContains(unsavedRows, "Speed", "4");

                EnemyAuthoringState reverted = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                IReadOnlyList<GameContentAuthoringPreviewRow> revertedRows = AttackGameContentPreviewSummaries.BuildEnemyRows(reverted);

                AssertRowContains(revertedRows, "Name", "Saved Enemy");
                AssertRowContains(revertedRows, "Health", "8");
                AssertRowContains(revertedRows, "Speed", "2");
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(asset);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void EnemyDefinitionAssetCreator_UpdateExistingAssetSavesSelectedEnemySections()
        {
            string rootFolder = "Assets/__EnemyGcaV2EditTests_" + Guid.NewGuid().ToString("N");
            GameObject prefabSource = null;
            try
            {
                string folderName = rootFolder.Substring("Assets/".Length);
                AssetDatabase.CreateFolder("Assets", folderName);
                prefabSource = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                prefabSource.name = "EnemyEditPrefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, rootFolder + "/EnemyEditPrefab.prefab");
                UnityEngine.Object.DestroyImmediate(prefabSource);
                prefabSource = null;

                var createState = new EnemyAuthoringState
                {
                    EnemyId = "enemy.test.v2-edit." + Guid.NewGuid().ToString("N"),
                    DisplayName = "Before Enemy Edit",
                    TagsCsv = "before",
                    OutputRoot = rootFolder,
                    Role = EnemyRole.Basic,
                    Prefab = prefab,
                    MaximumHealth = 8f,
                    MoveSpeed = 2f,
                    RewardValue = 1,
                    ContactDamage = 3f,
                    DamageTypeId = Physical.Value,
                    CollisionRadius = 0.3f
                };

                GameContentCreationResult created = EnemyDefinitionAssetCreator.CreateAssets(createState);
                Assert.That(created.Succeeded, Is.True, created.Message);
                var asset = (EnemyDefinitionAsset)created.CreatedRoot;
                Assert.That(asset, Is.Not.Null);

                EnemyAuthoringState editState = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                editState.DisplayName = "After Enemy Edit";
                editState.TagsCsv = "edited, enemy-v2";
                editState.Role = EnemyRole.Tank;
                editState.MaximumHealth = 24f;
                editState.MoveSpeed = 1.25f;
                editState.RewardValue = 3;
                editState.ContactDamage = 7f;
                editState.DamageTypeId = "damage.test.edited";
                editState.CollisionRadius = 0.55f;

                GameContentCreationResult saved = EnemyDefinitionAssetCreator.UpdateExistingAsset(asset, editState);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.DisplayName, Is.EqualTo("After Enemy Edit"));
                Assert.That(asset.Tags, Does.Contain("edited"));
                Assert.That(asset.Role, Is.EqualTo(EnemyRole.Tank));
                Assert.That(asset.Stats.MaximumHealth, Is.EqualTo(24f));
                Assert.That(asset.Stats.MoveSpeed, Is.EqualTo(1.25f));
                Assert.That(asset.Stats.RewardValue, Is.EqualTo(3));
                Assert.That(asset.Stats.ContactDamage, Is.EqualTo(7f));
                Assert.That(asset.Stats.DamageTypeId, Is.EqualTo("damage.test.edited"));
                Assert.That(asset.Stats.CollisionRadius, Is.EqualTo(0.55f));
                Assert.That(asset.Presentation.Prefab, Is.SameAs(prefab));
                Assert.That(EnemyDefinitionAssetCreator.ValidateForUpdate(editState, asset).IsValid, Is.True);
                IReadOnlyList<GameContentAuthoringPreviewRow> savedRows = AttackGameContentPreviewSummaries.BuildEnemyRows(AttackGameContentPreviewSelection.FromEnemyAsset(asset));
                AssertRowContains(savedRows, "Health", "24");
                AssertRowContains(savedRows, "Role", "Tank");
            }
            finally
            {
                if (prefabSource != null)
                    UnityEngine.Object.DestroyImmediate(prefabSource);
                if (AssetDatabase.IsValidFolder(rootFolder))
                    AssetDatabase.DeleteAsset(rootFolder);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void EnemyDefinitionAssetCreator_UpdateValidationBlocksMissingPrefabAndInvalidStats()
        {
            var prefab = new GameObject("EnemyValidationPrefab");
            EnemyDefinitionAsset asset = EnemyDefinitionAsset.CreateTransient(
                "enemy.validation.invalid",
                "Invalid Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value,
                prefab: prefab);
            try
            {
                EnemyAuthoringState edit = AttackGameContentPreviewSelection.FromEnemyAsset(asset);
                edit.Prefab = null;
                edit.MaximumHealth = -1f;
                edit.MoveSpeed = 0f;
                edit.RewardValue = -1;
                edit.ContactDamage = float.NaN;
                edit.DamageTypeId = string.Empty;
                edit.CollisionRadius = 0f;

                GameContentAuthoringValidationResult validation = EnemyDefinitionAssetCreator.ValidateForUpdate(edit, asset);
                GameContentCreationResult saved = EnemyDefinitionAssetCreator.UpdateExistingAsset(asset, edit);

                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.ErrorCount, Is.GreaterThanOrEqualTo(7));
                Assert.That(saved.Succeeded, Is.False);
                StringAssert.Contains("Fix validation errors", saved.Message);
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(asset);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void EnemyProviderV2Preview_MissingOptionalPresentationAssetsStayCalm()
        {
            var prefab = new GameObject("EnemyOptionalPresentationPrefab");
            try
            {
                var enemy = new EnemyAuthoringState
                {
                    EnemyId = "enemy.preview.optional",
                    DisplayName = "Optional Presentation",
                    Prefab = prefab,
                    MaximumHealth = 8f,
                    MoveSpeed = 2f
                };

                IReadOnlyList<DeucarianEditorTimelineEvent> events = EnemyProviderV2View.BuildTimelineEvents(enemy);
                IReadOnlyList<string> warnings = AttackGameContentPreviewSummaries.BuildEnemyWarnings(enemy);

                Assert.That(events.Count, Is.EqualTo(3));
                Assert.That(events[0].VisualAssigned, Is.False);
                Assert.That(events[0].AudioAssigned, Is.False);
                Assert.That(warnings, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void FullAttackPreview_BuildsBeamRolesForHitscan()
        {
            var beam = new GameObject("cursor-ray-beam");
            var impact = new GameObject("cursor-ray-impact");
            try
            {
                var attack = new AttackAuthoringState
                {
                    DisplayName = "Cursor Ray",
                    DeliveryMode = AttackRecipeDeliveryMode.Hitscan,
                    BeamVfxPrefab = beam,
                    ImpactVfxPrefab = impact
                };

                GameContentAuthoringActionPreview preview = AttackGameContentPreviewActions.BuildActionPreview(attack, false, 0d);

                Assert.That(preview.Mode, Is.EqualTo(GameContentAuthoringActionPreviewMode.Hitscan));
                Assert.That(preview.DeliveryTypeLabel, Is.EqualTo("Beam"));
                Assert.That(preview.BeamVfxPrefab, Is.SameAs(beam));
                Assert.That(preview.ImpactVfxPrefab, Is.SameAs(impact));
                AssertPreviewRoles(preview, "Source", "Beam", "Impact");
                Assert.That(PreviewRole(preview, "Beam").Label, Is.EqualTo("cursor-ray-beam"));
                Assert.That(GameContentAuthoringObjectPreviewUtility.BuildRoleLabelContent(PreviewRole(preview, "Beam")).text, Is.EqualTo("Beam: cursor-ray-beam"));
                Assert.That(PreviewRole(preview, "Beam").Asset, Is.SameAs(beam));
                Assert.That(PreviewRole(preview, "Impact").Asset, Is.SameAs(impact));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(beam);
                UnityEngine.Object.DestroyImmediate(impact);
            }
        }

        [Test]
        public void FullAttackPreview_BuildsHomingProjectileRoleFromSelectedPrefab()
        {
            var projectile = new GameObject("projectile-moss-seeker");
            try
            {
                var attack = new AttackAuthoringState
                {
                    DisplayName = "Moss Seeker",
                    DeliveryMode = AttackRecipeDeliveryMode.Projectile,
                    Homing = true,
                    ProjectilePrefab = projectile
                };

                GameContentAuthoringActionPreview preview = AttackGameContentPreviewActions.BuildActionPreview(attack, false, 0d);

                Assert.That(preview.DeliveryTypeLabel, Is.EqualTo("Homing Projectile"));
                AssertPreviewRoles(preview, "Source", "Projectile", "Target");
                Assert.That(PreviewRole(preview, "Projectile").Label, Does.Contain("projectile-moss-seeker"));
                Assert.That(PreviewRole(preview, "Projectile").Asset, Is.SameAs(projectile));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectile);
            }
        }

        [Test]
        public void FullAttackPreview_BuildsAreaAndStatusRoles()
        {
            var areaImpact = new GameObject("spore-pop-impact");
            var statusTick = new GameObject("sticky-bloom-tick");
            try
            {
                var area = new AttackAuthoringState
                {
                    DisplayName = "Spore Pop",
                    DeliveryMode = AttackRecipeDeliveryMode.Area,
                    ImpactVfxPrefab = areaImpact
                };
                var status = new AttackAuthoringState
                {
                    DisplayName = "Sticky Bloom",
                    DeliveryMode = AttackRecipeDeliveryMode.Aura,
                    TickVfxPrefab = statusTick
                };

                GameContentAuthoringActionPreview areaPreview = AttackGameContentPreviewActions.BuildActionPreview(area, false, 0d);
                GameContentAuthoringActionPreview statusPreview = AttackGameContentPreviewActions.BuildActionPreview(status, false, 0d);

                Assert.That(areaPreview.DeliveryTypeLabel, Is.EqualTo("AOE"));
                AssertPreviewRoles(areaPreview, "Origin", "Radius", "Targets");
                Assert.That(PreviewRole(areaPreview, "Radius").Asset, Is.SameAs(areaImpact));

                Assert.That(statusPreview.DeliveryTypeLabel, Is.EqualTo("Status"));
                AssertPreviewRoles(statusPreview, "Status Area", "Tick", "Target");
                Assert.That(PreviewRole(statusPreview, "Tick").Asset, Is.SameAs(statusTick));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(areaImpact);
                UnityEngine.Object.DestroyImmediate(statusTick);
            }
        }

        [Test]
        public void FullAttackPreview_CarriesSporePopProjectilePrefabIntoGamePreview()
        {
            var projectile = new GameObject("spore-pop-projectile");
            try
            {
                var attack = new AttackAuthoringState
                {
                    DisplayName = "Spore Pop",
                    DeliveryMode = AttackRecipeDeliveryMode.Projectile,
                    ProjectilePrefab = projectile
                };

                GameContentAuthoringActionPreview preview = AttackGameContentPreviewActions.BuildActionPreview(attack, false, 0d);

                Assert.That(preview.RenderMode, Is.EqualTo(GameContentAuthoringActionPreviewRenderMode.Game));
                Assert.That(preview.ProjectilePrefab, Is.SameAs(projectile));
                Assert.That(preview.PrimaryAsset, Is.SameAs(projectile));
                Assert.That(GameContentAuthoringObjectPreviewUtility.RequestsRoleLabels(preview), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectile);
            }
        }

        [Test]
        public void PreviewContext_ResolvesWeaponSourceAndContentSetEnemyTarget()
        {
            var attackAsset = ScriptableObject.CreateInstance<AttackDefinitionAsset>();
            var weaponAsset = ScriptableObject.CreateInstance<PreviewContextAsset>();
            var enemyAsset = ScriptableObject.CreateInstance<PreviewContextAsset>();
            var libraryEnemyAsset = ScriptableObject.CreateInstance<PreviewContextAsset>();
            var contentSetAsset = ScriptableObject.CreateInstance<PreviewContextAsset>();
            var weaponPrefab = new GameObject("Neutral Cursor Tower");
            var enemyPrefab = new GameObject("Runner Target");
            var libraryEnemyPrefab = new GameObject("Alpha Library Target");
            try
            {
                weaponAsset.Presentation = ScriptableObject.CreateInstance<PreviewContextPresentation>();
                enemyAsset.Presentation = ScriptableObject.CreateInstance<PreviewContextPresentation>();
                libraryEnemyAsset.Presentation = ScriptableObject.CreateInstance<PreviewContextPresentation>();
                weaponAsset.Presentation.Prefab = weaponPrefab;
                enemyAsset.Presentation.Prefab = enemyPrefab;
                libraryEnemyAsset.Presentation.Prefab = libraryEnemyPrefab;

                GameContentLibraryItem attack = CreateLibraryItem("attack.cursor", attackAsset, GameContentLibraryKind.Attack, "Cursor Ray");
                GameContentLibraryItem weapon = CreateLibraryItem("weapon.cursor", weaponAsset, GameContentLibraryKind.Weapon, "Cursor Tower");
                GameContentLibraryItem enemy = CreateLibraryItem("enemy.runner", enemyAsset, GameContentLibraryKind.Enemy, "Runner");
                GameContentLibraryItem libraryEnemy = CreateLibraryItem("enemy.alpha", libraryEnemyAsset, GameContentLibraryKind.Enemy, "Alpha Library Enemy");
                GameContentLibraryItem contentSet = CreateLibraryItem("set.basic", contentSetAsset, GameContentLibraryKind.ContentSet, "Basic Content Set");
                AddReverseReference(attack, weapon);
                AddDirectReference(contentSet, weapon);
                AddDirectReference(contentSet, enemy);

                IReadOnlyList<AttackGameContentPreviewContextOption> sources = AttackGameContentPreviewContext.BuildSourceOptions(attack);
                IReadOnlyList<AttackGameContentPreviewContextOption> targets = AttackGameContentPreviewContext.BuildTargetOptions(attack, new[] { attack, weapon, libraryEnemy, enemy, contentSet });

                Assert.That(sources[0].Prefab, Is.SameAs(weaponPrefab));
                Assert.That(sources[0].Item.Kind, Is.EqualTo(GameContentLibraryKind.Weapon));
                Assert.That(targets[0].Prefab, Is.SameAs(enemyPrefab));
                Assert.That(targets[0].Item.Kind, Is.EqualTo(GameContentLibraryKind.Enemy));
                Assert.That(targets[0].Item, Is.SameAs(enemy));
                Assert.That(targets[0].SortPriority, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attackAsset);
                UnityEngine.Object.DestroyImmediate(weaponAsset.Presentation);
                UnityEngine.Object.DestroyImmediate(enemyAsset.Presentation);
                UnityEngine.Object.DestroyImmediate(libraryEnemyAsset.Presentation);
                UnityEngine.Object.DestroyImmediate(weaponAsset);
                UnityEngine.Object.DestroyImmediate(enemyAsset);
                UnityEngine.Object.DestroyImmediate(libraryEnemyAsset);
                UnityEngine.Object.DestroyImmediate(contentSetAsset);
                UnityEngine.Object.DestroyImmediate(weaponPrefab);
                UnityEngine.Object.DestroyImmediate(enemyPrefab);
                UnityEngine.Object.DestroyImmediate(libraryEnemyPrefab);
            }
        }

        [Test]
        public void PreviewContext_ReportsNeutralFallbackStatus()
        {
            IReadOnlyList<AttackGameContentPreviewContextOption> sources = AttackGameContentPreviewContext.BuildSourceOptions(null);
            IReadOnlyList<AttackGameContentPreviewContextOption> targets = AttackGameContentPreviewContext.BuildTargetOptions(null, Array.Empty<GameContentLibraryItem>());

            Assert.That(sources[0].Fallback, Is.True);
            Assert.That(targets[0].Fallback, Is.True);
            Assert.That(AttackGameContentPreviewContext.BuildFallbackStatus(sources[0], targets[0]), Does.Contain("neutral source and target fallbacks"));
        }

        [Test]
        public void PreviewContext_SourceContainsNoThemedSortingPreferences()
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AttackGameContentPreviewContext).Assembly).resolvedPath;
            string source = File.ReadAllText(Path.Combine(packageRoot, "Editor", "AttackGameContentPreviewContext.cs"));

            Assert.That(source, Does.Not.Contain("moss dust mite"));
            Assert.That(source, Does.Not.Contain("dust mite"));
        }

        [Test]
        public void PreviewTimelineAudio_DoesNotRequestPlaybackWhenMuted()
        {
            Assert.That(AttackGameContentPreviewActions.ShouldPreviewTimelineAudio(true, true, -1, 0), Is.False);
            Assert.That(AttackGameContentPreviewActions.ShouldPreviewTimelineAudio(false, true, -1, 0), Is.True);
            Assert.That(AttackGameContentPreviewActions.ShouldPreviewTimelineAudio(false, true, 0, 0), Is.False);
            Assert.That(AttackGameContentPreviewActions.ShouldPreviewTimelineAudio(false, false, -1, 0), Is.False);
        }

        [Test]
        public void FullAttackPreview_MapsNonProjectileDeliveryModesToViewportActions()
        {
            Assert.That(BuildPreviewMode(AttackRecipeDeliveryMode.Hitscan), Is.EqualTo(GameContentAuthoringActionPreviewMode.Hitscan));
            Assert.That(BuildPreviewMode(AttackRecipeDeliveryMode.Area), Is.EqualTo(GameContentAuthoringActionPreviewMode.Area));
            Assert.That(BuildPreviewMode(AttackRecipeDeliveryMode.Aura), Is.EqualTo(GameContentAuthoringActionPreviewMode.Aura));
        }

        [Test]
        public void WavePreviewSummaries_ReportMissingEnemyReferencesAndTiming()
        {
            var state = new WaveAuthoringState();
            state.Entries.Clear();
            state.Entries.Add(new WaveEntryAuthoringState { Count = 5, BatchSize = 2, InitialDelayTicks = 3, IntervalTicks = 10, SpawnChannelId = "perimeter-west", ScalingTier = 2 });

            IReadOnlyList<GameContentAuthoringPreviewRow> rows = AttackGameContentPreviewSummaries.BuildWaveRows(state);
            IReadOnlyList<GameContentAuthoringPreviewTimelineItem> timeline = AttackGameContentPreviewSummaries.BuildWaveTimeline(state);
            IReadOnlyList<string> warnings = AttackGameContentPreviewSummaries.BuildWaveWarnings(state);

            AssertRowContains(rows, "Total Enemies", "5");
            AssertRowContains(rows, "Approx Duration", "23 tick(s)");
            Assert.AreEqual(1, timeline.Count);
            StringAssert.Contains("Missing enemy", timeline[0].Label);
            Assert.That(warnings, Has.Some.Contains("enemy reference is missing"));
        }

        [Test]
        public void WaveAuthoringProvider_UsesCustomV2Surface()
        {
            var provider = new WaveAuthoringProvider();

            Assert.That(provider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(WaveProviderV2PreviewModel.ExposesRedundantSelectButton, Is.False);
            Assert.That(GetWaveProviderV2State(provider), Is.Not.Null);
        }

        [Test]
        public void EnemyProvider_WaveProvider_AndContentLibraryUseV2Surfaces()
        {
            var enemyProvider = new EnemyAuthoringProvider();
            var waveProvider = new WaveAuthoringProvider();
            IGameContentAuthoringProvider libraryProvider = GameContentAuthoringProviderRegistry.Providers.FirstOrDefault(provider => provider.ProviderId == "com.deucarian.game-content-authoring.content-library");

            Assert.That(enemyProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(waveProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            if (libraryProvider != null)
                Assert.That(libraryProvider, Is.InstanceOf<IGameContentAuthoringSurfaceProvider>());
            Assert.That(GameContentLibraryV2UiContract.MainRowActionLabels, Does.Not.Contain("Select"));
        }

        [Test]
        public void WaveProviderV2ListModel_ClassifiesEnemyCountsDurationChannelsAndSearch()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient("enemy.wave.list", "Dust Mite", EnemyRole.Basic, 8f, 2f, 1, 3f, Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.moss.desk-dust",
                "Desk Dust",
                4,
                new[]
                {
                    new WaveEntryRecipe(enemy, 5, 2, 3, 10, "perimeter-west", 2),
                    new WaveEntryRecipe(enemy, 3, 1, 8, 4, "perimeter-east", 1)
                });
            try
            {
                GameContentLibraryItem item = CreateLibraryItem("wave.moss.desk-dust", wave, GameContentLibraryKind.Wave, "Desk Dust");
                WaveProviderV2ListItem model = WaveProviderV2ListItem.FromItem(item);

                Assert.That(model.TotalEnemyCount, Is.EqualTo(8));
                Assert.That(model.DurationTicks, Is.EqualTo(23));
                Assert.That(model.ChannelTooltip, Does.Contain("perimeter-west"));
                Assert.That(model.EnemyMix, Does.Contain("Dust Mite x8"));
                Assert.That(model.Matches("desk"), Is.True);
                Assert.That(model.Matches("perimeter-east"), Is.True);
                Assert.That(model.Matches("23"), Is.True);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(wave);
                EnemyDefinitionAssetCreator.DestroyTransient(enemy);
            }
        }

        [Test]
        public void WaveProviderV2Preview_ScopesAndChipsExposeDraftUnsavedAndDebug()
        {
            var state = new WaveAuthoringState { WaveId = "wave.preview", DisplayName = "Preview Wave" };
            state.Entries.Clear();
            state.Entries.Add(new WaveEntryAuthoringState { Count = 4, BatchSize = 1, InitialDelayTicks = 0, IntervalTicks = 6, SpawnChannelId = "perimeter-north" });
            var preview = new WaveProviderV2State { PreviewRenderMode = GameContentAuthoringActionPreviewRenderMode.Debug, PreviewSpeed = 2f };

            Assert.That(WaveProviderV2PreviewModel.GetScopeLabel(true, false), Is.EqualTo("Draft"));
            Assert.That(WaveProviderV2PreviewModel.GetScopeLabel(false, true), Is.EqualTo("Unsaved"));
            AssertChip(WaveProviderV2PreviewModel.BuildChips(state, preview), "Debug", DeucarianEditorStatus.Warning);
            AssertChip(WaveProviderV2PreviewModel.BuildChips(state, preview), "2x", DeucarianEditorStatus.Info);
            Assert.That(WaveProviderV2View.BuildEnemyMixSummary(state), Does.Contain("Missing enemy x4"));
            Assert.That(WaveProviderV2View.BuildChannelSummary(state), Is.EqualTo("perimeter-north"));
        }

        [Test]
        public void WaveProviderV2Preview_DraftFieldChangesUpdateFingerprintAndPreview()
        {
            var state = new WaveAuthoringState { WaveId = "wave.preview.changing", DisplayName = "Changing Wave" };
            state.Entries.Clear();
            state.Entries.Add(new WaveEntryAuthoringState { Count = 3, BatchSize = 1, InitialDelayTicks = 0, IntervalTicks = 10, SpawnChannelId = "perimeter-north" });

            string before = WaveProviderV2View.BuildStateFingerprint(state);
            int durationBefore = WaveProviderV2View.GetApproximateDurationTicks(state);
            state.Entries[0].Count = 6;
            state.Entries[0].IntervalTicks = 5;
            state.Entries[0].SpawnChannelId = "perimeter-east";
            string after = WaveProviderV2View.BuildStateFingerprint(state);

            Assert.That(after, Is.Not.EqualTo(before));
            Assert.That(WaveProviderV2View.GetTotalEnemyCount(state), Is.EqualTo(6));
            Assert.That(WaveProviderV2View.GetApproximateDurationTicks(state), Is.Not.EqualTo(durationBefore));
            Assert.That(WaveProviderV2View.BuildChannelSummary(state), Is.EqualTo("perimeter-east"));
        }

        [Test]
        public void WaveDefinitionAssetCreator_UpdateExistingAssetSavesSelectedWaveSections()
        {
            string rootFolder = "Assets/__WaveGcaV2EditTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(rootFolder));
            try
            {
                EnemyDefinitionAsset enemy = CreatePersistedEnemy(rootFolder, "enemy.wave.save.before", "Before Enemy");
                EnemyDefinitionAsset enemyAfter = CreatePersistedEnemy(rootFolder, "enemy.wave.save.after", "After Enemy");
                var createState = new WaveAuthoringState
                {
                    WaveId = "wave.test.v2-edit." + Guid.NewGuid().ToString("N"),
                    DisplayName = "Before Wave",
                    TagsCsv = "before",
                    OutputRoot = rootFolder,
                    StartTick = 2
                };
                createState.Entries.Clear();
                createState.Entries.Add(new WaveEntryAuthoringState { Enemy = enemy, Count = 3, BatchSize = 1, InitialDelayTicks = 1, IntervalTicks = 9, SpawnChannelId = "perimeter-north" });
                GameContentCreationResult created = WaveDefinitionAssetCreator.CreateAssets(createState);
                Assert.That(created.Succeeded, Is.True, created.Message);
                var asset = (WaveDefinitionAsset)created.CreatedRoot;

                WaveAuthoringState edit = AttackGameContentPreviewSelection.FromWaveAsset(asset);
                edit.DisplayName = "After Wave";
                edit.TagsCsv = "after";
                edit.StartTick = 5;
                edit.Entries[0].Enemy = enemyAfter;
                edit.Entries[0].Count = 7;
                edit.Entries[0].BatchSize = 2;
                edit.Entries[0].InitialDelayTicks = 3;
                edit.Entries[0].IntervalTicks = 6;
                edit.Entries[0].SpawnChannelId = "perimeter-east";
                edit.Entries[0].ScalingTier = 2;

                GameContentCreationResult saved = WaveDefinitionAssetCreator.UpdateExistingAsset(asset, edit);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.DisplayName, Is.EqualTo("After Wave"));
                Assert.That(asset.Schedule.StartTick, Is.EqualTo(5));
                Assert.That(asset.Entries.Entries[0].Enemy, Is.EqualTo(enemyAfter));
                Assert.That(asset.Entries.Entries[0].Count, Is.EqualTo(7));
                Assert.That(asset.Entries.Entries[0].SpawnChannelId, Is.EqualTo("perimeter-east"));
                Assert.That(WaveDefinitionAssetCreator.ValidateForUpdate(AttackGameContentPreviewSelection.FromWaveAsset(asset), asset).IsValid, Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(rootFolder);
            }
        }

        [Test]
        public void WaveProviderV2RevertReloadsSavedWaveData()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient("enemy.wave.revert", "Saved Enemy", EnemyRole.Basic, 8f, 2f, 1, 3f, Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.revert",
                "Saved Wave",
                2,
                new[] { new WaveEntryRecipe(enemy, 3, 1, 0, 10, "perimeter-north") });
            try
            {
                WaveAuthoringState edit = AttackGameContentPreviewSelection.FromWaveAsset(wave);
                edit.DisplayName = "Unsaved Wave";
                edit.StartTick = 99;
                edit.Entries[0].Count = 42;
                string dirtyFingerprint = WaveProviderV2View.BuildStateFingerprint(edit);

                WaveAuthoringState reverted = AttackGameContentPreviewSelection.FromWaveAsset(wave);

                Assert.That(WaveProviderV2View.BuildStateFingerprint(reverted), Is.Not.EqualTo(dirtyFingerprint));
                Assert.That(reverted.DisplayName, Is.EqualTo("Saved Wave"));
                Assert.That(reverted.StartTick, Is.EqualTo(2));
                Assert.That(reverted.Entries[0].Count, Is.EqualTo(3));
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(wave);
                EnemyDefinitionAssetCreator.DestroyTransient(enemy);
            }
        }

        [Test]
        public void WaveProviderV2State_ProviderSwitchClearsDraftAndUnsavedPreviewState()
        {
            var provider = new WaveAuthoringProvider();
            WaveProviderV2State state = GetWaveProviderV2State(provider);
            WaveDefinitionAsset transientWave = WaveDefinitionAsset.CreateTransient(
                "wave.switch",
                "Switch Wave",
                0,
                new[] { new WaveEntryRecipe(null, 1, 1, 0, 1, "perimeter-north") });
            state.BeginCreate();
            state.EditingState = new WaveAuthoringState { DisplayName = "Dirty Wave" };
            try
            {
                state.EditingContext = new GameContentAuthoringObjectEditorContext(CreateLibraryItem("wave.switch", transientWave, GameContentLibraryKind.Wave, "Switch Wave"), "saved");
                state.PreviewStatus = "Previewing unsaved edit";

                provider.OnSelected();

                Assert.That(state.Creating, Is.False);
                Assert.That(state.EditingState, Is.Null);
                Assert.That(state.EditingContext, Is.Null);
                Assert.That(state.PreviewStatus, Is.EqualTo("Preview idle"));
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(transientWave);
            }
        }

        [Test]
        public void WaveDefinitionAssetCreator_UpdateValidationBlocksMissingEnemyInvalidTimingAndChannel()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient("enemy.wave.valid", "Valid Enemy", EnemyRole.Basic, 8f, 2f, 1, 3f, Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.invalid.update",
                "Invalid Update",
                0,
                new[] { new WaveEntryRecipe(enemy, 3, 1, 0, 10, "perimeter-north") });
            try
            {
                WaveAuthoringState edit = AttackGameContentPreviewSelection.FromWaveAsset(wave);
                edit.StartTick = -1;
                edit.Entries[0].Enemy = null;
                edit.Entries[0].Count = 0;
                edit.Entries[0].BatchSize = 0;
                edit.Entries[0].IntervalTicks = -1;
                edit.Entries[0].SpawnChannelId = string.Empty;

                GameContentAuthoringValidationResult validation = WaveDefinitionAssetCreator.ValidateForUpdate(edit, wave);
                GameContentCreationResult saved = WaveDefinitionAssetCreator.UpdateExistingAsset(wave, edit);

                Assert.That(validation.IsValid, Is.False);
                Assert.That(FindIssue(validation, "Entries[0].Enemy", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(FindIssue(validation, "Entries[0].SpawnChannelId", GameContentAuthoringValidationSeverity.Error), Is.True);
                Assert.That(saved.Succeeded, Is.False);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(wave);
                EnemyDefinitionAssetCreator.DestroyTransient(enemy);
            }
        }

        [Test]
        public void PreviewStop_DoesNotDirtyActiveScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool wasDirty = scene.isDirty;

            foreach (IGameContentAuthoringProvider provider in GameContentAuthoringProviderRegistry.Providers)
            {
                if (provider.ProviderId.StartsWith("com.deucarian.attacks.", StringComparison.Ordinal))
                    provider.StopPreview();
            }

            Assert.AreEqual(wasDirty, scene.isDirty);
        }

        [Test]
        public void EnemyDefinitionValidation_RequiresPrefabForAssetCreationOnly()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.basic",
                "Basic",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value);

            ContentAuthoringValidationReport runtimeReport = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);
            ContentAuthoringValidationReport creationReport = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.AssetCreation);

            Assert.IsTrue(runtimeReport.IsValid);
            Assert.IsFalse(creationReport.IsValid);
        }

        [Test]
        public void EnemyDefinitionValidation_RejectsInvalidStats()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.invalid-stats",
                "Invalid Stats",
                EnemyRole.Basic,
                -1f,
                0f,
                -1,
                float.NaN,
                string.Empty,
                collisionRadius: 0f);

            ContentAuthoringValidationReport report = EnemyDefinitionValidator.Validate(enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);

            Assert.IsFalse(report.IsValid);
            Assert.That(report.ErrorCount, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void EnemyPresentationInvoker_GracefullyHandlesMissingOptionalAssets()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.presentation",
                "Presentation",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value);

            EnemyPresentationInvocationResult result = EnemyPresentationRuntimeInvoker.Invoke(
                enemy,
                EnemyPresentationEventKind.OnSpawn,
                Vector3.zero,
                Quaternion.identity);

            Assert.IsTrue(result.Invoked);
            Assert.IsFalse(result.AudioPlayed);
            Assert.IsFalse(result.VfxSpawned);
        }

        [Test]
        public void WaveDefinitionValidation_RequiresEnemyEntries()
        {
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.invalid",
                "Invalid",
                0,
                new[] { new WaveEntryRecipe(null, 3, 1, 0, 10, "perimeter-north") });

            ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void WaveDefinitionValidation_RejectsInvalidEntryNumbers()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.valid-for-invalid-wave",
                "Valid Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.invalid-numbers",
                "Invalid Numbers",
                -1,
                new[] { new WaveEntryRecipe(enemy, 0, 0, -1, -1, string.Empty, -1) });

            ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave);

            Assert.IsFalse(report.IsValid);
            Assert.That(report.ErrorCount, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void WaveDefinitionValidation_RejectsInvalidReferencedEnemy()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                string.Empty,
                "Invalid Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.invalid-enemy",
                "Invalid Enemy Ref",
                0,
                new[] { new WaveEntryRecipe(enemy, 2, 1, 0, 10, "perimeter-north") });

            ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void WaveDefinitionValidation_AcceptsValidEnemyReference()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.fast",
                "Fast",
                EnemyRole.Fast,
                5f,
                3.4f,
                1,
                2f,
                Physical.Value);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.valid",
                "Valid",
                6,
                new[] { new WaveEntryRecipe(enemy, 4, 1, 0, 10, "perimeter-east", 1) });

            ContentAuthoringValidationReport report = WaveDefinitionValidator.Validate(wave);

            Assert.IsTrue(report.IsValid);
        }

        [Test]
        public void WaveEntryId_UsesContentIdSemantics()
        {
            var numeric = new WaveEntryId("0");
            var providerOwned = WaveEntryId.CreateNew();

            Assert.That(numeric.Value, Is.EqualTo("0"));
            Assert.That(numeric.IsValid, Is.True);
            Assert.That(providerOwned.Value, Does.StartWith("entry-"));
            Assert.That(providerOwned.IsValid, Is.True);
            Assert.Throws<ArgumentException>(() => new WaveEntryId(string.Empty));
            Assert.Throws<ArgumentException>(() => new WaveEntryId("Entry Invalid"));
        }

        [Test]
        public void WaveDefinitionValidation_RequiresValidUniqueEntryIdsAndAllowsEquivalentRows()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.authoring.entry-id",
                "Entry ID Enemy",
                EnemyRole.Basic,
                8f,
                2f,
                1,
                3f,
                Physical.Value);
            WaveDefinitionAsset valid = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.entry-id.valid",
                "Valid Entry IDs",
                0,
                new[]
                {
                    new WaveEntryRecipe("alpha", enemy, 2, 1, 0, 10, "perimeter-north"),
                    new WaveEntryRecipe("beta", enemy, 2, 1, 0, 10, "perimeter-north")
                });
            WaveDefinitionAsset missing = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.entry-id.missing",
                "Missing Entry ID",
                0,
                new[] { new WaveEntryRecipe(string.Empty, enemy, 2, 1, 0, 10, "perimeter-north") });
            WaveDefinitionAsset invalid = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.entry-id.invalid",
                "Invalid Entry ID",
                0,
                new[] { new WaveEntryRecipe("Invalid ID", enemy, 2, 1, 0, 10, "perimeter-north") });
            WaveDefinitionAsset duplicate = WaveDefinitionAsset.CreateTransient(
                "wave.authoring.entry-id.duplicate",
                "Duplicate Entry IDs",
                0,
                new[]
                {
                    new WaveEntryRecipe("same", enemy, 2, 1, 0, 10, "perimeter-north"),
                    new WaveEntryRecipe("same", enemy, 2, 1, 0, 10, "perimeter-east")
                });
            try
            {
                ContentAuthoringValidationReport validReport = WaveDefinitionValidator.Validate(valid);
                ContentAuthoringValidationReport missingReport = WaveDefinitionValidator.Validate(missing);
                ContentAuthoringValidationReport invalidReport = WaveDefinitionValidator.Validate(invalid);
                ContentAuthoringValidationReport duplicateReport = WaveDefinitionValidator.Validate(duplicate);

                Assert.That(validReport.IsValid, Is.True, "Equivalent-value rows remain legal when their stable IDs differ.");
                Assert.That(HasContentIssue(missingReport, "Entries[0].EntryId", "required"), Is.True);
                Assert.That(HasContentIssue(invalidReport, "Entries[0].EntryId", "stable content ID"), Is.True);
                Assert.That(HasContentIssue(duplicateReport, "Entries[1].EntryId", "unique"), Is.True);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(valid);
                WaveDefinitionAssetCreator.DestroyTransient(missing);
                WaveDefinitionAssetCreator.DestroyTransient(invalid);
                WaveDefinitionAssetCreator.DestroyTransient(duplicate);
                EnemyDefinitionAssetCreator.DestroyTransient(enemy);
            }
        }

        [Test]
        public void WaveAuthoringAddCopyMoveAndFieldEditOwnAndPreserveIdentity()
        {
            WaveEntryAuthoringState first = WaveEntryAuthoringState.CreateNew();
            WaveEntryAuthoringState second = WaveEntryAuthoringState.CreateNew();
            first.Count = 7;
            first.SpawnChannelId = "perimeter-west";
            WaveEntryAuthoringState copy = WaveProviderV2View.CopyEntry(first);
            var state = new WaveAuthoringState();
            state.Entries.Clear();
            state.Entries.Add(first);
            state.Entries.Add(second);
            string firstId = first.EntryId;
            string secondId = second.EntryId;

            WaveProviderV2View.MoveEntry(state, 0, 1);
            state.Entries[1].Count = 9;
            state.Entries[1].SpawnChannelId = "perimeter-east";

            Assert.That(new WaveEntryId(firstId).IsValid, Is.True);
            Assert.That(new WaveEntryId(secondId).IsValid, Is.True);
            Assert.That(secondId, Is.Not.EqualTo(firstId), "Each Add path must own a fresh identity.");
            Assert.That(copy.EntryId, Is.Not.EqualTo(firstId), "Copy must create a new identity.");
            Assert.That(copy.Count, Is.EqualTo(7));
            Assert.That(copy.SpawnChannelId, Is.EqualTo("perimeter-west"));
            Assert.That(state.Entries[0], Is.SameAs(second));
            Assert.That(state.Entries[0].EntryId, Is.EqualTo(secondId));
            Assert.That(state.Entries[1], Is.SameAs(first));
            Assert.That(state.Entries[1].EntryId, Is.EqualTo(firstId));
        }

        [Test]
        public void WaveConfigureTransientPreviewAndUpdatePreserveEntryIdentityThroughUndoRedo()
        {
            string rootFolder = "Assets/__WaveEntryIdentityTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(rootFolder));
            try
            {
                EnemyDefinitionAsset enemy = CreatePersistedEnemy(rootFolder, "enemy.wave.entry-id", "Entry ID Enemy");
                var source = new WaveEntryRecipe("semantic-row", enemy, 3, 1, 0, 10, "perimeter-north");
                var entriesSection = ScriptableObject.CreateInstance<WaveEntriesDefinitionAsset>();
                entriesSection.Configure(new[] { source });
                Assert.That(entriesSection.Entries[0], Is.Not.SameAs(source));
                Assert.That(entriesSection.Entries[0].EntryId.Value, Is.EqualTo("semantic-row"));
                UnityEngine.Object.DestroyImmediate(entriesSection);

                WaveDefinitionAsset transient = WaveDefinitionAsset.CreateTransient(
                    "wave.entry-id.transient",
                    "Transient Identity",
                    0,
                    new[] { source });
                Assert.That(transient.Entries.Entries[0].EntryId.Value, Is.EqualTo("semantic-row"));
                WaveAuthoringState previewState = AttackGameContentPreviewSelection.FromWaveAsset(transient);
                Assert.That(previewState.Entries[0].EntryId, Is.EqualTo("semantic-row"));
                Assert.That(WaveProviderV2View.BuildStateFingerprint(previewState), Does.Contain("semantic-row"));
                WaveDefinitionAssetCreator.DestroyTransient(transient);

                var createState = new WaveAuthoringState
                {
                    WaveId = "wave.entry-id.update." + Guid.NewGuid().ToString("N"),
                    DisplayName = "Before Identity Update",
                    OutputRoot = rootFolder
                };
                createState.Entries.Clear();
                createState.Entries.Add(new WaveEntryAuthoringState
                {
                    EntryId = "persisted-row",
                    Enemy = enemy,
                    Count = 3,
                    BatchSize = 1,
                    IntervalTicks = 10,
                    SpawnChannelId = "perimeter-north"
                });
                GameContentCreationResult created = WaveDefinitionAssetCreator.CreateAssets(createState);
                Assert.That(created.Succeeded, Is.True, created.Message);
                var asset = (WaveDefinitionAsset)created.CreatedRoot;
                WaveAuthoringState edit = AttackGameContentPreviewSelection.FromWaveAsset(asset);
                edit.Entries[0].Count = 8;
                edit.Entries[0].SpawnChannelId = "perimeter-east";

                GameContentCreationResult saved = WaveDefinitionAssetCreator.UpdateExistingAsset(asset, edit);

                Assert.That(saved.Succeeded, Is.True, saved.Message);
                Assert.That(asset.Entries.Entries[0].EntryId.Value, Is.EqualTo("persisted-row"));
                Assert.That(asset.Entries.Entries[0].Count, Is.EqualTo(8));
                Undo.PerformUndo();
                Assert.That(asset.Entries.Entries[0].EntryId.Value, Is.EqualTo("persisted-row"));
                Assert.That(asset.Entries.Entries[0].Count, Is.EqualTo(3));
                Undo.PerformRedo();
                Assert.That(asset.Entries.Entries[0].EntryId.Value, Is.EqualTo("persisted-row"));
                Assert.That(asset.Entries.Entries[0].Count, Is.EqualTo(8));
            }
            finally
            {
                Undo.ClearAll();
                AssetDatabase.DeleteAsset(rootFolder);
            }
        }

        [Test]
        public void WaveEntryIdMigration_AssignsLegacyIndexesOnceAndReportsConflicts()
        {
            string rootFolder = "Assets/__WaveEntryIdMigrationTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(rootFolder));
            try
            {
                WaveDefinitionAsset legacy = CreatePersistedWaveWithEntryIds(rootFolder, "wave.migration.legacy", string.Empty, string.Empty);
                WaveEntryIdMigrationReport first = WaveEntryIdMigration.MigrateAssets(new[] { legacy }, true);
                WaveEntryIdMigrationReport second = WaveEntryIdMigration.MigrateAssets(new[] { legacy }, true);

                Assert.That(first.Succeeded, Is.True, first.CreateSummary());
                Assert.That(first.MigratedAssetCount, Is.EqualTo(1));
                Assert.That(legacy.Entries.Entries[0].EntryId.Value, Is.EqualTo("0"));
                Assert.That(legacy.Entries.Entries[1].EntryId.Value, Is.EqualTo("1"));
                Assert.That(second.MigratedAssetCount, Is.EqualTo(0));
                Assert.That(second.UnchangedAssetCount, Is.EqualTo(1));

                WaveDefinitionAsset partial = CreatePersistedWaveWithEntryIds(rootFolder, "wave.migration.partial", string.Empty, "1");
                WaveDefinitionAsset invalid = CreatePersistedWaveWithEntryIds(rootFolder, "wave.migration.invalid", "Invalid ID");
                WaveDefinitionAsset duplicate = CreatePersistedWaveWithEntryIds(rootFolder, "wave.migration.duplicate", "same", "same");
                WaveEntryIdMigrationReport conflicts = WaveEntryIdMigration.MigrateAssets(new[] { partial, invalid, duplicate }, false);

                Assert.That(conflicts.Succeeded, Is.False);
                Assert.That(conflicts.ConflictAssetCount, Is.EqualTo(3));
                Assert.That(conflicts.CreateSummary(), Does.Contain("partially assigned"));
                Assert.That(conflicts.CreateSummary(), Does.Contain("invalid"));
                Assert.That(conflicts.CreateSummary(), Does.Contain("duplicate"));
                Assert.That(partial.Entries.Entries[0].EntryId.IsEmpty, Is.True);
                Assert.That(partial.Entries.Entries[1].EntryId.Value, Is.EqualTo("1"));
            }
            finally
            {
                Undo.ClearAll();
                AssetDatabase.DeleteAsset(rootFolder);
            }
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

        private static void AssertRowContains(IReadOnlyList<GameContentAuthoringPreviewRow> rows, string label, string expectedValuePart)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (!string.Equals(rows[i].Label, label, StringComparison.Ordinal)) continue;
                StringAssert.Contains(expectedValuePart, rows[i].Value);
                return;
            }

            Assert.Fail("Expected preview row '" + label + "' was not found.");
        }

        private static string FlattenRows(IReadOnlyList<GameContentAuthoringPreviewRow> rows)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                builder.Append(rows[i].Label).Append('=').Append(rows[i].Value).Append(';');
            }

            return builder.ToString();
        }

        private static GameContentAuthoringActionPreviewMode BuildPreviewMode(AttackRecipeDeliveryMode deliveryMode)
        {
            var attack = new AttackAuthoringState { DeliveryMode = deliveryMode };
            return AttackGameContentPreviewActions.BuildActionPreview(attack, false, 0d).Mode;
        }

        private static void AssertPreviewRoles(GameContentAuthoringActionPreview preview, params string[] expectedRoles)
        {
            Assert.That(preview.Roles.Select(role => role.Role).ToArray(), Is.EqualTo(expectedRoles));
        }

        private static GameContentAuthoringActionPreviewRole PreviewRole(GameContentAuthoringActionPreview preview, string role)
        {
            GameContentAuthoringActionPreviewRole match = preview.Roles.FirstOrDefault(candidate => candidate != null && string.Equals(candidate.Role, role, StringComparison.Ordinal));
            Assert.That(match, Is.Not.Null, "Expected preview role " + role + ".");
            return match;
        }

        private static void AssertChip(IReadOnlyList<DeucarianEditorStatusChip> chips, string label, DeucarianEditorStatus status)
        {
            DeucarianEditorStatusChip match = chips.FirstOrDefault(candidate => string.Equals(candidate.Label, label, StringComparison.Ordinal));
            Assert.That(match.Label, Is.EqualTo(label), "Expected preview chip " + label + ".");
            Assert.That(match.Status, Is.EqualTo(status), "Preview chip " + label + " had the wrong status.");
        }

        private static bool FindIssue(GameContentAuthoringValidationResult validation, string path, GameContentAuthoringValidationSeverity severity)
        {
            Assert.That(validation, Is.Not.Null);
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                GameContentAuthoringValidationIssue issue = validation.Issues[i];
                if (issue.Severity == severity && string.Equals(issue.Path, path, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool HasContentIssue(ContentAuthoringValidationReport validation, string path, string messageFragment)
        {
            Assert.That(validation, Is.Not.Null);
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                ContentAuthoringValidationIssue issue = validation.Issues[i];
                if (string.Equals(issue.Path, path, StringComparison.Ordinal) &&
                    issue.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static AttackProviderV2State GetProviderV2State(AttackAuthoringProvider provider)
        {
            FieldInfo field = typeof(AttackAuthoringProvider).GetField("_v2State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "AttackAuthoringProvider._v2State was not found.");
            return (AttackProviderV2State)field.GetValue(provider);
        }

        private static EnemyProviderV2State GetEnemyProviderV2State(EnemyAuthoringProvider provider)
        {
            FieldInfo field = typeof(EnemyAuthoringProvider).GetField("_v2State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "EnemyAuthoringProvider._v2State was not found.");
            return (EnemyProviderV2State)field.GetValue(provider);
        }

        private static WaveProviderV2State GetWaveProviderV2State(WaveAuthoringProvider provider)
        {
            FieldInfo field = typeof(WaveAuthoringProvider).GetField("_v2State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "WaveAuthoringProvider._v2State was not found.");
            return (WaveProviderV2State)field.GetValue(provider);
        }

        private static EnemyDefinitionAsset CreatePersistedEnemy(string rootFolder, string enemyId, string displayName)
        {
            string folder = rootFolder + "/" + enemyId;
            string rootPath = folder + "/" + enemyId + "_EnemyDefinition.asset";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(rootFolder, enemyId);

            var stats = ScriptableObject.CreateInstance<EnemyStatsDefinitionAsset>();
            stats.Configure(8f, 2f, 1, 3f, Physical.Value, 0.3f);
            var presentation = ScriptableObject.CreateInstance<EnemyPresentationDefinitionAsset>();
            presentation.Configure(null, new[]
            {
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath)
            });
            var root = ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
            root.Configure(enemyId, displayName, null, EnemyRole.Basic, Array.Empty<string>(), stats, presentation);
            AssetDatabase.CreateAsset(root, rootPath);
            AssetDatabase.AddObjectToAsset(stats, root);
            AssetDatabase.AddObjectToAsset(presentation, root);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(stats);
            EditorUtility.SetDirty(presentation);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(rootPath);
        }

        private static WaveDefinitionAsset CreatePersistedWaveWithEntryIds(string rootFolder, string waveId, params string[] entryIds)
        {
            string folder = rootFolder + "/" + waveId;
            string rootPath = folder + "/" + waveId + "_WaveDefinition.asset";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(rootFolder, waveId);

            var schedule = ScriptableObject.CreateInstance<WaveScheduleDefinitionAsset>();
            schedule.Configure(0);
            var entries = ScriptableObject.CreateInstance<WaveEntriesDefinitionAsset>();
            var recipes = new WaveEntryRecipe[entryIds.Length];
            for (int i = 0; i < entryIds.Length; i++)
                recipes[i] = new WaveEntryRecipe(entryIds[i], null, 1, 1, 0, 1, "perimeter-north");
            entries.Configure(recipes);
            var root = ScriptableObject.CreateInstance<WaveDefinitionAsset>();
            root.Configure(waveId, waveId, Array.Empty<string>(), schedule, entries);
            AssetDatabase.CreateAsset(root, rootPath);
            AssetDatabase.AddObjectToAsset(schedule, root);
            AssetDatabase.AddObjectToAsset(entries, root);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(schedule);
            EditorUtility.SetDirty(entries);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<WaveDefinitionAsset>(rootPath);
        }

        private static GameContentLibraryItem CreateLibraryItem(string key, UnityEngine.Object asset, GameContentLibraryKind kind, string displayName)
        {
            ConstructorInfo constructor = typeof(GameContentLibraryItem).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(string),
                    typeof(UnityEngine.Object),
                    typeof(GameContentLibraryKind),
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    typeof(string)
                },
                null);
            Assert.That(constructor, Is.Not.Null, "GameContentLibraryItem constructor was not found.");
            return (GameContentLibraryItem)constructor.Invoke(new object[]
            {
                key,
                asset,
                kind,
                kind.ToString(),
                "Assets/" + key + ".asset",
                key,
                displayName
            });
        }

        private static void AddDirectReference(GameContentLibraryItem source, GameContentLibraryItem target)
        {
            InvokeReferenceMethod(source, "AddDirectReference", target);
        }

        private static void AddReverseReference(GameContentLibraryItem source, GameContentLibraryItem target)
        {
            InvokeReferenceMethod(source, "AddReverseReference", target);
        }

        private static void InvokeReferenceMethod(GameContentLibraryItem source, string methodName, GameContentLibraryItem target)
        {
            MethodInfo method = typeof(GameContentLibraryItem).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " was not found.");
            method.Invoke(source, new object[] { new GameContentLibraryReference(target, "PreviewContext.Test") });
        }

        private sealed class PreviewContextAsset : ScriptableObject
        {
            public PreviewContextPresentation Presentation;
        }

        private sealed class PreviewContextPresentation : ScriptableObject
        {
            public GameObject Prefab;
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
            public SpawnResult Spawn(SpawnRequest request) => new SpawnResult(true, SpawnFailureReason.None, new SpawnInstanceId(1), null, WorldSpawnDefenseAdapter.ToWorldRequest(request));
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
