namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Stateful runtime instance of a movement skill, bound to one <see cref="MovementController"/>.
    /// Created from a <see cref="MovementSkillData"/> template at controller initialization.
    ///
    /// Runtimes own per-controller state (cooldown timers, coyote/buffer counters, dash phase, ...).
    /// They mutate the controller through its public API (SetVerticalVelocity, SuppressGravity, ...).
    /// </summary>
    public abstract class MovementSkillRuntime
    {
        /// <summary>The controller this skill drives.</summary>
        protected readonly MovementController Controller;

        protected MovementSkillRuntime(MovementController controller)
        {
            Controller = controller;
        }

        /// <summary>
        /// Called each FixedUpdate by the controller, before walk/gravity are applied.
        /// Use for cooldown ticking, coyote-time decay, dash state advancement, per-frame velocity overrides.
        /// </summary>
        public virtual void OnTick(float deltaTime) { }

        /// <summary>The slot's button was pressed this frame.</summary>
        public virtual void OnInputPressed() { }

        /// <summary>The slot's button was released this frame.</summary>
        public virtual void OnInputReleased() { }
    }
}
