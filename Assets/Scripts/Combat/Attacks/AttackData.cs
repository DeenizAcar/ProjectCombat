using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Base class for attack-configuration assets. Pure data; per-controller execution state lives in
    /// the corresponding <see cref="AttackRuntime"/> returned from <see cref="CreateRuntime"/>.
    ///
    /// Frame counts assume 60 FPS FixedUpdate. The three phases (Startup / Active / Recovery) are
    /// the foundation for the cancel-window framework arriving in M3 — for now, the full attack runs
    /// to completion without cancellation.
    ///
    /// To add a new attack delivery method (e.g. projectile), subclass this with delivery-specific
    /// fields and subclass <see cref="AttackRuntime"/> with the matching behavior.
    /// </summary>
    public abstract class AttackData : ScriptableObject
    {
        [Header("Frame timing (60 FPS FixedUpdate)")]
        [Tooltip("Windup frames before hitboxes activate. Higher = heavier-feeling, more committed swing.")]
        [Range(0, 60)] public int StartupFrames = 6;

        [Tooltip("Frames during which hitboxes are active and hit detection runs.")]
        [Range(1, 30)] public int ActiveFrames = 4;

        [Tooltip("Wind-down frames after the active window. Movement is still locked through recovery.")]
        [Range(0, 60)] public int RecoveryFrames = 14;

        [Header("Effect on target")]
        [Tooltip("Damage number applied per hit. Tracked by the DamageReceiver and shown in the HUD.")]
        [Min(0)] public int Damage = 10;

        [Tooltip("Frames the attacker AND the receiver freeze on connect. Core of impact 'weight'. " +
                 "Snappy light (shinobi-like): 2-3. Weighty light: 4-6. Heavy: 8-12. Parries: 12+.")]
        [Range(0, 30)] public int HitStopFrames = 3;

        [Tooltip("Knockback velocity applied to the receiver on hit. X is positive forward — runtime " +
                 "flips it by the attacker's facing direction.")]
        public Vector2 KnockbackVelocity = new Vector2(4f, 1.5f);

        [Header("Effect on attacker")]
        [Tooltip("If true, the attacker's walk and skill input (jump/dash) are gated for the whole attack — " +
                 "committed Souls-like feel. If false, the attacker can keep walking/jumping mid-swing — " +
                 "snappy DMC/HK feel. Shinobi light: false. Tank heavy: true.")]
        public bool LocksMovement = true;

        [Tooltip("If true, gravity is suppressed and vertical velocity is zeroed on attack start — gives aerial " +
                 "swings a clean hang at the spawn height. Useful for snappy aerial attacks and (later) juggle moves.")]
        public bool SuppressGravity = false;

        [Header("Combo")]
        [Tooltip("Last N frames of the attack during which a press of the attack button advances the combo to " +
                 "the next step. Typically lives inside the Recovery phase. 0 = no cancel window (committed swing).")]
        [Range(0, 30)] public int CancelWindowFrames = 4;

        [Tooltip("Frames after the attack fully ends during which the combo can still advance (the 'still-in-combo' " +
                 "grace window). After this expires the combo step resets — next press starts at step 0.")]
        [Range(0, 60)] public int PostAttackGraceFrames = 8;

        /// <summary>Total frames before the attack completes (Startup + Active + Recovery).</summary>
        public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;

        /// <summary>Create the stateful runtime instance for this attack, bound to the controller that drives it.</summary>
        public abstract AttackRuntime CreateRuntime(AttackController controller);
    }
}
