using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    [Serializable]
    public sealed class WaveEntryRecipe
    {
        [SerializeField] private EnemyDefinitionAsset _enemy;
        [SerializeField] private int _count = 4;
        [SerializeField] private int _batchSize = 1;
        [SerializeField] private int _initialDelayTicks;
        [SerializeField] private int _intervalTicks = 12;
        [SerializeField] private string _spawnChannelId = "perimeter-north";
        [SerializeField] private int _scalingTier;

        public WaveEntryRecipe()
        {
        }

        public WaveEntryRecipe(EnemyDefinitionAsset enemy, int count, int batchSize, int initialDelayTicks, int intervalTicks, string spawnChannelId, int scalingTier = 0)
        {
            _enemy = enemy;
            _count = count;
            _batchSize = batchSize;
            _initialDelayTicks = initialDelayTicks;
            _intervalTicks = intervalTicks;
            _spawnChannelId = spawnChannelId ?? string.Empty;
            _scalingTier = scalingTier;
        }

        public EnemyDefinitionAsset Enemy => _enemy;
        public int Count => _count;
        public int BatchSize => _batchSize;
        public int InitialDelayTicks => _initialDelayTicks;
        public int IntervalTicks => _intervalTicks;
        public string SpawnChannelId => _spawnChannelId ?? string.Empty;
        public int ScalingTier => _scalingTier;
    }

    public readonly struct WaveDefinitionValidationOptions
    {
        public WaveDefinitionValidationOptions(bool validateEnemyDefinitions)
        {
            ValidateEnemyDefinitions = validateEnemyDefinitions;
        }

        public bool ValidateEnemyDefinitions { get; }
        public static WaveDefinitionValidationOptions RuntimeFriendly => new WaveDefinitionValidationOptions(true);
        public static WaveDefinitionValidationOptions AssetCreation => new WaveDefinitionValidationOptions(true);
    }

    public static class WaveDefinitionValidator
    {
        public static ContentAuthoringValidationReport Validate(WaveDefinitionAsset recipe)
        {
            return Validate(recipe, WaveDefinitionValidationOptions.RuntimeFriendly);
        }

        public static ContentAuthoringValidationReport Validate(WaveDefinitionAsset recipe, WaveDefinitionValidationOptions options)
        {
            var issues = new List<ContentAuthoringValidationIssue>();
            if (recipe == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Wave", "Wave definition is missing."));
                return new ContentAuthoringValidationReport(issues);
            }

            if (string.IsNullOrWhiteSpace(recipe.Id)) issues.Add(ContentAuthoringValidationIssue.Error("Wave.Id", "Wave ID is required."));
            if (string.IsNullOrWhiteSpace(recipe.DisplayName)) issues.Add(ContentAuthoringValidationIssue.Warning("Wave.DisplayName", "Display name is empty."));
            ValidateSchedule(recipe.Schedule, issues);
            ValidateEntries(recipe.Entries, options, issues);
            return new ContentAuthoringValidationReport(issues);
        }

        private static void ValidateSchedule(WaveScheduleDefinitionAsset schedule, List<ContentAuthoringValidationIssue> issues)
        {
            if (schedule == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Schedule", "Schedule section is required."));
                return;
            }

            if (schedule.StartTick < 0) issues.Add(ContentAuthoringValidationIssue.Error("Schedule.StartTick", "Wave start tick cannot be negative."));
        }

        private static void ValidateEntries(WaveEntriesDefinitionAsset entries, WaveDefinitionValidationOptions options, List<ContentAuthoringValidationIssue> issues)
        {
            if (entries == null)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Entries", "Entries section is required."));
                return;
            }

            IReadOnlyList<WaveEntryRecipe> items = entries.Entries;
            if (items.Count == 0)
            {
                issues.Add(ContentAuthoringValidationIssue.Error("Entries", "Add at least one wave entry."));
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                WaveEntryRecipe entry = items[i];
                string path = "Entries[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
                if (entry == null)
                {
                    issues.Add(ContentAuthoringValidationIssue.Error(path, "Wave entry is empty."));
                    continue;
                }

                if (entry.Enemy == null)
                {
                    issues.Add(ContentAuthoringValidationIssue.Error(path + ".Enemy", "Choose an enemy definition."));
                }
                else if (options.ValidateEnemyDefinitions)
                {
                    ContentAuthoringValidationReport enemyReport = EnemyDefinitionValidator.Validate(entry.Enemy, EnemyDefinitionValidationOptions.RuntimeFriendly);
                    if (!enemyReport.IsValid)
                        issues.Add(ContentAuthoringValidationIssue.Error(path + ".Enemy", "Referenced enemy is invalid."));
                }

                if (entry.Count <= 0) issues.Add(ContentAuthoringValidationIssue.Error(path + ".Count", "Count must be greater than zero."));
                if (entry.BatchSize <= 0) issues.Add(ContentAuthoringValidationIssue.Error(path + ".BatchSize", "Batch size must be greater than zero."));
                if (entry.BatchSize > entry.Count) issues.Add(ContentAuthoringValidationIssue.Warning(path + ".BatchSize", "Batch size is larger than count; the runtime will emit the group in one batch."));
                if (entry.InitialDelayTicks < 0) issues.Add(ContentAuthoringValidationIssue.Error(path + ".InitialDelayTicks", "Spawn delay cannot be negative."));
                if (entry.IntervalTicks < 0) issues.Add(ContentAuthoringValidationIssue.Error(path + ".IntervalTicks", "Spawn interval cannot be negative."));
                if (string.IsNullOrWhiteSpace(entry.SpawnChannelId)) issues.Add(ContentAuthoringValidationIssue.Error(path + ".SpawnChannelId", "Spawn channel/lane is required."));
                if (entry.ScalingTier < 0) issues.Add(ContentAuthoringValidationIssue.Error(path + ".ScalingTier", "Difficulty tier cannot be negative."));
            }
        }
    }
}
