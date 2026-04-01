using Fusion;
using UnityEngine;

namespace VoidRogues.GameFlow
{
    /// <summary>
    /// Manages the Ship hub scene: player customisation, room navigation, and the
    /// pre-mission lobby.
    ///
    /// Attach to a persistent GameObject in the Ship scene.
    /// </summary>
    public class ShipManager : NetworkBehaviour
    {
        [Header("Lobby Settings")]
        [SerializeField] private int _defaultMissionIndex = 2;

        // Tracks which players have pressed "Ready" in the lobby.
        [Networked, Capacity(4)]
        private NetworkArray<NetworkBool> _playerReady { get; }

        private ChangeDetector _changes;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShipReady();
            }
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsServer) return;

            // Check if all connected players are ready.
            bool allReady  = true;
            int  connected = 0;

            foreach (var player in Runner.ActivePlayers)
            {
                connected++;
                int idx = player.AsIndex % 4;
                if (!_playerReady[idx])
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady && connected > 0)
            {
                GameManager.Instance?.LoadMission(_defaultMissionIndex);
            }
        }

        // ------------------------------------------------------------------
        // Public API (called from UI)
        // ------------------------------------------------------------------

        /// <summary>Marks the local player as ready.  RPC'd to the host.</summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetReady(bool isReady, RpcInfo info = default)
        {
            int idx = info.Source.AsIndex % 4;
            _playerReady.Set(idx, isReady);
        }

        /// <summary>Returns whether a given player slot is ready.</summary>
        public bool IsPlayerReady(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4) return false;
            return _playerReady[slotIndex];
        }
    }
}
