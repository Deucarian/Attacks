using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class AttackMechanicsDefinitionAsset : ScriptableObject
    {
        [SerializeField] private int _cooldownTicks = 30;
        [SerializeField] private float _range = 6f;
        [SerializeField] private float _damageAmount = 8f;
        [SerializeField] private string _damageTypeId = "damage.physical";

        public int CooldownTicks => _cooldownTicks;
        public float Range => _range;
        public float DamageAmount => _damageAmount;
        public string DamageTypeId => _damageTypeId ?? string.Empty;

        public void Configure(int cooldownTicks, float range, float damageAmount, string damageTypeId)
        {
            _cooldownTicks = cooldownTicks;
            _range = range;
            _damageAmount = damageAmount;
            _damageTypeId = damageTypeId ?? string.Empty;
        }
    }
}
