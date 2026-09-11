using System;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Attacks.Editor
{
    [CustomPropertyDrawer(typeof(AttackKey), true)]
    public sealed class AttackKeyDrawer : DeucarianKeyDrawer
    {
        public override Type KeyType => typeof(AttackKey);
        public override Type DefinitionSetAttribute => typeof(AttackKeySetAttribute);
        public override string SetupHint => "Select an existing AttackKey; declare reusable keys once in a [AttackKeySet] class.";
    }
}
