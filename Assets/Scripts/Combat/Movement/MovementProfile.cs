using UnityEngine;

namespace ProjectCombat.Combat.Movement
{
    /// <summary>
    /// Per-style tuning for the universal movement layer: walking, gravity, air control, collision params.
    /// One asset per style (a tank's profile will walk slower and fall faster than a shinobi's).
    /// Referenced by <see cref="StyleProfile.MovementProfile"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Movement/Movement Profile", fileName = "MovementProfile")]
    public class MovementProfile : ScriptableObject
    {
        [Header("Walking")]
        [Tooltip("Max horizontal walk speed in units/second.")]
        [Min(0f)] public float WalkSpeed = 7f;

        [Tooltip("Seconds to accelerate from 0 to WalkSpeed on the ground. Lower = snappier start.")]
        [Range(0.01f, 1f)] public float GroundAccelTime = 0.08f;

        [Tooltip("Seconds to decelerate from WalkSpeed to 0 on the ground when input is released or reversed.")]
        [Range(0.01f, 1f)] public float GroundDecelTime = 0.05f;

        [Tooltip("Air responsiveness multiplier. 1 = same as ground, 0 = no air control. " +
                 "Lower values increase commit to whatever velocity you jumped with.")]
        [Range(0f, 1f)] public float AirControlMultiplier = 0.65f;

        [Header("Gravity")]
        [Tooltip("Base downward acceleration in units/second^2 while ascending or at apex.")]
        [Min(0f)] public float Gravity = 60f;

        [Tooltip("Multiplier applied to gravity while falling (vy < 0). >1 = faster fall, snappier jump feel.")]
        [Range(1f, 4f)] public float FallGravityMultiplier = 1.8f;

        [Tooltip("Terminal velocity cap (downward speed limit in units/second).")]
        [Min(0f)] public float MaxFallSpeed = 30f;

        [Header("Collision")]
        [Tooltip("Layer mask for solid geometry the controller collides with. Should NOT include the player's own layer.")]
        public LayerMask CollisionMask = ~0;

        [Tooltip("Small gap kept between the body and surfaces to prevent intersection / jitter.")]
        [Range(0.01f, 0.1f)] public float SkinWidth = 0.02f;

        [Tooltip("Distance below the body probed each frame for ground.")]
        [Range(0.01f, 0.3f)] public float GroundProbeDistance = 0.08f;

        [Tooltip("Maximum surface angle (degrees from horizontal) still considered ground.")]
        [Range(0f, 89f)] public float MaxGroundAngle = 50f;

        [Header("Edge correction")]
        [Tooltip("Max lateral nudge (units) applied when the head bonks a ledge corner during upward motion. " +
                 "0 = disabled. Used for jump-ledge forgiveness; harmless when no jump skill is active.")]
        [Range(0f, 0.5f)] public float EdgeCorrectionMaxNudge = 0.18f;
    }
}
