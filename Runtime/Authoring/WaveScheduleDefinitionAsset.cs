using UnityEngine;

namespace Deucarian.Attacks.Authoring
{
    public sealed class WaveScheduleDefinitionAsset : ScriptableObject
    {
        [SerializeField] private int _startTick;

        public int StartTick => _startTick;

        public void Configure(int startTick)
        {
            _startTick = startTick;
        }
    }
}
