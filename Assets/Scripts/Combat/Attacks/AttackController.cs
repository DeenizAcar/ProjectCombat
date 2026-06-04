using ProjectCombat.Combat.Movement;
using UnityEngine;

// AttackController and MovementController have a bidirectional wiring (attack reads style + facing from movement;
// movement reads IsAttacking / MovementIsLocked / SuppressesGravity from attack via pull-based properties).
// Both references are SerializeField, both populated by the setup tool.

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Player-side attack orchestrator. Hosts the currently-active <see cref="AttackRuntime"/>,
    /// owns the combo state (which chain step we're on), translates input into combo advancement,
    /// and coordinates hit-stop with the movement layer on connect.
    ///
    /// Combo model:
    /// - Combo state lives HERE, not on the attack data — so it survives style swaps.
    /// - Pressing Square fires the next step from the CURRENT style's LightCombo list.
    /// - Advancement is allowed inside the active attack's cancel window OR during the post-attack grace window.
    /// - Reaching the end of the chain wraps back to step 0 (Hollow Knight-style loop, timing-gated).
    /// - Grace window expiration without input resets combo step to -1 — next press is a fresh start.
    /// </summary>
    public class AttackController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] PlayerInputHandler inputHandler;
        [SerializeField] MovementController movementController;

        AttackRuntime currentAttack;
        int hitStopFramesRemaining;

        // Combo state. -1 = no active combo; 0..n-1 = currently on chain step N.
        int currentComboStep = -1;
        int postAttackGraceFramesRemaining;

        StyleProfile ActiveStyle => movementController != null ? movementController.ActiveStyle : null;

        /// <summary>Mirror of <see cref="MovementController.FacingDirection"/>; -1 or +1.</summary>
        public int FacingDirection => movementController != null ? movementController.FacingDirection : 1;

        /// <summary>True while an attack runtime is active (Startup / Active / Recovery).</summary>
        public bool IsAttacking => currentAttack != null && !currentAttack.IsFinished;

        /// <summary>True only while the active attack wants to lock the player's movement (per-attack flag).
        /// MovementController reads this to gate walk + skill input.</summary>
        public bool MovementIsLocked => IsAttacking && currentAttack.LocksMovement;

        /// <summary>True while the active attack suppresses gravity (per-attack flag). MovementController
        /// reads this each frame — pull-based to avoid racing the per-frame flag reset.</summary>
        public bool SuppressesGravity => IsAttacking && currentAttack.SuppressGravity;

        void OnEnable()
        {
            if (inputHandler == null) return;
            inputHandler.AttackLightPressed += HandleAttackLightPressed;
        }

        void OnDisable()
        {
            if (inputHandler == null) return;
            inputHandler.AttackLightPressed -= HandleAttackLightPressed;
        }

        void FixedUpdate()
        {
            if (hitStopFramesRemaining > 0)
            {
                hitStopFramesRemaining--;
                return;
            }

            if (currentAttack != null)
            {
                currentAttack.OnTick();
                if (currentAttack.IsFinished)
                {
                    // Start the post-attack grace window using the attack's own configured length.
                    postAttackGraceFramesRemaining = currentAttack.PostAttackGraceFrames;
                    currentAttack = null;
                }
            }
            else if (postAttackGraceFramesRemaining > 0)
            {
                postAttackGraceFramesRemaining--;
                if (postAttackGraceFramesRemaining == 0)
                    currentComboStep = -1;  // grace expired — next press is fresh
            }
        }

        void HandleAttackLightPressed()
        {
            var combo = ActiveStyle?.LightCombo;
            if (combo == null || combo.Count == 0) return;

            int nextStep;

            if (currentAttack != null && currentAttack.IsInCancelWindow)
            {
                // Cancel into next step — wraps at end of chain.
                nextStep = (currentComboStep + 1) % combo.Count;
            }
            else if (currentAttack != null)
            {
                // Mid-attack, outside cancel window — ignore.
                return;
            }
            else if (postAttackGraceFramesRemaining > 0)
            {
                // Inside post-attack grace — continue the chain.
                nextStep = (currentComboStep + 1) % combo.Count;
            }
            else
            {
                // Fresh start.
                nextStep = 0;
            }

            StartAttack(combo, nextStep);
        }

        void StartAttack(System.Collections.Generic.List<AttackData> combo, int step)
        {
            if (step < 0 || step >= combo.Count) return;
            var data = combo[step];
            if (data == null) return;

            if (movementController != null)
            {
                // Commit-style attacks halt momentum; snappy attacks let the attacker carry through.
                if (data.LocksMovement) movementController.SetHorizontalVelocity(0f);
                // Aerial-hang attacks zero vertical ONLY if falling — preserves jump arc when jumping then attacking.
                if (data.SuppressGravity && movementController.Velocity.y < 0f)
                    movementController.SetVerticalVelocity(0f);
            }

            currentAttack = data.CreateRuntime(this);
            currentComboStep = step;
            postAttackGraceFramesRemaining = 0;  // new attack supersedes any pending grace window
        }

        /// <summary>
        /// Called by the active runtime when a hit lands. Freezes this controller and the movement
        /// layer for the configured hit-stop window. The receiver freezes itself inside ApplyHit.
        /// </summary>
        public void OnHitLanded(int hitStopFrames)
        {
            hitStopFramesRemaining = Mathf.Max(hitStopFramesRemaining, hitStopFrames);
            if (movementController != null)
                movementController.BeginHitStop(hitStopFrames);
        }
    }
}
