using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Self-destructing visual played at the start of a melee attack's active window.
    /// Configured by <see cref="MeleeAttackData"/>'s slash-visual fields and spawned from
    /// <see cref="MeleeAttackRuntime"/>.
    ///
    /// To use your own art: assign a Sprite to <see cref="MeleeAttackData.SlashSprite"/>.
    /// All other transform/lifetime values stay data-tunable in the Inspector.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SlashEffect : MonoBehaviour
    {
        SpriteRenderer sr;
        int framesLeft;
        int totalFrames;
        bool fadeOut;
        Color startColor;

        /// <summary>Initialize on spawn. Always destroyed at frame count = 0.</summary>
        public void Initialize(Sprite sprite, int lifetimeFrames, bool fadeOut)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            startColor = sr.color;
            framesLeft = lifetimeFrames;
            totalFrames = Mathf.Max(1, lifetimeFrames);
            this.fadeOut = fadeOut;
        }

        void FixedUpdate()
        {
            framesLeft--;
            if (framesLeft <= 0)
            {
                Destroy(gameObject);
                return;
            }
            if (fadeOut && sr != null)
            {
                var c = startColor;
                c.a = framesLeft / (float)totalFrames;
                sr.color = c;
            }
        }
    }
}
