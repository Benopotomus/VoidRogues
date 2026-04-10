using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterRuntimeState
    {
        public int PredictionStartTick;
        public int PredictionTimeoutTick; // Max lifetime of predictive state

        private int _localIndex;
        public int LocalIndex => _localIndex;

        private int _fullIndex;
        public int FullIndex => _fullIndex;

        private NonPlayerCharacterManager _manager;

        private SceneContext _context;
        public SceneContext Context => _context;

        FNonPlayerCharacterData _npcData = new FNonPlayerCharacterData();
        public FNonPlayerCharacterData Data => _npcData;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = NonPlayerCharacterTable.TryGetDefinition(_npcData.DefinitionID);

                return _definition;
            }
        }

        private NonPlayerCharacterDataDefinition _dataDefinition;
        public NonPlayerCharacterDataDefinition DataDefinition
        {
            get
            {
                if (_npcData.DefinitionID == 0)
                    return null;

                if (_dataDefinition == null)
                    _dataDefinition = _npcData.DataDefinition;

                return _dataDefinition;
            }
        }

        public NonPlayerCharacterRuntimeState(NonPlayerCharacterManager manager, int localIndex, int fullIndex)
        {
            _manager = manager;
            _context = manager.Context;
            _localIndex = localIndex;
            _fullIndex = fullIndex;
        }

        public void CopyData(ref FNonPlayerCharacterData other)
        {
            _npcData.Copy(ref other);

            if (_npcData.DefinitionID == 0)
                return;

            _definition = NonPlayerCharacterTable.TryGetDefinition(_npcData.DefinitionID);
            _dataDefinition = _npcData.DataDefinition;
        }

        public void ApplyDamage(int damage, int hitReactIndex, int additiveHitReactIndex)
        {
            if (Definition == null || DataDefinition == null)
                return;

            int currentHealth = _dataDefinition.GetHealth(ref _npcData);
            damage = Mathf.Max(damage - Definition.DamageReduction, 0);
            damage = (int)((float)damage * (1.0f - Definition.DamageResistance));

            int postDamageHealth = Mathf.Max(currentHealth - damage, 0);

            _dataDefinition.SetHealth(postDamageHealth, ref _npcData);

            if (postDamageHealth == 0)
            {
                ENPCState nextState = GetNextStateFromState(ENPCState.Dead);
                _dataDefinition.SetState(nextState, ref _npcData);
            }
            else
            {
                if (damage > Definition.HitReactThreshold)
                {
                    ENPCState nextState = GetNextStateFromState(ENPCState.HitReact);
                    _dataDefinition.SetState(nextState, ref _npcData);
                    _dataDefinition.SetAnimationIndex(hitReactIndex, ref _npcData);
                }
                else
                {
                    _dataDefinition.SetAdditiveHitReactIndex(additiveHitReactIndex, ref _npcData);
                }
            }

            _manager.ReplicateRuntimeState(this);
        }

        public ENPCState GetNextStateFromState(ENPCState newState)
        {
            ENPCState currentState = _dataDefinition.GetState(ref _npcData);

            switch (newState)
            {
                case ENPCState.Inactive:
                    return newState;
                case ENPCState.HitReact:
                    switch (currentState)
                    {
                        case ENPCState.Dead:
                        case ENPCState.Inactive:
                            return currentState;
                    }
                    break;
            }

            return newState;
        }

        public bool IsActive()
        {
            return GetState() != ENPCState.Inactive;
        }

        public ETeamID GetTeam()
        {
            return _npcData.DataDefinition.GetTeamID(ref _npcData);
        }

        public ENPCSpawnType GetSpawnType()
        {
            return _npcData.SpawnType;
        }

        public EAttitude GetAttitude()
        {
            return _npcData.DataDefinition.GetAttitude(ref _npcData);
        }

        public void SetAttitude(EAttitude newAttitude)
        {
            if (_npcData.DefinitionID == 0)
                return;

            DataDefinition.SetAttitude(newAttitude, ref _npcData);
            _manager.ReplicateRuntimeState(this);
        }

        public int GetAdditiveHitReact()
        {
            return _npcData.DataDefinition.GetAdditiveHitReactIndex(ref _npcData);
        }

        public void SetAdditiveHitReact(int newAdditiveIndex)
        {
            if (_npcData.DefinitionID == 0)
                return;

            DataDefinition.SetAdditiveHitReactIndex(newAdditiveIndex, ref _npcData);
            _manager.ReplicateRuntimeState(this);
        }

        public ENPCState GetState()
        {
            if (_npcData.DefinitionID == 0)
                return ENPCState.Inactive;

            if (_npcData.Definition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref _npcData);
        }

        public void SetState(ENPCState newState)
        {
            if (_npcData.DefinitionID == 0)
                return;

            DataDefinition.SetState(newState, ref _npcData);
            _manager.ReplicateRuntimeState(this);
        }

        public int GetAnimationIndex()
        {
            return DataDefinition.GetAnimationIndex(ref _npcData);
        }

        public void SetAnimationIndex(int index)
        {
            DataDefinition.SetAnimationIndex(index, ref _npcData);
            _manager.ReplicateRuntimeState(this);
        }

        public Vector3 GetPosition()
        {
            return _npcData.Position;
        }

        public void SetPosition(Vector3 position)
        {
            _npcData.Position = position;
        }

        public Quaternion GetRotation()
        {
            return _npcData.Rotation;
        }

        public float GetYaw()
        {
            return _npcData.Yaw;
        }

        public byte GetRawCompressedYaw()
        {
            return _npcData.RawCompressedYaw;
        }

        public int GetTargetPlayerIndex()
        {
            return _npcData.TargetPlayerIndex;
        }

        public int GetHealth()
        {
            return DataDefinition.GetHealth(ref _npcData);
        }

        public int GetMaxHealth()
        {
            return Definition.MaxHealth;
        }

        public bool IsInvader()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _npcData) == ENPCSpawnType.Invader)
                return true;

            return false;
        }

        public ENPCState GetStateFromData(ref FNonPlayerCharacterData otherData)
        {
            if (_npcData.DefinitionID == 0)
                return ENPCState.Inactive;

            if (DataDefinition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref otherData);
        }
    }
}
