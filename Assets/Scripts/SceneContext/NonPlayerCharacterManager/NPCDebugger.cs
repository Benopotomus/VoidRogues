using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Runtime debugger for the NPC system.
    ///
    /// Draws an on-screen overlay showing active NPC states from the replicator system.
    /// Attach alongside NonPlayerCharacterManager on the SceneContext hierarchy.
    ///
    /// Toggled at runtime via <see cref="_toggleKey"/> (default: F9).
    /// </summary>
    public class NPCDebugger : CoreBehaviour
    {
        [Header("Settings")]
        [Tooltip("Key to toggle the debug overlay at runtime.")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F9;

        [Tooltip("Show gizmo circles around each NPC in the Scene view.")]
        [SerializeField] private bool _drawGizmos = true;

        [Tooltip("Color used for NPC gizmo circles.")]
        [SerializeField] private Color _gizmoColor = Color.cyan;

        [Header("References")]
        [Tooltip("NonPlayerCharacterManager to debug. Auto-resolves from sibling/parent if null.")]
        [SerializeField] private NonPlayerCharacterManager _manager;

        private bool _showOverlay;

        // GUI layout state
        private Vector2 _scrollPos;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showOverlay = !_showOverlay;
            }
        }

        private NonPlayerCharacterManager ResolveManager()
        {
            if (_manager != null) return _manager;
            _manager = GetComponentInParent<NonPlayerCharacterManager>();
            if (_manager == null)
            {
                _manager = GetComponentInChildren<NonPlayerCharacterManager>();
            }
            return _manager;
        }

        // ------------------------------------------------------------------
        // On-screen overlay
        // ------------------------------------------------------------------

        private void OnGUI()
        {
            if (!_showOverlay) return;

            var manager = ResolveManager();
            if (manager == null)
            {
                GUI.Label(new Rect(10, 10, 400, 30), "<b>[NPCDebugger]</b> No NonPlayerCharacterManager found.");
                return;
            }

            float panelWidth  = 500f;
            float panelHeight = 500f;

            GUILayout.BeginArea(new Rect(10, 10, panelWidth, panelHeight), GUI.skin.box);

            GUILayout.Label($"<b>NPC Debugger</b>  (toggle: {_toggleKey})");
            GUILayout.Space(4);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(panelHeight - 80));

            // Iterate through replicators and display active NPC data
            int totalActive = 0;
            for (int fullIndex = 0; fullIndex < NonPlayerCharacterConstants.MAX_NPC_REPS * NonPlayerCharacterConstants.MAX_REPLICATORS; fullIndex++)
            {
                var runtimeState = manager.GetNpcRuntimeStateAtIndex(fullIndex);
                if (runtimeState == null) continue;
                if (!runtimeState.IsActive()) continue;

                totalActive++;

                string stateName = runtimeState.GetState().ToString();
                string spawnType = runtimeState.GetSpawnType().ToString();
                Vector3 pos = runtimeState.GetPosition();

                int health = -1;
                int maxHealth = 0;
                if (runtimeState.Definition != null && runtimeState.DataDefinition != null)
                {
                    health = runtimeState.GetHealth();
                    maxHealth = runtimeState.GetMaxHealth();
                }

                string healthStr = health >= 0 ? $"HP:{health}/{maxHealth}" : "HP:N/A";

                GUILayout.Label($"[{fullIndex}] {spawnType}  State:{stateName}  Pos:({pos.x:F1},{pos.y:F1},{pos.z:F1})  {healthStr}");
            }

            if (totalActive == 0)
            {
                GUILayout.Label("No active NPCs.");
            }
            else
            {
                GUILayout.Label($"\nTotal active: {totalActive}");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------
        // Scene-view gizmos
        // ------------------------------------------------------------------

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            var manager = ResolveManager();
            if (manager == null) return;

            for (int fullIndex = 0; fullIndex < NonPlayerCharacterConstants.MAX_NPC_REPS * NonPlayerCharacterConstants.MAX_REPLICATORS; fullIndex++)
            {
                var runtimeState = manager.GetNpcRuntimeStateAtIndex(fullIndex);
                if (runtimeState == null) continue;
                if (!runtimeState.IsActive()) continue;

                Vector3 pos = runtimeState.GetPosition();

                // NPC position circle
                Gizmos.color = _gizmoColor;
                Gizmos.DrawWireSphere(pos, 0.3f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.4f, $"NPC[{fullIndex}]");
#endif
            }
        }
    }
}
