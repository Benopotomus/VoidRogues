namespace VoidRogues
{
    using Fusion;
    using Fusion.Addons.KCC;
    using UnityEngine;
    using VoidRogues.Players;

    /// <summary>
    /// Networked player character that reads <see cref="GameInput"/> from
    /// Fusion's input pipeline and drives a <see cref="KCC"/>
    /// on the XZ plane (no gravity, locked vertical axis).
    /// </summary>
    [RequireComponent(typeof(KCC))]
    public class PlayerCharacter : ContextBehaviour
    {
        // PUBLIC MEMBERS

        /// <summary>
        /// Input provider that lives on this character.
        /// Registers with Fusion's runner callbacks when this is the local player.
        /// </summary>
        public PlayerCharacterInput Input { get; private set; }

        public PlayerEntity OwningPlayer { get; set; }

        // PRIVATE MEMBERS

        private KCC _kcc;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            _kcc = GetComponent<KCC>();
            Input = GetComponent<PlayerCharacterInput>();

            // Link back to the owning PlayerEntity
            OwningPlayer = PlayerEntity.GetPlayerEntity(Runner, Object.InputAuthority);

            // Immediately zero out any gravity on the KCC data to prevent a single-frame drop.
            // NoGravityXZMovementProcessor (in KCC Settings.Processors) handles ongoing suppression.
            _kcc.FixedData.Gravity = Vector3.zero;
            _kcc.RenderData.Gravity = Vector3.zero;
            _kcc.FixedData.DynamicVelocity = Vector3.zero;
            _kcc.RenderData.DynamicVelocity = Vector3.zero;

            if (HasInputAuthority && Context != null)
            {
                Context.LocalPlayerCharacter = this;
                Context.ObservedPlayerCharacter = this;
                Context.ObservedPlayerRef = Object.InputAuthority;

                // Explicitly tell the camera to follow this character's transform.
                // This is more reliable than relying solely on ObservedPlayerCharacter
                // which can be overwritten by PlayerEntity.FixedUpdateNetwork before
                // the networked ActivePlayerCharacter property replicates.
                if (Context.Camera != null)
                {
                    Context.Camera.SetCameraFollow(transform);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out GameInput input))
            {
                // Convert 2D input (WASD / stick) to a 3D direction on the XZ plane.
                // MoveDirection is a Vector2: X = left/right, Y = forward/back.
                // Mapped to world XZ: Vector2.X → World X, Vector2.Y → World Z.
                // Y is explicitly zeroed to prevent any off-axis movement.
                Vector3 moveDirection = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
                _kcc.SetInputDirection(moveDirection);
            }
        }
    }
}
