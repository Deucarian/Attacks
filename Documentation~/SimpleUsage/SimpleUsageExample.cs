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
