namespace VoidRogues
{
    using Fusion;
    using Fusion.Addons.KCC;
    using UnityEngine;

    /// <summary>
    /// Networked player character that reads <see cref="GameInput"/> from
    /// Fusion's input pipeline and drives a <see cref="KCC"/>
    /// on the XZ plane (3D movement with gravity).
    /// </summary>
    [RequireComponent(typeof(KCC))]
    public class PlayerCharacter : ContextBehaviour
    {
        // PRIVATE MEMBERS

        private KCC _kcc;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            _kcc = GetComponent<KCC>();

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
                // MoveDirection is a Vector2: X = left/right, Y = forward/back.
                // Mapped to world XZ: Vector2.X → World X, Vector2.Y → World Z.
                Vector3 moveDirection = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
                _kcc.SetInputDirection(moveDirection);
            }
        }
    }
}
