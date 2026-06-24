using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed partial class AttackAuthoringWindow
    {
        private void DrawEnemyProvider(EnemyAuthoringState state)
        {
            EnemyDefinitionAsset preview = EnemyDefinitionAssetCreator.BuildTransient(state);
            ContentAuthoringValidationReport report;
            try
            {
                report = EnemyDefinitionAssetCreator.ValidateForCreation(state, preview);
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(preview);
            }

            DrawSection("Enemy Identity", () =>
            {
                state.EnemyId = EditorGUILayout.TextField("Stable ID", state.EnemyId);
                state.DisplayName = EditorGUILayout.TextField("Display Name", state.DisplayName);
                state.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", state.Icon, typeof(Sprite), false);
                state.Role = (EnemyRole)EditorGUILayout.EnumPopup("Role", state.Role);
                state.TagsCsv = EditorGUILayout.TextField("Tags", state.TagsCsv);
                state.OutputRoot = DrawOutputRootField(state.OutputRoot);
            });

            DrawSection("Stats", () =>
            {
                state.MaximumHealth = EditorGUILayout.FloatField("Max Health", state.MaximumHealth);
                state.MoveSpeed = EditorGUILayout.FloatField("Move Speed", state.MoveSpeed);
                state.RewardValue = EditorGUILayout.IntField("Reward Value", state.RewardValue);
                state.ContactDamage = EditorGUILayout.FloatField("Contact Damage", state.ContactDamage);
                state.DamageTypeId = EditorGUILayout.TextField("Damage Type ID", state.DamageTypeId);
                state.CollisionRadius = EditorGUILayout.FloatField("Collision Radius", state.CollisionRadius);
            });

            DrawSection("Presentation", () =>
            {
                state.Prefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", state.Prefab, typeof(GameObject), false);
                state.SpawnAudio = (AudioClip)EditorGUILayout.ObjectField("OnSpawn Audio", state.SpawnAudio, typeof(AudioClip), false);
                state.SpawnVfxPrefab = (GameObject)EditorGUILayout.ObjectField("OnSpawn VFX", state.SpawnVfxPrefab, typeof(GameObject), false);
                state.HitAudio = (AudioClip)EditorGUILayout.ObjectField("OnHit Audio", state.HitAudio, typeof(AudioClip), false);
                state.HitVfxPrefab = (GameObject)EditorGUILayout.ObjectField("OnHit VFX", state.HitVfxPrefab, typeof(GameObject), false);
                state.DeathAudio = (AudioClip)EditorGUILayout.ObjectField("OnDeath Audio", state.DeathAudio, typeof(AudioClip), false);
                state.DeathVfxPrefab = (GameObject)EditorGUILayout.ObjectField("OnDeath VFX", state.DeathVfxPrefab, typeof(GameObject), false);
            });

            DrawSection("Preview", () =>
            {
                foreach (string line in EnemyDefinitionAssetCreator.GetPreviewLines(state))
                    EditorGUILayout.LabelField(line, _muted);
                GUILayout.Space(6f);
                DrawValidation(report, "Ready to create one root EnemyDefinition asset with stats and presentation sub-assets.");
                GUILayout.Space(8f);
                GUI.enabled = report.IsValid;
                if (GUILayout.Button("Create Enemy Asset", _primaryButton, GUILayout.Height(30f)))
                {
                    _lastResult = EnemyDefinitionAssetCreator.CreateAssets(state);
                    if (_lastResult.CreatedRoot != null)
                    {
                        Selection.activeObject = _lastResult.CreatedRoot;
                        EditorGUIUtility.PingObject(_lastResult.CreatedRoot);
                    }
                }

                GUI.enabled = true;
                DrawLastResult();
            });
        }

        private void DrawWaveProvider(WaveAuthoringState state)
        {
            state.EnsureEntries();
            WaveDefinitionAsset preview = WaveDefinitionAssetCreator.BuildTransient(state);
            ContentAuthoringValidationReport report;
            try
            {
                report = WaveDefinitionAssetCreator.ValidateForCreation(state, preview);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(preview);
            }

            DrawSection("Wave Identity", () =>
            {
                state.WaveId = EditorGUILayout.TextField("Stable ID", state.WaveId);
                state.DisplayName = EditorGUILayout.TextField("Display Name", state.DisplayName);
                state.TagsCsv = EditorGUILayout.TextField("Tags", state.TagsCsv);
                state.StartTick = EditorGUILayout.IntField("Start Tick", state.StartTick);
                state.OutputRoot = DrawOutputRootField(state.OutputRoot);
            });

            DrawSection("Wave Entries", () =>
            {
                for (int i = 0; i < state.Entries.Count; i++)
                    DrawWaveEntry(state, i);

                GUILayout.Space(4f);
                if (GUILayout.Button("Add Entry", GUILayout.Height(24f)))
                    state.Entries.Add(new WaveEntryAuthoringState());
            });

            DrawSection("Preview", () =>
            {
                foreach (string line in WaveDefinitionAssetCreator.GetPreviewLines(state))
                    EditorGUILayout.LabelField(line, _muted);
                GUILayout.Space(6f);
                DrawValidation(report, "Ready to create one root WaveDefinition asset with schedule and entries sub-assets.");
                GUILayout.Space(8f);
                GUI.enabled = report.IsValid;
                if (GUILayout.Button("Create Wave Asset", _primaryButton, GUILayout.Height(30f)))
                {
                    _lastResult = WaveDefinitionAssetCreator.CreateAssets(state);
                    if (_lastResult.CreatedRoot != null)
                    {
                        Selection.activeObject = _lastResult.CreatedRoot;
                        EditorGUIUtility.PingObject(_lastResult.CreatedRoot);
                    }
                }

                GUI.enabled = true;
                DrawLastResult();
            });
        }

        private void DrawWaveEntry(WaveAuthoringState state, int index)
        {
            WaveEntryAuthoringState entry = state.Entries[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Entry " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), _sectionTitle);
                    GUI.enabled = state.Entries.Count > 1;
                    if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                    {
                        state.Entries.RemoveAt(index);
                        GUI.enabled = true;
                        return;
                    }

                    GUI.enabled = true;
                }

                entry.Enemy = (EnemyDefinitionAsset)EditorGUILayout.ObjectField("Enemy", entry.Enemy, typeof(EnemyDefinitionAsset), false);
                entry.Count = EditorGUILayout.IntField("Count", entry.Count);
                entry.BatchSize = EditorGUILayout.IntField("Batch Size", entry.BatchSize);
                entry.InitialDelayTicks = EditorGUILayout.IntField("Start Delay Ticks", entry.InitialDelayTicks);
                entry.IntervalTicks = EditorGUILayout.IntField("Interval Ticks", entry.IntervalTicks);
                entry.SpawnChannelId = EditorGUILayout.TextField("Lane / Channel", entry.SpawnChannelId);
                entry.ScalingTier = EditorGUILayout.IntField("Difficulty Tier", entry.ScalingTier);
            }
        }

        private string DrawOutputRootField(string outputRoot)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputRoot);
                DefaultAsset next = (DefaultAsset)EditorGUILayout.ObjectField("Output Root", asset, typeof(DefaultAsset), false);
                if (next != asset && next != null)
                {
                    string path = AssetDatabase.GetAssetPath(next);
                    if (AssetDatabase.IsValidFolder(path)) outputRoot = path;
                }

                if (GUILayout.Button(new GUIContent("Ping", "Ping output root"), GUILayout.Width(48f)) && asset != null)
                    EditorGUIUtility.PingObject(asset);
            }

            return EditorGUILayout.TextField("Output Path", outputRoot);
        }

        private void DrawValidation(ContentAuthoringValidationReport report, string readyMessage)
        {
            if (report == null || report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox(readyMessage, MessageType.Info);
                return;
            }

            string summary = report.ErrorCount == 0
                ? report.WarningCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " warning(s). You can create the asset after confirming any prompts."
                : report.ErrorCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocking issue(s) and " + report.WarningCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " warning(s).";
            EditorGUILayout.HelpBox(summary, report.ErrorCount == 0 ? MessageType.Warning : MessageType.Error);

            for (int i = 0; i < report.Issues.Count; i++)
            {
                ContentAuthoringValidationIssue issue = report.Issues[i];
                MessageType type = issue.Severity == ContentAuthoringValidationSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == ContentAuthoringValidationSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox(issue.Path + ": " + issue.Message, type);
            }
        }
    }

    internal sealed class EnemyAuthoringState
    {
        public string EnemyId = "enemy.example.basic";
        public string DisplayName = "Basic Enemy";
        public Sprite Icon;
        public EnemyRole Role = EnemyRole.Basic;
        public string TagsCsv = "enemy, basic";
        public string OutputRoot = "Assets/GameContent/Enemies";
        public GameObject Prefab;
        public float MaximumHealth = 8f;
        public float MoveSpeed = 2.2f;
        public int RewardValue = 1;
        public float ContactDamage = 3f;
        public string DamageTypeId = "damage.template.basic";
        public float CollisionRadius = 0.3f;
        public AudioClip SpawnAudio;
        public GameObject SpawnVfxPrefab;
        public AudioClip HitAudio;
        public GameObject HitVfxPrefab;
        public AudioClip DeathAudio;
        public GameObject DeathVfxPrefab;
    }

    internal sealed class WaveAuthoringState
    {
        public string WaveId = "wave.example.early";
        public string DisplayName = "Early Wave";
        public string TagsCsv = "wave, early";
        public string OutputRoot = "Assets/GameContent/Waves";
        public int StartTick;
        public readonly List<WaveEntryAuthoringState> Entries = new List<WaveEntryAuthoringState> { new WaveEntryAuthoringState() };

        public void EnsureEntries()
        {
            if (Entries.Count == 0) Entries.Add(new WaveEntryAuthoringState());
        }
    }

    internal sealed class WaveEntryAuthoringState
    {
        public EnemyDefinitionAsset Enemy;
        public int Count = 4;
        public int BatchSize = 1;
        public int InitialDelayTicks;
        public int IntervalTicks = 12;
        public string SpawnChannelId = "perimeter-north";
        public int ScalingTier;
    }

    internal static class EnemyDefinitionAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/Enemies";

        public static EnemyDefinitionAsset BuildTransient(EnemyAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static ContentAuthoringValidationReport ValidateForCreation(EnemyAuthoringState state, EnemyDefinitionAsset recipe)
        {
            var issues = new List<ContentAuthoringValidationIssue>(EnemyDefinitionValidator.Validate(recipe, EnemyDefinitionValidationOptions.AssetCreation).Issues);
            string folder = GetEnemyFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Enemy", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<EnemyDefinitionAsset>(state.EnemyId, asset => asset.Id))
                issues.Add(ContentAuthoringValidationIssue.Error("Enemy.Id", "Enemy IDs must be unique. Rename this enemy or edit the existing asset instead of creating another."));
            return new ContentAuthoringValidationReport(issues);
        }

        public static IReadOnlyList<string> GetPreviewLines(EnemyAuthoringState state)
        {
            return new[]
            {
                "Folder: " + GetEnemyFolder(state),
                "Root asset: " + GetFileStem(state) + "_EnemyDefinition.asset",
                "Sections: Stats, Presentation",
                "Stats: " + state.MaximumHealth.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " HP, " + state.MoveSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " speed, " + state.ContactDamage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " contact damage",
                "Role: " + state.Role + ", reward " + state.RewardValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "Runtime: template converts this data into enemy content and spawnable prefabs; optional audio/VFX are skipped when unset."
            };
        }

        public static ContentAssetCreationResult CreateAssets(EnemyAuthoringState state)
        {
            EnemyDefinitionAsset preview = BuildRecipe(state, true);
            ContentAuthoringValidationReport report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new ContentAssetCreationResult(false, "Fix validation errors before creating assets.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetEnemyFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new ContentAssetCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Enemy");
                if (!confirmed)
                    return new ContentAssetCreationResult(false, "Creation canceled before writing into existing folder.", null);
            }

            folder = GameContentAuthoringEditorPaths.EnsureFolder(folder, DefaultRoot);
            EnemyDefinitionAsset root = BuildRecipe(state, false);
            AssetDatabase.CreateAsset(root, rootPath);
            GameContentAuthoringEditorAssets.AddSubAsset(root.Stats, root, GetFileStem(state) + "_Stats");
            GameContentAuthoringEditorAssets.AddSubAsset(root.Presentation, root, GetFileStem(state) + "_Presentation");
            root.Configure(state.EnemyId, state.DisplayName, state.Icon, state.Role, GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv), root.Stats, root.Presentation);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new ContentAssetCreationResult(true, "Created enemy definition at " + rootPath, AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(rootPath));
        }

        public static void DestroyTransient(EnemyDefinitionAsset recipe)
        {
            if (recipe == null || recipe.hideFlags != HideFlags.HideAndDontSave) return;
            EnemyStatsDefinitionAsset stats = recipe.Stats;
            EnemyPresentationDefinitionAsset presentation = recipe.Presentation;
            GameContentAuthoringEditorAssets.DestroyTransientObject(stats);
            GameContentAuthoringEditorAssets.DestroyTransientObject(presentation);
            GameContentAuthoringEditorAssets.DestroyTransientObject(recipe);
        }

        private static EnemyDefinitionAsset BuildRecipe(EnemyAuthoringState state, bool transient)
        {
            var stats = ScriptableObject.CreateInstance<EnemyStatsDefinitionAsset>();
            var presentation = ScriptableObject.CreateInstance<EnemyPresentationDefinitionAsset>();
            var root = ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
            if (transient)
            {
                stats.hideFlags = HideFlags.HideAndDontSave;
                presentation.hideFlags = HideFlags.HideAndDontSave;
                root.hideFlags = HideFlags.HideAndDontSave;
            }

            stats.Configure(state.MaximumHealth, state.MoveSpeed, state.RewardValue, state.ContactDamage, state.DamageTypeId, state.CollisionRadius);
            presentation.Configure(state.Prefab, new[]
            {
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn, state.SpawnAudio, state.SpawnVfxPrefab),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit, state.HitAudio, state.HitVfxPrefab),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath, state.DeathAudio, state.DeathVfxPrefab)
            });
            root.Configure(state.EnemyId, state.DisplayName, state.Icon, state.Role, GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv), stats, presentation);
            return root;
        }

        private static string GetEnemyFolder(EnemyAuthoringState state)
        {
            string root = GameContentAuthoringEditorPaths.NormalizeAssetFolderPath(state.OutputRoot, DefaultRoot);
            return root.TrimEnd('/') + "/" + GetFileStem(state);
        }

        private static string GetRootPath(EnemyAuthoringState state)
        {
            return GetEnemyFolder(state) + "/" + GetFileStem(state) + "_EnemyDefinition.asset";
        }

        private static string GetFileStem(EnemyAuthoringState state)
        {
            return GameContentAuthoringEditorPaths.SanitizePathSegment(state.EnemyId, "NewEnemy");
        }
    }

    internal static class WaveDefinitionAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/Waves";

        public static WaveDefinitionAsset BuildTransient(WaveAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static ContentAuthoringValidationReport ValidateForCreation(WaveAuthoringState state, WaveDefinitionAsset recipe)
        {
            var issues = new List<ContentAuthoringValidationIssue>(WaveDefinitionValidator.Validate(recipe, WaveDefinitionValidationOptions.AssetCreation).Issues);
            string folder = GetWaveFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Wave", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<WaveDefinitionAsset>(state.WaveId, asset => asset.Id))
                issues.Add(ContentAuthoringValidationIssue.Error("Wave.Id", "Wave IDs must be unique. Rename this wave or edit the existing asset instead of creating another."));
            return new ContentAuthoringValidationReport(issues);
        }

        public static IReadOnlyList<string> GetPreviewLines(WaveAuthoringState state)
        {
            return new[]
            {
                "Folder: " + GetWaveFolder(state),
                "Root asset: " + GetFileStem(state) + "_WaveDefinition.asset",
                "Sections: Schedule, Entries",
                "Schedule: starts at tick " + state.StartTick.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "Entries: " + GetEntrySummary(state),
                "Runtime: template converts entries into Encounter waves and spawn groups; enemy references must be assigned and valid."
            };
        }

        public static ContentAssetCreationResult CreateAssets(WaveAuthoringState state)
        {
            WaveDefinitionAsset preview = BuildRecipe(state, true);
            ContentAuthoringValidationReport report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new ContentAssetCreationResult(false, "Fix validation errors before creating assets.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetWaveFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new ContentAssetCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Wave");
                if (!confirmed)
                    return new ContentAssetCreationResult(false, "Creation canceled before writing into existing folder.", null);
            }

            folder = GameContentAuthoringEditorPaths.EnsureFolder(folder, DefaultRoot);
            WaveDefinitionAsset root = BuildRecipe(state, false);
            AssetDatabase.CreateAsset(root, rootPath);
            GameContentAuthoringEditorAssets.AddSubAsset(root.Schedule, root, GetFileStem(state) + "_Schedule");
            GameContentAuthoringEditorAssets.AddSubAsset(root.Entries, root, GetFileStem(state) + "_Entries");
            root.Configure(state.WaveId, state.DisplayName, GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv), root.Schedule, root.Entries);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new ContentAssetCreationResult(true, "Created wave definition at " + rootPath, AssetDatabase.LoadAssetAtPath<WaveDefinitionAsset>(rootPath));
        }

        public static void DestroyTransient(WaveDefinitionAsset recipe)
        {
            if (recipe == null || recipe.hideFlags != HideFlags.HideAndDontSave) return;
            WaveScheduleDefinitionAsset schedule = recipe.Schedule;
            WaveEntriesDefinitionAsset entries = recipe.Entries;
            GameContentAuthoringEditorAssets.DestroyTransientObject(schedule);
            GameContentAuthoringEditorAssets.DestroyTransientObject(entries);
            GameContentAuthoringEditorAssets.DestroyTransientObject(recipe);
        }

        private static WaveDefinitionAsset BuildRecipe(WaveAuthoringState state, bool transient)
        {
            var schedule = ScriptableObject.CreateInstance<WaveScheduleDefinitionAsset>();
            var entries = ScriptableObject.CreateInstance<WaveEntriesDefinitionAsset>();
            var root = ScriptableObject.CreateInstance<WaveDefinitionAsset>();
            if (transient)
            {
                schedule.hideFlags = HideFlags.HideAndDontSave;
                entries.hideFlags = HideFlags.HideAndDontSave;
                root.hideFlags = HideFlags.HideAndDontSave;
            }

            schedule.Configure(state.StartTick);
            entries.Configure(GetEntries(state));
            root.Configure(state.WaveId, state.DisplayName, GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv), schedule, entries);
            return root;
        }

        private static IReadOnlyList<WaveEntryRecipe> GetEntries(WaveAuthoringState state)
        {
            state.EnsureEntries();
            var entries = new WaveEntryRecipe[state.Entries.Count];
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                entries[i] = new WaveEntryRecipe(entry.Enemy, entry.Count, entry.BatchSize, entry.InitialDelayTicks, entry.IntervalTicks, entry.SpawnChannelId, entry.ScalingTier);
            }

            return entries;
        }

        private static string GetWaveFolder(WaveAuthoringState state)
        {
            string root = GameContentAuthoringEditorPaths.NormalizeAssetFolderPath(state.OutputRoot, DefaultRoot);
            return root.TrimEnd('/') + "/" + GetFileStem(state);
        }

        private static string GetRootPath(WaveAuthoringState state)
        {
            return GetWaveFolder(state) + "/" + GetFileStem(state) + "_WaveDefinition.asset";
        }

        private static string GetFileStem(WaveAuthoringState state)
        {
            return GameContentAuthoringEditorPaths.SanitizePathSegment(state.WaveId, "NewWave");
        }

        private static string GetEntrySummary(WaveAuthoringState state)
        {
            int count = state.Entries == null ? 0 : state.Entries.Count;
            int total = 0;
            if (state.Entries != null)
                for (int i = 0; i < state.Entries.Count; i++)
                    total += Math.Max(0, state.Entries[i].Count);
            return count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " group(s), " + total.ToString(System.Globalization.CultureInfo.InvariantCulture) + " enemy spawn(s)";
        }
    }
}
