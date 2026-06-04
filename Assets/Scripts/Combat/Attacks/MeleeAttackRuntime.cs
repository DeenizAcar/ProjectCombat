using System.Collections.Generic;
using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Stateful melee attack execution. Advances phase by frame counters, runs hit detection
    /// each active-phase frame via OverlapBox, ensures each target is hit at most once per swing,
    /// and triggers hit-stop on both attacker and receiver on connect.
    ///
    /// Hit-stop note: the AttackController stops calling OnTick for the hit-stop duration, so the
    /// runtime does NOT need its own freeze counter — that would stack and double-freeze.
    /// </summary>
    public class MeleeAttackRuntime : AttackRuntime
    {
        // Shared placeholder sprite reused across all melee attacks that don't have art assigned yet.
        // Generated lazily and cached for the lifetime of the play session.
        static Sprite placeholderSprite;

        readonly MeleeAttackData data;
        readonly HashSet<DamageReceiver> alreadyHit = new();
        readonly Collider2D[] overlapBuffer = new Collider2D[8];

        int totalFrameCounter;

        public MeleeAttackRuntime(MeleeAttackData data, AttackController controller) : base(controller)
        {
            this.data = data;
        }

        public override bool IsFinished => Phase == AttackPhase.Done;
        public override bool LocksMovement => data.LocksMovement;
        public override bool SuppressGravity => data.SuppressGravity;
        public override int PostAttackGraceFrames => data.PostAttackGraceFrames;
        public override bool IsInCancelWindow =>
            data.CancelWindowFrames > 0 &&
            totalFrameCounter > data.TotalFrames - data.CancelWindowFrames &&
            Phase != AttackPhase.Done;

        public override void OnTick()
        {
            totalFrameCounter++;

            // Phase transitions are frame-accurate.
            if (totalFrameCounter == data.StartupFrames + 1)
            {
                Phase = AttackPhase.Active;
                SpawnSlashEffect();
            }
            else if (totalFrameCounter == data.StartupFrames + data.ActiveFrames + 1)
                Phase = AttackPhase.Recovery;

            if (totalFrameCounter > data.TotalFrames)
            {
                Phase = AttackPhase.Done;
                return;
            }

            if (Phase == AttackPhase.Active)
                DetectHits();
        }

        void DetectHits()
        {
            Vector2 center = ComputeHitboxWorldCenter();

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(data.HurtboxLayer);

            int count = Physics2D.OverlapBox(center, data.HitboxSize, 0f, filter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var hurtbox = overlapBuffer[i].GetComponent<Hurtbox>();
                if (hurtbox == null) continue;
                var receiver = hurtbox.Receiver;
                if (receiver == null || alreadyHit.Contains(receiver)) continue;

                alreadyHit.Add(receiver);
                receiver.ApplyHit(data.Damage, ComputeKnockback(), data.HitStopFrames);
                Controller.OnHitLanded(data.HitStopFrames);
            }
        }

        Vector2 ComputeHitboxWorldCenter()
        {
            Vector2 localOffset = data.HitboxOffset;
            localOffset.x *= Controller.FacingDirection;
            return (Vector2)Controller.transform.position + localOffset;
        }

        Vector2 ComputeKnockback()
        {
            Vector2 kb = data.KnockbackVelocity;
            kb.x *= Controller.FacingDirection;
            return kb;
        }

        void SpawnSlashEffect()
        {
            var go = new GameObject("SlashEffect");
            go.transform.SetParent(Controller.transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(
                data.SlashOffset.x * Controller.FacingDirection,
                data.SlashOffset.y,
                0f);
            go.transform.localScale = new Vector3(
                data.SlashScale.x * Controller.FacingDirection,
                data.SlashScale.y,
                1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, data.SlashRotation);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = data.SlashSortingOrder;

            var effect = go.AddComponent<SlashEffect>();
            effect.Initialize(
                data.SlashSprite != null ? data.SlashSprite : GetPlaceholderSprite(),
                data.SlashLifetimeFrames,
                data.SlashFadeOut);
        }

        static Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite != null) return placeholderSprite;

            // Thin white rectangle, 64x16 px @ 64 PPU => 1 unit wide, 0.25 unit tall in world space.
            const int width = 64;
            const int height = 16;
            var tex = new Texture2D(width, height) { name = "SlashPlaceholderTexture" };
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            placeholderSprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 64f);
            placeholderSprite.name = "SlashPlaceholder";
            return placeholderSprite;
        }
    }
}
