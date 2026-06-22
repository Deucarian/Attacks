using Deucarian.Attacks;
using Deucarian.Combat;

public static class AttacksMinimalSample
{
    public static DamageResolutionResult Run()
    {
        var damageType = new DamageTypeId("damage.physical");
        var catalog = new CombatCatalog(new[] { new DamageTypeDefinition(damageType) });
        var definition = new AttackDefinition(new AttackDefinitionId("attack.basic"), 1, damageType, 10);
        var runtime = new AttackRuntime(catalog, new[] { definition });
        runtime.RegisterSource(new AttackSourceSnapshot(new AttackSourceId("source.player"), new CombatantId("combatant.player")));

        var target = new HealthState(new CombatantId("combatant.enemy"), 25, 25);
        AttackResult attack = runtime.TryAttack(
            new AttackSourceId("source.player"),
            new AttackDefinitionId("attack.basic"),
            new[] { new AttackTargetCandidate(target.Id, target, 1) });

        return CombatDamageResolver.Resolve(attack.Intent.ResolutionRequest);
    }
}
