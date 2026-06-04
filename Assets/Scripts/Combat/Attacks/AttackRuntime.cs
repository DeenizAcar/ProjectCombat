namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Stateful per-execution instance of an attack. Owns frame counters, phase, hit-tracking,
    /// and (for hit-stop) a self-freeze counter so it stops advancing on impact.
    /// </summary>
    public abstract class AttackRuntime
    {
        /// <summary>The controller orchestrating this attack.</summary>
        protected readonly AttackController Controller;

        protected AttackRuntime(AttackController controller)
        {
            Controller = controller;
            Phase = AttackPhase.Startup;
        }

        public AttackPhase Phase { get; protected set; }

        /// <summary>True once the attack has completed (all phases finished or aborted).</summary>
        public abstract bool IsFinished { get; }

        /// <summary>Whether this attack locks the attacker's walk + skill input for its duration.</summary>
        public abstract bool LocksMovement { get; }

        /// <summary>Whether this attack suppresses gravity on the attacker for its duration.</summary>
        public abstract bool SuppressGravity { get; }

        /// <summary>True while the attack is in its cancel window (the final N frames of the attack's life cycle).
        /// During this window, an attack-input press advances the combo to the next step.</summary>
        public abstract bool IsInCancelWindow { get; }

        /// <summary>Number of frames after this attack ends during which the combo state persists ("still-in-combo" grace).
        /// After expiry, the combo step resets to fresh.</summary>
        public abstract int PostAttackGraceFrames { get; }

        /// <summary>Advance one fixed-update step. AttackController suppresses ticks during hit-stop.</summary>
        public abstract void OnTick();
    }

    /// <summary>
    /// Discrete phases of an attack's life cycle. Frame ranges live on <see cref="AttackData"/>;
    /// the runtime transitions between phases at frame boundaries.
    /// </summary>
    public enum AttackPhase
    {
        Startup,
        Active,
        Recovery,
        Done
    }
}
