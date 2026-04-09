// using DWD.Pooling; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterSpawningComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField]
        private Transform _spawnAttachment;

        // TODO: Port VisualEffectSpawner from LichLord
        // private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        private int _spawnEndTick;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
        }

        public void UpdateSpawningState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (tick > _spawnEndTick)
            {
                runtimeState.SetState(ENPCState.Idle);
            }
        }

        public void StartSpawnState(int tick)
        {
            NonPlayerCharacterSpawnState spawnState = _npc.RuntimeState.Definition.SpawnState;
            var animTrigger = spawnState.AnimationTrigger;

            _spawnEndTick = tick + (int)(spawnState.StateTime * 32);

            // TODO: Port visual effect spawning from LichLord
        }
    }
}
