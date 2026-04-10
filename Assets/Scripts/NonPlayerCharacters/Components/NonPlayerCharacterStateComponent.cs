using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterStateComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] private ENPCState _currentState = ENPCState.Inactive;
        public ENPCState CurrentState => _currentState;

        [SerializeField] private int _currentAnimIndex;
        public int CurrentAnimIndex => _currentAnimIndex;

        int _deathTicks = 64;
        int _deathEndTick;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick)
        {
            UpdateState(runtimeState, hasAuthority, tick, true);
        }

        public void StartRecycle()
        {
            _currentState = ENPCState.Inactive;
        }

        public void UpdateState(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick, bool forceUpdate = false)
        {
            UpdateStateChange(runtimeState, hasAuthority, tick, forceUpdate);

            if (hasAuthority)
                UpdateCurrentState(runtimeState, tick);
        }

        private void UpdateStateChange(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick, bool forceUpdate = false)
        {
            ENPCState oldState = _currentState;
            ENPCState newState = runtimeState.GetState();
            int animIndex = runtimeState.GetAnimationIndex();

            if (!forceUpdate)
            {
                if (_currentState == newState &&
                    _currentAnimIndex == animIndex)
                    return;
            }

            NPC.AnimationController.SetAnimationForState(oldState, newState);

            switch (oldState)
            {
                case ENPCState.Dead:
                case ENPCState.Inactive:
                    // TODO: Port FollowerEntity.Teleport from LichLord
                    break;
            }

            switch (newState)
            {
                case ENPCState.Idle:
                    NPC.Collider.enabled = true;
                    // TODO: Port Hurtbox from LichLord
                    // NPC.Hurtbox.SetHurtBoxesActive(true);
                    if (hasAuthority)
                    {
                        NPC.Movement.SetRVOSettings(false, 0.5f);
                        NPC.Movement.SetFollowerUpdatePosition(true);
                        NPC.Movement.SetFollowerUpdateRotation(true);
                        NPC.Movement.SetFollowerCanMove(true);
                    }
                    break;

                case ENPCState.Inactive:
                    // TODO: Port Hurtbox from LichLord
                    // NPC.Hurtbox.SetHurtBoxesActive(false);
                    if (hasAuthority)
                    {
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;

                case ENPCState.Dead:
                    _deathEndTick = tick + _deathTicks;
                    // TODO: Port Hurtbox from LichLord
                    // NPC.Hurtbox.SetHurtBoxesActive(false);
                    NPC.Collider.enabled = false;

                    if (hasAuthority)
                    {
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;

                case ENPCState.HitReact:
                    NPC.Collider.enabled = true;
                    NPC.HitReact.StartHitReact(newState, animIndex, tick);

                    if (hasAuthority)
                    {
                        NPC.Movement.SetRVOSettings(true, 0.5f);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerUpdatePosition(false);
                    }
                    break;

                case ENPCState.Maneuver_1:
                case ENPCState.Maneuver_2:
                case ENPCState.Maneuver_3:
                case ENPCState.Maneuver_4:
                    NPC.Collider.enabled = true;
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);

                    if (hasAuthority)
                    {
                        NPC.Movement.SetRVOSettings(true, 0.5f);
                    }
                    break;

                case ENPCState.Spawning:
                    // TODO: Port Hurtbox from LichLord
                    // NPC.Hurtbox.SetHurtBoxesActive(false);
                    NPC.Collider.enabled = false;
                    NPC.SpawningComponent.StartSpawnState(tick);

                    if (hasAuthority)
                    {
                        NPC.Movement.SetRVOSettings(true, 0.5f);
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;
            }

            _currentAnimIndex = animIndex;
            _currentState = newState;
        }

        private void UpdateCurrentState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            switch (runtimeState.GetState())
            {
                case ENPCState.Spawning:
                    NPC.SpawningComponent.UpdateSpawningState(runtimeState, tick);
                    break;
                case ENPCState.HitReact:
                    NPC.HitReact.UpdateHitReactState(runtimeState, tick);
                    break;
                case ENPCState.Dead:
                    if (tick > _deathEndTick)
                    {
                        runtimeState.SetState(ENPCState.Inactive);
                    }
                    break;
            }
        }
    }
}
