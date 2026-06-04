using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Melee attack: during active frames, opens a rectangular hitbox in front of the attacker
    /// (mirrored by facing direction) and damages any <see cref="Hurtbox"/> caught inside.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Combat/Attacks/Melee", fileName = "MeleeAttack")]
    public class MeleeAttackData : AttackData
    {
        [Header("Hitbox geometry (local to attacker; X mirrors with facing)")]
        [Tooltip("Center of the hitbox relative to the attacker's transform. " +
                 "X is positive forward — the runtime flips it by facing.")]
        public Vector2 HitboxOffset = new Vector2(1.0f, 0f);

        [Tooltip("Size (width, height) of the hitbox in units.")]
        public Vector2 HitboxSize = new Vector2(1.5f, 1.0f);

        [Header("Targeting")]
        [Tooltip("Layer mask the hitbox checks against. Should match the EnemyHurtbox layer.")]
        public LayerMask HurtboxLayer;

        [Header("Slash visual (optional)")]
        [Tooltip("Sprite shown for a brief moment when the active window starts. Leave null for a runtime " +
                 "placeholder rectangle; assign your own art when ready.")]
        public Sprite SlashSprite;

        [Tooltip("Position of the slash visual relative to the attacker. X mirrors with facing direction.")]
        public Vector2 SlashOffset = new Vector2(1f, 0f);

        [Tooltip("Scale of the slash visual. X mirrors with facing direction.")]
        public Vector2 SlashScale = new Vector2(1.5f, 0.6f);

        [Tooltip("Rotation of the slash visual in degrees (Z axis). Useful for diagonal-feel swings.")]
        [Range(-180f, 180f)] public float SlashRotation = -20f;

        [Tooltip("Frames the slash stays visible. Usually a touch longer than ActiveFrames so the visual " +
                 "carries through the impact pause.")]
        [Range(1, 30)] public int SlashLifetimeFrames = 7;

        [Tooltip("Linear alpha fade from full to zero over the lifetime.")]
        public bool SlashFadeOut = true;

        [Tooltip("Sorting order on the default sorting layer. Higher = drawn on top.")]
        public int SlashSortingOrder = 5;

        public override AttackRuntime CreateRuntime(AttackController controller) =>
            new MeleeAttackRuntime(this, controller);
    }
}
