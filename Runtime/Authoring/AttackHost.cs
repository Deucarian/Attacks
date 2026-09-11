using Deucarian.Diagnostics;
using System;
using Deucarian.Combat;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    /// <summary>Typed attack requests for one source. The supplied runtime owns cooldowns and produces existing attack intents.</summary>
    [DisallowMultipleComponent]
    public sealed class AttackHost : MonoBehaviour, IDiagnosticProvider
    {
        private AttackRuntime runtime;
        private CombatScope targets;
        private AttackSourceId source;
        private bool destroyed;

        public void Configure(AttackRuntime value, AttackSourceId sourceId, CombatScope targetScope)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(AttackHost));
            if (runtime != null) throw new InvalidOperationException("AttackHost '" + name + "' is already configured.");
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (sourceId.IsEmpty) throw new ArgumentException("Register an attack source and supply its ID when configuring this host.", nameof(sourceId));
            targets = targetScope ?? throw new ArgumentNullException(nameof(targetScope));
            source = sourceId;
            runtime = value;
        }

        public AttackResult Request(AttackKey attack, CombatantHandle target)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(AttackHost));
            if (runtime == null) throw new InvalidOperationException("AttackHost '" + name + "' is not configured. Supply its AttackRuntime, registered source ID and combat target scope during startup.");
            if (attack == null) throw new ArgumentNullException(nameof(attack), "Select an AttackKey or pass a named attack definition.");
            if (!targets.Contains(target) || !target.TryGetState(out var health, out _, out var defense))
                return new AttackResult(false, AttackFailureReason.InvalidCandidate, null, default);
            var result = runtime.TryAttack(source, new AttackDefinitionId(attack.Id),
                new[] { new AttackTargetCandidate(health.Id, health, 0, defense: defense) });
            if (result.FailureReason == AttackFailureReason.UnknownAttack)
                throw new InvalidOperationException("AttackHost '" + name + "' cannot find attack '" + attack.Id + "'. Add its definition to this host's AttackRuntime.");
            if (result.FailureReason == AttackFailureReason.UnknownSource)
                throw new InvalidOperationException("AttackHost '" + name + "' has an unregistered source. Register its source in the configured AttackRuntime before requesting attacks.");
            return result;
        }
        private void OnDestroy() { diagnosticRegistration?.Dispose(); diagnosticRegistration = null;  destroyed = true; runtime = null; targets = null; }
        private DiagnosticProviderRegistration diagnosticRegistration;
        private void Awake() => diagnosticRegistration = DiagnosticProviderRegistry.Register(this);
        string IDiagnosticProvider.ProviderId => "attacks.host." + GetInstanceID();
        string IDiagnosticProvider.DisplayName => "AttackHost";
        void IDiagnosticProvider.Collect(DiagnosticReportBuilder builder)
        {
            bool configured = runtime != null;
            builder.AddSection(((IDiagnosticProvider)this).ProviderId, "AttackHost")
                .AddItem("configured", "Configured", configured ? "Ready" : "Call Configure during startup",
                    configured ? DiagnosticSeverity.Info : DiagnosticSeverity.Warning)
                .AddItem("enabled", "Enabled", isActiveAndEnabled.ToString());
        }
    }
}
