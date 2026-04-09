using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Runtime debugger for the NPC system.
    ///
    /// Draws an on-screen overlay showing active NPC count, per-NPC state, and
    /// gizmos in the Scene view. Attach alongside
    /// <see cref="NonPlayerCharacterManager"/> on the SceneContext hierarchy.
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

        [Tooltip("Color used for NPC wander-target gizmo.")]
        [SerializeField] private Color _wanderTargetColor = Color.yellow;

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

            float panelWidth  = 420f;
            float panelHeight = 500f;

            GUILayout.BeginArea(new Rect(10, 10, panelWidth, panelHeight), GUI.skin.box);

            GUILayout.Label($"<b>NPC Debugger</b>  (toggle: {_toggleKey})");
            GUILayout.Label($"Active NPCs: <b>{manager.ActiveNPCCount}</b> / {NonPlayerCharacterManager.MaxNPCs}");
            GUILayout.Space(4);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(panelHeight - 80));

            for (int i = 0; i < NonPlayerCharacterManager.MaxNPCs; i++)
            {
                var state = manager.GetNPCState(i);
                if (!state.IsActive) continue;

                string animName = state.AnimState switch
                {
                    0 => "Idle",
                    1 => "Walk",
                    2 => "Talk",
                    3 => "Interact",
                    _ => $"Unknown({state.AnimState})"
                };

                string dialogueName = state.DialogueState switch
                {
                    0 => "None",
                    1 => "Greeting",
                    2 => "InDialogue",
                    3 => "Farewell",
                    _ => $"Unknown({state.DialogueState})"
                };

                string interacting = state.InteractingPlayer >= 0
                    ? $"Player {state.InteractingPlayer}"
                    : "—";

                GUILayout.Label($"[{i}] Type:{state.TypeIndex}  Pos:{state.Position:F1}  " +
                                $"Anim:{animName}  Dlg:{dialogueName}  Int:{interacting}");
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

            for (int i = 0; i < NonPlayerCharacterManager.MaxNPCs; i++)
            {
                var state = manager.GetNPCState(i);
                if (!state.IsActive) continue;

                Vector3 pos = new Vector3(state.Position.x, state.Position.y, 0f);

                // NPC position circle
                Gizmos.color = _gizmoColor;
                Gizmos.DrawWireSphere(pos, 0.3f);

                // Wander target
                Gizmos.color = _wanderTargetColor;
                Vector3 wanderPos = new Vector3(state.WanderTarget.x, state.WanderTarget.y, 0f);
                Gizmos.DrawLine(pos, wanderPos);
                Gizmos.DrawWireSphere(wanderPos, 0.1f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.4f, $"NPC[{i}]");
#endif
            }
        }
    }
}
