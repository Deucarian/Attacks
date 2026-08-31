using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;

namespace Deucarian.Attacks.Tests
{
    public sealed class AttacksControlCenterTests
    {
        [Test]
        public void ContributionRegistersStableAuthoringActions()
        {
            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture();
            DeucarianToolDescriptor tool = snapshot.Tools.Single(candidate =>
                candidate.Id == "deucarian.attacks.authoring");
            DeucarianControlCenterCard card = snapshot.Cards.Single(candidate =>
                candidate.Id == "com.deucarian.attacks.authoring");

            Assert.That(tool.Area, Is.EqualTo(DeucarianControlCenterArea.Authoring));
            Assert.That(card.Area, Is.EqualTo(DeucarianControlCenterArea.Authoring));
            CollectionAssert.AreEqual(
                new[] { "open-authoring", "migrate-wave-entry-ids" },
                card.Actions.Select(action => action.Id).ToArray());
            Assert.That(
                card.Actions.Single(action => action.Id == "migrate-wave-entry-ids")
                    .RequiresConfirmation,
                Is.True);
        }
    }
}