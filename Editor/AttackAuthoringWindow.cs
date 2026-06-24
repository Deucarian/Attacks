using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed partial class AttackAuthoringWindow : EditorWindow
    {
        private const string WindowTitle = "Game Content Authoring";
        private readonly AttackAuthoringState _attackState = new AttackAuthoringState();
        private readonly EnemyAuthoringState _enemyState = new EnemyAuthoringState();
        private readonly WaveAuthoringState _waveState = new WaveAuthoringState();
        private Vector2 _scroll;
        private int _selectedProvider;
        private GUIStyle _headerTitle;
        private GUIStyle _headerSubtitle;
        private GUIStyle _card;
        private GUIStyle _sectionTitle;
        private GUIStyle _muted;
        private GUIStyle _providerButton;
        private GUIStyle _selectedProviderButton;
        private GUIStyle _primaryButton;
        private GUIStyle _issueStyle;
        private bool _stylesReady;
        private ContentAssetCreationResult _lastResult;

        [MenuItem("Deucarian/Game Content Authoring")]
        public static void Open()
        {
            AttackAuthoringWindow window = GetWindow<AttackAuthoringWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 640f);
            window.Show();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawProviderRail();
                using (new EditorGUILayout.VerticalScope())
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    IGameContentAuthoringProvider provider = GameContentAuthoringProviderRegistry.Providers[Mathf.Clamp(_selectedProvider, 0, GameContentAuthoringProviderRegistry.Providers.Count - 1)];
                    provider.Draw(this);
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 76f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.12f, 0.14f, 1f));
            var accent = new Rect(rect.x, rect.yMax - 3f, rect.width, 3f);
            EditorGUI.DrawRect(accent, new Color(0.1f, 0.68f, 0.62f, 1f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 28f), "Deucarian Game Content Authoring", _headerTitle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 42f, rect.width - 36f, 22f), "Create linked gameplay recipes without turning one asset into a drawer full of unrelated fields.", _headerSubtitle);
        }

        private void DrawProviderRail()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(240f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Content Types", _sectionTitle);
                for (int i = 0; i < GameContentAuthoringProviderRegistry.Providers.Count; i++)
                {
                    IGameContentAuthoringProvider provider = GameContentAuthoringProviderRegistry.Providers[i];
                    GUI.enabled = provider.Enabled;
                    GUIStyle style = i == _selectedProvider ? _selectedProviderButton : _providerButton;
                    if (GUILayout.Button(new GUIContent(provider.DisplayName, provider.Description), style, GUILayout.Height(44f)))
                        SelectProvider(i);
                    GUI.enabled = true;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Authoring Suite", _muted);
                EditorGUILayout.LabelField("Attack, Enemy, Wave", _sectionTitle);
            }
        }

        private void DrawAttackProvider(AttackAuthoringState state)
        {
            AttackDefinitionAsset preview = AttackRecipeAssetCreator.BuildTransient(state);
            AttackRecipeValidationReport report;
            try
            {
                report = AttackRecipeAssetCreator.ValidateForCreation(state, preview);
            }
            finally
            {
                AttackRecipeAssetCreator.DestroyTransient(preview);
            }

            DrawSection("Attack Identity", () =>
            {
                state.AttackId = EditorGUILayout.TextField("Stable ID", state.AttackId);
                state.DisplayName = EditorGUILayout.TextField("Display Name", state.DisplayName);
                state.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", state.Icon, typeof(Sprite), false);
                state.TagsCsv = EditorGUILayout.TextField("Tags", state.TagsCsv);
                state.OutputRoot = DrawOutputRootField(state.OutputRoot);
            });

            DrawSection("Mechanics", () =>
            {
                state.DamageTypeId = EditorGUILayout.TextField("Damage Type ID", state.DamageTypeId);
                state.DamageAmount = EditorGUILayout.FloatField("Damage", state.DamageAmount);
                state.CooldownTicks = EditorGUILayout.IntField("Cooldown Ticks", state.CooldownTicks);
                state.Range = EditorGUILayout.FloatField("Range", state.Range);
                state.TargetingMode = (AttackRecipeTargetingMode)EditorGUILayout.EnumPopup("Targeting", state.TargetingMode);
            });

            DrawSection("Delivery", () =>
            {
                state.DeliveryMode = (AttackRecipeDeliveryMode)EditorGUILayout.EnumPopup("Mode", state.DeliveryMode);
                if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
                {
                    state.ProjectileDefinitionId = EditorGUILayout.TextField("Projectile ID", state.ProjectileDefinitionId);
                    state.ProjectileSpawnableId = EditorGUILayout.TextField("Spawnable ID", state.ProjectileSpawnableId);
                    state.ProjectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", state.ProjectilePrefab, typeof(GameObject), false);
                    state.ProjectileSpeed = EditorGUILayout.FloatField("Speed", state.ProjectileSpeed);
                    state.ProjectileLifetimeTicks = EditorGUILayout.IntField("Lifetime Ticks", state.ProjectileLifetimeTicks);
                    state.Homing = EditorGUILayout.Toggle("Homing", state.Homing);
                    state.HomingTurnRate = EditorGUILayout.FloatField("Turn Rate", state.HomingTurnRate);
                    state.PierceCount = EditorGUILayout.IntField("Pierce Count", state.PierceCount);
                    state.Radius = EditorGUILayout.FloatField("Radius", state.Radius);
                }
                else if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                {
                    state.BeamVfxPrefab = (GameObject)EditorGUILayout.ObjectField("Beam/Tracer VFX", state.BeamVfxPrefab, typeof(GameObject), false);
                    state.ImpactVfxPrefab = (GameObject)EditorGUILayout.ObjectField("Impact VFX", state.ImpactVfxPrefab, typeof(GameObject), false);
                    state.MaxHits = EditorGUILayout.IntField("Max Hits", state.MaxHits);
                }
                else
                {
                    state.Radius = EditorGUILayout.FloatField("Radius", state.Radius);
                    state.MaxHits = EditorGUILayout.IntField("Max Hits", state.MaxHits);
                    if (state.DeliveryMode == AttackRecipeDeliveryMode.Aura)
                        state.TickIntervalSeconds = EditorGUILayout.FloatField("Tick Interval", state.TickIntervalSeconds);
                }
            });

            DrawSection("Status Effects", () =>
            {
                state.IncludeStatusEffect = EditorGUILayout.Toggle("Include Status", state.IncludeStatusEffect);
                if (state.IncludeStatusEffect)
                {
                    state.StatusId = EditorGUILayout.TextField("Status ID", state.StatusId);
                    state.StatusDurationTicks = EditorGUILayout.IntField("Duration Ticks", state.StatusDurationTicks);
                    state.StatusTickRateTicks = EditorGUILayout.IntField("Tick Rate Ticks", state.StatusTickRateTicks);
                    state.StatusStrength = EditorGUILayout.FloatField("Strength", state.StatusStrength);
                    state.StatusMaxStacks = EditorGUILayout.IntField("Max Stacks", state.StatusMaxStacks);
                    state.StatusStackingPolicy = (Deucarian.Combat.StatusStackingPolicy)EditorGUILayout.EnumPopup("Stacking", state.StatusStackingPolicy);
                    state.StatusEffectNote = EditorGUILayout.TextField("Effect Note", state.StatusEffectNote);
                }
            });

            DrawSection("Presentation", () =>
            {
                state.CastAudio = (AudioClip)EditorGUILayout.ObjectField("OnCast Audio", state.CastAudio, typeof(AudioClip), false);
                state.FireAudio = (AudioClip)EditorGUILayout.ObjectField("OnFire Audio", state.FireAudio, typeof(AudioClip), false);
                state.ImpactAudio = (AudioClip)EditorGUILayout.ObjectField("OnImpact Audio", state.ImpactAudio, typeof(AudioClip), false);
                state.CastVfxPrefab = (GameObject)EditorGUILayout.ObjectField("OnCast VFX", state.CastVfxPrefab, typeof(GameObject), false);
                state.FireVfxPrefab = (GameObject)EditorGUILayout.ObjectField("OnFire VFX", state.FireVfxPrefab, typeof(GameObject), false);
                state.ImpactVfxPresentationPrefab = (GameObject)EditorGUILayout.ObjectField("OnImpact VFX", state.ImpactVfxPresentationPrefab, typeof(GameObject), false);
            });

            DrawSection("Preview", () =>
            {
                foreach (string line in AttackRecipeAssetCreator.GetPreviewLines(state))
                    EditorGUILayout.LabelField(line, _muted);
                GUILayout.Space(6f);
                DrawValidation(report);
                GUILayout.Space(8f);
                if (DrawCreateButton("Create Attack Asset", report.IsValid))
                {
                    _lastResult = AttackRecipeAssetCreator.CreateAssets(state);
                    if (_lastResult.CreatedRoot != null)
                    {
                        Selection.activeObject = _lastResult.CreatedRoot;
                        EditorGUIUtility.PingObject(_lastResult.CreatedRoot);
                    }
                }

                DrawLastResult();
            });
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

        private void DrawValidation(AttackRecipeValidationReport report)
        {
            if (report == null || report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Ready to create one root AttackDefinition asset with focused sub-assets.", MessageType.Info);
                return;
            }

            string summary = report.ErrorCount == 0
                ? report.WarningCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " warning(s). You can create the asset after confirming any prompts."
                : report.ErrorCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocking issue(s) and " + report.WarningCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " warning(s).";
            EditorGUILayout.HelpBox(summary, report.ErrorCount == 0 ? MessageType.Warning : MessageType.Error);

            for (int i = 0; i < report.Issues.Count; i++)
            {
                AttackRecipeValidationIssue issue = report.Issues[i];
                MessageType type = issue.Severity == AttackRecipeValidationSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == AttackRecipeValidationSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox(issue.Path + ": " + issue.Message, type);
            }
        }

        private void DrawLastResult()
        {
            if (_lastResult == null) return;
            GUILayout.Space(8f);
            if (!_lastResult.Succeeded)
            {
                EditorGUILayout.HelpBox(_lastResult.Message, MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(_lastResult.Message, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField("Created Root", _lastResult.CreatedRoot, typeof(UnityEngine.Object), false);
                if (GUILayout.Button(new GUIContent("Ping", "Ping created root asset"), GUILayout.Width(48f)))
                    EditorGUIUtility.PingObject(_lastResult.CreatedRoot);
            }
        }

        private bool DrawCreateButton(string label, bool enabled)
        {
            var content = new GUIContent(
                label,
                enabled ? "Create the root asset and linked section assets." : "Fix blocking validation issues before creating this asset.");
            using (new EditorGUI.DisabledScope(!enabled))
                return GUILayout.Button(content, _primaryButton, GUILayout.Height(30f));
        }

        private void DrawSection(string title, Action draw)
        {
            using (new EditorGUILayout.VerticalScope(_card))
            {
                EditorGUILayout.LabelField(title, _sectionTitle);
                GUILayout.Space(3f);
                draw();
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _headerTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, normal = { textColor = Color.white } };
            _headerSubtitle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = 11, normal = { textColor = new Color(0.78f, 0.87f, 0.87f) } };
            _card = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 10), margin = new RectOffset(8, 8, 6, 8) };
            _sectionTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _muted = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.58f, 0.64f, 0.66f) } };
            _issueStyle = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
            _providerButton = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, padding = new RectOffset(10, 10, 6, 6) };
            _selectedProviderButton = new GUIStyle(_providerButton);
            _selectedProviderButton.normal.textColor = Color.white;
            _selectedProviderButton.normal.background = Texture2D.grayTexture;
            _primaryButton = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold };
            _stylesReady = true;
        }

        private void SelectProvider(int index)
        {
            if (_selectedProvider == index) return;
            _selectedProvider = index;
            _lastResult = null;
            _scroll = Vector2.zero;
            GUI.FocusControl(null);
        }

        private interface IGameContentAuthoringProvider
        {
            string DisplayName { get; }
            string Description { get; }
            bool Enabled { get; }
            void Draw(AttackAuthoringWindow window);
        }

        private sealed class AttackProvider : IGameContentAuthoringProvider
        {
            public string DisplayName => "Attack";
            public string Description => "Create a root AttackDefinition with mechanics, targeting, delivery, status, and presentation sections.";
            public bool Enabled => true;
            public void Draw(AttackAuthoringWindow window) => window.DrawAttackProvider(window._attackState);
        }

        private sealed class EnemyProvider : IGameContentAuthoringProvider
        {
            public string DisplayName => "Enemy";
            public string Description => "Create a root EnemyDefinition with stats and presentation sections.";
            public bool Enabled => true;
            public void Draw(AttackAuthoringWindow window) => window.DrawEnemyProvider(window._enemyState);
        }

        private sealed class WaveProvider : IGameContentAuthoringProvider
        {
            public string DisplayName => "Wave";
            public string Description => "Create a root WaveDefinition with schedule and spawn entry sections.";
            public bool Enabled => true;
            public void Draw(AttackAuthoringWindow window) => window.DrawWaveProvider(window._waveState);
        }

        private sealed class FutureProvider : IGameContentAuthoringProvider
        {
            private readonly string _displayName;
            public FutureProvider(string displayName) { _displayName = displayName; }
            public string DisplayName => _displayName;
            public string Description => "Planned content authoring provider.";
            public bool Enabled => false;
            public void Draw(AttackAuthoringWindow window) { }
        }

        private static class GameContentAuthoringProviderRegistry
        {
            private static readonly IGameContentAuthoringProvider[] Items =
            {
                new AttackProvider(),
                new EnemyProvider(),
                new WaveProvider(),
                new FutureProvider("Upgrade"),
                new FutureProvider("Tower/Weapon"),
                new FutureProvider("VFX/Audio Preset")
            };

            public static IReadOnlyList<IGameContentAuthoringProvider> Providers => Items;
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
        public GameObject CastVfxPrefab;
        public GameObject FireVfxPrefab;
        public GameObject ImpactVfxPresentationPrefab;
    }

    internal static class AttackRecipeAssetCreator
    {
        public static AttackDefinitionAsset BuildTransient(AttackAuthoringState state)
        {
            return BuildRecipe(state, true);
        }

        public static AttackRecipeValidationReport ValidateForCreation(AttackAuthoringState state, AttackDefinitionAsset recipe)
        {
            var issues = new List<AttackRecipeValidationIssue>(AttackRecipeValidator.Validate(recipe, AttackRecipeValidationOptions.AssetCreation).Issues);
            string folder = GetAttackFolder(state);
            string rootPath = folder + "/" + GetFileStem(state) + "_AttackDefinition.asset";
            GameContentAuthoringEditorAssets.AddAttackPathIssues(issues, state.OutputRoot, "Assets/GameContent/Attacks", folder, rootPath, "Attack", "OutputRoot");
            if (HasDuplicateAttackId(state.AttackId))
                issues.Add(new AttackRecipeValidationIssue(AttackRecipeValidationSeverity.Error, "Attack IDs must be unique. Rename this attack or edit the existing asset instead of creating another.", "Attack.Id"));
            return new AttackRecipeValidationReport(issues);
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

        public static ContentAssetCreationResult CreateAssets(AttackAuthoringState state)
        {
            AttackDefinitionAsset preview = BuildRecipe(state, true);
            AttackRecipeValidationReport report;
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

            string folder = GetAttackFolder(state);
            string rootPath = folder + "/" + GetFileStem(state) + "_AttackDefinition.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                return new ContentAssetCreationResult(false, "Asset already exists: " + rootPath, null);
            if (AssetDatabase.IsValidFolder(folder) && FolderContainsAssets(folder))
            {
                bool confirmed = GameContentAuthoringEditorAssets.ConfirmExistingFolder(folder, "Attack");
                if (!confirmed)
                    return new ContentAssetCreationResult(false, "Creation canceled before writing into existing folder.", null);
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
            return new ContentAssetCreationResult(true, "Created attack recipe at " + rootPath, AssetDatabase.LoadAssetAtPath<AttackDefinitionAsset>(rootPath));
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
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnTick, spawnPointRole: AttackPresentationSpawnPointRole.Target),
                new AttackPresentationEventRecipe(AttackPresentationEventKind.OnExpire, spawnPointRole: AttackPresentationSpawnPointRole.ImpactPoint)
            };
        }

        private static void AddSubAsset(ScriptableObject subAsset, UnityEngine.Object root, string name)
        {
            GameContentAuthoringEditorAssets.AddSubAsset(subAsset, root, name);
        }

        private static bool HasDuplicateAttackId(string attackId)
        {
            return GameContentAuthoringEditorAssets.HasDuplicateId<AttackDefinitionAsset>(attackId, asset => asset.Id);
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
}
