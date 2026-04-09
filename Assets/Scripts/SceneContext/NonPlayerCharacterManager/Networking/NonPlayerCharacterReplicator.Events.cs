using Fusion;

namespace VoidRogues
{
    public partial class NonPlayerCharacterReplicator : ContextBehaviour
    {
        public void Predict_DealDamageToNPC(int index, int damage, int hitReactIndex, int additiveHitReactIndex)
        {
            var targetData = _npcDatas.Get(index);

            int predictionTicks = 32;

            if (_predictedStates.TryGetValue(index, out NonPlayerCharacterRuntimeState predictedState))
            {
                predictedState.ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
                predictedState.PredictionStartTick = Runner.Tick + 0;
                predictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
            }
            else
            {
                int fullIndex = index + (NonPlayerCharacterConstants.MAX_NPC_REPS * Index);
                NonPlayerCharacterRuntimeState newPredictedState = new NonPlayerCharacterRuntimeState(this, index, fullIndex);
                newPredictedState.CopyData(ref targetData);
                newPredictedState.ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
                newPredictedState.PredictionStartTick = Runner.Tick + 0;
                newPredictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
                _predictedStates[index] = newPredictedState;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToNPC(int index, int damage, int hitReactIndex, int additiveHitReactIndex)
        {
            _localRuntimeStates[index].ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetNPCState(int index, ENPCState newState)
        {
            _localRuntimeStates[index].SetState(newState);
        }
    }
}
