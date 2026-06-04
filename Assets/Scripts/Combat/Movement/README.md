# Movement System

Player movement for ProjectCombat. Built as **two layers** so that styles (shinobi, tank, etc.) share the same physics and walking behavior while owning their own mobility abilities (jump, dash, roll, ...).

## Layers

### 1. Universal layer — `MovementController`

Single MonoBehaviour on the player. Handles:

- Walking (horizontal input → smoothed acceleration, air-control attenuation)
- Gravity application (asymmetric: faster falling than rising)
- Ground detection (downward `Cast` each frame against the collision mask)
- Collide-and-slide collision (Kinematic Rigidbody2D + `Rigidbody2D.Cast`)
- Facing direction tracking
- A small velocity-modification API for skills to drive non-walking motion

Reads tuning from a `MovementProfile` ScriptableObject. The profile is per-style (a tank's walk feels heavier than a shinobi's), but the controller itself is shared.

### 2. Style-scoped layer — `MovementSkill`

Each style declares which mobility abilities it owns. A `StyleProfile` SO holds:

- A reference to the style's `MovementProfile`
- Up to 3 `MovementSkillData` references, one per input slot:
  - `Slot1Skill` → X (PS controller south face button)
  - `Slot2Skill` → O (east face)
  - `Slot3Skill` → Triangle (north face)

At controller initialization, each non-null `MovementSkillData` instantiates a runtime counterpart (`MovementSkillRuntime`) that lives on the controller. **Data SOs are stateless config; runtime classes hold per-controller state** (cooldown timers, coyote frames, etc.). This is the flyweight pattern: SO = template, runtime = instance.

Currently shipped skills:

- `JumpSkill` — variable height, asymmetric gravity, apex hang, coyote time, jump buffer
- `DashSkill` — Souls-like horizontal burst, brief i-frames, cooldown, ground-only by default

## Adding a new movement skill

1. Create a new `MovementSkillData` subclass under `Skills/` (e.g. `WallJumpData.cs`). Add tuning fields. Tag with `[CreateAssetMenu(...)]`.
2. Create the matching `MovementSkillRuntime` subclass. Override `OnTick`, `OnInputPressed`, `OnInputReleased` as needed. Read from your `Data` field; mutate the `Controller` through its public API.
3. Override `MovementSkillData.CreateRuntime(MovementController)` to return your runtime.
4. Create the SO asset in the project and slot it into a `StyleProfile`.

No changes to `MovementController` needed unless the skill requires a brand-new controller capability (e.g. wall detection — that would land in the universal layer).

## Input flow

`PlayerInputHandler` wraps the new Input System. It exposes typed C# events the movement layer listens to: `MoveChanged(Vector2)`, `Slot1Pressed/Released`, `Slot2Pressed/Released`, `Slot3Pressed/Released`. Subscriptions are managed in `OnEnable`/`OnDisable`.

`MovementController` subscribes on enable and dispatches input events to whichever runtime skill is bound to that slot in the active style.

## Touching this code

- Tuning **values** (walk speed, jump velocity, dash distance, etc.) → edit the ScriptableObject asset in the Inspector. No code change.
- Tuning **behavior** (e.g. "wall jump should also restore air dash") → edit the skill's runtime class.
- Tuning **physics primitives** (collision casting, gravity loop) → edit `MovementController`. This is the most load-bearing file. Touch carefully.
- New movement skill type → see "Adding a new movement skill" above.

## Assembly boundary

This code lives in the `ProjectCombat.Combat` assembly. The movement system cannot reference UI, Audio, or Enemy code. Other systems wishing to read combat state should depend on this assembly, not the other way around.

## Scene & asset setup

See `MILESTONE_1_SETUP.md` (sibling file) for step-by-step instructions to wire up a test scene with shinobi movement.
