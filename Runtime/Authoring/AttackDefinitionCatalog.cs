using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    /// <summary>Generated project definitions; scene composition supplies the existing runtime's other dependencies.</summary>
    public sealed class AttackDefinitionCatalog : ScriptableObject
    {
        public const string ResourcePath = "Deucarian/Definitions/AttackDefinitionCatalog";
        [SerializeField] private AttackDefinitionAsset[] definitions = Array.Empty<AttackDefinitionAsset>();
        public IReadOnlyList<AttackDefinitionAsset> Definitions => Array.AsReadOnly(definitions);
        public static AttackDefinitionCatalog LoadProject() => Resources.Load<AttackDefinitionCatalog>(ResourcePath) ??
            throw new InvalidOperationException("Create a Attack definition in the Definitions editor before loading the project catalog.");
        public AttackDefinitionAsset Get(AttackKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key), "Select a definition in the Inspector or pass a generated key.");
            foreach (var definition in definitions) if (definition != null && definition.Id == key.Id) return definition;
            throw new InvalidOperationException("The Attack catalog does not contain '" + key.Id + "'. Synchronize this definition in the Definitions editor.");
        }
        public AttackDefinition[] CreateRuntimeDefinitions()
        {
            var result = new AttackDefinition[definitions.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Length; i++)
            {
                var definition = definitions[i];
                if (definition == null || !ids.Add(definition.Id)) throw new InvalidOperationException("The Attack catalog contains a missing or duplicate definition. Synchronize it in the Definitions editor.");
                try { result[i] = definition.ToRuntimeDefinition(); }
                catch (Exception error) { throw new InvalidOperationException("Complete Attack definition '" + definition.DisplayName + "' in the Definitions editor: " + error.Message, error); }
            }
            return result;
        }
    }
}
