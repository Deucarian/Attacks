using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Deucarian.Attacks.Authoring;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed class AttackGameContentPreviewController
    {
        private string _status = "Preview idle";
        private bool _fullPreviewPlaying;
        private double _fullPreviewStartTime;

        public void Draw(GameContentAuthoringPreviewContext context, AttackAuthoringState state)
        {
            if (context == null) return;
            state = AttackGameContentPreviewSelection.ResolveAttackState(context, state);
            context.SetStatus(_status);

            context.DrawCard("Attack Playback", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawPrimaryButton("Preview Full Attack", true, GUILayout.Height(26f)))
                    {
                        _fullPreviewPlaying = true;
                        _fullPreviewStartTime = EditorApplication.timeSinceStartup;
                        SetStatus(context, AttackGameContentPreviewActions.PreviewFullAttack(state));
                    }

                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Width(104f), GUILayout.Height(26f)))
                        Stop(context);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawSecondaryButton("Preview OnCast", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewAttackEvent(state, AttackPresentationEventKind.OnCast));
                    if (context.DrawSecondaryButton("Preview OnFire", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewAttackEvent(state, AttackPresentationEventKind.OnFire));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool statusEnabled = state != null && state.IncludeStatusEffect;
                    if (context.DrawSecondaryButton("Preview OnImpact", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewAttackEvent(state, AttackPresentationEventKind.OnImpact));
                    if (context.DrawSecondaryButton("Preview Status Tick", statusEnabled, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewAttackEvent(state, AttackPresentationEventKind.OnTick));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool statusEnabled = state != null && state.IncludeStatusEffect;
                    if (context.DrawSecondaryButton("Preview OnExpire", statusEnabled, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewAttackEvent(state, AttackPresentationEventKind.OnExpire));
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Combat Summary", () =>
            {
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildAttackRows(state));
            });

            context.DrawCard("Model And Presentation", () =>
            {
                GameContentAuthoringActionPreview actionPreview = AttackGameContentPreviewActions.BuildActionPreview(
                    state,
                    _fullPreviewPlaying,
                    _fullPreviewStartTime);
                context.DrawObjectPreview(
                    AttackGameContentPreviewSummaries.GetPrimaryAttackPreviewAsset(state),
                    "Primary Visual",
                    "Assign a projectile prefab, beam VFX, impact VFX, or presentation VFX to see a visual preview.",
                    new GameContentAuthoringObjectPreviewOptions
                    {
                        MinimumHeight = 212f,
                        ActionPreview = actionPreview
                    });
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildAttackPresentationRows(state));
                context.DrawAssetRow("Projectile", state == null ? null : state.ProjectilePrefab, "Not assigned");
                context.DrawAssetRow("Beam VFX", state == null ? null : state.BeamVfxPrefab, "Not assigned");
                context.DrawAssetRow("Impact VFX", state == null ? null : state.ImpactVfxPrefab, "Not assigned");
            });

            context.DrawCard("Expected Result", () =>
            {
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildAttackExpectedRows(state));
            });

            context.DrawWarnings(AttackGameContentPreviewSummaries.BuildAttackWarnings(state));
        }

        public void Stop()
        {
            AttackEditorPreviewAudio.StopAll();
            _fullPreviewPlaying = false;
            _status = "Preview stopped";
        }

        private void Stop(GameContentAuthoringPreviewContext context)
        {
            Stop();
            context.SetStatus(_status);
        }

        private void SetStatus(GameContentAuthoringPreviewContext context, string status)
        {
            _status = string.IsNullOrWhiteSpace(status) ? "Preview idle" : status;
            context.SetStatus(_status);
        }
    }

    internal sealed class EnemyGameContentPreviewController
    {
        private string _status = "Preview idle";

        public void Draw(GameContentAuthoringPreviewContext context, EnemyAuthoringState state)
        {
            if (context == null) return;
            state = AttackGameContentPreviewSelection.ResolveEnemyState(context, state);
            context.SetStatus(_status);

            context.DrawCard("Enemy Playback", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawSecondaryButton("Preview Spawn", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewEnemyEvent(state, EnemyPresentationEventKind.OnSpawn));
                    if (context.DrawSecondaryButton("Preview Hit", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewEnemyEvent(state, EnemyPresentationEventKind.OnHit));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawSecondaryButton("Preview Death", true, GUILayout.Height(24f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewEnemyEvent(state, EnemyPresentationEventKind.OnDeath));
                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Height(24f)))
                        Stop(context);
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Enemy Model", () =>
            {
                context.DrawObjectPreview(state == null ? null : state.Prefab, "Prefab / Model", "Assign an enemy prefab to see its editor thumbnail.");
                context.DrawAssetRow("Spawn VFX", state == null ? null : state.SpawnVfxPrefab, "Not assigned");
                context.DrawAssetRow("Hit VFX", state == null ? null : state.HitVfxPrefab, "Not assigned");
                context.DrawAssetRow("Death VFX", state == null ? null : state.DeathVfxPrefab, "Not assigned");
            });

            context.DrawCard("Stats Summary", () =>
            {
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyRows(state));
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildEnemyPresentationRows(state));
            });

            context.DrawWarnings(AttackGameContentPreviewSummaries.BuildEnemyWarnings(state));
        }

        public void Stop()
        {
            AttackEditorPreviewAudio.StopAll();
            _status = "Preview stopped";
        }

        private void Stop(GameContentAuthoringPreviewContext context)
        {
            Stop();
            context.SetStatus(_status);
        }

        private void SetStatus(GameContentAuthoringPreviewContext context, string status)
        {
            _status = string.IsNullOrWhiteSpace(status) ? "Preview idle" : status;
            context.SetStatus(_status);
        }
    }

    internal sealed class WaveGameContentPreviewController
    {
        private string _status = "Preview idle";

        public void Draw(GameContentAuthoringPreviewContext context, WaveAuthoringState state)
        {
            if (context == null) return;
            state = AttackGameContentPreviewSelection.ResolveWaveState(context, state);
            context.SetStatus(_status);

            context.DrawCard("Wave Preview", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawPrimaryButton("Preview Timeline", true, GUILayout.Height(26f)))
                        SetStatus(context, AttackGameContentPreviewActions.PreviewWaveTimeline(state));
                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Width(104f), GUILayout.Height(26f)))
                        Stop(context);
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Wave Summary", () =>
            {
                context.DrawSummaryRows(AttackGameContentPreviewSummaries.BuildWaveRows(state));
            });

            context.DrawCard("Spawn Timeline", () =>
            {
                context.DrawTimeline(AttackGameContentPreviewSummaries.BuildWaveTimeline(state));
            });

            context.DrawWarnings(AttackGameContentPreviewSummaries.BuildWaveWarnings(state));
        }

        public void Stop()
        {
            AttackEditorPreviewAudio.StopAll();
            _status = "Preview stopped";
        }

        private void Stop(GameContentAuthoringPreviewContext context)
        {
            Stop();
            context.SetStatus(_status);
        }

        private void SetStatus(GameContentAuthoringPreviewContext context, string status)
        {
            _status = string.IsNullOrWhiteSpace(status) ? "Preview idle" : status;
            context.SetStatus(_status);
        }
    }

    internal static class AttackGameContentPreviewActions
    {
        public static string PreviewFullAttack(AttackAuthoringState state)
        {
            return PreviewFullAttack(state, false);
        }

        public static string PreviewFullAttack(AttackAuthoringState state, bool muted)
        {
            if (state == null) return "Attack preview unavailable: authoring state is missing.";

            string audioStatus = muted ? "Audio muted." : PreviewFirstAvailableAttackAudio(state);
            return "Full attack preview: OnCast -> OnFire -> delivery -> OnImpact"
                + (state.IncludeStatusEffect ? " -> Status Tick -> OnExpire" : string.Empty)
                + ". " + audioStatus;
        }

        public static GameContentAuthoringActionPreview BuildActionPreview(AttackAuthoringState state, bool playing, double startTime)
        {
            if (state == null) return null;
            var preview = new GameContentAuthoringActionPreview
            {
                PrimaryAsset = AttackGameContentPreviewSummaries.GetPrimaryAttackPreviewAsset(state),
                ProjectilePrefab = state.ProjectilePrefab,
                BeamVfxPrefab = state.BeamVfxPrefab,
                FireVfxPrefab = state.FireVfxPrefab,
                ImpactVfxPrefab = state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab,
                Mode = GetActionPreviewMode(state.DeliveryMode),
                IncludeStatusEffect = state.IncludeStatusEffect,
                Playing = playing,
                StartTime = startTime,
                StaticNormalizedTime = 0.5f,
                DurationSeconds = state.IncludeStatusEffect ? 3f : 2.4f,
                Label = string.IsNullOrWhiteSpace(state.DisplayName) ? state.AttackId : state.DisplayName,
                DeliveryTypeLabel = GetPreviewDeliveryLabel(state)
            };
            AddPreviewRoles(preview, state);
            return preview;
        }

        public static string PreviewAttackEvent(AttackAuthoringState state, AttackPresentationEventKind eventKind)
        {
            return PreviewAttackEvent(state, eventKind, false);
        }

        public static string PreviewAttackEvent(AttackAuthoringState state, AttackPresentationEventKind eventKind, bool muted)
        {
            if (state == null) return eventKind + " preview unavailable: authoring state is missing.";
            AudioClip clip = GetAttackAudio(state, eventKind);
            GameObject vfx = GetAttackVfx(state, eventKind);
            string visual = vfx == null ? "no VFX assigned" : "VFX " + vfx.name;
            if (muted)
                return eventKind + " preview: " + visual + "; audio muted.";
            if (clip == null)
                return eventKind + " preview: " + visual + "; no audio clip assigned.";

            string audioMessage;
            AttackEditorPreviewAudio.TryPlay(clip, out audioMessage);
            return eventKind + " preview: " + visual + "; " + audioMessage;
        }

        public static string PreviewEnemyEvent(EnemyAuthoringState state, EnemyPresentationEventKind eventKind)
        {
            if (state == null) return eventKind + " preview unavailable: authoring state is missing.";
            AudioClip clip = GetEnemyAudio(state, eventKind);
            GameObject vfx = GetEnemyVfx(state, eventKind);
            string visual = vfx == null ? "no VFX assigned" : "VFX " + vfx.name;
            if (clip == null)
                return eventKind + " preview: " + visual + "; no audio clip assigned.";

            string audioMessage;
            AttackEditorPreviewAudio.TryPlay(clip, out audioMessage);
            return eventKind + " preview: " + visual + "; " + audioMessage;
        }

        public static string PreviewWaveTimeline(WaveAuthoringState state)
        {
            if (state == null) return "Wave preview unavailable: authoring state is missing.";
            int total = AttackGameContentPreviewSummaries.GetWaveTotalEnemyCount(state);
            int duration = AttackGameContentPreviewSummaries.GetWaveApproximateDurationTicks(state);
            return "Wave timeline preview: " + total.ToString(CultureInfo.InvariantCulture) + " enemies over about " + duration.ToString(CultureInfo.InvariantCulture) + " tick(s).";
        }

        private static string PreviewFirstAvailableAttackAudio(AttackAuthoringState state)
        {
            AudioClip clip = state.CastAudio ?? state.FireAudio ?? state.ImpactAudio ?? state.TickAudio ?? state.ExpireAudio;
            if (clip == null) return "No attack audio clips assigned; visual summary only.";

            string audioMessage;
            AttackEditorPreviewAudio.TryPlay(clip, out audioMessage);
            return audioMessage;
        }

        private static GameContentAuthoringActionPreviewMode GetActionPreviewMode(AttackRecipeDeliveryMode mode)
        {
            switch (mode)
            {
                case AttackRecipeDeliveryMode.Projectile:
                    return GameContentAuthoringActionPreviewMode.Projectile;
                case AttackRecipeDeliveryMode.Hitscan:
                    return GameContentAuthoringActionPreviewMode.Hitscan;
                case AttackRecipeDeliveryMode.Area:
                    return GameContentAuthoringActionPreviewMode.Area;
                case AttackRecipeDeliveryMode.Aura:
                    return GameContentAuthoringActionPreviewMode.Aura;
                default:
                    return GameContentAuthoringActionPreviewMode.Static;
            }
        }

        private static void AddPreviewRoles(GameContentAuthoringActionPreview preview, AttackAuthoringState state)
        {
            if (preview == null || state == null)
                return;

            switch (state.DeliveryMode)
            {
                case AttackRecipeDeliveryMode.Hitscan:
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Source", string.Empty));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Beam", GetObjectLabel(state.BeamVfxPrefab, "beam/tracer"), state.BeamVfxPrefab));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Impact", "Impact Point", state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab));
                    break;
                case AttackRecipeDeliveryMode.Area:
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Origin", "Cast Origin"));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Radius", "AOE Radius", state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Targets", "Target Dummies"));
                    break;
                case AttackRecipeDeliveryMode.Aura:
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Status Area", "Affected Area", state.TickVfxPrefab ?? state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Tick", "Tick Marker", state.TickVfxPrefab));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Target", "Target Dummy"));
                    break;
                default:
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Source", string.Empty));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Projectile", GetObjectLabel(state.ProjectilePrefab, "projectile"), state.ProjectilePrefab));
                    preview.Roles.Add(new GameContentAuthoringActionPreviewRole("Target", "Target Dummy"));
                    break;
            }
        }

        private static string GetPreviewDeliveryLabel(AttackAuthoringState state)
        {
            if (state == null)
                return "Delivery";

            switch (state.DeliveryMode)
            {
                case AttackRecipeDeliveryMode.Hitscan:
                    return "Beam";
                case AttackRecipeDeliveryMode.Area:
                    return "AOE";
                case AttackRecipeDeliveryMode.Aura:
                    return "Status";
                default:
                    return state.Homing ? "Homing Projectile" : "Projectile";
            }
        }

        private static string GetObjectLabel(UnityEngine.Object asset, string fallback)
        {
            return asset == null || string.IsNullOrWhiteSpace(asset.name) ? fallback : asset.name;
        }

        private static AudioClip GetAttackAudio(AttackAuthoringState state, AttackPresentationEventKind eventKind)
        {
            switch (eventKind)
            {
                case AttackPresentationEventKind.OnCast:
                    return state.CastAudio;
                case AttackPresentationEventKind.OnFire:
                    return state.FireAudio;
                case AttackPresentationEventKind.OnImpact:
                    return state.ImpactAudio;
                case AttackPresentationEventKind.OnTick:
                    return state.TickAudio;
                case AttackPresentationEventKind.OnExpire:
                    return state.ExpireAudio;
                default:
                    return null;
            }
        }

        private static GameObject GetAttackVfx(AttackAuthoringState state, AttackPresentationEventKind eventKind)
        {
            switch (eventKind)
            {
                case AttackPresentationEventKind.OnCast:
                    return state.CastVfxPrefab;
                case AttackPresentationEventKind.OnFire:
                    return state.FireVfxPrefab;
                case AttackPresentationEventKind.OnImpact:
                    return state.ImpactVfxPresentationPrefab;
                case AttackPresentationEventKind.OnTick:
                    return state.TickVfxPrefab;
                case AttackPresentationEventKind.OnExpire:
                    return state.ExpireVfxPrefab;
                default:
                    return null;
            }
        }

        private static AudioClip GetEnemyAudio(EnemyAuthoringState state, EnemyPresentationEventKind eventKind)
        {
            switch (eventKind)
            {
                case EnemyPresentationEventKind.OnSpawn:
                    return state.SpawnAudio;
                case EnemyPresentationEventKind.OnHit:
                    return state.HitAudio;
                case EnemyPresentationEventKind.OnDeath:
                    return state.DeathAudio;
                default:
                    return null;
            }
        }

        private static GameObject GetEnemyVfx(EnemyAuthoringState state, EnemyPresentationEventKind eventKind)
        {
            switch (eventKind)
            {
                case EnemyPresentationEventKind.OnSpawn:
                    return state.SpawnVfxPrefab;
                case EnemyPresentationEventKind.OnHit:
                    return state.HitVfxPrefab;
                case EnemyPresentationEventKind.OnDeath:
                    return state.DeathVfxPrefab;
                default:
                    return null;
            }
        }
    }

    internal static class AttackGameContentPreviewSelection
    {
        public static AttackAuthoringState ResolveAttackState(GameContentAuthoringPreviewContext context, AttackAuthoringState createState)
        {
            AttackDefinitionAsset selected = context == null ? null : context.GetSelectedAsset<AttackDefinitionAsset>();
            return selected == null ? createState : FromAttackAsset(selected);
        }

        public static EnemyAuthoringState ResolveEnemyState(GameContentAuthoringPreviewContext context, EnemyAuthoringState createState)
        {
            EnemyDefinitionAsset selected = context == null ? null : context.GetSelectedAsset<EnemyDefinitionAsset>();
            return selected == null ? createState : FromEnemyAsset(selected);
        }

        public static WaveAuthoringState ResolveWaveState(GameContentAuthoringPreviewContext context, WaveAuthoringState createState)
        {
            WaveDefinitionAsset selected = context == null ? null : context.GetSelectedAsset<WaveDefinitionAsset>();
            return selected == null ? createState : FromWaveAsset(selected);
        }

        private static AttackAuthoringState FromAttackAsset(AttackDefinitionAsset asset)
        {
            var state = new AttackAuthoringState
            {
                AttackId = asset.Id,
                DisplayName = asset.DisplayName,
                Icon = asset.Icon,
                TagsCsv = JoinTags(asset.Tags)
            };

            AttackMechanicsDefinitionAsset mechanics = asset.Mechanics;
            if (mechanics != null)
            {
                state.CooldownTicks = mechanics.CooldownTicks;
                state.Range = mechanics.Range;
                state.DamageAmount = mechanics.DamageAmount;
                state.DamageTypeId = mechanics.DamageTypeId;
            }

            AttackTargetingDefinitionAsset targeting = asset.Targeting;
            if (targeting != null)
                state.TargetingMode = targeting.Mode;

            AttackDeliveryDefinitionAsset delivery = asset.Delivery;
            if (delivery != null)
            {
                state.DeliveryMode = delivery.Mode;
                state.ProjectileDefinitionId = delivery.ProjectileDefinitionId;
                state.ProjectileSpawnableId = delivery.ProjectileSpawnableId;
                state.ProjectilePrefab = delivery.ProjectilePrefab;
                state.ProjectileSpeed = delivery.ProjectileSpeed;
                state.ProjectileLifetimeTicks = delivery.ProjectileLifetimeTicks;
                state.Homing = delivery.Homing;
                state.HomingTurnRate = delivery.HomingTurnRate;
                state.PierceCount = delivery.PierceCount;
                state.Radius = delivery.Radius;
                state.BeamVfxPrefab = delivery.BeamVfxPrefab;
                state.ImpactVfxPrefab = delivery.ImpactVfxPrefab;
                state.MaxHits = delivery.MaxHits;
                state.TickIntervalSeconds = delivery.TickIntervalSeconds;
            }

            AttackStatusEffectRecipe status = FirstStatus(asset.StatusEffects);
            state.IncludeStatusEffect = status != null;
            if (status != null)
            {
                state.StatusId = status.StatusId;
                state.StatusDurationTicks = status.DurationTicks;
                state.StatusTickRateTicks = status.TickRateTicks;
                state.StatusStrength = status.Strength;
                state.StatusMaxStacks = status.MaxStacks;
                state.StatusStackingPolicy = status.StackingPolicy;
                state.StatusEffectNote = status.EffectNote;
            }

            ApplyAttackPresentation(asset.Presentation, state);
            return state;
        }

        private static EnemyAuthoringState FromEnemyAsset(EnemyDefinitionAsset asset)
        {
            var state = new EnemyAuthoringState
            {
                EnemyId = asset.Id,
                DisplayName = asset.DisplayName,
                Icon = asset.Icon,
                Role = asset.Role,
                TagsCsv = JoinTags(asset.Tags)
            };

            EnemyStatsDefinitionAsset stats = asset.Stats;
            if (stats != null)
            {
                state.MaximumHealth = stats.MaximumHealth;
                state.MoveSpeed = stats.MoveSpeed;
                state.RewardValue = stats.RewardValue;
                state.ContactDamage = stats.ContactDamage;
                state.DamageTypeId = stats.DamageTypeId;
                state.CollisionRadius = stats.CollisionRadius;
            }

            EnemyPresentationDefinitionAsset presentation = asset.Presentation;
            if (presentation != null)
            {
                state.Prefab = presentation.Prefab;
                ApplyEnemyEvent(presentation, EnemyPresentationEventKind.OnSpawn, out state.SpawnAudio, out state.SpawnVfxPrefab);
                ApplyEnemyEvent(presentation, EnemyPresentationEventKind.OnHit, out state.HitAudio, out state.HitVfxPrefab);
                ApplyEnemyEvent(presentation, EnemyPresentationEventKind.OnDeath, out state.DeathAudio, out state.DeathVfxPrefab);
            }

            return state;
        }

        private static WaveAuthoringState FromWaveAsset(WaveDefinitionAsset asset)
        {
            var state = new WaveAuthoringState
            {
                WaveId = asset.Id,
                DisplayName = asset.DisplayName,
                TagsCsv = JoinTags(asset.Tags),
                StartTick = asset.Schedule == null ? 0 : asset.Schedule.StartTick
            };

            state.Entries.Clear();
            IReadOnlyList<WaveEntryRecipe> entries = asset.Entries == null ? Array.Empty<WaveEntryRecipe>() : asset.Entries.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEntryRecipe entry = entries[i];
                if (entry == null) continue;
                state.Entries.Add(new WaveEntryAuthoringState
                {
                    Enemy = entry.Enemy,
                    Count = entry.Count,
                    BatchSize = entry.BatchSize,
                    InitialDelayTicks = entry.InitialDelayTicks,
                    IntervalTicks = entry.IntervalTicks,
                    SpawnChannelId = entry.SpawnChannelId,
                    ScalingTier = entry.ScalingTier
                });
            }

            state.EnsureEntries();
            return state;
        }

        private static void ApplyAttackPresentation(AttackPresentationDefinitionAsset presentation, AttackAuthoringState state)
        {
            if (presentation == null) return;
            ApplyAttackEvent(presentation, AttackPresentationEventKind.OnCast, out state.CastAudio, out state.CastVfxPrefab);
            ApplyAttackEvent(presentation, AttackPresentationEventKind.OnFire, out state.FireAudio, out state.FireVfxPrefab);
            ApplyAttackEvent(presentation, AttackPresentationEventKind.OnImpact, out state.ImpactAudio, out state.ImpactVfxPresentationPrefab);
            ApplyAttackEvent(presentation, AttackPresentationEventKind.OnTick, out state.TickAudio, out state.TickVfxPrefab);
            ApplyAttackEvent(presentation, AttackPresentationEventKind.OnExpire, out state.ExpireAudio, out state.ExpireVfxPrefab);
        }

        private static void ApplyAttackEvent(AttackPresentationDefinitionAsset presentation, AttackPresentationEventKind eventKind, out AudioClip audio, out GameObject vfx)
        {
            audio = null;
            vfx = null;
            if (presentation == null || !presentation.TryGetEvent(eventKind, out AttackPresentationEventRecipe recipe) || recipe == null)
                return;

            audio = recipe.AudioClip;
            vfx = recipe.VfxPrefab;
        }

        private static void ApplyEnemyEvent(EnemyPresentationDefinitionAsset presentation, EnemyPresentationEventKind eventKind, out AudioClip audio, out GameObject vfx)
        {
            audio = null;
            vfx = null;
            if (presentation == null || !presentation.TryGetEvent(eventKind, out EnemyPresentationEventRecipe recipe) || recipe == null)
                return;

            audio = recipe.AudioClip;
            vfx = recipe.VfxPrefab;
        }

        private static AttackStatusEffectRecipe FirstStatus(AttackStatusEffectsDefinitionAsset statusEffects)
        {
            IReadOnlyList<AttackStatusEffectRecipe> statuses = statusEffects == null ? Array.Empty<AttackStatusEffectRecipe>() : statusEffects.StatusEffects;
            for (int i = 0; i < statuses.Count; i++)
            {
                if (statuses[i] != null)
                    return statuses[i];
            }

            return null;
        }

        private static string JoinTags(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0) return string.Empty;
            var values = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (!string.IsNullOrWhiteSpace(tag)) values.Add(tag.Trim());
            }

            return string.Join(", ", values.ToArray());
        }
    }

    internal static class AttackGameContentPreviewSummaries
    {
        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildAttackRows(AttackAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("Name", state.DisplayName),
                Row("ID", state.AttackId),
                Row("Damage", FormatFloat(state.DamageAmount) + " " + state.DamageTypeId),
                Row("Cooldown", state.CooldownTicks.ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("Range", FormatFloat(state.Range)),
                Row("Targeting", state.TargetingMode.ToString()),
                Row("Delivery", GetAttackDeliverySummary(state)),
                Row("Status", GetAttackStatusSummary(state))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildAttackPresentationRows(AttackAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("OnCast", FormatEvent(state.CastAudio, state.CastVfxPrefab)),
                Row("OnFire", FormatEvent(state.FireAudio, state.FireVfxPrefab)),
                Row("OnImpact", FormatEvent(state.ImpactAudio, state.ImpactVfxPresentationPrefab)),
                Row("OnTick", FormatEvent(state.TickAudio, state.TickVfxPrefab)),
                Row("OnExpire", FormatEvent(state.ExpireAudio, state.ExpireVfxPrefab))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildAttackExpectedRows(AttackAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            string target = state.TargetingMode == AttackRecipeTargetingMode.ForwardDirection
                ? "dummy in forward lane"
                : "highest-priority dummy target";
            string result = target + " receives " + FormatFloat(state.DamageAmount) + " " + state.DamageTypeId + " damage";
            if (state.IncludeStatusEffect)
            {
                result += " and " + state.StatusId + " for " + state.StatusDurationTicks.ToString(CultureInfo.InvariantCulture) + " tick(s)";
            }

            return new[]
            {
                Row("Test Dummy", target),
                Row("Expected", result),
                Row("Optional Assets", "Missing audio, VFX, and model references are skipped by preview and runtime presentation.")
            };
        }

        public static IReadOnlyList<string> BuildAttackWarnings(AttackAuthoringState state)
        {
            if (state == null) return new[] { "Attack preview state is missing." };
            var warnings = new List<string>();
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile && state.ProjectilePrefab == null)
                warnings.Add("Projectile delivery has no prefab assigned; runtime data still converts, but the visual preview has no projectile model.");
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan && state.BeamVfxPrefab == null)
                warnings.Add("Hitscan delivery has no beam/tracer VFX assigned; preview will show data only.");
            if (state.IncludeStatusEffect && state.TickVfxPrefab == null && state.TickAudio == null)
                warnings.Add("Status effect has no OnTick presentation asset; status behavior still previews through data.");
            if (!HasAnyAttackAudio(state))
                warnings.Add("No attack audio clips assigned. Audio preview buttons remain safe and report visual summaries only.");
            return warnings;
        }

        public static UnityEngine.Object GetPrimaryAttackPreviewAsset(AttackAuthoringState state)
        {
            if (state == null) return null;
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile && state.ProjectilePrefab != null) return state.ProjectilePrefab;
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan && state.BeamVfxPrefab != null) return state.BeamVfxPrefab;
            return state.ImpactVfxPresentationPrefab ?? state.ImpactVfxPrefab ?? state.FireVfxPrefab ?? state.CastVfxPrefab ?? state.TickVfxPrefab ?? state.ExpireVfxPrefab;
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildEnemyRows(EnemyAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("Name", state.DisplayName),
                Row("ID", state.EnemyId),
                Row("Role", state.Role.ToString()),
                Row("Health", FormatFloat(state.MaximumHealth)),
                Row("Speed", FormatFloat(state.MoveSpeed)),
                Row("Reward", state.RewardValue.ToString(CultureInfo.InvariantCulture)),
                Row("Contact", FormatFloat(state.ContactDamage) + " " + state.DamageTypeId),
                Row("Radius", FormatFloat(state.CollisionRadius))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildEnemyPresentationRows(EnemyAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            return new[]
            {
                Row("OnSpawn", FormatEvent(state.SpawnAudio, state.SpawnVfxPrefab)),
                Row("OnHit", FormatEvent(state.HitAudio, state.HitVfxPrefab)),
                Row("OnDeath", FormatEvent(state.DeathAudio, state.DeathVfxPrefab))
            };
        }

        public static IReadOnlyList<string> BuildEnemyWarnings(EnemyAuthoringState state)
        {
            if (state == null) return new[] { "Enemy preview state is missing." };
            var warnings = new List<string>();
            if (state.Prefab == null)
                warnings.Add("Enemy prefab is not assigned; asset creation validation will block until a prefab/model reference is chosen.");
            if (state.MaximumHealth <= 0f)
                warnings.Add("Enemy health must be greater than zero.");
            if (state.MoveSpeed <= 0f)
                warnings.Add("Enemy speed must be greater than zero.");
            return warnings;
        }

        public static IReadOnlyList<GameContentAuthoringPreviewRow> BuildWaveRows(WaveAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewRow>();
            state.EnsureEntries();
            return new[]
            {
                Row("Name", state.DisplayName),
                Row("ID", state.WaveId),
                Row("Start Tick", state.StartTick.ToString(CultureInfo.InvariantCulture)),
                Row("Entries", state.Entries.Count.ToString(CultureInfo.InvariantCulture)),
                Row("Total Enemies", GetWaveTotalEnemyCount(state).ToString(CultureInfo.InvariantCulture)),
                Row("Approx Duration", GetWaveApproximateDurationTicks(state).ToString(CultureInfo.InvariantCulture) + " tick(s)"),
                Row("Difficulty", GetWaveDifficultySummary(state)),
                Row("Channels", GetWaveChannelSummary(state))
            };
        }

        public static IReadOnlyList<GameContentAuthoringPreviewTimelineItem> BuildWaveTimeline(WaveAuthoringState state)
        {
            if (state == null) return Array.Empty<GameContentAuthoringPreviewTimelineItem>();
            state.EnsureEntries();
            var items = new List<GameContentAuthoringPreviewTimelineItem>();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                int batchSize = Math.Max(1, entry.BatchSize);
                int batches = entry.Count <= 0 ? 0 : (int)Math.Ceiling(entry.Count / (double)batchSize);
                int firstTick = Math.Max(0, state.StartTick + entry.InitialDelayTicks);
                int lastTick = firstTick + Math.Max(0, batches - 1) * Math.Max(0, entry.IntervalTicks);
                string enemy = entry.Enemy == null ? "Missing enemy" : entry.Enemy.DisplayName;
                string label = "Entry " + (i + 1).ToString(CultureInfo.InvariantCulture) + ": " + enemy;
                string detail = entry.Count.ToString(CultureInfo.InvariantCulture) + " total, "
                    + batchSize.ToString(CultureInfo.InvariantCulture) + " per batch on " + entry.SpawnChannelId
                    + ", tier " + entry.ScalingTier.ToString(CultureInfo.InvariantCulture);
                items.Add(new GameContentAuthoringPreviewTimelineItem(label, firstTick.ToString(CultureInfo.InvariantCulture) + "-" + lastTick.ToString(CultureInfo.InvariantCulture), detail));
            }

            return items;
        }

        public static IReadOnlyList<string> BuildWaveWarnings(WaveAuthoringState state)
        {
            if (state == null) return new[] { "Wave preview state is missing." };
            state.EnsureEntries();
            var warnings = new List<string>();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                string prefix = "Entry " + (i + 1).ToString(CultureInfo.InvariantCulture) + ": ";
                if (entry.Enemy == null) warnings.Add(prefix + "enemy reference is missing.");
                if (entry.Count <= 0) warnings.Add(prefix + "count must be greater than zero.");
                if (entry.BatchSize <= 0) warnings.Add(prefix + "batch size must be greater than zero.");
                if (entry.IntervalTicks < 0) warnings.Add(prefix + "interval cannot be negative.");
                if (string.IsNullOrWhiteSpace(entry.SpawnChannelId)) warnings.Add(prefix + "spawn channel is required.");
            }

            return warnings;
        }

        public static int GetWaveTotalEnemyCount(WaveAuthoringState state)
        {
            if (state == null || state.Entries == null) return 0;
            int total = 0;
            for (int i = 0; i < state.Entries.Count; i++)
                total += Math.Max(0, state.Entries[i].Count);
            return total;
        }

        public static int GetWaveApproximateDurationTicks(WaveAuthoringState state)
        {
            if (state == null || state.Entries == null || state.Entries.Count == 0) return 0;
            int endTick = Math.Max(0, state.StartTick);
            for (int i = 0; i < state.Entries.Count; i++)
            {
                WaveEntryAuthoringState entry = state.Entries[i];
                int batches = entry.BatchSize <= 0 || entry.Count <= 0 ? 0 : (int)Math.Ceiling(entry.Count / (double)entry.BatchSize);
                int entryEnd = state.StartTick + Math.Max(0, entry.InitialDelayTicks) + Math.Max(0, batches - 1) * Math.Max(0, entry.IntervalTicks);
                endTick = Math.Max(endTick, entryEnd);
            }

            return Math.Max(0, endTick - Math.Max(0, state.StartTick));
        }

        private static GameContentAuthoringPreviewRow Row(string label, string value)
        {
            return new GameContentAuthoringPreviewRow(label, value);
        }

        private static string GetAttackDeliverySummary(AttackAuthoringState state)
        {
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Projectile)
            {
                string homing = state.Homing ? ", homing " + FormatFloat(state.HomingTurnRate) + " deg/s" : string.Empty;
                string pierce = state.PierceCount > 0 ? ", pierces " + state.PierceCount.ToString(CultureInfo.InvariantCulture) : string.Empty;
                return "Projectile " + state.ProjectileDefinitionId + " at " + FormatFloat(state.ProjectileSpeed) + " units/s" + homing + pierce;
            }

            if (state.DeliveryMode == AttackRecipeDeliveryMode.Hitscan)
                return "Hitscan/tracer, " + state.MaxHits.ToString(CultureInfo.InvariantCulture) + " max hit(s)";
            if (state.DeliveryMode == AttackRecipeDeliveryMode.Area)
                return "Area radius " + FormatFloat(state.Radius) + ", " + state.MaxHits.ToString(CultureInfo.InvariantCulture) + " max hit(s)";
            return "Aura radius " + FormatFloat(state.Radius) + ", ticks every " + FormatFloat(state.TickIntervalSeconds) + " second(s)";
        }

        private static string GetAttackStatusSummary(AttackAuthoringState state)
        {
            if (!state.IncludeStatusEffect) return "None";
            return state.StatusId + ", " + state.StatusDurationTicks.ToString(CultureInfo.InvariantCulture) + " tick duration, "
                + state.StatusTickRateTicks.ToString(CultureInfo.InvariantCulture) + " tick rate, strength " + FormatFloat(state.StatusStrength);
        }

        private static string GetWaveDifficultySummary(WaveAuthoringState state)
        {
            int max = 0;
            for (int i = 0; i < state.Entries.Count; i++)
                max = Math.Max(max, state.Entries[i].ScalingTier);
            return "Max tier " + max.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetWaveChannelSummary(WaveAuthoringState state)
        {
            var channels = new List<string>();
            for (int i = 0; i < state.Entries.Count; i++)
            {
                string channel = state.Entries[i].SpawnChannelId;
                if (string.IsNullOrWhiteSpace(channel) || channels.Contains(channel)) continue;
                channels.Add(channel);
            }

            return channels.Count == 0 ? "None" : string.Join(", ", channels.ToArray());
        }

        private static string FormatEvent(AudioClip audio, GameObject vfx)
        {
            string audioText = audio == null ? "no audio" : "audio " + audio.name;
            string vfxText = vfx == null ? "no VFX" : "VFX " + vfx.name;
            return audioText + ", " + vfxText;
        }

        private static bool HasAnyAttackAudio(AttackAuthoringState state)
        {
            return state.CastAudio != null
                || state.FireAudio != null
                || state.ImpactAudio != null
                || state.TickAudio != null
                || state.ExpireAudio != null;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    internal static class AttackEditorPreviewAudio
    {
        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayPreviewClipMethod = FindMethod("PlayPreviewClip") ?? FindMethod("PlayClip");
        private static readonly MethodInfo StopPreviewClipMethod = FindMethod("StopAllPreviewClips") ?? FindMethod("StopAllClips");

        public static bool TryPlay(AudioClip clip, out string message)
        {
            if (clip == null)
            {
                message = "No audio clip assigned.";
                return false;
            }

            if (PlayPreviewClipMethod == null)
            {
                message = "Audio preview is unavailable in this Unity editor; clip " + clip.name + " was not played.";
                return false;
            }

            try
            {
                StopAll();
                PlayPreviewClipMethod.Invoke(null, BuildArguments(PlayPreviewClipMethod, clip));
                message = "audio clip " + clip.name + " started.";
                return true;
            }
            catch (Exception exception)
            {
                message = "Audio preview failed gracefully: " + exception.GetType().Name + ".";
                return false;
            }
        }

        public static void StopAll()
        {
            if (StopPreviewClipMethod == null) return;
            try
            {
                StopPreviewClipMethod.Invoke(null, Array.Empty<object>());
            }
            catch
            {
                // Audio preview is best-effort editor UI only.
            }
        }

        private static MethodInfo FindMethod(string name)
        {
            if (AudioUtilType == null) return null;
            MethodInfo[] methods = AudioUtilType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (!string.Equals(methods[i].Name, name, StringComparison.Ordinal)) continue;
                ParameterInfo[] parameters = methods[i].GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType != typeof(AudioClip)) continue;
                return methods[i];
            }

            return null;
        }

        private static object[] BuildArguments(MethodInfo method, AudioClip clip)
        {
            ParameterInfo[] parameters = method.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (parameterType == typeof(AudioClip)) args[i] = clip;
                else if (parameterType == typeof(int)) args[i] = 0;
                else if (parameterType == typeof(bool)) args[i] = false;
                else if (parameterType.IsValueType) args[i] = Activator.CreateInstance(parameterType);
                else args[i] = null;
            }

            return args;
        }
    }
}
