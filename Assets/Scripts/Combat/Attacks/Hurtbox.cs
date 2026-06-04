using UnityEngine;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// A region that can be hit by attacks. Usually sits on a child GameObject of an enemy whose
    /// collider is a trigger on the EnemyHurtbox layer. Forwards hits to the owning
    /// <see cref="DamageReceiver"/> (which actually applies damage and reaction).
    ///
    /// Multiple hurtboxes can route to the same receiver (e.g. body + head with different sizes
    /// — useful later for crit zones).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        [Tooltip("The receiver that takes damage when this hurtbox is hit. Usually on the parent GameObject.")]
        [SerializeField] DamageReceiver receiver;

        public DamageReceiver Receiver => receiver;

        void Reset()
        {
            receiver = GetComponentInParent<DamageReceiver>();
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }
}
