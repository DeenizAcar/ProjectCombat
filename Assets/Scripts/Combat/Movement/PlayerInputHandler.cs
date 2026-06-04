using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCombat.Combat.Movement
{
    /// <summary>
    /// Translates new-Input-System actions into typed C# events for the movement layer.
    ///
    /// Authoring:
    ///   1. Create an InputActionAsset in the project (Create > Input Actions).
    ///   2. Define an action map containing:
    ///        - "Move"  (Value / Vector2) — bound to left stick + WASD
    ///        - "Slot1" (Button)          — bound to PS X / Keyboard Space
    ///        - "Slot2" (Button)          — bound to PS O / Keyboard Left Shift
    ///        - "Slot3" (Button)          — bound to PS Triangle / Keyboard E
    ///   3. Drag the resulting InputActionReferences into the fields below.
    ///
    /// Subscriptions are managed in OnEnable/OnDisable — instances are safe to re-enable.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Action references")]
        [Tooltip("Vector2 action for movement input (left stick / WASD).")]
        [SerializeField] InputActionReference moveAction;

        [Tooltip("Button action bound to slot 1 (PS controller X / south face button).")]
        [SerializeField] InputActionReference slot1Action;

        [Tooltip("Button action bound to slot 2 (PS controller O / east face button).")]
        [SerializeField] InputActionReference slot2Action;

        [Tooltip("Button action bound to slot 3 (PS controller Triangle / north face button).")]
        [SerializeField] InputActionReference slot3Action;

        [Header("Attack action references")]
        [Tooltip("Button action for the light attack (PS controller Square / west face button).")]
        [SerializeField] InputActionReference attackLightAction;

        [Header("Style swap action references (D-pad)")]
        [Tooltip("Button action for style slot 1 (D-pad Up).")]
        [SerializeField] InputActionReference style1Action;

        [Tooltip("Button action for style slot 2 (D-pad Right).")]
        [SerializeField] InputActionReference style2Action;

        [Tooltip("Button action for style slot 3 (D-pad Down).")]
        [SerializeField] InputActionReference style3Action;

        [Tooltip("Button action for style slot 4 (D-pad Left).")]
        [SerializeField] InputActionReference style4Action;

        public event Action<Vector2> MoveChanged;
        public event Action Slot1Pressed;
        public event Action Slot1Released;
        public event Action Slot2Pressed;
        public event Action Slot2Released;
        public event Action Slot3Pressed;
        public event Action Slot3Released;
        public event Action AttackLightPressed;
        public event Action AttackLightReleased;
        public event Action Style1Pressed;
        public event Action Style2Pressed;
        public event Action Style3Pressed;
        public event Action Style4Pressed;

        void OnEnable()
        {
            BindAction(moveAction, OnMovePerformed, OnMoveCanceled);
            BindAction(slot1Action, OnSlot1Performed, OnSlot1Canceled);
            BindAction(slot2Action, OnSlot2Performed, OnSlot2Canceled);
            BindAction(slot3Action, OnSlot3Performed, OnSlot3Canceled);
            BindAction(attackLightAction, OnAttackLightPerformed, OnAttackLightCanceled);
            BindAction(style1Action, OnStyle1Performed, NoOp);
            BindAction(style2Action, OnStyle2Performed, NoOp);
            BindAction(style3Action, OnStyle3Performed, NoOp);
            BindAction(style4Action, OnStyle4Performed, NoOp);
        }

        void OnDisable()
        {
            UnbindAction(moveAction, OnMovePerformed, OnMoveCanceled);
            UnbindAction(slot1Action, OnSlot1Performed, OnSlot1Canceled);
            UnbindAction(slot2Action, OnSlot2Performed, OnSlot2Canceled);
            UnbindAction(slot3Action, OnSlot3Performed, OnSlot3Canceled);
            UnbindAction(attackLightAction, OnAttackLightPerformed, OnAttackLightCanceled);
            UnbindAction(style1Action, OnStyle1Performed, NoOp);
            UnbindAction(style2Action, OnStyle2Performed, NoOp);
            UnbindAction(style3Action, OnStyle3Performed, NoOp);
            UnbindAction(style4Action, OnStyle4Performed, NoOp);
        }

        static void BindAction(InputActionReference reference,
                               Action<InputAction.CallbackContext> performed,
                               Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed += performed;
            reference.action.canceled += canceled;
            if (!reference.action.enabled) reference.action.Enable();
        }

        static void UnbindAction(InputActionReference reference,
                                 Action<InputAction.CallbackContext> performed,
                                 Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= performed;
            reference.action.canceled -= canceled;
        }

        void OnMovePerformed(InputAction.CallbackContext ctx) => MoveChanged?.Invoke(ctx.ReadValue<Vector2>());
        void OnMoveCanceled(InputAction.CallbackContext _)    => MoveChanged?.Invoke(Vector2.zero);

        void OnSlot1Performed(InputAction.CallbackContext _) => Slot1Pressed?.Invoke();
        void OnSlot1Canceled(InputAction.CallbackContext _)  => Slot1Released?.Invoke();
        void OnSlot2Performed(InputAction.CallbackContext _) => Slot2Pressed?.Invoke();
        void OnSlot2Canceled(InputAction.CallbackContext _)  => Slot2Released?.Invoke();
        void OnSlot3Performed(InputAction.CallbackContext _) => Slot3Pressed?.Invoke();
        void OnSlot3Canceled(InputAction.CallbackContext _)  => Slot3Released?.Invoke();

        void OnAttackLightPerformed(InputAction.CallbackContext _) => AttackLightPressed?.Invoke();
        void OnAttackLightCanceled(InputAction.CallbackContext _)  => AttackLightReleased?.Invoke();

        void OnStyle1Performed(InputAction.CallbackContext _) => Style1Pressed?.Invoke();
        void OnStyle2Performed(InputAction.CallbackContext _) => Style2Pressed?.Invoke();
        void OnStyle3Performed(InputAction.CallbackContext _) => Style3Pressed?.Invoke();
        void OnStyle4Performed(InputAction.CallbackContext _) => Style4Pressed?.Invoke();

        // Style actions don't expose released events; the swap is fire-and-forget on press.
        static void NoOp(InputAction.CallbackContext _) { }
    }
}
