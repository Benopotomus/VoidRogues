using UnityEngine;

namespace VoidRogues.NPCs
{
    /// <summary>
    /// Visual representation of a single NPC in the scene.
    ///
    /// Following the LichLord <c>NonPlayerCharacter</c> pattern, this is a local-only
    /// <see cref="MonoBehaviour"/> (no <see cref="Fusion.NetworkObject"/>).
    /// The <see cref="NPCManager"/> creates one instance per active NPC slot and
    /// drives its state every render frame from the corresponding
    /// <see cref="NPCState"/> struct.
    ///
    /// Responsibilities:
    ///   - Holds a <see cref="Collider2D"/> for player-interaction raycasts.
    ///   - Drives the <see cref="Animator"/> based on <see cref="NPCState.AnimState"/>.
    ///   - Shows/hides the interaction prompt UI element.
    ///   - Provides an <see cref="SlotIndex"/> so external systems (e.g. the player
    ///     interaction controller) can resolve which NPC a collider belongs to.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NonPlayerCharacter : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        [Header("Interaction")]
        [Tooltip("UI element shown when a player is close enough to interact.")]
        [SerializeField] private GameObject _interactionPrompt;

        [Header("Visual")]
        [Tooltip("Optional SpriteRenderer for facing-direction flipping.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // ------------------------------------------------------------------
        // Runtime
        // ------------------------------------------------------------------

        /// <summary>
        /// Index into <see cref="NPCManager._npcs"/> array.
        /// Set by <see cref="NPCManager"/> when the visual is created.
        /// </summary>
        public int SlotIndex { get; private set; } = -1;

        /// <summary>
        /// The <see cref="NPCDefinition"/> associated with this NPC.
        /// </summary>
        public NPCDefinition Definition { get; private set; }

        private Animator   _animator;
        private Collider2D _collider;

        private byte _lastAnimState = 255; // Force first update
        private byte _lastDialogueState = 255;
        private int  _lastInteractingPlayer = -2; // sentinel

        // ------------------------------------------------------------------
        // API – called by NPCManager
        // ------------------------------------------------------------------

        /// <summary>
        /// Initialises the <see cref="NonPlayerCharacter"/> after instantiation.
        /// Called once by <see cref="NPCManager.EnsureVisual"/>.
        /// </summary>
        public void Initialise(int slotIndex, NPCDefinition definition)
        {
            SlotIndex  = slotIndex;
            Definition = definition;

            _animator  = GetComponentInChildren<Animator>();
            _collider  = GetComponent<Collider2D>();

            if (_animator != null && definition.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = definition.AnimatorController;
            }

            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(false);
            }

            gameObject.name = $"NPC_{slotIndex}_{definition.NPCName}";
        }

        /// <summary>
        /// Applies the replicated <see cref="NPCState"/> to the local visual.
        /// Called each render frame by <see cref="NPCManager"/>.
        /// </summary>
        public void ApplyState(NPCState state)
        {
            // Position
            transform.position = state.Position;

            // Face direction based on velocity
            if (_spriteRenderer != null && state.Velocity.sqrMagnitude > 0.01f)
            {
                _spriteRenderer.flipX = state.Velocity.x < 0f;
            }

            // Animation state – only update on change to avoid spamming the animator
            if (state.AnimState != _lastAnimState)
            {
                _lastAnimState = state.AnimState;
                if (_animator != null)
                {
                    _animator.SetInteger("State", state.AnimState);
                }
            }

            // Dialogue state
            if (state.DialogueState != _lastDialogueState)
            {
                _lastDialogueState = state.DialogueState;
                if (_animator != null)
                {
                    _animator.SetInteger("DialogueState", state.DialogueState);
                }
            }

            // Interaction prompt visibility
            if (state.InteractingPlayer != _lastInteractingPlayer)
            {
                _lastInteractingPlayer = state.InteractingPlayer;
                if (_interactionPrompt != null)
                {
                    _interactionPrompt.SetActive(state.InteractingPlayer >= 0);
                }
            }
        }

        /// <summary>
        /// Hides the visual and resets cached state.
        /// Called by <see cref="NPCManager"/> when the NPC slot is deactivated.
        /// </summary>
        public void Deactivate()
        {
            gameObject.SetActive(false);
            _lastAnimState = 255;
            _lastDialogueState = 255;
            _lastInteractingPlayer = -2;

            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(false);
            }
        }

        // ------------------------------------------------------------------
        // Public accessors for external systems
        // ------------------------------------------------------------------

        /// <summary>Returns the <see cref="Collider2D"/> used for interaction raycasts.</summary>
        public Collider2D GetCollider() => _collider;
    }
}
