using System;
using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Receives hits from attacks. Tracks total damage taken, applies knockback through a
    /// Rigidbody2D (so Unity gravity + ground collision handle the arc naturally), flashes the
    /// sprite, and freezes its physics during hit-stop so the reaction syncs with the attacker's
    /// impact pause.
    ///
    /// M2 dummy: infinite HP. Death/respawn arrives in a later milestone.
    ///
    /// Execution order is set below the default so the receiver's frame counters update BEFORE
    /// the AttackController's hit detection runs on the same frame — keeps hit-stop frame-accurate
    /// regardless of Unity's otherwise-undefined Update ordering.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class DamageReceiver : MonoBehaviour
    {
        [Header("Visual feedback")]
        [Tooltip("Sprite that flashes on hit. Usually the receiver's own SpriteRenderer.")]
        [SerializeField] SpriteRenderer visualRenderer;

        [Tooltip("Color the sprite tints toward at peak flash.")]
        [SerializeField] Color flashColor = Color.red;

        [Tooltip("Frames the flash decays over. Independent of hit-stop frames.")]
        [Range(1, 60)] [SerializeField] int flashDurationFrames = 10;

        int totalDamage;
        int hitStopFramesRemaining;
        int flashFramesRemaining;
        Color originalColor;
        Rigidbody2D rb;

        /// <summary>Cumulative damage taken since scene start. Read by the HUD.</summary>
        public int TotalDamage => totalDamage;

        /// <summary>Fires after damage is applied. HUD subscribes; other listeners can hook later.</summary>
        public event Action Damaged;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (visualRenderer != null) originalColor = visualRenderer.color;

            // Defensive physics config — keeps runtime behavior sane even if Inspector wasn't fully set up.
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
            if (rb.mass < 0.01f) rb.mass = 5f;
        }

        /// <summary>
        /// Called by an attack runtime on connect. Applies damage, knockback, flash, and self hit-stop.
        /// </summary>
        public void ApplyHit(int damage, Vector2 knockback, int hitStopFrames)
        {
            totalDamage += damage;

            // Knockback replaces current velocity outright — one hit decides the launch trajectory.
            rb.linearVelocity = knockback;

            flashFramesRemaining = flashDurationFrames;
            if (visualRenderer != null) visualRenderer.color = flashColor;

            hitStopFramesRemaining = hitStopFrames;
            if (hitStopFramesRemaining > 0)
                rb.simulated = false;  // freeze physics for the impact pause

            Damaged?.Invoke();
        }

        void FixedUpdate()
        {
            // Hit-stop: physics frozen via rb.simulated=false; just tick the counter and return.
            if (hitStopFramesRemaining > 0)
            {
                hitStopFramesRemaining--;
                if (hitStopFramesRemaining == 0)
                    rb.simulated = true;
                return;
            }

            // Flash decay — runs after the impact pause ends, easing color back to original.
            if (visualRenderer == null) return;
            if (flashFramesRemaining > 0)
            {
                flashFramesRemaining--;
                float t = flashFramesRemaining / (float)flashDurationFrames;
                visualRenderer.color = Color.Lerp(originalColor, flashColor, t);
            }
            else if (visualRenderer.color != originalColor)
            {
                visualRenderer.color = originalColor;
            }
        }
    }
}
