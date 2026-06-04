using System.Collections.Generic;
using ProjectCombat.Combat.Attacks;
using ProjectCombat.Combat.Movement.Skills;
using UnityEngine;

namespace ProjectCombat.Combat.Movement
{
    /// <summary>
    /// Universal movement layer for the player character.
    ///
    /// Responsibilities:
    /// - Apply horizontal walk input with style-tuned acceleration.
    /// - Apply asymmetric gravity (faster fall than rise).
    /// - Drive movement-skill runtimes (Jump, Dash, ...) by ticking them and forwarding input events.
    /// - Resolve collisions via Cast-based collide-and-slide on a Kinematic Rigidbody2D.
    /// - Maintain ground state and facing direction.
    /// - Expose a small velocity-modification API for skills to drive non-walk motion.
    ///
    /// Not its job: combat hitboxes, animation, per-skill behavior, style swapping logic.
    /// (Style swap will arrive in a later milestone — for now the active style is set once at Awake.)
    ///
    /// This file is the most load-bearing in the movement system; changes here ripple through every skill.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class MovementController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Currently active style. For Milestone 1 this is set once at Awake; later a style-swap system will drive it.")]
        [SerializeField] StyleProfile activeStyle;

        [Tooltip("Input source. Drag a PlayerInputHandler from this GameObject (or a parent).")]
        [SerializeField] PlayerInputHandler inputHandler;

        [Tooltip("Optional. When assigned, movement skill input and walk input are gated while an attack is in progress. " +
                 "Leave null on objects that don't attack.")]
        [SerializeField] AttackController attackController;

        Rigidbody2D rb;
        CapsuleCollider2D body;

        // Active-style cache.
        MovementProfile profile;
        readonly Dictionary<InputSlot, MovementSkillRuntime> skillsBySlot = new();

        // Persistent physics state.
        Vector2 velocity;
        Vector2 moveInput;
        bool isGrounded;
        int facingDirection = 1;

        // Per-frame skill-driven modifiers (cleared at the start of each FixedUpdate).
        float gravityMultiplierThisFrame = 1f;
        bool suppressGravityThisFrame;
        bool suppressWalkInputThisFrame;

        // Cross-system: set by AttackController on hit landing; freezes the entire movement update for N frames.
        int hitStopFramesRemaining;

        // Per-frame phase-through layers, pushed by movement skills (dash, roll) and applied to collision
        // before ResolveMotion / UpdateGroundedState. Cleared at the top of each FixedUpdate.
        int phaseThroughLayersThisFrame;

        // Cast buffers — sized for the worst realistic case to avoid runtime allocation.
        readonly RaycastHit2D[] castBuffer = new RaycastHit2D[8];
        ContactFilter2D contactFilter;

        // ---------- Public read API ----------

        public Vector2 Velocity => velocity;
        public Vector2 MoveInput => moveInput;
        public bool IsGrounded => isGrounded;
        public int FacingDirection => facingDirection;
        public StyleProfile ActiveStyle => activeStyle;

        // ---------- Public skill-driven API ----------

        public void SetVelocity(Vector2 v) => velocity = v;
        public void SetHorizontalVelocity(float vx) => velocity.x = vx;
        public void SetVerticalVelocity(float vy) => velocity.y = vy;

        /// <summary>Multiply gravity this frame by the given factor (resets to 1 next frame).</summary>
        public void SetGravityMultiplierThisFrame(float mult) => gravityMultiplierThisFrame = mult;

        /// <summary>Skip gravity application this frame (used by dash, future grapple, ...).</summary>
        public void SuppressGravityThisFrame() => suppressGravityThisFrame = true;

        /// <summary>Skip walk-input → velocity application this frame (used by dash to lock velocity).</summary>
        public void SuppressWalkInputThisFrame() => suppressWalkInputThisFrame = true;

        /// <summary>
        /// Exclude the given layers from the player's collision detection this frame. Skills push this
        /// during their active windows to phase through specific layers (e.g. dash through enemies).
        /// Bits accumulate via OR — multiple skills can phase different layers simultaneously.
        /// </summary>
        public void SetPhaseThroughLayersThisFrame(LayerMask mask)
            => phaseThroughLayersThisFrame |= mask.value;

        /// <summary>
        /// Freeze the entire movement update for N frames. Called by the AttackController when a hit lands.
        /// Stacks via Max: a later, longer hit-stop extends but does not shorten.
        /// </summary>
        public void BeginHitStop(int frames) => hitStopFramesRemaining = Mathf.Max(hitStopFramesRemaining, frames);

        // ---------- Unity lifecycle ----------

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<CapsuleCollider2D>();

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;  // We own gravity in this controller, not the physics engine.
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.simulated = true;

            LoadStyle(activeStyle);
        }

        void OnEnable()
        {
            if (inputHandler == null) return;
            inputHandler.MoveChanged   += HandleMove;
            inputHandler.Slot1Pressed  += HandleSlot1Press;
            inputHandler.Slot1Released += HandleSlot1Release;
            inputHandler.Slot2Pressed  += HandleSlot2Press;
            inputHandler.Slot2Released += HandleSlot2Release;
            inputHandler.Slot3Pressed  += HandleSlot3Press;
            inputHandler.Slot3Released += HandleSlot3Release;
        }

        void OnDisable()
        {
            if (inputHandler == null) return;
            inputHandler.MoveChanged   -= HandleMove;
            inputHandler.Slot1Pressed  -= HandleSlot1Press;
            inputHandler.Slot1Released -= HandleSlot1Release;
            inputHandler.Slot2Pressed  -= HandleSlot2Press;
            inputHandler.Slot2Released -= HandleSlot2Release;
            inputHandler.Slot3Pressed  -= HandleSlot3Press;
            inputHandler.Slot3Released -= HandleSlot3Release;
        }

        void FixedUpdate()
        {
            if (profile == null) return;

            // Hit-stop: freeze the whole update for N frames after an attack lands.
            if (hitStopFramesRemaining > 0)
            {
                hitStopFramesRemaining--;
                return;
            }

            float dt = Time.fixedDeltaTime;

            // Clear per-frame modifiers — skills will repopulate them in OnTick.
            gravityMultiplierThisFrame = 1f;
            suppressGravityThisFrame = false;
            suppressWalkInputThisFrame = false;
            phaseThroughLayersThisFrame = 0;

            bool attackLocked = attackController != null && attackController.MovementIsLocked;

            // Movement skills (jump/dash) don't tick while committed to an attack — they can't accumulate
            // coyote/cooldown state mid-swing. Cancel windows in M3+ will refine this gate per-phase.
            if (!attackLocked)
            {
                foreach (var skill in skillsBySlot.Values)
                    skill?.OnTick(dt);
            }

            if (!suppressWalkInputThisFrame && !attackLocked)
                ApplyWalkInput(dt);

            bool gravitySuppressed = suppressGravityThisFrame
                || (attackController != null && attackController.SuppressesGravity);
            if (!gravitySuppressed)
                ApplyGravity(dt);

            // Apply this frame's phase-through layers to the contact filter — both ResolveMotion's
            // axis casts AND UpdateGroundedState's down-probe respect the masked-out layers.
            int effectiveMask = profile.CollisionMask & ~phaseThroughLayersThisFrame;
            contactFilter.SetLayerMask(effectiveMask);

            ResolveMotion(velocity * dt);

            UpdateGroundedState();
            UpdateFacing();
        }

        // ---------- Style loading ----------

        /// <summary>
        /// Load (or swap to) a style at runtime. Clears any active movement-skill state and re-instantiates
        /// the new style's skills. Safe to call mid-frame from input callbacks — collection-modification is
        /// safe because skill iteration happens only in FixedUpdate, which won't interleave with input callbacks.
        /// </summary>
        public void LoadStyle(StyleProfile style)
        {
            if (style == null)
            {
                Debug.LogError($"{nameof(MovementController)}: no StyleProfile assigned on '{name}'. Movement will not run.", this);
                return;
            }
            if (style.MovementProfile == null)
            {
                Debug.LogError($"{nameof(MovementController)}: StyleProfile '{style.name}' has no MovementProfile assigned.", this);
                return;
            }

            activeStyle = style;
            profile = style.MovementProfile;

            skillsBySlot.Clear();
            TryLoadSkill(style.Slot1Skill, InputSlot.Slot1);
            TryLoadSkill(style.Slot2Skill, InputSlot.Slot2);
            TryLoadSkill(style.Slot3Skill, InputSlot.Slot3);

            contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(profile.CollisionMask);
        }

        void TryLoadSkill(MovementSkillData data, InputSlot slot)
        {
            if (data != null)
                skillsBySlot[slot] = data.CreateRuntime(this);
        }

        // ---------- Motion application ----------

        void ApplyWalkInput(float dt)
        {
            float targetVx = moveInput.x * profile.WalkSpeed;
            float airMult = isGrounded ? 1f : Mathf.Max(profile.AirControlMultiplier, 0.0001f);

            // Pick accel vs decel: accelerating only when input agrees with current velocity AND speeds it up.
            bool sameDir = targetVx != 0f && Mathf.Sign(targetVx) == Mathf.Sign(velocity.x);
            bool speedingUp = sameDir && Mathf.Abs(targetVx) > Mathf.Abs(velocity.x);
            bool startingFromRest = Mathf.Approximately(velocity.x, 0f) && targetVx != 0f;

            float timeToReach = (speedingUp || startingFromRest)
                ? profile.GroundAccelTime
                : profile.GroundDecelTime;

            // Lower air-control multiplier ⇒ longer time-to-reach ⇒ less responsive in air.
            timeToReach = Mathf.Max(timeToReach / airMult, 0.0001f);

            float maxStep = profile.WalkSpeed * (dt / timeToReach);
            velocity.x = Mathf.MoveTowards(velocity.x, targetVx, maxStep);
        }

        void ApplyGravity(float dt)
        {
            float g = profile.Gravity * gravityMultiplierThisFrame;
            if (velocity.y < 0f)
                g *= profile.FallGravityMultiplier;

            velocity.y -= g * dt;

            if (velocity.y < -profile.MaxFallSpeed)
                velocity.y = -profile.MaxFallSpeed;
        }

        // ---------- Collision resolution ----------

        void ResolveMotion(Vector2 motion)
        {
            if (motion.x != 0f) ResolveAxis(new Vector2(Mathf.Sign(motion.x), 0f), Mathf.Abs(motion.x), isHorizontal: true);
            if (motion.y != 0f) ResolveAxis(new Vector2(0f, Mathf.Sign(motion.y)), Mathf.Abs(motion.y), isHorizontal: false);
        }

        /// <summary>
        /// Cast in `dir` by `distance + skinWidth`, move up to the first contact (minus skin width),
        /// and zero the corresponding velocity component on block. Vertical-up blocks try edge correction.
        /// </summary>
        void ResolveAxis(Vector2 dir, float distance, bool isHorizontal)
        {
            float castDistance = distance + profile.SkinWidth;
            int hits = rb.Cast(dir, contactFilter, castBuffer, castDistance);

            float allowed = distance;
            bool blocked = false;
            for (int i = 0; i < hits; i++)
            {
                float surfaceAllowed = Mathf.Max(0f, castBuffer[i].distance - profile.SkinWidth);
                if (surfaceAllowed < allowed)
                {
                    allowed = surfaceAllowed;
                    blocked = true;
                }
            }

            rb.position += dir * allowed;

            if (!blocked) return;

            // Vertical-up block: try ledge-corner edge correction before killing upward velocity.
            if (!isHorizontal && dir.y > 0f && profile.EdgeCorrectionMaxNudge > 0f && TryEdgeCorrection())
                return;

            if (isHorizontal) velocity.x = 0f;
            else velocity.y = 0f;
        }

        /// <summary>
        /// Attempt to clear a ceiling head-bonk by nudging the body sideways up to EdgeCorrectionMaxNudge.
        /// Sweeps left and right in small steps; takes the first clear offset.
        /// </summary>
        bool TryEdgeCorrection()
        {
            const float step = 0.02f;
            for (float offset = step; offset <= profile.EdgeCorrectionMaxNudge + 0.0001f; offset += step)
            {
                if (TryNudge(Vector2.right * offset)) return true;
                if (TryNudge(Vector2.left  * offset)) return true;
            }
            return false;
        }

        bool TryNudge(Vector2 delta)
        {
            Vector2 candidateCenter = (Vector2)body.bounds.center + delta;
            Collider2D overlap = Physics2D.OverlapCapsule(
                candidateCenter,
                body.size,
                body.direction,
                0f,
                profile.CollisionMask);
            if (overlap != null) return false;

            rb.position += delta;
            return true;
        }

        // ---------- State updates ----------

        void UpdateGroundedState()
        {
            int hits = rb.Cast(Vector2.down, contactFilter, castBuffer, profile.GroundProbeDistance);
            bool grounded = false;
            for (int i = 0; i < hits; i++)
            {
                float angle = Vector2.Angle(castBuffer[i].normal, Vector2.up);
                if (angle <= profile.MaxGroundAngle)
                {
                    grounded = true;
                    break;
                }
            }
            isGrounded = grounded;
        }

        void UpdateFacing()
        {
            if (Mathf.Abs(moveInput.x) > 0.1f)
                facingDirection = moveInput.x > 0f ? 1 : -1;
        }

        // ---------- Input dispatch ----------

        void HandleMove(Vector2 value) => moveInput = value;

        void HandleSlot1Press()   => DispatchPress(InputSlot.Slot1);
        void HandleSlot1Release() => DispatchRelease(InputSlot.Slot1);
        void HandleSlot2Press()   => DispatchPress(InputSlot.Slot2);
        void HandleSlot2Release() => DispatchRelease(InputSlot.Slot2);
        void HandleSlot3Press()   => DispatchPress(InputSlot.Slot3);
        void HandleSlot3Release() => DispatchRelease(InputSlot.Slot3);

        void DispatchPress(InputSlot slot)
        {
            if (attackController != null && attackController.MovementIsLocked) return;
            if (skillsBySlot.TryGetValue(slot, out var skill))
                skill.OnInputPressed();
        }

        void DispatchRelease(InputSlot slot)
        {
            if (attackController != null && attackController.MovementIsLocked) return;
            if (skillsBySlot.TryGetValue(slot, out var skill))
                skill.OnInputReleased();
        }
    }
}
