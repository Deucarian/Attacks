using System;
using Deucarian.Attacks.Editor;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Attacks.Tests
{
    public sealed class AttackAuthoringHeaderStyleTests
    {
        [TestCase(0, 15, true)]
        [TestCase(1, 15, true)]
        [TestCase(2, 14, false)]
        public void HeaderPreservesDomainMetricsWithoutMutatingOrSharingTheSource(int provider, int fontSize, bool wraps)
        {
            var source = new GUIStyle
            {
                fontSize = 9, fontStyle = FontStyle.Italic, wordWrap = false,
                alignment = TextAnchor.MiddleRight, margin = new RectOffset(1, 2, 3, 4)
            };
            source.normal.textColor = Color.magenta;
            GUIStyle first = CreateCache(provider, () => source).Value;
            GUIStyle second = CreateCache(provider, () => source).Value;
            Assert.That(first, Is.Not.SameAs(source).And.Not.SameAs(second));
            Assert.That(first.fontSize, Is.EqualTo(fontSize));
            Assert.That(first.fontStyle, Is.EqualTo(provider == 2 ? FontStyle.Italic : FontStyle.Bold));
            Assert.That(first.wordWrap, Is.EqualTo(wraps));
            Assert.That(first.alignment, Is.EqualTo(TextAnchor.MiddleRight));
            Assert.That(first.margin.left, Is.EqualTo(1));
            Assert.That(first.margin.bottom, Is.EqualTo(4));
            Assert.That(first.normal.textColor, Is.EqualTo(DeucarianEditorTheme.Text));
            first.fontSize = 31;
            first.normal.textColor = Color.green;
            Assert.That(second.fontSize, Is.EqualTo(fontSize));
            Assert.That(second.normal.textColor, Is.EqualTo(DeucarianEditorTheme.Text));
            Assert.That(source.fontSize, Is.EqualTo(9));
            Assert.That(source.fontStyle, Is.EqualTo(FontStyle.Italic));
            Assert.That(source.wordWrap, Is.False);
            Assert.That(source.normal.textColor, Is.EqualTo(Color.magenta));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void HeaderDefersGuiStyleAccessRetriesUnavailableContextAndThenKeepsTheSameStyle(int provider)
        {
            int reads = 0;
            Lazy<GUIStyle> cache = CreateCache(provider, () =>
            {
                reads++;
                if (reads == 1) throw new InvalidOperationException("GUI style source is not available yet.");
                return new GUIStyle();
            });
            Assert.That(reads, Is.Zero, "Creating the cache must not touch EditorStyles before GUI use.");
            Assert.That(cache.IsValueCreated, Is.False);
            Assert.Throws<InvalidOperationException>(() => { var unused = cache.Value; });
            Assert.That(reads, Is.EqualTo(1));
            Assert.That(cache.IsValueCreated, Is.False);
            GUIStyle first = cache.Value;
            Assert.That(reads, Is.EqualTo(2));
            Assert.That(cache.IsValueCreated, Is.True);
            first.fontSize = 29;
            Assert.That(cache.Value, Is.SameAs(first));
            Assert.That(cache.Value.fontSize, Is.EqualTo(29), "The successful cached style must retain its identity and edits.");
            Assert.That(reads, Is.EqualTo(2));
        }

        private static Lazy<GUIStyle> CreateCache(int provider, Func<GUIStyle> source)
        {
            switch (provider)
            {
                case 0: return AttackAuthoringFields.CreateHeaderStyleCache(source);
                case 1: return EnemyAuthoringFields.CreateHeaderStyleCache(source);
                case 2: return WaveAuthoringFields.CreateHeaderStyleCache(source);
                default: throw new ArgumentOutOfRangeException(nameof(provider));
            }
        }
    }
}
