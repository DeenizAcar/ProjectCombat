using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Stateful jump behavior. Tracks coyote time, jump buffer, and applies apex-hang gravity
    /// softening during the jump arc. Edge correction (ledge-corner forgiveness) lives on
    /// <see cref="MovementController"/> because it needs collision-cast access.
    /// </summary>
    public class JumpSkillRuntime : MovementSkillRuntime
    {
        readonly JumpSkillData data;

        int coyoteFramesLeft;
        int bufferFramesLeft;
        bool isInJumpAscent;      // true between jump initiation and the moment we begin falling — gates apex hang
        bool wasGroundedLastTick;

        public JumpSkillRuntime(JumpSkillData data, MovementController controller) : base(controller)
        {
            this.data = data;
        }

        public override void OnTick(float deltaTime)
        {
            // Refresh coyote window while grounded; tick down once airborne.
            if (Controller.IsGrounded)
                coyoteFramesLeft = data.CoyoteFrames;
            else if (coyoteFramesLeft > 0)
                coyoteFramesLeft--;

            // Tick down the buffer window each frame.
            if (bufferFramesLeft > 0)
                bufferFramesLeft--;

            // Buffered jump fires the frame after touchdown.
            if (Controller.IsGrounded && !wasGroundedLastTick && bufferFramesLeft > 0)
                ExecuteJump();

            wasGroundedLastTick = Controller.IsGrounded;

            // Apex hang: while in jump ascent and near the peak, soften gravity.
            if (isInJumpAscent && Mathf.Abs(Controller.Velocity.y) < data.ApexVelocityThreshold)
                Controller.SetGravityMultiplierThisFrame(data.ApexGravityMultiplier);

            // Ascent phase ends once we begin falling.
            if (Controller.Velocity.y <= 0f)
                isInJumpAscent = false;
        }

        public override void OnInputPressed()
        {
            // Always open a buffer window, even mid-air, so the buffered-jump path can fire on landing.
            bufferFramesLeft = data.BufferFrames;

            // Immediate jump if grounded OR within the coyote window.
            if (Controller.IsGrounded || coyoteFramesLeft > 0)
                ExecuteJump();
        }

        void ExecuteJump()
        {
            Controller.SetVerticalVelocity(data.InitialJumpVelocity);
            isInJumpAscent = true;
            coyoteFramesLeft = 0;
            bufferFramesLeft = 0;
        }
    }
}
