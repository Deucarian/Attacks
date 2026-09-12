using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor.Definitions
{
    [Serializable]
    public sealed class AttackDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("_icon")] public Sprite Icon;
        [DefinitionField("_tags")] public string[] Tags = Array.Empty<string>();
        [DefinitionSection("_mechanics", typeof(AttackMechanicsDefinitionAsset))] public AttackMechanicsDefinitionSpec Mechanics = new AttackMechanicsDefinitionSpec();
        [DefinitionSection("_targeting", typeof(AttackTargetingDefinitionAsset))] public AttackTargetingDefinitionSpec Targeting = new AttackTargetingDefinitionSpec();
        [DefinitionSection("_delivery", typeof(AttackDeliveryDefinitionAsset))] public AttackDeliveryDefinitionSpec Delivery = new AttackDeliveryDefinitionSpec();
        [DefinitionSection("_statusEffects", typeof(AttackStatusEffectsDefinitionAsset))] public AttackStatusEffectsDefinitionSpec StatusEffects = new AttackStatusEffectsDefinitionSpec();
        [DefinitionSection("_presentation", typeof(AttackPresentationDefinitionAsset))] public AttackPresentationDefinitionSpec Presentation = new AttackPresentationDefinitionSpec();
        [DefinitionField("_upgradeHookId")] public string UpgradeHookId = string.Empty;
        [DefinitionField("_balancingNotes")] public string BalancingNotes = string.Empty;
    }
}
