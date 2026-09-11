using System;

namespace Deucarian.Attacks
{
    /// <summary>Marks an authoritative set of named AttackKey fields or properties for the Inspector.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AttackKeySetAttribute : Attribute { }
}
