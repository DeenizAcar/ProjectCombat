# Milestone 1 — Setup Guide

This walks through wiring up a test scene with shinobi movement (walk + jump + dash) from scratch. Estimated time: 10-15 minutes the first time.

## 1. Create the input actions asset

1. In the Project window, right-click in `Assets/` and choose **Create → Input Actions**. Name it `PlayerInput`.
2. Double-click to open the Input Actions editor.
3. Under **Action Maps**, add a map called `Player` (or use the existing default and rename).
4. Under **Actions**, define exactly these four:

   | Name  | Action Type | Control Type | Bindings (examples)                                            |
   |-------|-------------|--------------|----------------------------------------------------------------|
   | Move  | Value       | Vector2      | Left Stick (gamepad) + WASD composite (keyboard)               |
   | Slot1 | Button      | Button       | Buttons → South (`<Gamepad>/buttonSouth`) + Space (keyboard)   |
   | Slot2 | Button      | Button       | Buttons → East (`<Gamepad>/buttonEast`) + Left Shift (keyboard) |
   | Slot3 | Button      | Button       | Buttons → North (`<Gamepad>/buttonNorth`) + E (keyboard)        |

5. Click **Save Asset**.

> The PS controller mapping is: South = X, East = O, North = Triangle. Unity's `<Gamepad>` paths use these directional names rather than vendor-specific button names, so the mapping is automatic.

## 2. Create the ScriptableObject configuration

In `Assets/` (e.g. inside a new `Assets/Data/Shinobi/` folder for tidiness), create:

1. **MovementProfile** → right-click → `Create → ProjectCombat → Movement → Movement Profile`. Name it `ShinobiMovementProfile`.
2. **JumpSkill** → `Create → ProjectCombat → Movement → Skills → Jump`. Name it `ShinobiJump`.
3. **DashSkill** → `Create → ProjectCombat → Movement → Skills → Dash`. Name it `ShinobiDash`.
4. **StyleProfile** → `Create → ProjectCombat → Combat → Style Profile`. Name it `ShinobiStyle`.
   - Set `StyleName` = "Shinobi".
   - Drag `ShinobiMovementProfile` into `Movement Profile`.
   - Drag `ShinobiJump` into `Slot1Skill`.
   - Drag `ShinobiDash` into `Slot2Skill`.
   - Leave `Slot3Skill` empty.

## 3. Set up layers

1. **Edit → Project Settings → Tags and Layers**. Add a layer called `Ground` (e.g. layer index 6) and another called `Player` (e.g. layer index 7).
2. Open `ShinobiMovementProfile`. Set `Collision Mask` to **only** `Ground` (uncheck everything else). The mask must NOT include the `Player` layer.

## 4. Build the test scene

1. **File → New Scene → Basic (2D)**. Save it as `Assets/Scenes/MovementTest.unity`.
2. Create the player:
   - GameObject → 2D Object → Sprites → Square. Rename to `Player`. Set its layer to `Player`.
   - Add component: **Capsule Collider 2D**. Default size is fine (~1 × 1).
   - Add component: **Rigidbody 2D**. (The controller will overwrite settings on Awake — body type will become Kinematic.)
   - Add component: **MovementController**.
   - Add component: **PlayerInputHandler**.
3. Wire the player:
   - On `PlayerInputHandler`, expand each Action Reference field. Pick the matching action from the `PlayerInput` asset (`Player/Move`, `Player/Slot1`, `Player/Slot2`, `Player/Slot3`).
   - On `MovementController`, drag `ShinobiStyle` into `Active Style`. Drag the same Player GameObject's `PlayerInputHandler` into `Input Handler`.
4. Build ground and platforms:
   - Create several 2D Squares to act as ground/platforms. Set each one's layer to `Ground`. Add a `Box Collider 2D` if it doesn't already have one. Scale and position to form a small test arena (a wide floor, a couple of platforms at varying heights, one platform with a ledge overhang for edge-correction testing).
5. Make sure the camera covers the play area (the default camera is fine if you scale things appropriately).

## 5. Run

Press **Play**. You should be able to:

- Walk left/right (analog stick or A/D).
- Jump (X button or Space) — hold for full height, release early to cut short.
- Dash on the ground (O button or Left Shift) — short horizontal burst with cooldown.
- Jump-buffer: pressing jump just before landing fires on touchdown.
- Coyote-jump: jumping a few frames after walking off a ledge still works.

## 6. Tune

All numbers live in the four ScriptableObject assets — change them at runtime (Play mode) and feel the difference immediately. Suggested starting points are pre-populated; iterate until the shinobi feels "kelebek gibi uçuyor".

If anything feels wrong or you hit an error, paste the message back and I'll debug.

## Known caveats / future work

- No combat yet — attack/parry come next milestone after shinobi movement is signed off.
- No animation hooks yet — the player is a static sprite. Animation state will be a separate layer that subscribes to events from the controller (TBD).
- Edge correction sweeps left then right at each offset step — for asymmetric ledges this is fine. If asymmetric feel emerges, we'll bias toward the direction of horizontal velocity.
- Dash i-frames are tracked-only; no hit system is consuming them yet.
