using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    public sealed class AttackContentRecordProjection
    {
        public AttackContentRecordProjection(
            GameContentRecordDescriptor record,
            float damage,
            float cooldownSeconds,
            float range,
            string targetingMode,
            string deliveryMode,
            string payloadRecordId,
            int projectileCount,
            float areaRadius,
            float durationSeconds,
            string statusSummary,
            string presentationSummary,
            IEnumerable<GameContentRecordKey> relatedUpgradeKeys = null,
            GameContentRecordKey evolutionKey = null)
        {
            Record = record;
            Damage = damage;
            CooldownSeconds = cooldownSeconds;
            Range = range;
            TargetingMode = targetingMode ?? string.Empty;
            DeliveryMode = deliveryMode ?? string.Empty;
            PayloadRecordId = payloadRecordId ?? string.Empty;
            ProjectileCount = projectileCount;
            AreaRadius = areaRadius;
            DurationSeconds = durationSeconds;
            StatusSummary = statusSummary ?? string.Empty;
            PresentationSummary = presentationSummary ?? string.Empty;
            RelatedUpgradeKeys = relatedUpgradeKeys == null
                ? Array.Empty<GameContentRecordKey>()
                : relatedUpgradeKeys.Where(value => value != null).ToArray();
            EvolutionKey = evolutionKey;
        }

        public GameContentRecordDescriptor Record { get; }
        public float Damage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public string TargetingMode { get; }
        public string DeliveryMode { get; }
        public string PayloadRecordId { get; }
        public int ProjectileCount { get; }
        public float AreaRadius { get; }
        public float DurationSeconds { get; }
        public string StatusSummary { get; }
        public string PresentationSummary { get; }
        public IReadOnlyList<GameContentRecordKey> RelatedUpgradeKeys { get; }
        public GameContentRecordKey EvolutionKey { get; }
    }

    public sealed class EnemyContentRecordProjection
    {
        public EnemyContentRecordProjection(
            GameContentRecordDescriptor record,
            string role,
            float health,
            float moveSpeed,
            float radius,
            float contactDamage,
            float contactIntervalSeconds,
            int experienceReward,
            string lifecycleBehavior,
            bool majorThreat,
            string lifeBarBehavior,
            string offscreenMarkerBehavior,
            string presentationSummary,
            string gameSpecificSummary)
        {
            Record = record;
            Role = role ?? string.Empty;
            Health = health;
            MoveSpeed = moveSpeed;
            Radius = radius;
            ContactDamage = contactDamage;
            ContactIntervalSeconds = contactIntervalSeconds;
            ExperienceReward = experienceReward;
            LifecycleBehavior = lifecycleBehavior ?? string.Empty;
            MajorThreat = majorThreat;
            LifeBarBehavior = lifeBarBehavior ?? string.Empty;
            OffscreenMarkerBehavior = offscreenMarkerBehavior ?? string.Empty;
            PresentationSummary = presentationSummary ?? string.Empty;
            GameSpecificSummary = gameSpecificSummary ?? string.Empty;
        }

        public GameContentRecordDescriptor Record { get; }
        public string Role { get; }
        public float Health { get; }
        public float MoveSpeed { get; }
        public float Radius { get; }
        public float ContactDamage { get; }
        public float ContactIntervalSeconds { get; }
        public int ExperienceReward { get; }
        public string LifecycleBehavior { get; }
        public bool MajorThreat { get; }
        public string LifeBarBehavior { get; }
        public string OffscreenMarkerBehavior { get; }
        public string PresentationSummary { get; }
        public string GameSpecificSummary { get; }
    }

    public sealed class EncounterContentRecordProjection
    {
        public EncounterContentRecordProjection(
            GameContentRecordDescriptor record,
            string encounterKind,
            float durationSeconds,
            float victoryTimeSeconds,
            float firstEliteSeconds,
            float firstDreadEliteSeconds,
            float minibossSeconds,
            float bossSeconds,
            bool endless,
            string escalationSummary,
            IEnumerable<GameContentRecordKey> enemyKeys = null,
            IEnumerable<GameContentRecordKey> rewardKeys = null)
        {
            Record = record;
            EncounterKind = encounterKind ?? string.Empty;
            DurationSeconds = durationSeconds;
            VictoryTimeSeconds = victoryTimeSeconds;
            FirstEliteSeconds = firstEliteSeconds;
            FirstDreadEliteSeconds = firstDreadEliteSeconds;
            MinibossSeconds = minibossSeconds;
            BossSeconds = bossSeconds;
            Endless = endless;
            EscalationSummary = escalationSummary ?? string.Empty;
            EnemyKeys = enemyKeys == null ? Array.Empty<GameContentRecordKey>() : enemyKeys.Where(value => value != null).ToArray();
            RewardKeys = rewardKeys == null ? Array.Empty<GameContentRecordKey>() : rewardKeys.Where(value => value != null).ToArray();
        }

        public GameContentRecordDescriptor Record { get; }
        public string EncounterKind { get; }
        public float DurationSeconds { get; }
        public float VictoryTimeSeconds { get; }
        public float FirstEliteSeconds { get; }
        public float FirstDreadEliteSeconds { get; }
        public float MinibossSeconds { get; }
        public float BossSeconds { get; }
        public bool Endless { get; }
        public string EscalationSummary { get; }
        public IReadOnlyList<GameContentRecordKey> EnemyKeys { get; }
        public IReadOnlyList<GameContentRecordKey> RewardKeys { get; }
    }

    internal sealed class AttackPackAwareLensState
    {
        public readonly GameContentRecordLensBrowserState Browser = new GameContentRecordLensBrowserState();
    }

    internal sealed class EnemyPackAwareLensState
    {
        public readonly GameContentRecordLensBrowserState Browser = new GameContentRecordLensBrowserState();
    }

    internal sealed class EncounterPackAwareLensState
    {
        public readonly GameContentRecordLensBrowserState Browser = new GameContentRecordLensBrowserState();
    }

    internal static class AttackPackAwareLensView
    {
        public static void Draw(
            GameContentAuthoringSurfaceContext context,
            GameContentLensDescriptor lens,
            AttackPackAwareLensState state)
        {
            GameContentRecordLensBrowser.Draw(
                context,
                lens,
                state.Browser,
                record => DrawDetails(record),
                record => DrawPreview(record));
        }

        private static void DrawDetails(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<AttackContentRecordProjection>.TryProject(record, out AttackContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("No installed adapter exposes common Attack fields for this record. Source metadata remains available below.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Attack", DeucarianEditorStyles.SectionTitle);
            Row("Damage", projection.Damage);
            Row("Cooldown", projection.CooldownSeconds, "s");
            Row("Range", projection.Range);
            GameContentRecordLensBrowser.DrawRow("Targeting", projection.TargetingMode);
            GameContentRecordLensBrowser.DrawRow("Delivery", projection.DeliveryMode);
            GameContentRecordLensBrowser.DrawRow("Payload", Empty(projection.PayloadRecordId));
            GameContentRecordLensBrowser.DrawRow("Projectile Count", projection.ProjectileCount.ToString(CultureInfo.InvariantCulture));
            Row("Area Radius", projection.AreaRadius);
            Row("Duration", projection.DurationSeconds, "s");
            GameContentRecordLensBrowser.DrawRow("Status", Empty(projection.StatusSummary));
            GameContentRecordLensBrowser.DrawRow("Presentation", Empty(projection.PresentationSummary));
            GameContentRecordLensBrowser.DrawRow("Related Upgrades", projection.RelatedUpgradeKeys.Count.ToString(CultureInfo.InvariantCulture));
            GameContentRecordLensBrowser.DrawRow("Evolution", projection.EvolutionKey == null ? "None" : projection.EvolutionKey.SourceRecordId);
        }

        private static void DrawPreview(GameContentRecordDescriptor record)
        {
            EditorGUILayout.LabelField(record.DisplayName, DeucarianEditorStyles.SectionTitle);
            if (!GameContentRecordProjectionRegistry<AttackContentRecordProjection>.TryProject(record, out AttackContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("Preview adapter unavailable.", MessageType.Warning);
                return;
            }

            DeucarianEditorStatusBadge.Draw("Read-only pack record", DeucarianEditorStatus.Info, GUILayout.MinWidth(138f));
            Row("Damage", projection.Damage);
            Row("Interval", projection.CooldownSeconds, "s");
            Row("Range", projection.Range);
            GameContentRecordLensBrowser.DrawRow("Delivery", projection.DeliveryMode);
            GameContentRecordLensBrowser.DrawRow("Payload", Empty(projection.PayloadRecordId));
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(projection.PresentationSummary)
                    ? "No prefab or VFX is assigned by this read-only source. The preview uses authored numeric values."
                    : projection.PresentationSummary,
                MessageType.Info);
        }

        private static void Row(string label, float value, string suffix = null)
        {
            GameContentRecordLensBrowser.DrawRow(label, value.ToString("0.###", CultureInfo.InvariantCulture) + (suffix ?? string.Empty));
        }

        private static string Empty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    internal static class EnemyPackAwareLensView
    {
        public static void Draw(
            GameContentAuthoringSurfaceContext context,
            GameContentLensDescriptor lens,
            EnemyPackAwareLensState state)
        {
            GameContentRecordLensBrowser.Draw(
                context,
                lens,
                state.Browser,
                DrawDetails,
                DrawPreview);
        }

        private static void DrawDetails(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<EnemyContentRecordProjection>.TryProject(record, out EnemyContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("No installed adapter exposes common Enemy fields for this record.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Enemy", DeucarianEditorStyles.SectionTitle);
            GameContentRecordLensBrowser.DrawRow("Role", projection.Role);
            Row("Health", projection.Health);
            Row("Move Speed", projection.MoveSpeed);
            Row("Radius", projection.Radius);
            Row("Contact Damage", projection.ContactDamage);
            Row("Contact Interval", projection.ContactIntervalSeconds, "s");
            GameContentRecordLensBrowser.DrawRow("XP / Reward", projection.ExperienceReward.ToString(CultureInfo.InvariantCulture));
            GameContentRecordLensBrowser.DrawRow("Lifecycle", Empty(projection.LifecycleBehavior));
            GameContentRecordLensBrowser.DrawRow("Major Threat", projection.MajorThreat ? "Yes" : "No");
            GameContentRecordLensBrowser.DrawRow("Life Bar", Empty(projection.LifeBarBehavior));
            GameContentRecordLensBrowser.DrawRow("Offscreen Marker", Empty(projection.OffscreenMarkerBehavior));
            GameContentRecordLensBrowser.DrawRow("Presentation", Empty(projection.PresentationSummary));
            if (!string.IsNullOrWhiteSpace(projection.GameSpecificSummary))
            {
                EditorGUILayout.LabelField("Game-Specific", DeucarianEditorStyles.SectionTitle);
                EditorGUILayout.LabelField(projection.GameSpecificSummary, EditorStyles.wordWrappedLabel);
            }
        }

        private static void DrawPreview(GameContentRecordDescriptor record)
        {
            EditorGUILayout.LabelField(record.DisplayName, DeucarianEditorStyles.SectionTitle);
            if (!GameContentRecordProjectionRegistry<EnemyContentRecordProjection>.TryProject(record, out EnemyContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("Preview adapter unavailable.", MessageType.Warning);
                return;
            }

            DeucarianEditorStatusBadge.Draw("Read-only pack record", DeucarianEditorStatus.Info, GUILayout.MinWidth(138f));
            GameContentRecordLensBrowser.DrawRow("Role", projection.Role);
            Row("Health", projection.Health);
            Row("Speed", projection.MoveSpeed);
            Row("Contact Damage", projection.ContactDamage);
            GameContentRecordLensBrowser.DrawRow("Threat UI", projection.LifeBarBehavior + " / " + projection.OffscreenMarkerBehavior);
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(projection.PresentationSummary)
                    ? "No prefab is assigned by this read-only source. The preview uses authored combat stats."
                    : projection.PresentationSummary,
                MessageType.Info);
        }

        private static void Row(string label, float value, string suffix = null)
        {
            GameContentRecordLensBrowser.DrawRow(label, value.ToString("0.###", CultureInfo.InvariantCulture) + (suffix ?? string.Empty));
        }

        private static string Empty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }
    }

    internal static class EncounterPackAwareLensView
    {
        public static void Draw(
            GameContentAuthoringSurfaceContext context,
            GameContentLensDescriptor lens,
            EncounterPackAwareLensState state)
        {
            GameContentRecordLensBrowser.Draw(
                context,
                lens,
                state.Browser,
                DrawDetails,
                DrawPreview);
        }

        private static void DrawDetails(GameContentRecordDescriptor record)
        {
            if (!GameContentRecordProjectionRegistry<EncounterContentRecordProjection>.TryProject(record, out EncounterContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("No installed adapter exposes common Encounter fields for this record.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Wave / Encounter", DeucarianEditorStyles.SectionTitle);
            GameContentRecordLensBrowser.DrawRow("Kind", projection.EncounterKind);
            Row("Duration", projection.DurationSeconds, "s");
            Row("Victory", projection.VictoryTimeSeconds, "s");
            GameContentRecordLensBrowser.DrawRow("Endless", projection.Endless ? "Yes" : "No");
            GameContentRecordLensBrowser.DrawRow("Enemy Roles", projection.EnemyKeys.Count.ToString(CultureInfo.InvariantCulture));
            GameContentRecordLensBrowser.DrawRow("Rewards", projection.RewardKeys.Count.ToString(CultureInfo.InvariantCulture));
            GameContentRecordLensBrowser.DrawRow("Escalation", string.IsNullOrWhiteSpace(projection.EscalationSummary) ? "Authored timeline" : projection.EscalationSummary);
        }

        private static void DrawPreview(GameContentRecordDescriptor record)
        {
            EditorGUILayout.LabelField(record.DisplayName, DeucarianEditorStyles.SectionTitle);
            if (!GameContentRecordProjectionRegistry<EncounterContentRecordProjection>.TryProject(record, out EncounterContentRecordProjection projection))
            {
                EditorGUILayout.HelpBox("Timeline adapter unavailable.", MessageType.Warning);
                return;
            }

            DeucarianEditorStatusBadge.Draw("Read-only timeline", DeucarianEditorStatus.Info, GUILayout.MinWidth(124f));
            float duration = Mathf.Max(0.001f, projection.DurationSeconds);
            Timeline("First Elite", projection.FirstEliteSeconds, duration);
            Timeline("Dread Elite", projection.FirstDreadEliteSeconds, duration);
            Timeline("Miniboss", projection.MinibossSeconds, duration);
            Timeline("Boss", projection.BossSeconds, duration);
            Timeline("Victory", projection.VictoryTimeSeconds, duration);
            GameContentRecordLensBrowser.DrawRow("Duration", projection.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s");
        }

        private static void Timeline(string label, float seconds, float duration)
        {
            if (seconds < 0f) return;
            Rect rect = GUILayoutUtility.GetRect(10f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(seconds / duration), label + "  " + seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s");
        }

        private static void Row(string label, float value, string suffix)
        {
            GameContentRecordLensBrowser.DrawRow(label, value.ToString("0.###", CultureInfo.InvariantCulture) + suffix);
        }
    }
}
