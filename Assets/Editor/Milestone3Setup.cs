using System.IO;
using ProjectCombat.Combat.Attacks;
using ProjectCombat.Combat.Movement;
using UnityEditor;
using UnityEngine;

namespace ProjectCombat.Combat.EditorTools
{
    /// <summary>
    /// Setup helpers for Milestone 3 (Shinobi 3-hit combo chain).
    /// Creates Light2 and Light3 attack assets with starter tuning and wires the chain on ShinobiStyle.
    /// Idempotent — safe to re-run.
    /// </summary>
    public static class Milestone3Setup
    {
        const string DataFolder = "Assets/Data/Shinobi";
        const string AttacksFolder = DataFolder + "/Attacks";
        const string ShinobiStylePath = DataFolder + "/ShinobiStyle.asset";
        const string Light1Path = AttacksFolder + "/ShinobiLight1.asset";
        const string Light2Path = AttacksFolder + "/ShinobiLight2.asset";
        const string Light3Path = AttacksFolder + "/ShinobiLight3.asset";

        [MenuItem("Tools/ProjectCombat/Milestone 3/Run Full Setup", false, 1)]
        public static void RunFullSetup()
        {
            var light1 = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(Light1Path);
            if (light1 == null)
            {
                EditorUtility.DisplayDialog("Milestone 3",
                    "ShinobiLight1.asset not found. Run Milestone 2 setup first.", "OK");
                return;
            }

            var light2 = EnsureAttack(Light2Path, ConfigureLight2);
            var light3 = EnsureAttack(Light3Path, ConfigureLight3);

            // Copy HurtboxLayer from Light1 so all combo hits target the same layer mask.
            light2.HurtboxLayer = light1.HurtboxLayer;
            light3.HurtboxLayer = light1.HurtboxLayer;
            EditorUtility.SetDirty(light2);
            EditorUtility.SetDirty(light3);

            WireShinobiCombo(light1, light2, light3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<StyleProfile>(ShinobiStylePath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            EditorUtility.DisplayDialog(
                "Milestone 3 setup complete",
                "Created/updated:\n" +
                "  - ShinobiLight2.asset (slash, mid hit)\n" +
                "  - ShinobiLight3.asset (finisher, heavier)\n\n" +
                "ShinobiStyle.LightCombo wired: [Light1, Light2, Light3].\n\n" +
                "Press Play. Tap Square → Light1. Tap during its cancel window → Light2. Tap again → Light3. " +
                "After Light3 (or in its window) tap → wraps to Light1. Wait out the grace and the next tap is a fresh combo.",
                "OK");
        }

        [MenuItem("Tools/ProjectCombat/Milestone 3/Step A - Create Light2 & Light3", false, 11)]
        public static void CreateLight2Light3Menu()
        {
            var l1 = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(Light1Path);
            if (l1 == null)
            {
                EditorUtility.DisplayDialog("Milestone 3", "ShinobiLight1.asset missing — run Milestone 2 first.", "OK");
                return;
            }
            var l2 = EnsureAttack(Light2Path, ConfigureLight2);
            var l3 = EnsureAttack(Light3Path, ConfigureLight3);
            l2.HurtboxLayer = l1.HurtboxLayer;
            l3.HurtboxLayer = l1.HurtboxLayer;
            EditorUtility.SetDirty(l2);
            EditorUtility.SetDirty(l3);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = l2;
        }

        [MenuItem("Tools/ProjectCombat/Milestone 3/Step B - Wire ShinobiStyle Combo", false, 12)]
        public static void WireComboMenu()
        {
            var l1 = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(Light1Path);
            var l2 = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(Light2Path);
            var l3 = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(Light3Path);
            if (l1 == null || l2 == null || l3 == null)
            {
                EditorUtility.DisplayDialog("Milestone 3",
                    "One or more light attack assets missing. Run Step A first.", "OK");
                return;
            }
            WireShinobiCombo(l1, l2, l3);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // -------------------- Tuning starters --------------------

        static void ConfigureLight2(MeleeAttackData a)
        {
            // Mid-combo slash: slightly wider arc, a touch more damage, marginally bigger hit-stop.
            a.StartupFrames = 4;
            a.ActiveFrames = 5;
            a.RecoveryFrames = 8;
            a.Damage = 14;
            a.HitStopFrames = 3;
            a.KnockbackVelocity = new Vector2(5f, 1.5f);
            a.LocksMovement = false;
            a.SuppressGravity = true;
            a.CancelWindowFrames = 5;
            a.PostAttackGraceFrames = 8;
            a.HitboxOffset = new Vector2(1.1f, 0f);
            a.HitboxSize = new Vector2(1.7f, 1.1f);
            a.SlashOffset = new Vector2(1.1f, 0f);
            a.SlashScale = new Vector2(1.7f, 0.65f);
            a.SlashRotation = 15f;
            a.SlashLifetimeFrames = 7;
            a.SlashFadeOut = true;
            a.SlashSortingOrder = 5;
        }

        static void ConfigureLight3(MeleeAttackData a)
        {
            // Finisher: more windup, bigger punch, larger knockback, longer hit-stop, more grace for the loopback feel.
            a.StartupFrames = 6;
            a.ActiveFrames = 5;
            a.RecoveryFrames = 12;
            a.Damage = 20;
            a.HitStopFrames = 5;
            a.KnockbackVelocity = new Vector2(8f, 3f);
            a.LocksMovement = false;
            a.SuppressGravity = true;
            a.CancelWindowFrames = 4;
            a.PostAttackGraceFrames = 12;
            a.HitboxOffset = new Vector2(1.2f, 0.1f);
            a.HitboxSize = new Vector2(1.9f, 1.2f);
            a.SlashOffset = new Vector2(1.2f, 0.1f);
            a.SlashScale = new Vector2(1.9f, 0.75f);
            a.SlashRotation = -30f;
            a.SlashLifetimeFrames = 8;
            a.SlashFadeOut = true;
            a.SlashSortingOrder = 5;
        }

        // -------------------- Helpers --------------------

        static MeleeAttackData EnsureAttack(string path, System.Action<MeleeAttackData> configureIfNew)
        {
            var existing = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<MeleeAttackData>();
            configureIfNew(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void WireShinobiCombo(MeleeAttackData l1, MeleeAttackData l2, MeleeAttackData l3)
        {
            var style = AssetDatabase.LoadAssetAtPath<StyleProfile>(ShinobiStylePath);
            if (style == null)
            {
                Debug.LogError("Milestone 3: ShinobiStyle.asset not found.");
                return;
            }
            style.LightCombo.Clear();
            style.LightCombo.Add(l1);
            style.LightCombo.Add(l2);
            style.LightCombo.Add(l3);
            EditorUtility.SetDirty(style);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName)) return;
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
