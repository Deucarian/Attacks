using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Attacks.Authoring;
using Deucarian.Combat;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor.Definitions
{
    public sealed class WaveDefinitionSchema : DeucarianSerializedDefinitionSchema<WaveDefinitionAsset, WaveDefinitionSpec>
    {
        public override string Id => "waves";
        public override string DisplayName => "Waves";
        protected override string IdPath => "_id";
        protected override string NamePath => "_displayName";
        public override void ValidateAssetReady(ScriptableObject asset)
        {
            base.ValidateAssetReady(asset);
            var issues = WaveDefinitionValidator.Validate((WaveDefinitionAsset)asset).Issues.Where(x => x.Severity == ContentAuthoringValidationSeverity.Error).Select(x => x.Path + ": " + x.Message).ToArray();
            if (issues.Length > 0) throw new InvalidOperationException("Complete Waves definition '" + asset.name + "' in Definitions: " + string.Join("; ", issues));
        }
        [MenuItem("Assets/Create/Deucarian/Attacks/Wave Definition")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new WaveDefinitionSchema(), "NewWave"); }
    }
}
