using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor.Definitions
{
    public sealed class EnemyDefinitionSchema : DeucarianSerializedDefinitionSchema<EnemyDefinitionAsset, EnemyDefinitionSpec>
    {
        public override string Id => "enemies";
        public override string DisplayName => "Enemies";
        protected override string IdPath => "_id";
        protected override string NamePath => "_displayName";
        public override void ValidateAssetReady(ScriptableObject asset)
        {
            base.ValidateAssetReady(asset);
            var issues = EnemyDefinitionValidator.Validate((EnemyDefinitionAsset)asset).Issues.Where(x => x.Severity == ContentAuthoringValidationSeverity.Error).Select(x => x.Path + ": " + x.Message).ToArray();
            if (issues.Length > 0) throw new InvalidOperationException("Complete Enemies definition '" + asset.name + "' in Definitions: " + string.Join("; ", issues));
        }
        [MenuItem("Assets/Create/Deucarian/Attacks/Enemy Definition")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new EnemyDefinitionSchema(), "NewEnemy"); }
    }
}
