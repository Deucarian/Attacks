using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor.Definitions
{
    public sealed class AttackDefinitionSchema : DeucarianSerializedDefinitionSchema<AttackDefinitionAsset, AttackDefinitionSpec>
    {
        public override string Id => "attacks";
        public override string DisplayName => "Attacks";
        protected override string IdPath => "_id";
        protected override string NamePath => "_displayName";
        public override void ValidateAssetReady(ScriptableObject asset)
        {
            base.ValidateAssetReady(asset);
            var issues = AttackRecipeValidator.Validate((AttackDefinitionAsset)asset).Issues.Where(x => x.IsError).Select(x => x.Path + ": " + x.Message).ToArray();
            if (issues.Length > 0) throw new InvalidOperationException("Complete Attacks definition '" + asset.name + "' in Definitions: " + string.Join("; ", issues));
        }
        public override void RefreshCatalog(bool validateOnly = false)
        {
            var definitions = AssetDatabase.FindAssets("t:AttackDefinitionAsset", new[] { "Assets" })
                .Select(x => AssetDatabase.LoadAssetAtPath<AttackDefinitionAsset>(AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => x != null).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (definitions.GroupBy(x => x.Id).Any(x => x.Count() > 1)) throw new InvalidOperationException("Attack definition IDs must be unique.");
            DeucarianDefinitionCatalog.Update<AttackDefinitionCatalog>("Assets/DeucarianDefinitions/Resources/Deucarian/Definitions/AttackDefinitionCatalog.asset", "definitions", definitions, validateOnly);
        }
        [MenuItem("Assets/Create/Deucarian/Attacks/Attack Definition")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new AttackDefinitionSchema(), "NewAttack"); }
    }
}
