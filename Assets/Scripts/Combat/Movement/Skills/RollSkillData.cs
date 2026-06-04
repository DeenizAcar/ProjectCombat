using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Souls-like directional roll. Fixed-velocity horizontal burst in the facing direction (or backward
    /// if input opposes facing), with early i-frames for read-the-attack defense, then a cooldown.
    /// Ground-only by default — air roll is intentionally absent for committed-feel styles like Tank.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Movement/Skills/Roll", fileName = "RollSkill")]
    public class RollSkillData : MovementSkillData
    {
        [Header("Motion")]
        [Tooltip("Horizontal velocity applied during the roll window, in units/second.")]
        [Range(0f, 50f)] public float RollVelocity = 14f;

        [Tooltip("Duration of the roll in frames at 60 FPS. Velocity stays locked through this window; " +
                 "walk input is suppressed. Recovery is the part of the duration past the i-frame window.")]
        [Range(1, 60)] public int RollDurationFrames = 18;

        [Header("Recovery")]
        [Tooltip("Frames after the roll ends before another roll can be triggered.")]
        [Range(0, 120)] public int CooldownFrames = 14;

        [Header("Defense")]
        [Tooltip("Frames at the start of the roll during which the character is invulnerable. " +
                 "Currently tracked-only — the hit/damage system reads this flag.")]
        [Range(0, 30)] public int InvulnerabilityFrames = 10;

        [Header("Restrictions")]
        [Tooltip("If true, roll can only initiate while grounded.")]
        public bool GroundOnly = true;

        [Header("Phase-through")]
        [Tooltip("Layers excluded from the player's collision detection during the roll window. " +
                 "Set to EnemyBody for tank shoulder-bash plow-through.")]
        public LayerMask PhaseThroughLayers;

        [Header("Contact damage (shoulder bash)")]
        [Tooltip("If true, the roll opens a hitbox each active frame and damages any hurtbox it overlaps. " +
                 "Tank: true. Shinobi-style evasive rolls: false.")]
        public bool DealsDamage;

        [Tooltip("Damage applied per hit (each receiver hit at most once per roll).")]
        [Min(0)] public int Damage = 15;

        [Tooltip("Hit-stop frames applied to the receiver on contact. The roller itself is NOT hit-stopped — " +
                 "the bash plows through.")]
        [Range(0, 30)] public int HitStopFrames = 4;

        [Tooltip("Knockback velocity applied to the receiver on hit. X is positive forward — runtime " +
                 "flips it by the roll direction so the enemy flies in the bash direction.")]
        public Vector2 KnockbackOnHit = new Vector2(8f, 1f);

        [Tooltip("Size of the contact hitbox, centered on the player. Should roughly match player body footprint.")]
        public Vector2 HitboxSize = new Vector2(1.2f, 1.2f);

        [Tooltip("Local offset of the contact hitbox from the player position. Usually zero.")]
        public Vector2 HitboxOffset = Vector2.zero;

        [Tooltip("Layer mask the contact hitbox checks against. Match the EnemyHurtbox layer (same as attacks).")]
        public LayerMask HurtboxLayer;

        public override MovementSkillRuntime CreateRuntime(MovementController controller) =>
            new RollSkillRuntime(this, controller);
    }
}
