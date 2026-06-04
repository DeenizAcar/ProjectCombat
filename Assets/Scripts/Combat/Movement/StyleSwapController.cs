using UnityEngine;

namespace ProjectCombat.Combat.Movement
{
    /// <summary>
    /// Routes D-pad press input to style swaps on the player's MovementController. Holds the
    /// four-slot style loadout (one per D-pad direction). Optionally tints the player sprite to
    /// the active style's PlayerTint so the swap is visible without per-style art.
    ///
    /// Swap behavior:
    /// - Style swap is allowed AT ANY TIME — even mid-attack or mid-roll.
    /// - The CURRENT attack continues with its original data (already-instantiated runtime).
    /// - The NEXT attack (post-cancel-window or post-grace) reads ActiveStyle.LightCombo[step] of
    ///   the now-active style, so the combo chain "carries over" into the new style.
    /// - Movement-skill state (mid-dash, mid-roll) is reset on swap — LoadStyle re-instantiates the skill kit.
    /// </summary>
    public class StyleSwapController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] PlayerInputHandler inputHandler;
        [SerializeField] MovementController movementController;
        [Tooltip("Optional. SpriteRenderer to tint with the active style's PlayerTint color. Usually the player's own renderer.")]
        [SerializeField] SpriteRenderer playerSprite;

        [Header("Style loadout (D-pad)")]
        [Tooltip("Style activated by D-pad Up.")]
        [SerializeField] StyleProfile style1Up;

        [Tooltip("Style activated by D-pad Right.")]
        [SerializeField] StyleProfile style2Right;

        [Tooltip("Style activated by D-pad Down.")]
        [SerializeField] StyleProfile style3Down;

        [Tooltip("Style activated by D-pad Left.")]
        [SerializeField] StyleProfile style4Left;

        void OnEnable()
        {
            if (inputHandler == null) return;
            inputHandler.Style1Pressed += HandleStyle1;
            inputHandler.Style2Pressed += HandleStyle2;
            inputHandler.Style3Pressed += HandleStyle3;
            inputHandler.Style4Pressed += HandleStyle4;
        }

        void OnDisable()
        {
            if (inputHandler == null) return;
            inputHandler.Style1Pressed -= HandleStyle1;
            inputHandler.Style2Pressed -= HandleStyle2;
            inputHandler.Style3Pressed -= HandleStyle3;
            inputHandler.Style4Pressed -= HandleStyle4;
        }

        void Start()
        {
            // Apply tint of the style the MovementController booted with, so the player sprite
            // matches the active style from frame one.
            ApplyTintToCurrentStyle();
        }

        void HandleStyle1() => Swap(style1Up);
        void HandleStyle2() => Swap(style2Right);
        void HandleStyle3() => Swap(style3Down);
        void HandleStyle4() => Swap(style4Left);

        void Swap(StyleProfile target)
        {
            if (target == null) return;
            if (movementController == null) return;
            if (movementController.ActiveStyle == target) return;

            movementController.LoadStyle(target);
            ApplyTint(target.PlayerTint);
        }

        void ApplyTintToCurrentStyle()
        {
            if (movementController == null || movementController.ActiveStyle == null) return;
            ApplyTint(movementController.ActiveStyle.PlayerTint);
        }

        void ApplyTint(Color color)
        {
            if (playerSprite != null) playerSprite.color = color;
        }
    }
}
