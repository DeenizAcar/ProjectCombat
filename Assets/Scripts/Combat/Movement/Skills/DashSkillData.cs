using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Horizontal dash: fixed-velocity burst in the input direction (or facing if no input),
    /// brief i-frames at startup, cooldown gate. Ground/air availability is per-style via the
    /// <see cref="GroundOnly"/> toggle — shinobi allows air dash, heavier styles likely don't.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Movement/Skills/Dash", fileName = "DashSkill")]
    public class DashSkillData : MovementSkillData
    {
        [Header("Motion")]
        [Tooltip("Horizontal velocity applied during the dash window, in units/second.")]
        [Range(0f, 50f)] public float DashVelocity = 22f;

        [Tooltip("Duration of the dash in frames at 60 FPS. Velocity stays locked through this window; " +
                 "walk input and gravity are suppressed.")]
        [Range(1, 30)] public int DashDurationFrames = 8;

        [Header("Recovery")]
        [Tooltip("Frames after the dash ends before another dash can be triggered.")]
        [Range(0, 120)] public int CooldownFrames = 30;

        [Header("Defense")]
        [Tooltip("Frames at the start of the dash during which the character is invulnerable. " +
                 "Currently tracked-only — the hit/damage system will read this flag later.")]
        [Range(0, 30)] public int InvulnerabilityFrames = 6;

        [Header("Restrictions")]
        [Tooltip("If true, dash can only initiate while grounded. Heavy styles set this true; shinobi-like styles leave it false for air mobility.")]
        public bool GroundOnly = false;

        [Header("Phase-through")]
        [Tooltip("Layers excluded from the player's collision detection during the dash window. " +
                 "Set to EnemyBody for shinobi-style ninja-dash through enemies. Leave 0 for solid dash.")]
        public LayerMask PhaseThroughLayers;

        public override MovementSkillRuntime CreateRuntime(MovementController controller) =>
            new DashSkillRuntime(this, controller);
    }
}
