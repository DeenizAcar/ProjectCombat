using UnityEngine;

namespace ProjectCombat.Combat.Movement.Skills
{
    /// <summary>
    /// Base class for movement-skill configuration assets (jump, dash, roll, ...).
    /// Pure data: instances are stateless templates. Per-controller state lives on the
    /// runtime instance returned by <see cref="CreateRuntime"/>.
    ///
    /// To add a new movement skill:
    ///   1. Subclass this with the tuning fields ([CreateAssetMenu] attribute on the subclass).
    ///   2. Subclass <see cref="MovementSkillRuntime"/> with the behavior (state + per-tick logic).
    ///   3. Override CreateRuntime to return your runtime, passing `this` and the controller.
    ///   4. Author the SO asset in the project, slot it into a <see cref="StyleProfile"/>.
    /// </summary>
    public abstract class MovementSkillData : ScriptableObject
    {
        /// <summary>
        /// Instantiate the runtime counterpart for this data SO, bound to the controller it will drive.
        /// </summary>
        public abstract MovementSkillRuntime CreateRuntime(MovementController controller);
    }
}
