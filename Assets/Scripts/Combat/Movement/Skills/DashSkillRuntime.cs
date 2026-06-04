using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Stateful dash behavior. During the active window, locks horizontal velocity to the dash burst,
    /// zeroes vertical velocity, and tells the controller to skip gravity and walk-input application.
    /// Exposes <see cref="IsInvulnerable"/> for future damage-system consumption.
    /// </summary>
    public class DashSkillRuntime : MovementSkillRuntime
    {
        readonly DashSkillData data;

        int framesUntilReady;          // cooldown timer (counts down each FixedUpdate)
        int framesRemainingInDash;     // active dash timer
        int framesRemainingInIFrames;  // invulnerability timer
        int dashDirection;             // -1 or +1, captured at dash initiation

        public bool IsActive => framesRemainingInDash > 0;
        public bool IsInvulnerable => framesRemainingInIFrames > 0;

        public DashSkillRuntime(DashSkillData data, MovementController controller) : base(controller)
        {
            this.data = data;
        }

        public override void OnTick(float deltaTime)
        {
            if (framesUntilReady > 0)
                framesUntilReady--;

            if (framesRemainingInIFrames > 0)
                framesRemainingInIFrames--;

            if (framesRemainingInDash > 0)
            {
                Controller.SetHorizontalVelocity(dashDirection * data.DashVelocity);
                Controller.SetVerticalVelocity(0f);
                Controller.SuppressGravityThisFrame();
                Controller.SuppressWalkInputThisFrame();
                if (data.PhaseThroughLayers.value != 0)
                    Controller.SetPhaseThroughLayersThisFrame(data.PhaseThroughLayers);
                framesRemainingInDash--;
            }
        }

        public override void OnInputPressed()
        {
            if (framesUntilReady > 0) return;
            if (data.GroundOnly && !Controller.IsGrounded) return;

            // Direction priority: pressed input > current facing.
            float inputX = Controller.MoveInput.x;
            dashDirection = Mathf.Abs(inputX) > 0.1f
                ? (inputX > 0f ? 1 : -1)
                : Controller.FacingDirection;

            framesRemainingInDash = data.DashDurationFrames;
            framesRemainingInIFrames = data.InvulnerabilityFrames;
            // Cooldown starts after the dash ends, so total lockout = duration + cooldown.
            framesUntilReady = data.DashDurationFrames + data.CooldownFrames;
        }
    }
}
