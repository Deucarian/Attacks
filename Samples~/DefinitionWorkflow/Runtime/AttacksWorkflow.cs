using System;
using UnityEngine;
using Deucarian.Combat;
using Deucarian.Combat.Unity;
namespace Deucarian.Attacks.Authoring.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class AttacksWorkflow : MonoBehaviour
    {
        [SerializeField] private AttackTrigger trigger;
        [SerializeField] private Combatant target;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public void Attack() { trigger.ApplyDirectDamage(); status = "Health: " + target.CurrentHealth + ". Request: " + trigger.LastResult.FailureReason; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("Attacks — definition workflow");
            GUILayout.Label("Select an attack definition and a live combatant. AttackTrigger resolves direct damage through Combat; the host advances cooldowns.");
            GUILayout.Space(12);
            if (GUILayout.Button("Request and resolve attack", GUILayout.Height(32))) { try { Attack(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
