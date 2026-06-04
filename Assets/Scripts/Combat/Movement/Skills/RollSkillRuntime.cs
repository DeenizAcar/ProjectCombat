using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Stateful roll behavior. Forward by default; backward if input direction opposes facing.
    /// Locks horizontal velocity for the duration, exposes invulnerability for the early frames,
    /// suppresses walk input so the player can't fight against the roll.
    /// </summary>
    public class RollSkillRuntime : MovementSkillRuntime
    {
        readonly RollSkillData data;

        int framesUntilReady;           // cooldown timer
        int framesRemainingInRoll;      // active roll timer
        int framesRemainingInIFrames;   // invulnerability timer
        int rollDirection;              // -1 or +1, captured at roll initiation

        public bool IsActive => framesRemainingInRoll > 0;
        public bool IsInvulnerable => framesRemainingInIFrames > 0;

        public RollSkillRuntime(RollSkillData data, MovementController controller) : base(controller)
        {
            this.data = data;
        }

        public override void OnTick(float deltaTime)
        {
            if (framesUntilReady > 0)
                framesUntilReady--;

            if (framesRemainingInIFrames > 0)
                framesRemainingInIFrames--;

            if (framesRemainingInRoll > 0)
            {
                Controller.SetHorizontalVelocity(rollDirection * data.RollVelocity);
                Controller.SuppressWalkInputThisFrame();
                framesRemainingInRoll--;
            }
        }

        public override void OnInputPressed()
        {
            if (framesUntilReady > 0) return;
            if (data.GroundOnly && !Controller.IsGrounded) return;

            // Direction: forward by default. If input opposes facing, roll backward (Souls-style backstep).
            float inputX = Controller.MoveInput.x;
            bool inputOpposesFacing = Mathf.Abs(inputX) > 0.1f
                && Mathf.Sign(inputX) != Mathf.Sign(Controller.FacingDirection);

            rollDirection = inputOpposesFacing ? -Controller.FacingDirection : Controller.FacingDirection;

            framesRemainingInRoll = data.RollDurationFrames;
            framesRemainingInIFrames = data.InvulnerabilityFrames;
            // Cooldown starts after the roll ends, so total lockout = duration + cooldown.
            framesUntilReady = data.RollDurationFrames + data.CooldownFrames;
        }
    }
}
