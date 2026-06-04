using System.Collections.Generic;
using ProjectCombat.Combat.Attacks;
using ProjectCombat.Combat.Movement.Skills;
using UnityEngine;

namespace ProjectCombat.Combat.Movement
{
    /// <summary>
    /// Top-level configuration of a single combat style (Shinobi, Tank, ...).
    /// Bundles the style's movement tuning with its mobility skill kit (up to 3 slots).
    /// Future combat data (moveset, weapon visuals, parry windows) will hang off this same asset.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectCombat/Combat/Style Profile", fileName = "StyleProfile")]
    public class StyleProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name for editor convenience. Not user-facing yet.")]
        public string StyleName;

        [Tooltip("Color tint applied to the player sprite while this style is active. Minimal visual feedback " +
                 "for style swap until real per-style art lands. White = no tint.")]
        public Color PlayerTint = Color.white;

        [Header("Movement")]
        [Tooltip("Universal-layer movement tuning (walk speed, gravity, air control). Required.")]
        public MovementProfile MovementProfile;

        [Tooltip("Movement skill bound to slot 1 (PS controller X / south face button). Null = empty slot.")]
        public MovementSkillData Slot1Skill;

        [Tooltip("Movement skill bound to slot 2 (PS controller O / east face button). Null = empty slot.")]
        public MovementSkillData Slot2Skill;

        [Tooltip("Movement skill bound to slot 3 (PS controller Triangle / north face button). Null = empty slot.")]
        public MovementSkillData Slot3Skill;

        [Header("Attacks")]
        [Tooltip("Light attack combo chain. Index 0 = first hit on a fresh tap; subsequent hits fire on Square " +
                 "presses inside the previous attack's cancel window or post-attack grace window. List size can grow.")]
        public List<AttackData> LightCombo = new();

        /// <summary>
        /// Returns the skill bound to the given slot, or null if the slot is empty.
        /// </summary>
        public MovementSkillData GetSkill(InputSlot slot) => slot switch
        {
            InputSlot.Slot1 => Slot1Skill,
            InputSlot.Slot2 => Slot2Skill,
            InputSlot.Slot3 => Slot3Skill,
            _ => null
        };
    }

    /// <summary>
    /// Identifies which input slot a movement skill is bound to.
    /// Slot1/2/3 map to PS controller X / O / Triangle (south / east / north face buttons).
    /// </summary>
    public enum InputSlot
    {
        Slot1,
        Slot2,
        Slot3
    }
}
