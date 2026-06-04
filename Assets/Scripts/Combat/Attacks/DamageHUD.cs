using UnityEngine;
using UnityEngine.UI;

namespace ProjectCombat.Combat.Attacks
{
    /// <summary>
    /// Simple debug HUD that displays cumulative damage dealt to a single <see cref="DamageReceiver"/>.
    /// Stand-in until a real combat UI exists. Drives a UnityEngine.UI Text on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class DamageHUD : MonoBehaviour
    {
        [Tooltip("The receiver whose damage we display. Usually the training dummy.")]
        [SerializeField] DamageReceiver source;

        [Tooltip("Text format. {0} is replaced with the cumulative damage number.")]
        [SerializeField] string format = "Damage: {0}";

        Text text;

        void Awake() => text = GetComponent<Text>();

        void OnEnable()
        {
            if (source != null) source.Damaged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (source != null) source.Damaged -= Refresh;
        }

        void Refresh()
        {
            if (text == null || source == null) return;
            text.text = string.Format(format, source.TotalDamage);
        }
    }
}
