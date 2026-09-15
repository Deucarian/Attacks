using System;
using Deucarian.Combat;
using Deucarian.Combat.Unity;
using UnityEngine;
namespace Deucarian.Attacks.Authoring
{
    /// <summary>Direct-damage component. Projectile delivery remains in the explicit projectile integration.</summary>
    public sealed class AttackTrigger : MonoBehaviour
    {
        [SerializeField] private AttackHost host;
        [SerializeField] private AttackKey attack;
        [SerializeField] private Combatant target;
        public AttackResult LastResult { get; private set; }
        public AttackResult RequestAttack()
        {
            if (host == null || target == null) throw new InvalidOperationException("Assign a configured AttackHost and Combatant target to AttackTrigger.");
            return LastResult = host.Request(attack, target.Handle);
        }
        public void ApplyDirectDamage()
        {
            var result = RequestAttack();
            if (!result.Succeeded) return;
            if (result.Intent.Kind != AttackIntentKind.DirectDamage) throw new InvalidOperationException("Select a direct-damage definition for ApplyDirectDamage. Route projectile intents through your ProjectileRuntime integration.");
            CombatDamageResolver.Resolve(result.Intent.ResolutionRequest);
        }
    }
}
