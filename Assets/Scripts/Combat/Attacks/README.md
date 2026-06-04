# Attack System

Player offense for ProjectCombat. Mirrors the Movement system's two-layer pattern:

- **Data layer:** `AttackData` (abstract SO) + concrete subclasses like `MeleeAttackData`. Pure tuning/config.
- **Runtime layer:** `AttackRuntime` (abstract) + concrete subclasses like `MeleeAttackRuntime`. Stateful execution of one attack instance.
- **Orchestrator:** `AttackController` (MonoBehaviour on player). Receives input, owns the currently-active runtime, drives its lifecycle.

## Frame-segmented attacks

Every attack has three phases, frame-accurate in `FixedUpdate` (60 FPS):

1. **Startup** — windup; no hitboxes active; movement is locked.
2. **Active** — hitbox(es) active; hit detection runs each frame; hit-stop fires on connect.
3. **Recovery** — wind-down; no hitboxes; movement still locked (cancel windows for combos arrive in M3+).

Frame counts and damage live on the `AttackData` SO. This frame-segmentation is the foundation for the cancel-window framework arriving in M3.

## Damage delivery abstraction

`AttackData` is abstract because future styles will use non-melee delivery (e.g. projectile, AoE field). The concrete subclass declares HOW damage is delivered:

- `MeleeAttackData` → activates a hitbox in front of the attacker during active frames; uses `Physics2D.OverlapBox` against the `HurtboxLayer`.
- (future) `ProjectileAttackData` → spawns a projectile prefab during active frames.

The contract is: during Active, the runtime is responsible for finding `Hurtbox` components in its delivery zone and calling `Receiver.ApplyHit(...)`.

## Hit-stop

When an attack connects:

1. The runtime calls `DamageReceiver.ApplyHit(damage, knockback, hitStopFrames)`. The receiver applies damage, knockback, flash, and freezes its own update for `hitStopFrames`.
2. The runtime calls `AttackController.BeginHitStop(hitStopFrames)`. The controller freezes attack ticking AND tells `MovementController.BeginHitStop(hitStopFrames)`.

Net effect: for N frames, the player can't move/swing, the dummy can't recoil/flash — visually the world pauses on impact. Camera, UI, and other systems keep running. `Time.timeScale` is untouched.

## Movement lock during attack

While an attack is active:

- `AttackController.IsAttacking` is true.
- `MovementController` checks this each frame and skips walk-input application AND skill input dispatch. This produces Souls-like commit feel: you can't dodge or jump mid-swing. Cancel windows (M3+) will allow specific overrides.

## Adding a new attack

1. Subclass `AttackData` if the delivery method is new (e.g. `ProjectileAttackData`). Otherwise reuse `MeleeAttackData`.
2. Subclass `AttackRuntime` to implement the delivery (if data type is new).
3. Author the SO asset, tune frame counts/damage/hit-stop/hitbox geometry in the Inspector.
4. Slot the asset into a `StyleProfile` field (e.g. `LightAttack`, future `LaunchAttack`, `DownStrike`, `UpStrike`).
5. Add an `InputActionReference` to `PlayerInputHandler` for the input that triggers it, and an event wiring in `AttackController`.

For M2: only one slot (`LightAttack` on Square) is wired. Future inputs (Square hold, R1, L1) are tracked in project memory but not yet built.

## Touching this code

- Tuning values (frame counts, damage, hitbox size) → edit the `MeleeAttackData` asset in Inspector. No code change.
- Adding new attack types → see "Adding a new attack".
- Hit-stop mechanics → `AttackController`, `MovementController`, `DamageReceiver` each implement their own `BeginHitStop`. Coordination point is `MeleeAttackRuntime.OnHitLanded`.
- Movement-lock rule → `MovementController` reads `attackController.IsAttacking`. Cancel windows (M3) will replace this hard lock with phase-aware checks.

## Assembly boundary

Lives in `ProjectCombat.Combat` assembly alongside Movement. Hurtbox/DamageReceiver components are also Combat — enemy-specific behavior (AI, dialogue) will live in a separate assembly that depends on Combat for the hit-receiving contract.
