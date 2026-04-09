namespace VoidRogues
{
    using Fusion;
    using UnityEngine;

    /// <summary>
    /// Networked player character that reads <see cref="GameInput"/> from
    /// Fusion's input pipeline and drives a <see cref="NetworkCharacterController"/>
    /// on the XZ plane (3D movement with gravity).
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class PlayerCharacter : ContextBehaviour
    {
        // PRIVATE MEMBERS

        private NetworkCharacterController _cc;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            _cc = GetComponent<NetworkCharacterController>();

            if (HasInputAuthority && Context != null)
            {
                Context.LocalPlayerCharacter = this;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out GameInput input))
            {
                // Convert 2D input (WASD / stick) to a 3D direction on the XZ plane.
                // Input X  → World X   (strafe left/right)
                // Input Y  → World Z   (forward/back)
                Vector3 moveDirection = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
                _cc.Move(moveDirection);
            }
            else
            {
                // No input available (proxy / no authority) – still apply gravity.
                _cc.Move(default);
            }
        }
    }
}
