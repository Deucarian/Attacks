using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    [InitializeOnLoad]
    internal static class AttackGameContentAuthoringProviderRegistration
    {
        static AttackGameContentAuthoringProviderRegistration()
        {
            GameContentAuthoringProviderRegistry.Register(new AttackAuthoringProvider());
            GameContentAuthoringProviderRegistry.Register(new EnemyAuthoringProvider());
            GameContentAuthoringProviderRegistry.Register(new WaveAuthoringProvider());
        }
    }

    internal sealed class AttackAuthoringProvider : IGameContentAuthoringProvider, IGameContentAuthoringSurfaceProvider, IGameContentAuthoringLensProvider
    {
        private readonly AttackAuthoringState _state = new AttackAuthoringState();
        private readonly AttackGameContentPreviewController _preview = new AttackGameContentPreviewController();
        private readonly AttackProviderV2State _v2State = new AttackProviderV2State();
        private readonly AttackProviderV2View _v2View = new AttackProviderV2View();
        private readonly AttackPackAwareLensState _packState = new AttackPackAwareLensState();

        public string ProviderId => "com.deucarian.attacks.attack";
        public string DisplayName => "Attacks";
        public string Description => "Inspect attack-capable records in the selected pack or author standalone Attack assets in Project Content.";
        public int SortOrder => 100;
        public bool Enabled => true;
        public GameContentLensDescriptor Lens { get; } = new GameContentLensDescriptor(
            "attack",
            "Attacks",
            "Combat",
            "attack",
            100,
            new[] { GameContentRecordCapabilities.Attack });
        public void OnSelected()
        {
            _v2State.ResetProviderSession();
            _packState.Browser.SearchText = string.Empty;
        }
        public void DrawPreview(GameContentAuthoringPreviewContext context) { _preview.Draw(context, _state); }
        public void StopPreview()
        {
            _preview.Stop();
            _v2State.StopPreview();
        }

        public void DrawCustomAuthoringSurface(GameContentAuthoringSurfaceContext context)
        {
            if (context.PackContext != null && !context.PackContext.IsProjectContent)
            {
                AttackPackAwareLensView.Draw(context, Lens, _packState);
                return;
            }
            _v2View.Draw(context, _state, _preview, _v2State);
        }

        public void Draw(GameContentAuthoringContext context)
        {
            AttackDefinitionAsset preview = AttackRecipeAssetCreator.BuildTransient(_state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = AttackRecipeAssetCreator.ValidateForCreation(_state, preview);
            }
            finally
            {
                AttackRecipeAssetCreator.DestroyTransient(preview);
            }

            context.DrawSection("Attack Identity", () =>
            {
                _state.AttackId = context.DrawTextField("Stable ID", _state.AttackId);
                _state.DisplayName = context.DrawTextField("Display Name", _state.DisplayName);
                _state.Icon = context.DrawObjectField("Icon", _state.Icon);
                _state.TagsCsv = context.DrawTextField("Tags", _state.TagsCsv);
                _state.OutputRoot = context.DrawOutputRootField(_state.OutputRoot);
            });

            context.DrawSection("Mechanics", () =>
            {
                _state.DamageTypeId = context.DrawTextField("Damage Type ID", _state.DamageTypeId);
                _state.DamageAmount = context.DrawFloatField("Damage", _state.DamageAmount);
                _state.CooldownTicks = context.DrawIntField("Cooldown Ticks", _state.CooldownTicks);
                _state.Range = context.DrawFloatField("Range", _state.Range);
                _state.TargetingMode = context.DrawEnumPopup("Targeting", _state.TargetingMode);
            });

            context.DrawSection("Delivery", () =>
            {
                _state.DeliveryMode = context.DrawEnumPopup("Mode", _state.DeliveryMode);
                if (_state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
                {
                    _state.ProjectileDefinitionId = context.DrawTextField("Projectile ID", _state.ProjectileDefinitionId);
                    _state.ProjectileSpawnableId = context.DrawTextField("Spawnable ID", _state.ProjectileSpawnableId);
                    _state.ProjectilePrefab = context.DrawObjectField("Projectile Prefab", _state.ProjectilePrefab);
                    _state.ProjectileSpeed = context.DrawFloatField("Speed", _state.ProjectileSpeed);
                    _state.ProjectileLifetimeTicks = context.DrawIntField("Lifetime Ticks", _state.ProjectileLifetimeTicks);
                    _state.Homing = context.DrawToggle("Homing", _state.Homing);
                    _state.HomingTurnRate = context.DrawFloatField("Turn Rate", _state.HomingTurnRate);
                    _state.PierceCount = context.DrawIntField("Pierce Count", _state.PierceCount);
                    _state.Radius = context.DrawFloatField("Radius", _state.Radius);
                }
                else if (_state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                {
                    _state.BeamVfxPrefab = context.DrawObjectField("Beam/Tracer VFX", _state.BeamVfxPrefab);
                    _state.ImpactVfxPrefab = context.DrawObjectField("Impact VFX", _state.ImpactVfxPrefab);
                    _state.MaxHits = context.DrawIntField("Max Hits", _state.MaxHits);
                }
                else
                {
                    _state.Radius = context.DrawFloatField("Radius", _state.Radius);
                    _state.MaxHits = context.DrawIntField("Max Hits", _state.MaxHits);
                    if (_state.DeliveryMode == AttackRecipeDeliveryMode.Aura)
                        _state.TickIntervalSeconds = context.DrawFloatField("Tick Interval", _state.TickIntervalSeconds);
                }
            });

            context.DrawSection("Status Effects", () =>
            {
                _state.IncludeStatusEffect = context.DrawToggle("Include Status", _state.IncludeStatusEffect);
                if (_state.IncludeStatusEffect)
                {
                    _state.StatusId = context.DrawTextField("Status ID", _state.StatusId);
                    _state.StatusDurationTicks = context.DrawIntField("Duration Ticks", _state.StatusDurationTicks);
                    _state.StatusTickRateTicks = context.DrawIntField("Tick Rate Ticks", _state.StatusTickRateTicks);
                    _state.StatusStrength = context.DrawFloatField("Strength", _state.StatusStrength);
                    _state.StatusMaxStacks = context.DrawIntField("Max Stacks", _state.StatusMaxStacks);
                    _state.StatusStackingPolicy = context.DrawEnumPopup("Stacking", _state.StatusStackingPolicy);
                    _state.StatusEffectNote = context.DrawTextField("Effect Note", _state.StatusEffectNote);
                }
            });

            context.DrawSection("Presentation", () =>
            {
                _state.CastAudio = context.DrawObjectField("OnCast Audio", _state.CastAudio);
                _state.FireAudio = context.DrawObjectField("OnFire Audio", _state.FireAudio);
                _state.ImpactAudio = context.DrawObjectField("OnImpact Audio", _state.ImpactAudio);
                _state.TickAudio = context.DrawObjectField("OnTick Audio", _state.TickAudio);
                _state.ExpireAudio = context.DrawObjectField("OnExpire Audio", _state.ExpireAudio);
                _state.CastVfxPrefab = context.DrawObjectField("OnCast VFX", _state.CastVfxPrefab);
                _state.FireVfxPrefab = context.DrawObjectField("OnFire VFX", _state.FireVfxPrefab);
                _state.ImpactVfxPresentationPrefab = context.DrawObjectField("OnImpact VFX", _state.ImpactVfxPresentationPrefab);
                _state.TickVfxPrefab = context.DrawObjectField("OnTick VFX", _state.TickVfxPrefab);
                _state.ExpireVfxPrefab = context.DrawObjectField("OnExpire VFX", _state.ExpireVfxPrefab);
            });

            context.DrawSection("Preview", () =>
            {
                foreach (string line in AttackRecipeAssetCreator.GetPreviewLines(_state))
                    EditorGUILayout.LabelField(line, context.MutedStyle);
                GUILayout.Space(6f);
                context.DrawValidation(report, "Ready to create one root AttackDefinition asset with focused sub-assets.");
                GUILayout.Space(8f);
                if (context.DrawCreateButton("Create Attack Asset", report.IsValid))
                    context.SetCreationResult(AttackRecipeAssetCreator.CreateAssets(_state));
                context.DrawCreationResult();
            });
        }
    }

    internal sealed class EnemyAuthoringProvider : IGameContentAuthoringProvider, IGameContentAuthoringSurfaceProvider, IGameContentAuthoringLensProvider
    {
        private readonly EnemyAuthoringState _state = new EnemyAuthoringState();
        private readonly EnemyGameContentPreviewController _preview = new EnemyGameContentPreviewController();
        private readonly EnemyProviderV2State _v2State = new EnemyProviderV2State();
        private readonly EnemyProviderV2View _v2View = new EnemyProviderV2View();
        private readonly EnemyPackAwareLensState _packState = new EnemyPackAwareLensState();

        public string ProviderId => "com.deucarian.attacks.enemy";
        public string DisplayName => "Enemies";
        public string Description => "Inspect enemy-capable records in the selected pack or author standalone Enemy assets in Project Content.";
        public int SortOrder => 110;
        public bool Enabled => true;
        public GameContentLensDescriptor Lens { get; } = new GameContentLensDescriptor(
            "enemy",
            "Enemies",
            "Actors",
            "enemy",
            200,
            new[] { GameContentRecordCapabilities.Enemy });
        public void OnSelected()
        {
            _v2State.ResetProviderSession();
            _packState.Browser.SearchText = string.Empty;
        }
        public void DrawPreview(GameContentAuthoringPreviewContext context) { _preview.Draw(context, _state); }
        public void StopPreview()
        {
            _preview.Stop();
            _v2State.StopPreview();
        }

        public void DrawCustomAuthoringSurface(GameContentAuthoringSurfaceContext context)
        {
            if (context.PackContext != null && !context.PackContext.IsProjectContent)
            {
                EnemyPackAwareLensView.Draw(context, Lens, _packState);
                return;
            }
            _v2View.Draw(context, _state, _preview, _v2State);
        }

        public void Draw(GameContentAuthoringContext context)
        {
            EnemyDefinitionAsset preview = EnemyDefinitionAssetCreator.BuildTransient(_state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = EnemyDefinitionAssetCreator.ValidateForCreation(_state, preview);
            }
            finally
            {
                EnemyDefinitionAssetCreator.DestroyTransient(preview);
            }

            context.DrawSection("Enemy Identity", () =>
            {
                _state.EnemyId = context.DrawTextField("Stable ID", _state.EnemyId);
                _state.DisplayName = context.DrawTextField("Display Name", _state.DisplayName);
                _state.Icon = context.DrawObjectField("Icon", _state.Icon);
                _state.Role = context.DrawEnumPopup("Role", _state.Role);
                _state.TagsCsv = context.DrawTextField("Tags", _state.TagsCsv);
                _state.OutputRoot = context.DrawOutputRootField(_state.OutputRoot);
            });

            context.DrawSection("Stats", () =>
            {
                _state.MaximumHealth = context.DrawFloatField("Max Health", _state.MaximumHealth);
                _state.MoveSpeed = context.DrawFloatField("Move Speed", _state.MoveSpeed);
                _state.RewardValue = context.DrawIntField("Reward Value", _state.RewardValue);
                _state.ContactDamage = context.DrawFloatField("Contact Damage", _state.ContactDamage);
                _state.DamageTypeId = context.DrawTextField("Damage Type ID", _state.DamageTypeId);
                _state.CollisionRadius = context.DrawFloatField("Collision Radius", _state.CollisionRadius);
            });

            context.DrawSection("Presentation", () =>
            {
                _state.Prefab = context.DrawObjectField("Enemy Prefab", _state.Prefab);
                _state.SpawnAudio = context.DrawObjectField("OnSpawn Audio", _state.SpawnAudio);
                _state.SpawnVfxPrefab = context.DrawObjectField("OnSpawn VFX", _state.SpawnVfxPrefab);
                _state.HitAudio = context.DrawObjectField("OnHit Audio", _state.HitAudio);
                _state.HitVfxPrefab = context.DrawObjectField("OnHit VFX", _state.HitVfxPrefab);
                _state.DeathAudio = context.DrawObjectField("OnDeath Audio", _state.DeathAudio);
                _state.DeathVfxPrefab = context.DrawObjectField("OnDeath VFX", _state.DeathVfxPrefab);
            });

            context.DrawSection("Preview", () =>
            {
                foreach (string line in EnemyDefinitionAssetCreator.GetPreviewLines(_state))
                    EditorGUILayout.LabelField(line, context.MutedStyle);
                GUILayout.Space(6f);
                context.DrawValidation(report, "Ready to create one root EnemyDefinition asset with stats and presentation sub-assets.");
                GUILayout.Space(8f);
                if (context.DrawCreateButton("Create Enemy Asset", report.IsValid))
                    context.SetCreationResult(EnemyDefinitionAssetCreator.CreateAssets(_state));
                context.DrawCreationResult();
            });
        }
    }

    internal sealed class WaveAuthoringProvider : IGameContentAuthoringProvider, IGameContentAuthoringSurfaceProvider, IGameContentAuthoringLensProvider
    {
        private readonly WaveAuthoringState _state = new WaveAuthoringState();
        private readonly WaveGameContentPreviewController _preview = new WaveGameContentPreviewController();
        private readonly WaveProviderV2State _v2State = new WaveProviderV2State();
        private readonly WaveProviderV2View _v2View = new WaveProviderV2View();
        private readonly EncounterPackAwareLensState _packState = new EncounterPackAwareLensState();

        public string ProviderId => "com.deucarian.attacks.wave";
        public string DisplayName => "Wave / Encounter";
        public string Description => "Inspect waves, run profiles, and timed encounters or author standalone Wave assets in Project Content.";
        public int SortOrder => 120;
        public bool Enabled => true;
        public GameContentLensDescriptor Lens { get; } = new GameContentLensDescriptor(
            "wave-encounter",
            "Wave / Encounter",
            "Encounters",
            "wave",
            300,
            new[] { GameContentRecordCapabilities.Encounter, GameContentRecordCapabilities.Wave });
        public void OnSelected() { _v2State.ResetProviderSession(); _packState.Browser.SearchText = string.Empty; }
        public void DrawPreview(GameContentAuthoringPreviewContext context) { _preview.Draw(context, _state); }
        public void StopPreview() { _preview.Stop(); _v2State.StopPreview(); }
        public void DrawCustomAuthoringSurface(GameContentAuthoringSurfaceContext context)
        {
            if (context.PackContext != null && !context.PackContext.IsProjectContent)
            {
                EncounterPackAwareLensView.Draw(context, Lens, _packState);
                return;
            }
            _v2View.Draw(context, _state, _preview, _v2State);
        }

        public void Draw(GameContentAuthoringContext context)
        {
            _state.EnsureEntries();
            WaveDefinitionAsset preview = WaveDefinitionAssetCreator.BuildTransient(_state);
            GameContentAuthoringValidationResult report;
            try
            {
                report = WaveDefinitionAssetCreator.ValidateForCreation(_state, preview);
            }
            finally
            {
                WaveDefinitionAssetCreator.DestroyTransient(preview);
            }

            context.DrawSection("Wave Identity", () =>
            {
                _state.WaveId = context.DrawTextField("Stable ID", _state.WaveId);
                _state.DisplayName = context.DrawTextField("Display Name", _state.DisplayName);
                _state.TagsCsv = context.DrawTextField("Tags", _state.TagsCsv);
                _state.StartTick = context.DrawIntField("Start Tick", _state.StartTick);
                _state.OutputRoot = context.DrawOutputRootField(_state.OutputRoot);
            });

            context.DrawSection("Wave Entries", () =>
            {
                for (int i = 0; i < _state.Entries.Count; i++)
                    DrawWaveEntry(context, i);

                GUILayout.Space(4f);
                if (context.DrawSecondaryButton("Add Entry", true, GUILayout.Height(24f)))
                    _state.Entries.Add(new WaveEntryAuthoringState());
            });

            context.DrawSection("Preview", () =>
            {
                foreach (string line in WaveDefinitionAssetCreator.GetPreviewLines(_state))
                    EditorGUILayout.LabelField(line, context.MutedStyle);
                GUILayout.Space(6f);
                context.DrawValidation(report, "Ready to create one root WaveDefinition asset with schedule and entries sub-assets.");
                GUILayout.Space(8f);
                if (context.DrawCreateButton("Create Wave Asset", report.IsValid))
                    context.SetCreationResult(WaveDefinitionAssetCreator.CreateAssets(_state));
                context.DrawCreationResult();
            });
        }

        private void DrawWaveEntry(GameContentAuthoringContext context, int index)
        {
            WaveEntryAuthoringState entry = _state.Entries[index];
            bool remove = false;
            context.DrawInlineCard(() =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Entry " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), context.SectionTitleStyle);
                    if (context.DrawSecondaryButton("Remove", _state.Entries.Count > 1, GUILayout.Width(72f)))
                    {
                        remove = true;
                    }
                }

                if (remove)
                    return;

                entry.Enemy = context.DrawObjectField("Enemy", entry.Enemy);
                entry.Count = context.DrawIntField("Count", entry.Count);
                entry.BatchSize = context.DrawIntField("Batch Size", entry.BatchSize);
                entry.InitialDelayTicks = context.DrawIntField("Start Delay Ticks", entry.InitialDelayTicks);
                entry.IntervalTicks = context.DrawIntField("Interval Ticks", entry.IntervalTicks);
                entry.SpawnChannelId = context.DrawTextField("Lane / Channel", entry.SpawnChannelId);
                entry.ScalingTier = context.DrawIntField("Difficulty Tier", entry.ScalingTier);
            });

            if (remove)
                _state.Entries.RemoveAt(index);
        }
    }

    internal sealed class AttackAuthoringState
    {
        public string AttackId = "attack.example.fire-orb";
        public string DisplayName = "Fire Orb";
        public Sprite Icon;
        public string TagsCsv = "projectile, fire";
        public string OutputRoot = "Assets/GameContent/Attacks";
        public string DamageTypeId = "damage.fire";
        public float DamageAmount = 8f;
        public int CooldownTicks = 30;
        public float Range = 6f;
        public AttackRecipeTargetingMode TargetingMode = AttackRecipeTargetingMode.Nearest;
        public AttackRecipeDeliveryMode DeliveryMode = AttackRecipeDeliveryMode.Projectile;
        public string ProjectileDefinitionId = "projectile.example.fire-orb";
        public string ProjectileSpawnableId = "projectile.example.fire-orb";
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 8f;
        public int ProjectileLifetimeTicks = 120;
        public bool Homing;
        public float HomingTurnRate = 180f;
        public int PierceCount;
        public float Radius = 1.5f;
        public GameObject BeamVfxPrefab;
        public GameObject ImpactVfxPrefab;
        public int MaxHits = 1;
        public float TickIntervalSeconds = 0.5f;
        public bool IncludeStatusEffect;
        public string StatusId = "status.example.burning";
        public int StatusDurationTicks = 90;
        public int StatusTickRateTicks = 30;
        public float StatusStrength = 1f;
        public int StatusMaxStacks = 1;
        public Deucarian.Combat.StatusStackingPolicy StatusStackingPolicy = Deucarian.Combat.StatusStackingPolicy.UniqueRefresh;
        public string StatusEffectNote = "Placeholder status hook.";
        public AudioClip CastAudio;
        public AudioClip FireAudio;
        public AudioClip ImpactAudio;
        public AudioClip TickAudio;
        public AudioClip ExpireAudio;
        public GameObject CastVfxPrefab;
        public GameObject FireVfxPrefab;
        public GameObject ImpactVfxPresentationPrefab;
        public GameObject TickVfxPrefab;
        public GameObject ExpireVfxPrefab;
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

    internal static class AttackRecipeAssetCreator
    {
        public static AttackDefinitionAsset BuildTransient(AttackAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static GameContentAuthoringValidationResult ValidateForCreation(AttackAuthoringState state, AttackDefinitionAsset recipe)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            AttackRecipeValidationReport domainReport = AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.AssetCreation);
            AddAttackIssues(issues, domainReport);
            string folder = GetAttackFolder(state);
            string rootPath = folder + "/" + GetFileStem(state) + "_AttackDefinition.asset";
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, "Assets/GameContent/Attacks", folder, rootPath, "Attack", "OutputRoot");
            if (HasDuplicateAttackId(state.AttackId))
                issues.Add(GameContentAuthoringValidationIssue.Error("Attack.Id", "Attack IDs must be unique. Rename this attack or edit the existing asset instead of creating another."));
            return new GameContentAuthoringValidationResult(issues);
        }

        public static GameContentAuthoringValidationResult ValidateForUpdate(AttackAuthoringState state, AttackDefinitionAsset existingAsset)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (state == null)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error("Attack", "No attack edit state is available."));
                return new GameContentAuthoringValidationResult(issues);
            }

            AttackDefinitionAsset preview = BuildRecipe(state, true);
            try
            {
                AddAttackIssues(issues, AttackRecipeValidator.Validate(preview, AttackRecipeValidationOptions.RuntimeFriendly));
                if (existingAsset == null)
                    issues.Add(GameContentAuthoringValidationIssue.Error("Attack", "No existing attack asset is selected."));
                else if (HasDuplicateAttackIdExcept(state.AttackId, existingAsset))
                    issues.Add(GameContentAuthoringValidationIssue.Error("Attack.Id", "Attack IDs must be unique. Another attack already uses this stable ID."));
                return new GameContentAuthoringValidationResult(issues);
            }
            finally
            {
                DestroyTransient(preview);
            }
        }

        public static IReadOnlyList<string> GetPreviewLines(AttackAuthoringState state)
        {
            string folder = GetAttackFolder(state);
            return new[]
            {
                "Folder: " + folder,
                "Root asset: " + GetFileStem(state) + "_AttackDefinition.asset",
                "Sections: Mechanics, Targeting, Delivery, StatusEffects, Presentation",
                "Delivery: " + GetDeliverySummary(state),
                "Status: " + GetStatusSummary(state),
                "Runtime: converts to a pure AttackDefinition; optional audio/VFX are skipped when unset."
            };
        }

        public static GameContentCreationResult CreateAssets(AttackAuthoringState state)
        {
            AttackDefinitionAsset preview = BuildRecipe(state, true);
            GameContentAuthoringValidationResult report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new GameContentCreationResult(false, "Fix validation errors before creating assets.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetAttackFolder(state);
            string rootPath = folder + "/" + GetFileStem(state) + "_AttackDefinition.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new GameContentCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Attack");
                if (!confirmed)
                    return new GameContentCreationResult(false, "Creation canceled before writing into existing folder.", null);
            }

            folder = EnsureFolder(folder);

            AttackDefinitionAsset root = BuildRecipe(state, false);
            AssetDatabase.CreateAsset(root, rootPath);
            AddSubAsset(root.Mechanics, root, GetFileStem(state) + "_Mechanics");
            AddSubAsset(root.Targeting, root, GetFileStem(state) + "_Targeting");
            AddSubAsset(root.Delivery, root, GetFileStem(state) + "_Delivery");
            AddSubAsset(root.StatusEffects, root, GetFileStem(state) + "_StatusEffects");
            AddSubAsset(root.Presentation, root, GetFileStem(state) + "_Presentation");
            root.Configure(
                state.AttackId,
                state.DisplayName,
                state.Icon,
                SplitCsv(state.TagsCsv),
                root.Mechanics,
                root.Targeting,
                root.Delivery,
                root.StatusEffects,
                root.Presentation);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Created attack recipe at " + rootPath, AssetDatabase.LoadAssetAtPath<AttackDefinitionAsset>(rootPath));
        }

        public static GameContentCreationResult UpdateExistingAsset(AttackDefinitionAsset root, AttackAuthoringState state)
        {
            if (root == null)
                return new GameContentCreationResult(false, "No attack asset selected.", null);
            if (state == null)
                return new GameContentCreationResult(false, "No attack edit state is available.", root);

            GameContentAuthoringValidationResult report = ValidateForUpdate(state, root);
            if (!report.IsValid)
                return new GameContentCreationResult(false, "Fix validation errors before saving this attack.", root);

            string rootPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(rootPath))
                return new GameContentCreationResult(false, "Selected attack is not a persisted asset.", root);

            string stem = GetFileStem(state);
            AttackMechanicsDefinitionAsset mechanics = EnsureSectionAsset(root.Mechanics, rootPath, stem + "_Mechanics");
            AttackTargetingDefinitionAsset targeting = EnsureSectionAsset(root.Targeting, rootPath, stem + "_Targeting");
            AttackDeliveryDefinitionAsset delivery = EnsureSectionAsset(root.Delivery, rootPath, stem + "_Delivery");
            AttackStatusEffectsDefinitionAsset statuses = EnsureSectionAsset(root.StatusEffects, rootPath, stem + "_StatusEffects");
            AttackPresentationDefinitionAsset presentation = EnsureSectionAsset(root.Presentation, rootPath, stem + "_Presentation");

            Undo.RegisterCompleteObjectUndo(root, "Save Attack");
            Undo.RegisterCompleteObjectUndo(mechanics, "Save Attack Mechanics");
            Undo.RegisterCompleteObjectUndo(targeting, "Save Attack Targeting");
            Undo.RegisterCompleteObjectUndo(delivery, "Save Attack Delivery");
            Undo.RegisterCompleteObjectUndo(statuses, "Save Attack Status Effects");
            Undo.RegisterCompleteObjectUndo(presentation, "Save Attack Presentation");

            mechanics.Configure(state.CooldownTicks, state.Range, state.DamageAmount, state.DamageTypeId);
            targeting.Configure(state.TargetingMode);
            ConfigureDelivery(delivery, state);
            statuses.Configure(GetStatuses(state));
            presentation.Configure(GetPresentationEvents(state));
            root.Configure(
                state.AttackId,
                state.DisplayName,
                state.Icon,
                SplitCsv(state.TagsCsv),
                mechanics,
                targeting,
                delivery,
                statuses,
                presentation,
                root.UpgradeHookId,
                root.BalancingNotes);

            MarkDirty(mechanics);
            MarkDirty(targeting);
            MarkDirty(delivery);
            MarkDirty(statuses);
            MarkDirty(presentation);
            MarkDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Saved attack " + root.DisplayName + ".", root);
        }

        public static void DestroyTransient(AttackDefinitionAsset recipe)
        {
            if (recipe == null || recipe.hideFlags != HideFlags.HideAndDontSave) return;
            AttackMechanicsDefinitionAsset mechanics = recipe.Mechanics;
            AttackTargetingDefinitionAsset targeting = recipe.Targeting;
            AttackDeliveryDefinitionAsset delivery = recipe.Delivery;
            AttackStatusEffectsDefinitionAsset statuses = recipe.StatusEffects;
            AttackPresentationDefinitionAsset presentation = recipe.Presentation;
            DestroyTransientObject(mechanics);
            DestroyTransientObject(targeting);
            DestroyTransientObject(delivery);
            DestroyTransientObject(statuses);
            DestroyTransientObject(presentation);
            DestroyTransientObject(recipe);
        }

        private static AttackDefinitionAsset BuildRecipe(AttackAuthoringState state, bool transient)
        {
            var mechanics = ScriptableObject.CreateInstance<AttackMechanicsDefinitionAsset>();
            var targeting = ScriptableObject.CreateInstance<AttackTargetingDefinitionAsset>();
            var delivery = ScriptableObject.CreateInstance<AttackDeliveryDefinitionAsset>();
            var statuses = ScriptableObject.CreateInstance<AttackStatusEffectsDefinitionAsset>();
            var presentation = ScriptableObject.CreateInstance<AttackPresentationDefinitionAsset>();
            var root = ScriptableObject.CreateInstance<AttackDefinitionAsset>();
            if (transient)
            {
                mechanics.hideFlags = HideFlags.HideAndDontSave;
                targeting.hideFlags = HideFlags.HideAndDontSave;
                delivery.hideFlags = HideFlags.HideAndDontSave;
                statuses.hideFlags = HideFlags.HideAndDontSave;
                presentation.hideFlags = HideFlags.HideAndDontSave;
                root.hideFlags = HideFlags.HideAndDontSave;
            }

            mechanics.Configure(state.CooldownTicks, state.Range, state.DamageAmount, state.DamageTypeId);
            targeting.Configure(state.TargetingMode);
            ConfigureDelivery(delivery, state);
            statuses.Configure(GetStatuses(state));
            presentation.Configure(GetPresentationEvents(state));
            root.Configure(
                state.AttackId,
                state.DisplayName,
                state.Icon,
                SplitCsv(state.TagsCsv),
                mechanics,
                targeting,
                delivery,
                statuses,
                presentation);
            return root;
        }

        private static void ConfigureDelivery(AttackDeliveryDefinitionAsset delivery, AttackAuthoringState state)
        {
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
            {
                delivery.ConfigureProjectile(
                    state.ProjectileDefinitionId,
                    state.ProjectileSpawnableId,
                    state.ProjectilePrefab,
                    state.ProjectileSpeed,
                    state.ProjectileLifetimeTicks,
                    state.Homing,
                    state.HomingTurnRate,
                    state.PierceCount,
                    state.Radius);
            }
            else if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
            {
                delivery.ConfigureHitscan(state.BeamVfxPrefab, state.ImpactVfxPrefab, state.MaxHits);
            }
            else if (state.DeliveryMode == AttackRecipeDeliveryMode.Area)
            {
                delivery.ConfigureArea(state.Radius, state.MaxHits);
            }
            else
            {
                delivery.ConfigureAura(state.Radius, state.TickIntervalSeconds);
            }
        }

        private static IReadOnlyList<AttackStatusEffectRecipe> GetStatuses(AttackAuthoringState state)
        {
            if (!state.IncludeStatusEffect) return Array.Empty<AttackStatusEffectRecipe>();
            return new[]
            {
                new AttackStatusEffectRecipe(
                    state.StatusId,
                    state.StatusDurationTicks,
                    state.StatusTickRateTicks,
                    state.StatusStrength,
                    state.StatusMaxStacks,
                    state.StatusStackingPolicy,
                    effectNote: state.StatusEffectNote)
            };
        }

        private static IReadOnlyList<AttackPresentationEventRecipe> GetPresentationEvents(AttackAuthoringState state)
        {
            return new[]
            {
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnCast, state.CastAudio, state.CastVfxPrefab, AttackPresentationSpawnPointRole.Caster),
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnFire, state.FireAudio, state.FireVfxPrefab, AttackPresentationSpawnPointRole.Muzzle),
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnImpact, state.ImpactAudio, state.ImpactVfxPresentationPrefab, AttackPresentationSpawnPointRole.ImpactPoint),
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnTick, state.TickAudio, state.TickVfxPrefab, AttackPresentationSpawnPointRole.Target),
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnExpire, state.ExpireAudio, state.ExpireVfxPrefab, AttackPresentationSpawnPointRole.ImpactPoint)
            };
        }

        private static void AddAttackIssues(List<GameContentAuthoringValidationIssue> issues, AttackRecipeValidationReport report)
        {
            if (report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                AttackRecipeValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == AttackRecipeValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == AttackRecipeValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }
        }

        private static void AddSubAsset(ScriptableObject subAsset, UnityEngine.Object root, string name)
        {
            GameContentAuthoringEditorAssets.AddSubAsset(subAsset, root, name);
        }

        private static bool HasDuplicateAttackId(string attackId)
        {
            return GameContentAuthoringEditorAssets.HasDuplicateId<AttackDefinitionAsset>(attackId, asset => asset.Id);
        }

        private static bool HasDuplicateAttackIdExcept(string attackId, AttackDefinitionAsset existingAsset)
        {
            return GameContentAuthoringEditorAssets.HasDuplicateIdExcept<AttackDefinitionAsset>(attackId, existingAsset, asset => asset.Id);
        }

        private static T EnsureSectionAsset<T>(T section, string rootPath, string name) where T : ScriptableObject
        {
            if (section != null)
                return section;

            T created = ScriptableObject.CreateInstance<T>();
            AddSubAsset(created, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath), name);
            return created;
        }

        private static void MarkDirty(UnityEngine.Object asset)
        {
            if (asset != null)
                EditorUtility.SetDirty(asset);
        }

        private static string[] SplitCsv(string csv)
        {
            return GameContentAuthoringEditorAssets.SplitCsv(csv);
        }

        private static string GetAttackFolder(AttackAuthoringState state)
        {
            string root = NormalizeAssetFolderPath(state.OutputRoot);
            return root.TrimEnd('/') + "/" + SanitizePathSegment(state.AttackId);
        }

        private static string GetFileStem(AttackAuthoringState state)
        {
            return SanitizePathSegment(state.AttackId);
        }

        private static string SanitizePathSegment(string value)
        {
            return GameContentAuthoringEditorPaths.SanitizePathSegment(value, "NewAttack");
        }

        private static string EnsureFolder(string folder)
        {
            return GameContentAuthoringEditorPaths.EnsureFolder(folder, "Assets/GameContent/Attacks");
        }

        private static bool FolderContainsAssets(string folder)
        {
            return GameContentAuthoringEditorPaths.FolderContainsAssets(folder);
        }

        private static string NormalizeAssetFolderPath(string path)
        {
            return GameContentAuthoringEditorPaths.NormalizeAssetFolderPath(path, "Assets/GameContent/Attacks");
        }

        private static string GetDeliverySummary(AttackAuthoringState state)
        {
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
            {
                string homing = state.Homing ? ", homing" : string.Empty;
                return "Projectile " + state.ProjectileDefinitionId + " -> " + state.ProjectileSpawnableId + " at " + state.ProjectileSpeed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " units/s" + homing;
            }

            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                return "Hitscan with up to " + state.MaxHits.ToString(System.Globalization.CultureInfo.InvariantCulture) + " hit(s)";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Area)
                return "Area radius " + state.Radius.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " with up to " + state.MaxHits.ToString(System.Globalization.CultureInfo.InvariantCulture) + " hit(s)";
            return "Aura radius " + state.Radius.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " every " + state.TickIntervalSeconds.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " second(s)";
        }

        private static string GetStatusSummary(AttackAuthoringState state)
        {
            return state.IncludeStatusEffect
                ? state.StatusId + " for " + state.StatusDurationTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " tick(s)"
                : "None";
        }

        private static void DestroyTransientObject(UnityEngine.Object target)
        {
            GameContentAuthoringEditorAssets.DestroyTransientObject(target);
        }
    }

    internal static class EnemyDefinitionAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/Enemies";

        public static EnemyDefinitionAsset BuildTransient(EnemyAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static GameContentAuthoringValidationResult ValidateForCreation(EnemyAuthoringState state, EnemyDefinitionAsset recipe)
        {
            var issues = ToSharedIssues(EnemyDefinitionValidator.Validate(recipe, EnemyDefinitionValidationOptions.AssetCreation));
            string folder = GetEnemyFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Enemy", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<EnemyDefinitionAsset>(state.EnemyId, asset => asset.Id))
                issues.Add(GameContentAuthoringValidationIssue.Error("Enemy.Id", "Enemy IDs must be unique. Rename this enemy or edit the existing asset instead of creating another."));
            return new GameContentAuthoringValidationResult(issues);
        }

        public static GameContentAuthoringValidationResult ValidateForUpdate(EnemyAuthoringState state, EnemyDefinitionAsset existingAsset)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (state == null)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error("Enemy", "No enemy edit state is available."));
                return new GameContentAuthoringValidationResult(issues);
            }

            EnemyDefinitionAsset preview = BuildRecipe(state, true);
            try
            {
                issues.AddRange(ToSharedIssues(EnemyDefinitionValidator.Validate(preview, EnemyDefinitionValidationOptions.AssetCreation)));
                if (existingAsset == null)
                    issues.Add(GameContentAuthoringValidationIssue.Error("Enemy", "No existing enemy asset is selected."));
                else if (HasDuplicateEnemyIdExcept(state.EnemyId, existingAsset))
                    issues.Add(GameContentAuthoringValidationIssue.Error("Enemy.Id", "Enemy IDs must be unique. Another enemy already uses this stable ID."));
                return new GameContentAuthoringValidationResult(issues);
            }
            finally
            {
                DestroyTransient(preview);
            }
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

        public static GameContentCreationResult CreateAssets(EnemyAuthoringState state)
        {
            EnemyDefinitionAsset preview = BuildRecipe(state, true);
            GameContentAuthoringValidationResult report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new GameContentCreationResult(false, "Fix validation errors before creating assets.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetEnemyFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new GameContentCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Enemy");
                if (!confirmed)
                    return new GameContentCreationResult(false, "Creation canceled before writing into existing folder.", null);
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
            return new GameContentCreationResult(true, "Created enemy definition at " + rootPath, AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(rootPath));
        }

        public static GameContentCreationResult UpdateExistingAsset(EnemyDefinitionAsset root, EnemyAuthoringState state)
        {
            if (root == null)
                return new GameContentCreationResult(false, "No enemy asset selected.", null);
            if (state == null)
                return new GameContentCreationResult(false, "No enemy edit state is available.", root);

            GameContentAuthoringValidationResult report = ValidateForUpdate(state, root);
            if (!report.IsValid)
                return new GameContentCreationResult(false, "Fix validation errors before saving this enemy.", root);

            string rootPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(rootPath))
                return new GameContentCreationResult(false, "Selected enemy is not a persisted asset.", root);

            string stem = GetFileStem(state);
            EnemyStatsDefinitionAsset stats = EnsureSectionAsset(root.Stats, rootPath, stem + "_Stats");
            EnemyPresentationDefinitionAsset presentation = EnsureSectionAsset(root.Presentation, rootPath, stem + "_Presentation");

            Undo.RegisterCompleteObjectUndo(root, "Save Enemy");
            Undo.RegisterCompleteObjectUndo(stats, "Save Enemy Stats");
            Undo.RegisterCompleteObjectUndo(presentation, "Save Enemy Presentation");

            stats.Configure(state.MaximumHealth, state.MoveSpeed, state.RewardValue, state.ContactDamage, state.DamageTypeId, state.CollisionRadius);
            presentation.Configure(state.Prefab, GetPresentationEvents(state));
            root.Configure(
                state.EnemyId,
                state.DisplayName,
                state.Icon,
                state.Role,
                GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv),
                stats,
                presentation,
                root.BalancingNotes);

            MarkDirty(stats);
            MarkDirty(presentation);
            MarkDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Saved enemy " + root.DisplayName + ".", root);
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

        private static IReadOnlyList<EnemyPresentationEventRecipe> GetPresentationEvents(EnemyAuthoringState state)
        {
            return new[]
            {
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnSpawn, state.SpawnAudio, state.SpawnVfxPrefab),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnHit, state.HitAudio, state.HitVfxPrefab),
                new EnemyPresentationEventRecipe(EnemyPresentationEventKind.OnDeath, state.DeathAudio, state.DeathVfxPrefab)
            };
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

        private static bool HasDuplicateEnemyIdExcept(string enemyId, EnemyDefinitionAsset existingAsset)
        {
            return GameContentAuthoringEditorAssets.HasDuplicateIdExcept<EnemyDefinitionAsset>(enemyId, existingAsset, asset => asset.Id);
        }

        private static T EnsureSectionAsset<T>(T section, string rootPath, string name) where T : ScriptableObject
        {
            if (section != null)
                return section;

            T created = ScriptableObject.CreateInstance<T>();
            GameContentAuthoringEditorAssets.AddSubAsset(created, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath), name);
            return created;
        }

        private static void MarkDirty(UnityEngine.Object asset)
        {
            if (asset != null)
                EditorUtility.SetDirty(asset);
        }

        private static List<GameContentAuthoringValidationIssue> ToSharedIssues(ContentAuthoringValidationReport report)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (report == null) return issues;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                ContentAuthoringValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == ContentAuthoringValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == ContentAuthoringValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }

            return issues;
        }
    }

    internal static class WaveDefinitionAssetCreator
    {
        private const string DefaultRoot = "Assets/GameContent/Waves";

        public static WaveDefinitionAsset BuildTransient(WaveAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static GameContentAuthoringValidationResult ValidateForCreation(WaveAuthoringState state, WaveDefinitionAsset recipe)
        {
            var issues = ToSharedIssues(WaveDefinitionValidator.Validate(recipe, WaveDefinitionValidationOptions.AssetCreation));
            string folder = GetWaveFolder(state);
            string rootPath = GetRootPath(state);
            GameContentAuthoringEditorAssets.AddPathIssues(issues, state.OutputRoot, DefaultRoot, folder, rootPath, "Wave", "OutputRoot");
            if (GameContentAuthoringEditorAssets.HasDuplicateId<WaveDefinitionAsset>(state.WaveId, asset => asset.Id))
                issues.Add(GameContentAuthoringValidationIssue.Error("Wave.Id", "Wave IDs must be unique. Rename this wave or edit the existing asset instead of creating another."));
            return new GameContentAuthoringValidationResult(issues);
        }

        public static GameContentAuthoringValidationResult ValidateForUpdate(WaveAuthoringState state, WaveDefinitionAsset existingAsset)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (state == null)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error("Wave", "No wave edit state is available."));
                return new GameContentAuthoringValidationResult(issues);
            }

            WaveDefinitionAsset preview = BuildRecipe(state, true);
            try
            {
                issues.AddRange(ToSharedIssues(WaveDefinitionValidator.Validate(preview, WaveDefinitionValidationOptions.RuntimeFriendly)));
                if (existingAsset == null)
                    issues.Add(GameContentAuthoringValidationIssue.Error("Wave", "No existing wave asset is selected."));
                else if (GameContentAuthoringEditorAssets.HasDuplicateIdExcept<WaveDefinitionAsset>(state.WaveId, existingAsset, asset => asset.Id))
                    issues.Add(GameContentAuthoringValidationIssue.Error("Wave.Id", "Wave IDs must be unique. Another wave already uses this stable ID."));
                return new GameContentAuthoringValidationResult(issues);
            }
            finally
            {
                DestroyTransient(preview);
            }
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

        public static GameContentCreationResult CreateAssets(WaveAuthoringState state)
        {
            WaveDefinitionAsset preview = BuildRecipe(state, true);
            GameContentAuthoringValidationResult report;
            try
            {
                report = ValidateForCreation(state, preview);
                if (!report.IsValid)
                    return new GameContentCreationResult(false, "Fix validation errors before creating assets.", null);
            }
            finally
            {
                DestroyTransient(preview);
            }

            string folder = GetWaveFolder(state);
            string rootPath = GetRootPath(state);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new GameContentCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Wave");
                if (!confirmed)
                    return new GameContentCreationResult(false, "Creation canceled before writing into existing folder.", null);
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
            return new GameContentCreationResult(true, "Created wave definition at " + rootPath, AssetDatabase.LoadAssetAtPath<WaveDefinitionAsset>(rootPath));
        }

        public static GameContentCreationResult UpdateExistingAsset(WaveDefinitionAsset root, WaveAuthoringState state)
        {
            if (root == null)
                return new GameContentCreationResult(false, "No wave asset selected.", null);
            if (state == null)
                return new GameContentCreationResult(false, "No wave edit state is available.", root);

            GameContentAuthoringValidationResult report = ValidateForUpdate(state, root);
            if (!report.IsValid)
                return new GameContentCreationResult(false, "Fix validation errors before saving this wave.", root);

            string rootPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(rootPath))
                return new GameContentCreationResult(false, "Selected wave is not a persisted asset.", root);

            string stem = GetFileStem(state);
            WaveScheduleDefinitionAsset schedule = EnsureSectionAsset(root.Schedule, rootPath, stem + "_Schedule");
            WaveEntriesDefinitionAsset entries = EnsureSectionAsset(root.Entries, rootPath, stem + "_Entries");

            Undo.RegisterCompleteObjectUndo(root, "Save Wave");
            Undo.RegisterCompleteObjectUndo(schedule, "Save Wave Schedule");
            Undo.RegisterCompleteObjectUndo(entries, "Save Wave Entries");

            schedule.Configure(state.StartTick);
            entries.Configure(GetEntries(state));
            root.Configure(
                state.WaveId,
                state.DisplayName,
                GameContentAuthoringEditorAssets.SplitCsv(state.TagsCsv),
                schedule,
                entries,
                root.BalancingNotes);

            MarkDirty(schedule);
            MarkDirty(entries);
            MarkDirty(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return new GameContentCreationResult(true, "Saved wave " + root.DisplayName + ".", root);
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

        private static T EnsureSectionAsset<T>(T section, string rootPath, string name) where T : ScriptableObject
        {
            if (section != null)
                return section;

            T created = ScriptableObject.CreateInstance<T>();
            GameContentAuthoringEditorAssets.AddSubAsset(created, AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath), name);
            return created;
        }

        private static void MarkDirty(UnityEngine.Object asset)
        {
            if (asset != null)
                EditorUtility.SetDirty(asset);
        }

        private static List<GameContentAuthoringValidationIssue> ToSharedIssues(ContentAuthoringValidationReport report)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (report == null) return issues;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                ContentAuthoringValidationIssue issue = report.Issues[i];
                GameContentAuthoringValidationSeverity severity = issue.Severity == ContentAuthoringValidationSeverity.Error
                    ? GameContentAuthoringValidationSeverity.Error
                    : issue.Severity == ContentAuthoringValidationSeverity.Warning
                        ? GameContentAuthoringValidationSeverity.Warning
                        : GameContentAuthoringValidationSeverity.Info;
                issues.Add(new GameContentAuthoringValidationIssue(severity, issue.Path, issue.Message));
            }

            return issues;
        }
    }
}
