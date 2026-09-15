using System;
using Deucarian.Editor;
using Deucarian.Attacks.Authoring;

namespace Deucarian.Attacks.Editor
{
    public sealed class AttackKeySource : DeucarianAssetKeySource<AttackDefinitionAsset>
    {
        public override Type KeyType => typeof(AttackKey);
        public override Type DefinitionSetAttribute => typeof(AttackKeySetAttribute);
        public override string GeneratedClassName => "ProjectAttacks";
        protected override DeucarianKeyChoice ReadDefinition(AttackDefinitionAsset asset) =>
            new DeucarianKeyChoice(asset.Id, asset.DisplayName);
    }
}
