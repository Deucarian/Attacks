using System;
using System.Collections.Generic;
using Deucarian.Combat;
using Deucarian.GameplayFoundation;

namespace Deucarian.Attacks
{
    public readonly struct AttackSourceId : IEquatable<AttackSourceId>, IComparable<AttackSourceId> { private readonly ContentId _value; public AttackSourceId(string value) { _value = new ContentId(value); } public string Value => _value.Value; public bool IsEmpty => _value.IsEmpty; public bool Equals(AttackSourceId other) => _value.Equals(other._value); public override bool Equals(object obj) => obj is AttackSourceId other && Equals(other); public override int GetHashCode() => _value.GetHashCode(); public int CompareTo(AttackSourceId other) => _value.CompareTo(other._value); public override string ToString() => Value; }
    public readonly struct AttackDefinitionId : IEquatable<AttackDefinitionId>, IComparable<AttackDefinitionId> { private readonly ContentId _value; public AttackDefinitionId(string value) { _value = new ContentId(value); } public string Value => _value.Value; public bool IsEmpty => _value.IsEmpty; public bool Equals(AttackDefinitionId other) => _value.Equals(other._value); public override bool Equals(object obj) => obj is AttackDefinitionId other && Equals(other); public override int GetHashCode() => _value.GetHashCode(); public int CompareTo(AttackDefinitionId other) => _value.CompareTo(other._value); public override string ToString() => Value; }

    public enum AttackTargetPolicy { HighestScore = 0, LowestScore = 1 }
    public enum AttackIntentKind { DirectDamage = 0 }
    public enum AttackFailureReason { None = 0, UnknownSource = 1, UnknownAttack = 2, SourceDisabled = 3, NotReady = 4, NoCandidates = 5, InvalidCandidate = 6, InvalidInput = 7 }

    public sealed class AttackDefinition
    {
        private readonly StatusApplicationRequest[] _statuses;
        public AttackDefinition(AttackDefinitionId id, int cooldownTicks, DamageTypeId damageTypeId, double baseDamage, AttackTargetPolicy targetPolicy = AttackTargetPolicy.HighestScore, bool readyOnRegister = true, CombatDefenseSnapshot defense = null, IReadOnlyList<StatusApplicationRequest> statuses = null)
        {
            if (id.IsEmpty) throw new ArgumentException("Attack definition id cannot be empty.", nameof(id));
            if (cooldownTicks < 0) throw new ArgumentOutOfRangeException(nameof(cooldownTicks));
            if (damageTypeId.IsEmpty) throw new ArgumentException("Damage type id cannot be empty.", nameof(damageTypeId));
            CombatNumbers.RequireNonNegative(baseDamage, nameof(baseDamage));
            Id = id; CooldownTicks = cooldownTicks; DamageTypeId = damageTypeId; BaseDamage = baseDamage; TargetPolicy = targetPolicy; ReadyOnRegister = readyOnRegister; Defense = defense ?? new CombatDefenseSnapshot();
            _statuses = statuses == null ? Array.Empty<StatusApplicationRequest>() : Copy(statuses);
        }
        public AttackDefinitionId Id { get; }
        public int CooldownTicks { get; }
        public DamageTypeId DamageTypeId { get; }
        public double BaseDamage { get; }
        public AttackTargetPolicy TargetPolicy { get; }
        public bool ReadyOnRegister { get; }
        public CombatDefenseSnapshot Defense { get; }
        public IReadOnlyList<StatusApplicationRequest> Statuses => _statuses;
        private static StatusApplicationRequest[] Copy(IReadOnlyList<StatusApplicationRequest> source) { var copy = new StatusApplicationRequest[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    public readonly struct AttackSourceSnapshot
    {
        public AttackSourceSnapshot(AttackSourceId id, CombatantId combatantId, bool enabled = true, CombatSourceSnapshot combatSource = null, double damageMultiplier = 1d)
        {
            if (id.IsEmpty) throw new ArgumentException("Attack source id cannot be empty.", nameof(id));
            if (combatantId.IsEmpty) throw new ArgumentException("Combatant id cannot be empty.", nameof(combatantId));
            CombatNumbers.RequireNonNegative(damageMultiplier, nameof(damageMultiplier));
            Id = id; CombatantId = combatantId; Enabled = enabled; CombatSource = combatSource ?? new CombatSourceSnapshot(); DamageMultiplier = damageMultiplier;
        }
        public AttackSourceId Id { get; }
        public CombatantId CombatantId { get; }
        public bool Enabled { get; }
        public CombatSourceSnapshot CombatSource { get; }
        public double DamageMultiplier { get; }
    }

    public readonly struct AttackTargetCandidate
    {
        public AttackTargetCandidate(CombatantId combatantId, HealthState health, double score, bool valid = true, CombatDefenseSnapshot defense = null)
        {
            CombatantId = combatantId; Health = health; Score = score; Valid = valid; Defense = defense ?? new CombatDefenseSnapshot();
        }
        public CombatantId CombatantId { get; }
        public HealthState Health { get; }
        public double Score { get; }
        public bool Valid { get; }
        public CombatDefenseSnapshot Defense { get; }
        public bool IsUsable => Valid && !CombatantId.IsEmpty && Health != null && Health.Id.Equals(CombatantId) && !double.IsNaN(Score) && !double.IsInfinity(Score);
    }

    public readonly struct AttackTargetSelection
    {
        public AttackTargetSelection(bool found, AttackTargetCandidate target, double score) { Found = found; Target = target; Score = score; }
        public bool Found { get; }
        public AttackTargetCandidate Target { get; }
        public double Score { get; }
    }

    public struct AttackCooldownState
    {
        public AttackCooldownState(int remainingTicks) { if (remainingTicks < 0) throw new ArgumentOutOfRangeException(nameof(remainingTicks)); RemainingTicks = remainingTicks; }
        public int RemainingTicks { get; private set; }
        public bool IsReady => RemainingTicks <= 0;
        public void Start(int cooldownTicks) { if (cooldownTicks < 0) throw new ArgumentOutOfRangeException(nameof(cooldownTicks)); RemainingTicks = cooldownTicks; }
        public void Advance(int ticks) { if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks)); RemainingTicks = Math.Max(0, RemainingTicks - ticks); }
        public void Reset() { RemainingTicks = 0; }
    }

    public sealed class AttackIntent
    {
        public AttackIntent(AttackIntentKind kind, AttackSourceId sourceId, AttackDefinitionId definitionId, AttackTargetSelection selection, DamageRequest damageRequest, DamageResolutionRequest resolutionRequest)
        {
            Kind = kind; SourceId = sourceId; DefinitionId = definitionId; Selection = selection; DamageRequest = damageRequest; ResolutionRequest = resolutionRequest;
        }
        public AttackIntentKind Kind { get; }
        public AttackSourceId SourceId { get; }
        public AttackDefinitionId DefinitionId { get; }
        public AttackTargetSelection Selection { get; }
        public DamageRequest DamageRequest { get; }
        public DamageResolutionRequest ResolutionRequest { get; }
    }

    public readonly struct AttackResult
    {
        public AttackResult(bool succeeded, AttackFailureReason failureReason, AttackIntent intent, AttackCooldownState cooldown)
        {
            Succeeded = succeeded; FailureReason = failureReason; Intent = intent; Cooldown = cooldown;
        }
        public bool Succeeded { get; }
        public AttackFailureReason FailureReason { get; }
        public AttackIntent Intent { get; }
        public AttackCooldownState Cooldown { get; }
    }

    public interface IAttackTargetSelector { AttackTargetSelection Select(AttackDefinition definition, IReadOnlyList<AttackTargetCandidate> candidates, out AttackFailureReason failureReason); }
    public interface IAttackDamageRequestFactory { DamageResolutionRequest Create(CombatCatalog catalog, AttackDefinition definition, AttackSourceSnapshot source, AttackTargetSelection selection); }

    public sealed class DeterministicAttackTargetSelector : IAttackTargetSelector
    {
        public AttackTargetSelection Select(AttackDefinition definition, IReadOnlyList<AttackTargetCandidate> candidates, out AttackFailureReason failureReason)
        {
            failureReason = AttackFailureReason.None;
            if (definition == null || candidates == null) { failureReason = AttackFailureReason.InvalidInput; return default; }
            if (candidates.Count == 0) { failureReason = AttackFailureReason.NoCandidates; return default; }
            bool found = false; AttackTargetCandidate best = default; double bestScore = 0d;
            for (int i = 0; i < candidates.Count; i++)
            {
                AttackTargetCandidate candidate = candidates[i];
                if (!candidate.IsUsable) continue;
                bool better = !found || IsBetter(definition.TargetPolicy, candidate.Score, candidate.CombatantId, bestScore, best.CombatantId);
                if (better) { found = true; best = candidate; bestScore = candidate.Score; }
            }
            if (!found) { failureReason = AttackFailureReason.InvalidCandidate; return default; }
            return new AttackTargetSelection(true, best, bestScore);
        }
        private static bool IsBetter(AttackTargetPolicy policy, double score, CombatantId id, double bestScore, CombatantId bestId)
        {
            if (policy == AttackTargetPolicy.LowestScore)
                return score < bestScore || score.Equals(bestScore) && id.CompareTo(bestId) < 0;
            return score > bestScore || score.Equals(bestScore) && id.CompareTo(bestId) < 0;
        }
    }

    public sealed class DirectDamageRequestFactory : IAttackDamageRequestFactory
    {
        public DamageResolutionRequest Create(CombatCatalog catalog, AttackDefinition definition, AttackSourceSnapshot source, AttackTargetSelection selection)
        {
            if (catalog == null || definition == null || !selection.Found) return new DamageResolutionRequest(catalog, null, null, null);
            double amount = definition.BaseDamage * source.DamageMultiplier;
            var damage = new DamageRequest(selection.Target.CombatantId, new[] { new DamageComponent(definition.DamageTypeId, amount) }, source.CombatSource, selection.Target.Defense, source.CombatantId, definition.Statuses);
            return new DamageResolutionRequest(catalog, selection.Target.Health, null, damage);
        }
    }

    public readonly struct AttackSourceStatMapping
    {
        public AttackSourceStatMapping(StatId criticalChance, StatId criticalMultiplier, StatId flatPenetration, StatId percentPenetration, StatId damageMultiplier)
        {
            CriticalChance = criticalChance; CriticalMultiplier = criticalMultiplier; FlatPenetration = flatPenetration; PercentPenetration = percentPenetration; DamageMultiplier = damageMultiplier;
        }
        public StatId CriticalChance { get; }
        public StatId CriticalMultiplier { get; }
        public StatId FlatPenetration { get; }
        public StatId PercentPenetration { get; }
        public StatId DamageMultiplier { get; }
    }

    public static class AttackSourceStatAdapter
    {
        public static AttackSourceSnapshot FromStats(AttackSourceId sourceId, CombatantId combatantId, StatBlock stats, AttackSourceStatMapping mapping, bool enabled = true)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            double multiplier = mapping.DamageMultiplier.IsEmpty ? 1d : stats.GetValue(mapping.DamageMultiplier);
            var combatSource = new CombatSourceSnapshot(
                mapping.CriticalChance.IsEmpty ? 0d : stats.GetValue(mapping.CriticalChance),
                mapping.CriticalMultiplier.IsEmpty ? 1.5d : stats.GetValue(mapping.CriticalMultiplier),
                mapping.FlatPenetration.IsEmpty ? 0d : stats.GetValue(mapping.FlatPenetration),
                mapping.PercentPenetration.IsEmpty ? 0d : stats.GetValue(mapping.PercentPenetration));
            return new AttackSourceSnapshot(sourceId, combatantId, enabled, combatSource, multiplier);
        }
    }

    public sealed class AttackSnapshot
    {
        public AttackSnapshot(IReadOnlyList<AttackSourceSnapshot> sources, IReadOnlyList<AttackCooldownSnapshot> cooldowns)
        {
            Sources = Copy(sources); Cooldowns = Copy(cooldowns);
        }
        public IReadOnlyList<AttackSourceSnapshot> Sources { get; }
        public IReadOnlyList<AttackCooldownSnapshot> Cooldowns { get; }
        private static T[] Copy<T>(IReadOnlyList<T> source) { if (source == null) return Array.Empty<T>(); var copy = new T[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    public readonly struct AttackCooldownSnapshot
    {
        public AttackCooldownSnapshot(AttackSourceId sourceId, AttackDefinitionId definitionId, int remainingTicks) { SourceId = sourceId; DefinitionId = definitionId; RemainingTicks = remainingTicks; }
        public AttackSourceId SourceId { get; }
        public AttackDefinitionId DefinitionId { get; }
        public int RemainingTicks { get; }
    }

    public sealed class AttackRuntime
    {
        private readonly Dictionary<AttackDefinitionId, AttackDefinition> _definitions = new Dictionary<AttackDefinitionId, AttackDefinition>();
        private readonly Dictionary<AttackSourceId, SourceState> _sources = new Dictionary<AttackSourceId, SourceState>();
        private readonly CombatCatalog _catalog;
        private readonly IAttackTargetSelector _selector;
        private readonly IAttackDamageRequestFactory _factory;

        public AttackRuntime(CombatCatalog catalog, IReadOnlyList<AttackDefinition> definitions, IAttackTargetSelector selector = null, IAttackDamageRequestFactory factory = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (definitions == null || definitions.Count == 0) throw new ArgumentException("At least one attack definition is required.", nameof(definitions));
            for (int i = 0; i < definitions.Count; i++)
            {
                AttackDefinition definition = definitions[i] ?? throw new ArgumentException("Attack definition cannot be null.");
                if (_definitions.ContainsKey(definition.Id)) throw new ArgumentException("Duplicate attack definition: " + definition.Id);
                _definitions.Add(definition.Id, definition);
            }
            _selector = selector ?? new DeterministicAttackTargetSelector();
            _factory = factory ?? new DirectDamageRequestFactory();
        }

        public void RegisterSource(AttackSourceSnapshot source)
        {
            var state = new SourceState(source);
            foreach (AttackDefinition definition in _definitions.Values)
                state.Cooldowns.Add(definition.Id, new AttackCooldownState(definition.ReadyOnRegister ? 0 : definition.CooldownTicks));
            _sources[source.Id] = state;
        }

        public bool SetSourceEnabled(AttackSourceId sourceId, bool enabled)
        {
            if (!_sources.TryGetValue(sourceId, out SourceState state)) return false;
            state.Source = new AttackSourceSnapshot(state.Source.Id, state.Source.CombatantId, enabled, state.Source.CombatSource, state.Source.DamageMultiplier);
            return true;
        }

        public void Tick(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            foreach (SourceState source in _sources.Values)
            {
                AttackDefinitionId[] keys = new AttackDefinitionId[source.Cooldowns.Count];
                source.Cooldowns.Keys.CopyTo(keys, 0);
                for (int i = 0; i < keys.Length; i++)
                {
                    AttackCooldownState cooldown = source.Cooldowns[keys[i]];
                    cooldown.Advance(ticks);
                    source.Cooldowns[keys[i]] = cooldown;
                }
            }
        }

        public AttackResult TryAttack(AttackSourceId sourceId, AttackDefinitionId definitionId, IReadOnlyList<AttackTargetCandidate> candidates)
        {
            if (!_sources.TryGetValue(sourceId, out SourceState source)) return new AttackResult(false, AttackFailureReason.UnknownSource, null, default);
            if (!_definitions.TryGetValue(definitionId, out AttackDefinition definition)) return new AttackResult(false, AttackFailureReason.UnknownAttack, null, default);
            if (!source.Source.Enabled) return new AttackResult(false, AttackFailureReason.SourceDisabled, null, source.Cooldowns[definitionId]);
            AttackCooldownState cooldown = source.Cooldowns[definitionId];
            if (!cooldown.IsReady) return new AttackResult(false, AttackFailureReason.NotReady, null, cooldown);
            AttackTargetSelection selection = _selector.Select(definition, candidates, out AttackFailureReason failure);
            if (!selection.Found) return new AttackResult(false, failure, null, cooldown);
            DamageResolutionRequest resolution = _factory.Create(_catalog, definition, source.Source, selection);
            var intent = new AttackIntent(AttackIntentKind.DirectDamage, sourceId, definitionId, selection, resolution.Damage, resolution);
            cooldown.Start(definition.CooldownTicks);
            source.Cooldowns[definitionId] = cooldown;
            return new AttackResult(true, AttackFailureReason.None, intent, cooldown);
        }

        public AttackSnapshot CreateSnapshot()
        {
            var sources = new AttackSourceSnapshot[_sources.Count];
            int index = 0; foreach (SourceState source in _sources.Values) sources[index++] = source.Source;
            Array.Sort(sources, (a, b) => a.Id.CompareTo(b.Id));
            var cooldowns = new List<AttackCooldownSnapshot>();
            foreach (SourceState source in _sources.Values)
                foreach (KeyValuePair<AttackDefinitionId, AttackCooldownState> pair in source.Cooldowns)
                    cooldowns.Add(new AttackCooldownSnapshot(source.Source.Id, pair.Key, pair.Value.RemainingTicks));
            cooldowns.Sort((a, b) => { int sourceCompare = a.SourceId.CompareTo(b.SourceId); return sourceCompare != 0 ? sourceCompare : a.DefinitionId.CompareTo(b.DefinitionId); });
            return new AttackSnapshot(sources, cooldowns);
        }

        public static AttackRuntime FromSnapshot(CombatCatalog catalog, IReadOnlyList<AttackDefinition> definitions, AttackSnapshot snapshot, IAttackTargetSelector selector = null, IAttackDamageRequestFactory factory = null)
        {
            var runtime = new AttackRuntime(catalog, definitions, selector, factory);
            if (snapshot == null) return runtime;
            for (int i = 0; i < snapshot.Sources.Count; i++) runtime.RegisterSource(snapshot.Sources[i]);
            for (int i = 0; i < snapshot.Cooldowns.Count; i++)
            {
                AttackCooldownSnapshot c = snapshot.Cooldowns[i];
                if (runtime._sources.TryGetValue(c.SourceId, out SourceState source) && source.Cooldowns.ContainsKey(c.DefinitionId))
                    source.Cooldowns[c.DefinitionId] = new AttackCooldownState(c.RemainingTicks);
            }
            return runtime;
        }

        private sealed class SourceState
        {
            public SourceState(AttackSourceSnapshot source) { Source = source; }
            public AttackSourceSnapshot Source;
            public readonly Dictionary<AttackDefinitionId, AttackCooldownState> Cooldowns = new Dictionary<AttackDefinitionId, AttackCooldownState>();
        }
    }
}
