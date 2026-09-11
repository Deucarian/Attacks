# Simple usage

Copy the reference example into your project, add SimpleUsageExample and assign its scoped host references. Its serialized definition fields use the same typed keys as code.

Configure AttackHost with the existing AttackRuntime, a registered source and the CombatScope that issues targets. The runtime catalog must contain this attack. CombatHost.Register returns the target handle; release it when that target leaves. Request returns the existing attack intent/result for the combat owner to apply; it does not tick or apply damage twice. Authored AttackDefinitionAssets generate `Deucarian.Generated.ProjectAttacks` keys.

Definitions are authored once in SampleDefinitions.cs where applicable; the caller never invents an ID. Replace the sample set with your project's central definitions. A selected key proves its identity and payload type; startup still needs to bind that definition in the correct scope. Missing configuration reports how to fix it. Dynamic targets and choices are issued by their owner instead of selected from a definition dropdown.
```csharp
using UnityEngine;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
namespace Deucarian.Attacks.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private AttackHost attacks;
        [SerializeField] private AttackKey attack = Attacks.Slash;
        public AttackResult Slash(CombatantHandle target) => attacks.Request(attack, target);
    }
}
```
