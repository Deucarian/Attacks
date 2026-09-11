using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;

namespace Deucarian.Attacks.Editor
{
    [InitializeOnLoad]
    internal static class AttacksControlCenter
    {
        private const string PackageId = "com.deucarian.attacks";
        private const string ToolId = "deucarian.attacks.authoring";

        static AttacksControlCenter()
        {
            DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                ToolId,
                "Attack Content",
                "Author attacks, enemies, and waves in Game Content Authoring.",
                DeucarianControlCenterArea.Authoring,
                GameContentAuthoringWindow.Open,
                PackageId,
                searchTerms: new[] { "attack", "enemy", "wave", "migration" },
                order: 120, createPage: () => GameContentAuthoringWindow.CreatePage(ToolId,
                    "Attack content", "Shape attacks, enemies and waves.", "com.deucarian.attacks.")));
            DeucarianControlCenterRegistry.RegisterCardProvider(new Provider());
        }

        private static void MigrateWaveEntryIds()
        {
            WaveEntryIdMigrationReport report =
                WaveEntryIdMigration.MigrateProjectOwnedWaveAssets();
            EditorUtility.DisplayDialog(
                report.Succeeded
                    ? "Wave Entry ID Migration"
                    : "Wave Entry ID Migration Conflicts",
                report.CreateSummary(),
                "OK");
        }

        private sealed class Provider : IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                yield return new DeucarianControlCenterCard(
                    PackageId + ".authoring",
                    DeucarianControlCenterArea.Authoring,
                    "Attacks, Enemies, and Waves",
                    "Open domain-owned authoring providers or migrate project-owned wave IDs.",
                    PackageId,
                    DeucarianControlCenterStatus.Success,
                    "3 authoring providers available",
                    order: 120,
                    details: new[]
                    {
                        "Only provider availability and migration summaries are exposed."
                    },
                    actions: new[]
                    {
                        new DeucarianControlCenterAction(
                            "open-authoring",
                            "Open Authoring",
                            GameContentAuthoringWindow.Open, navigationToolId: DeucarianToolIds.GameContentAuthoring),
                        new DeucarianControlCenterAction(
                            "migrate-wave-entry-ids",
                            "Migrate Wave Entry IDs",
                            MigrateWaveEntryIds,
                            "Assign stable IDs to eligible project-owned wave entries.",
                            requiresConfirmation: true)
                    },
                    searchTerms: new[] { "attack", "enemy", "wave" });
            }
        }
    }
}
