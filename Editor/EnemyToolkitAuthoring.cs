using System;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Attacks.Editor
{
    internal static class EnemyToolkitAuthoring
    {
        internal static VisualElement Create(GameContentAuthoringSurfaceContext context)
        {
            var asset = context.SelectedItem?.Asset as EnemyDefinitionAsset;
            return GameContentToolkitDraftEditor.Create(context,
                () => asset == null ? new EnemyAuthoringState() : AttackGameContentPreviewSelection.FromEnemyAsset(asset),
                EnemyAuthoringDraft.BuildStateFingerprint,
                state => asset == null ? EnemyAuthoringSession.ValidateDraft(state) : EnemyDefinitionAssetCreator.ValidateForUpdate(state, asset),
                state => asset == null ? EnemyDefinitionAssetCreator.CreateAssets(state) : EnemyDefinitionAssetCreator.UpdateExistingAsset(asset, state),
                (root, state) => { Fields(root, state, context); AttackToolkitPreview.AddEnemy(root, state); });
        }
        private static void Fields(VisualElement root, EnemyAuthoringState state, GameContentAuthoringSurfaceContext context)
        {
            var form = new DeucarianEditorWorkspaceForm(root);
            form.Text("DisplayName", "Name", () => state.DisplayName, value => state.DisplayName = value);
            form.NumberWithSlider("MaximumHealth", "Health", 0, 200, () => state.MaximumHealth, value => state.MaximumHealth = value);
            form.NumberWithSlider("MoveSpeed", "Speed", 0, 10, () => state.MoveSpeed, value => state.MoveSpeed = value);
            form.Asset("Icon", "Icon", typeof(Sprite), () => state.Icon, value => state.Icon = (Sprite)value);
            var advanced = form.Section("Advanced data", true);
            advanced.Text("EnemyId", "Enemy Id", () => state.EnemyId, value => state.EnemyId = value);
            advanced.Enum("Role", "Role", () => state.Role, value => state.Role = value);
            advanced.Text("TagsCsv", "Tags Csv", () => state.TagsCsv, value => state.TagsCsv = value);
            advanced.Text("OutputRoot", "Output Root", () => state.OutputRoot, value => state.OutputRoot = value);
            advanced.Asset("Prefab", "Prefab", typeof(GameObject), () => state.Prefab, value => state.Prefab = (GameObject)value);
            advanced.Integer("RewardValue", "Reward Value", () => state.RewardValue, value => state.RewardValue = value);
            advanced.Number("ContactDamage", "Contact Damage", () => state.ContactDamage, value => state.ContactDamage = value);
            advanced.Text("DamageTypeId", "Damage Type Id", () => state.DamageTypeId, value => state.DamageTypeId = value);
            advanced.Number("CollisionRadius", "Collision Radius", () => state.CollisionRadius, value => state.CollisionRadius = value);
            advanced.Asset("SpawnAudio", "Spawn Audio", typeof(AudioClip), () => state.SpawnAudio, value => state.SpawnAudio = (AudioClip)value);
            advanced.Asset("SpawnVfxPrefab", "Spawn Vfx Prefab", typeof(GameObject), () => state.SpawnVfxPrefab, value => state.SpawnVfxPrefab = (GameObject)value);
            advanced.Asset("HitAudio", "Hit Audio", typeof(AudioClip), () => state.HitAudio, value => state.HitAudio = (AudioClip)value);
            advanced.Asset("HitVfxPrefab", "Hit Vfx Prefab", typeof(GameObject), () => state.HitVfxPrefab, value => state.HitVfxPrefab = (GameObject)value);
            advanced.Asset("DeathAudio", "Death Audio", typeof(AudioClip), () => state.DeathAudio, value => state.DeathAudio = (AudioClip)value);
            advanced.Asset("DeathVfxPrefab", "Death Vfx Prefab", typeof(GameObject), () => state.DeathVfxPrefab, value => state.DeathVfxPrefab = (GameObject)value);

        }
    }
}
