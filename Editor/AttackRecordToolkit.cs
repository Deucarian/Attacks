using System;
using System.Globalization;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Attacks.Editor
{
    internal static class AttackRecordToolkit
    {
        internal static VisualElement Attack(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<AttackContentRecordProjection>.TryProject(record, out var p)) return null;
            var root = new VisualElement { name = "record-attack-details" };
            var form = new DeucarianEditorWorkspaceForm(root);
            form.ReadOnly(null, "Damage", () => Convert.ToString(p.Damage, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Cooldown (s)", () => Convert.ToString(p.CooldownSeconds, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Range", () => Convert.ToString(p.Range, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Targeting", () => Convert.ToString(p.TargetingMode, CultureInfo.InvariantCulture));
            form = form.Section("Attack details", true);
            form.ReadOnly(null, "Delivery", () => Convert.ToString(p.DeliveryMode, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Payload", () => Convert.ToString(p.PayloadRecordId, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Projectile count", () => Convert.ToString(p.ProjectileCount, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Area radius", () => Convert.ToString(p.AreaRadius, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Duration (s)", () => Convert.ToString(p.DurationSeconds, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Status", () => Convert.ToString(p.StatusSummary, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Presentation", () => Convert.ToString(p.PresentationSummary, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Related upgrades", () => Convert.ToString(p.RelatedUpgradeKeys.Count, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Evolution", () => Convert.ToString(p.EvolutionKey?.SourceRecordId ?? "None", CultureInfo.InvariantCulture));
            if (record.Preview != null) GameContentToolkitPreview.Add(root, () => record.Preview, null);
            return root;
        }

        internal static VisualElement Enemy(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<EnemyContentRecordProjection>.TryProject(record, out var p)) return null;
            var root = new VisualElement { name = "record-enemy-details" };
            var form = new DeucarianEditorWorkspaceForm(root);
            form.ReadOnly(null, "Role", () => Convert.ToString(p.Role, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Health", () => Convert.ToString(p.Health, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Move speed", () => Convert.ToString(p.MoveSpeed, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Radius", () => Convert.ToString(p.Radius, CultureInfo.InvariantCulture));
            form = form.Section("Enemy details", true);
            form.ReadOnly(null, "Contact damage", () => Convert.ToString(p.ContactDamage, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Contact interval (s)", () => Convert.ToString(p.ContactIntervalSeconds, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Experience reward", () => Convert.ToString(p.ExperienceReward, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Lifecycle", () => Convert.ToString(p.LifecycleBehavior, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Major threat", () => Convert.ToString(p.MajorThreat ? "Yes" : "No", CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Life bar", () => Convert.ToString(p.LifeBarBehavior, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Offscreen marker", () => Convert.ToString(p.OffscreenMarkerBehavior, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Presentation", () => Convert.ToString(p.PresentationSummary, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Game-specific", () => Convert.ToString(p.GameSpecificSummary, CultureInfo.InvariantCulture));
            if (record.Preview != null) GameContentToolkitPreview.Add(root, () => record.Preview, null);
            return root;
        }

        internal static VisualElement Wave(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<EncounterContentRecordProjection>.TryProject(record, out var p)) return null;
            var root = new VisualElement { name = "record-wave-details" };
            var form = new DeucarianEditorWorkspaceForm(root);
            form.ReadOnly(null, "Kind", () => Convert.ToString(p.EncounterKind, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Duration (s)", () => Convert.ToString(p.DurationSeconds, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Victory (s)", () => Convert.ToString(p.VictoryTimeSeconds, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Endless", () => Convert.ToString(p.Endless ? "Yes" : "No", CultureInfo.InvariantCulture));
            form = form.Section("Encounter details", true);
            form.ReadOnly(null, "Enemy roles", () => Convert.ToString(p.EnemyKeys.Count, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Rewards", () => Convert.ToString(p.RewardKeys.Count, CultureInfo.InvariantCulture));
            form.ReadOnly(null, "Escalation", () => Convert.ToString(p.EscalationSummary, CultureInfo.InvariantCulture));
            var timeline = form.Section("Authored timeline", true);
            AddMilestone(timeline, "First elite", p.FirstEliteSeconds, p.DurationSeconds);
            AddMilestone(timeline, "Dread elite", p.FirstDreadEliteSeconds, p.DurationSeconds);
            AddMilestone(timeline, "Miniboss", p.MinibossSeconds, p.DurationSeconds);
            AddMilestone(timeline, "Boss", p.BossSeconds, p.DurationSeconds);
            AddMilestone(timeline, "Victory", p.VictoryTimeSeconds, p.DurationSeconds);
            return root;
        }

        private static void AddMilestone(DeucarianEditorWorkspaceForm form, string label, float seconds, float duration)
        {
            if (seconds < 0) return;
            form.ReadOnly(null, label, () => seconds.ToString("0.###", CultureInfo.InvariantCulture) + " s");
            var track = DeucarianEditorWorkspaceControls.Region(null, "dw-progress");
            var fill = DeucarianEditorWorkspaceControls.Region(null, "dw-progress-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(seconds / Mathf.Max(.001f, duration)) * 100);
            track.Add(fill); form.Root.Add(track);
        }
    }
}
