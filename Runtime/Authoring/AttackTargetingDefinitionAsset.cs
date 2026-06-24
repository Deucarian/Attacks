using Deucarian.Attacks;
using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class AttackTargetingDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AttackRecipeTargetingMode _mode = AttackRecipeTargetingMode.Nearest;
        [SerializeField] private int _maxTargets = 1;
        [SerializeField] private bool _requiresLineOfSight;

        public AttackRecipeTargetingMode Mode => _mode;
        public int MaxTargets => _maxTargets;
        public bool RequiresLineOfSight => _requiresLineOfSight;

        public void Configure(AttackRecipeTargetingMode mode, int maxTargets = 1, bool requiresLineOfSight = false)
        {
            _mode = mode;
            _maxTargets = maxTargets;
            _requiresLineOfSight = requiresLineOfSight;
        }

        public AttackTargetPolicy ToRuntimePolicy()
        {
            return _mode == AttackRecipeTargetingMode.LowestHealth
                ? AttackTargetPolicy.LowestScore
                : AttackTargetPolicy.HighestScore;
        }
    }
}
