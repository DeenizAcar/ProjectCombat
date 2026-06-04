using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Jump movement skill configuration. Fixed-height jump with short apex hang, coyote time, jump buffer.
    /// Asymmetric gravity is configured on <see cref="MovementProfile"/> (it's a controller-wide physics property,
    /// not a per-skill one). Variable-height-on-release is intentionally absent — design decision (2026-06):
    /// jump is style-scoped already, committed feel preferred over release-cut nuance.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Movement/Skills/Jump", fileName = "JumpSkill")]
    public class JumpSkillData : MovementSkillData
    {
        [Header("Core arc")]
        [Tooltip("Vertical velocity applied when jump initiates. Single, fixed launch impulse.")]
        [Range(0f, 30f)] public float InitialJumpVelocity = 17f;

        [Header("Apex hang")]
        [Tooltip("When |vertical velocity| is below this threshold during the jump, apex gravity multiplier kicks in. " +
                 "Creates a brief float near the peak to ease aerial maneuvering.")]
        [Range(0f, 10f)] public float ApexVelocityThreshold = 3f;

        [Tooltip("Gravity multiplier during apex hang. 0.4 = 40% of normal gravity at the peak.")]
        [Range(0f, 1f)] public float ApexGravityMultiplier = 0.4f;

        [Header("Forgiveness windows")]
        [Tooltip("Frames after leaving a ledge during which jump is still allowed. ~6 frames at 60 FPS = 0.1s.")]
        [Range(0, 30)] public int CoyoteFrames = 6;

        [Tooltip("Frames before landing during which a jump press is buffered and fires on touchdown.")]
        [Range(0, 30)] public int BufferFrames = 6;

        public override MovementSkillRuntime CreateRuntime(MovementController controller) =>
            new JumpSkillRuntime(this, controller);
    }
}
