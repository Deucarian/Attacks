using System;
using System.Collections.Generic;
using Deucarian.Attacks;
using Deucarian.Combat;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    [CreateAssetMenu(menuName = "Deucarian/Attacks/Attack Definition", fileName = "AttackDefinition")]
    public sealed class AttackDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _id = "attack.example.basic";
        [SerializeField] private string _displayName = "Example Attack";
        [SerializeField] private Sprite _icon;
        [SerializeField] private string[] _tags = Array.Empty<string>();
        [SerializeField] private AttackMechanicsDefinitionAsset _mechanics;
        [SerializeField] private AttackTargetingDefinitionAsset _targeting;
        [SerializeField] private AttackDeliveryDefinitionAsset _delivery;
        [SerializeField] private AttackStatusEffectsDefinitionAsset _statusEffects;
        [SerializeField] private AttackPresentationDefinitionAsset _presentation;
        [SerializeField] private string _upgradeHookId = string.Empty;
        [SerializeField] private string _balancingNotes = string.Empty;

        public string Id => _id ?? string.Empty;
        public string DisplayName => _displayName ?? string.Empty;
        public Sprite Icon => _icon;
        public IReadOnlyList<string> Tags => _tags ?? Array.Empty<string>();
        public AttackMechanicsDefinitionAsset Mechanics => _mechanics;
        public AttackTargetingDefinitionAsset Targeting => _targeting;
        public AttackDeliveryDefinitionAsset Delivery => _delivery;
        public AttackStatusEffectsDefinitionAsset StatusEffects => _statusEffects;
        public AttackPresentationDefinitionAsset Presentation => _presentation;
        public string UpgradeHookId => _upgradeHookId ?? string.Empty;
        public string BalancingNotes => _balancingNotes ?? string.Empty;

        public void Configure(
            string id,
            string displayName,
            Sprite icon,
            IReadOnlyList<string> tags,
            AttackMechanicsDefinitionAsset mechanics,
            AttackTargetingDefinitionAsset targeting,
            AttackDeliveryDefinitionAsset delivery,
            AttackStatusEffectsDefinitionAsset statusEffects,
            AttackPresentationDefinitionAsset presentation,
            string upgradeHookId = "",
            string balancingNotes = "")
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _icon = icon;
            _tags = CopyTags(tags);
            _mechanics = mechanics;
            _targeting = targeting;
            _delivery = delivery;
            _statusEffects = statusEffects;
            _presentation = presentation;
            _upgradeHookId = upgradeHookId ?? string.Empty;
            _balancingNotes = balancingNotes ?? string.Empty;
        }

        public AttackDefinition ToRuntimeDefinition()
        {
            if (_mechanics == null) throw new InvalidOperationException("Attack recipe has no mechanics section.");
            StatusApplicationRequest[] statuses = _statusEffects == null
                ? Array.Empty<StatusApplicationRequest>()
                : _statusEffects.CreateApplicationRequests();
            AttackTargetPolicy targetPolicy = _targeting == null
                ? AttackTargetPolicy.HighestScore
                : _targeting.ToRuntimePolicy();
            return new AttackDefinition(
                new AttackDefinitionId(Id),
                _mechanics.CooldownTicks,
                new DamageTypeId(_mechanics.DamageTypeId),
                _mechanics.DamageAmount,
                targetPolicy,
                true,
                statuses: statuses);
        }

        public StatusEffectDefinition[] CreateStatusDefinitions()
        {
            return _statusEffects == null
                ? Array.Empty<StatusEffectDefinition>()
                : _statusEffects.CreateRuntimeDefinitions();
        }

        public static AttackDefinitionAsset CreateTransient(
            string id,
            string displayName,
            AttackRecipeDeliveryMode deliveryMode,
            string damageTypeId,
            float damageAmount,
            int cooldownTicks,
            float range,
            AttackRecipeTargetingMode targetingMode,
            IReadOnlyList<AttackStatusEffectRecipe> statuses = null,
            string projectileDefinitionId = "",
            string projectileSpawnableId = "",
            float projectileSpeed = 8f,
            int projectileLifetimeTicks = 120,
            bool homing = false,
            int pierceCount = 0)
        {
            var mechanics = CreateInstance<AttackMechanicsDefinitionAsset>();
            mechanics.hideFlags = HideFlags.HideAndDontSave;
            mechanics.Configure(cooldownTicks, range, damageAmount, damageTypeId);

            var targeting = CreateInstance<AttackTargetingDefinitionAsset>();
            targeting.hideFlags = HideFlags.HideAndDontSave;
            targeting.Configure(targetingMode);

            var delivery = CreateInstance<AttackDeliveryDefinitionAsset>();
            delivery.hideFlags = HideFlags.HideAndDontSave;
            if (deliveryMode == AttackRecipeDeliveryMode.Projectile)
            {
                delivery.ConfigureProjectile(
                    projectileDefinitionId,
                    projectileSpawnableId,
                    null,
                    projectileSpeed,
                    projectileLifetimeTicks,
                    homing,
                    pierceCount: pierceCount);
            }
            else if (deliveryMode == AttackRecipeDeliveryMode.Hitscan)
            {
                delivery.ConfigureHitscan(null, null);
            }
            else if (deliveryMode == AttackRecipeDeliveryMode.Area)
            {
                delivery.ConfigureArea(2f);
            }
            else
            {
                delivery.ConfigureAura(2f, 0.5f);
            }

            var statusSection = CreateInstance<AttackStatusEffectsDefinitionAsset>();
            statusSection.hideFlags = HideFlags.HideAndDontSave;
            statusSection.Configure(statuses);

            var presentation = CreateInstance<AttackPresentationDefinitionAsset>();
            presentation.hideFlags = HideFlags.HideAndDontSave;

            var root = CreateInstance<AttackDefinitionAsset>();
            root.hideFlags = HideFlags.HideAndDontSave;
            root.Configure(id, displayName, null, Array.Empty<string>(), mechanics, targeting, delivery, statusSection, presentation);
            return root;
        }

        private static string[] CopyTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0) return Array.Empty<string>();
            var copy = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) copy.Add(tag.Trim());
            }

            return copy.ToArray();
        }
    }
}
