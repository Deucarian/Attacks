using Deucarian.Diagnostics;
using NUnit.Framework;
using UnityEngine;
namespace Deucarian.Attacks.Authoring.Tests
{
    public sealed class HostDiagnosticsTests
    {
        [Test]
        public void LiveHostRegistersSanitizedHealthAndDestroyReleasesIt()
        {
            int before = DiagnosticProviderRegistry.SnapshotProviders().Count;
            var go = new GameObject("private application label");
            try
            {
                var host = go.AddComponent<AttackHost>();
                Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before + 1));
                Assert.That(((IDiagnosticProvider)host).ProviderId, Does.Not.Contain(go.name));
                Assert.That(((IDiagnosticProvider)host).DisplayName, Does.Not.Contain(go.name));
                Assert.DoesNotThrow(() => DiagnosticProviderRegistry.BuildReport());
            }
            finally { Object.DestroyImmediate(go); }
            Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before));
        }
    }
}
