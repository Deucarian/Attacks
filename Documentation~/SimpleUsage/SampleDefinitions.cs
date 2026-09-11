namespace Deucarian.Attacks.Samples.SimpleUsage
{
    [AttackKeySet]
    public static class Attacks
    {
        public static AttackKey Slash => new Definition();
        private sealed class Definition : AttackKey
        {
            public Definition() : base("sample.slash") { }
        }
    }
}
