using System.Collections.Generic;
using System.IO;
using ProjectCombat.Combat.Attacks;
using ProjectCombat.Combat.Movement;
using ProjectCombat.Combat.Movement.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectCombat.Combat.EditorTools
{
    /// <summary>
    /// Setup helpers for Milestone 4 (Tank style + D-pad style swap).
    /// Creates Tank movement/attack/style assets, adds Style1-4 input actions,
    /// wires the StyleSwapController on the player. Idempotent.
    /// </summary>
    public static class Milestone4Setup
    {
        const string TankFolder = "Assets/Data/Tank";
        const string TankAttacksFolder = TankFolder + "/Attacks";
        const string TankMovementProfilePath = TankFolder + "/TankMovementProfile.asset";
        const string TankRollPath = TankFolder + "/TankRoll.asset";
        const string TankLight1Path = TankAttacksFolder + "/TankLight1.asset";
        const string TankLight2Path = TankAttacksFolder + "/TankLight2.asset";
        const string TankLight3Path = TankAttacksFolder + "/TankLight3.asset";
        const string TankStylePath = TankFolder + "/TankStyle.asset";

        const string ShinobiStylePath = "Assets/Data/Shinobi/ShinobiStyle.asset";
        const string ShinobiLight1Path = "Assets/Data/Shinobi/Attacks/ShinobiLight1.asset";

        const string EnemyHurtboxLayer = "EnemyHurtbox";
        const string PlayerLayer = "Player";

        const string PlayerMapName = "Player";

        // -------------------- Full setup --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 4/Run Full Setup", false, 1)]
        public static void RunFullSetup()
        {
            bool inputOk = EnsureStyleSwapActions();
            var tankStyle = EnsureTankAssets();
            bool playerOk = ConfigurePlayerForStyleSwap(tankStyle, out string playerWarn);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = "Milestone 4 setup complete.\n\n";
            msg += inputOk ? "✓ Style1-4 input actions present.\n"
                          : "⚠ Could not auto-add Style1-4 input actions. Add manually:\n" +
                            "  Style1 = <Gamepad>/dpad/up + Key 1\n" +
                            "  Style2 = <Gamepad>/dpad/right + Key 2\n" +
                            "  Style3 = <Gamepad>/dpad/down + Key 3\n" +
                            "  Style4 = <Gamepad>/dpad/left + Key 4\n";
            msg += "✓ Tank assets in " + TankFolder + ".\n";
            msg += playerOk ? "✓ Player has StyleSwapController. D-pad Up = Shinobi, Right = Tank.\n"
                            : $"⚠ Player: {playerWarn}\n";
            msg += "\nPress Play.\n";
            msg += "Walk + attack as Shinobi. Press D-pad Right (or Key 2) → tint goes warm red, swap to Tank.\n";
            msg += "Square attacks now are slow axe swings. Combo step is preserved across swap.\n";
            msg += "Press D-pad Up (or Key 1) to swap back to Shinobi.";

            EditorUtility.DisplayDialog("Milestone 4", msg, "OK");
        }

        // -------------------- Step A: input actions --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 4/Step A - Add Style1-4 Input Actions", false, 11)]
        public static void EnsureStyleSwapActionsMenu()
        {
            bool ok = EnsureStyleSwapActions();
            EditorUtility.DisplayDialog("Style swap actions",
                ok ? "Style1-4 added or already present." : "Auto-add failed. Add manually and re-run.",
                "OK");
        }

        static bool EnsureStyleSwapActions()
        {
            string targetPath = null;
            InputActionAsset asset = null;
            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (candidate?.FindActionMap(PlayerMapName)?.FindAction("Move") != null)
                {
                    targetPath = path;
                    asset = candidate;
                    break;
                }
            }
            if (asset == null)
            {
                Debug.LogError("Milestone 4: No InputActionAsset with a Player/Move action found.");
                return false;
            }

            var map = asset.FindActionMap(PlayerMapName);
            bool anyAdded = false;

            asset.Disable();
            anyAdded |= AddIfMissing(map, "Style1", new[] { "<Gamepad>/dpad/up", "<Keyboard>/1" });
            anyAdded |= AddIfMissing(map, "Style2", new[] { "<Gamepad>/dpad/right", "<Keyboard>/2" });
            anyAdded |= AddIfMissing(map, "Style3", new[] { "<Gamepad>/dpad/down", "<Keyboard>/3" });
            anyAdded |= AddIfMissing(map, "Style4", new[] { "<Gamepad>/dpad/left", "<Keyboard>/4" });
            asset.Enable();

            if (anyAdded)
            {
                try
                {
                    string json = asset.ToJson();
                    File.WriteAllText(targetPath, json);
                    AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Milestone 4: failed to persist Style1-4 actions: {e.Message}");
                    return false;
                }
            }

            // Verify
            var reloaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(targetPath);
            var rmap = reloaded?.FindActionMap(PlayerMapName);
            return rmap?.FindAction("Style1") != null
                && rmap.FindAction("Style2") != null
                && rmap.FindAction("Style3") != null
                && rmap.FindAction("Style4") != null;
        }

        static bool AddIfMissing(InputActionMap map, string name, string[] bindings)
        {
            if (map.FindAction(name) != null) return false;
            var action = map.AddAction(name, InputActionType.Button);
            foreach (var b in bindings) action.AddBinding(b);
            return true;
        }

        // -------------------- Step B: tank assets --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 4/Step B - Create Tank Assets", false, 12)]
        public static void CreateTankAssetsMenu()
        {
            EnsureTankAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<StyleProfile>(TankStylePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        static StyleProfile EnsureTankAssets()
        {
            EnsureFolder(TankAttacksFolder);

            // Hurtbox layer mask shared with shinobi (target the same EnemyHurtbox layer).
            int hurtLayerIdx = LayerMask.NameToLayer(EnemyHurtboxLayer);
            LayerMask hurtMask = hurtLayerIdx >= 0 ? (LayerMask)(1 << hurtLayerIdx) : default;

            var moveProfile = EnsureAsset<MovementProfile>(TankMovementProfilePath, ConfigureTankMovement);
            var roll        = EnsureAsset<RollSkillData>(TankRollPath, ConfigureTankRoll);
            var l1          = EnsureAsset<MeleeAttackData>(TankLight1Path, a => { ConfigureTankLight1(a); a.HurtboxLayer = hurtMask; });
            var l2          = EnsureAsset<MeleeAttackData>(TankLight2Path, a => { ConfigureTankLight2(a); a.HurtboxLayer = hurtMask; });
            var l3          = EnsureAsset<MeleeAttackData>(TankLight3Path, a => { ConfigureTankLight3(a); a.HurtboxLayer = hurtMask; });

            // Ensure HurtboxLayer correct even on re-runs (in case layer wasn't ready first time).
            l1.HurtboxLayer = hurtMask; EditorUtility.SetDirty(l1);
            l2.HurtboxLayer = hurtMask; EditorUtility.SetDirty(l2);
            l3.HurtboxLayer = hurtMask; EditorUtility.SetDirty(l3);

            var style = EnsureAsset<StyleProfile>(TankStylePath, s => ConfigureTankStyle(s, moveProfile, roll, l1, l2, l3));

            // Always ensure style wiring (idempotent — overwrites references each run).
            style.StyleName = "Tank";
            style.PlayerTint = new Color(0.95f, 0.5f, 0.4f, 1f);
            style.MovementProfile = moveProfile;
            style.Slot1Skill = roll;
            style.Slot2Skill = null;
            style.Slot3Skill = null;
            if (style.LightCombo == null) style.LightCombo = new List<AttackData>();
            style.LightCombo.Clear();
            style.LightCombo.Add(l1);
            style.LightCombo.Add(l2);
            style.LightCombo.Add(l3);
            EditorUtility.SetDirty(style);

            return style;
        }

        // -------------------- Step C: player wiring --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 4/Step C - Configure Player Style Swap", false, 13)]
        public static void ConfigurePlayerForStyleSwapMenu()
        {
            var tankStyle = AssetDatabase.LoadAssetAtPath<StyleProfile>(TankStylePath);
            if (tankStyle == null)
            {
                EditorUtility.DisplayDialog("Milestone 4",
                    "TankStyle.asset missing. Run Step B or Run Full Setup first.", "OK");
                return;
            }
            ConfigurePlayerForStyleSwap(tankStyle, out string warn);
            EditorUtility.DisplayDialog("Player style swap",
                string.IsNullOrEmpty(warn) ? "Player wired for style swap." : $"Warning: {warn}",
                "OK");
        }

        static bool ConfigurePlayerForStyleSwap(StyleProfile tankStyle, out string warning)
        {
            warning = "";
            var player = ResolvePlayer();
            if (player == null) { warning = "no player GameObject in scene."; return false; }

            var movement = player.GetComponent<MovementController>();
            var input    = player.GetComponent<PlayerInputHandler>();
            if (movement == null || input == null)
            {
                warning = "player needs MovementController + PlayerInputHandler.";
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(player, "Configure Style Swap");

            var swap = player.GetComponent<StyleSwapController>();
            if (swap == null) swap = player.AddComponent<StyleSwapController>();

            var shinobiStyle = AssetDatabase.LoadAssetAtPath<StyleProfile>(ShinobiStylePath);

            var so = new SerializedObject(swap);
            so.FindProperty("inputHandler").objectReferenceValue = input;
            so.FindProperty("movementController").objectReferenceValue = movement;
            so.FindProperty("playerSprite").objectReferenceValue = player.GetComponent<SpriteRenderer>();
            so.FindProperty("style1Up").objectReferenceValue = shinobiStyle;     // D-pad Up = Shinobi
            so.FindProperty("style2Right").objectReferenceValue = tankStyle;     // D-pad Right = Tank
            // style3Down, style4Left left empty intentionally for future styles
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire Style1-4 InputActionReferences on PlayerInputHandler.
            WireStyleSwapInputReferences(input);

            EditorSceneManager.MarkSceneDirty(player.scene);
            return true;
        }

        static void WireStyleSwapInputReferences(PlayerInputHandler input)
        {
            string foundPath = null;
            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset?.FindActionMap(PlayerMapName)?.FindAction("Style1") != null)
                {
                    foundPath = path;
                    break;
                }
            }
            if (foundPath == null) return;

            var refs = new Dictionary<string, InputActionReference>();
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(foundPath))
            {
                if (sub is InputActionReference iar && iar.action != null)
                    refs[iar.action.name] = iar;
            }

            var so = new SerializedObject(input);
            TrySet(so, "style1Action", refs, "Style1");
            TrySet(so, "style2Action", refs, "Style2");
            TrySet(so, "style3Action", refs, "Style3");
            TrySet(so, "style4Action", refs, "Style4");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TrySet(SerializedObject so, string propName, Dictionary<string, InputActionReference> refs, string actionName)
        {
            if (!refs.TryGetValue(actionName, out var iar)) return;
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = iar;
        }

        // -------------------- Tuning starters --------------------

        static void ConfigureTankMovement(MovementProfile p)
        {
            p.WalkSpeed = 4.5f;
            p.GroundAccelTime = 0.15f;
            p.GroundDecelTime = 0.12f;
            p.AirControlMultiplier = 0.4f;
            p.Gravity = 60f;
            p.FallGravityMultiplier = 2.0f;
            p.MaxFallSpeed = 35f;
            int groundLayerIdx = LayerMask.NameToLayer("Ground");
            p.CollisionMask = groundLayerIdx >= 0 ? (LayerMask)(1 << groundLayerIdx) : ~0;
            p.SkinWidth = 0.02f;
            p.GroundProbeDistance = 0.08f;
            p.MaxGroundAngle = 50f;
            p.EdgeCorrectionMaxNudge = 0f;
        }

        static void ConfigureTankRoll(RollSkillData r)
        {
            r.RollVelocity = 14f;
            r.RollDurationFrames = 18;
            r.CooldownFrames = 14;
            r.InvulnerabilityFrames = 10;
            r.GroundOnly = true;
        }

        static void ConfigureTankLight1(MeleeAttackData a)
        {
            a.StartupFrames = 12;
            a.ActiveFrames = 4;
            a.RecoveryFrames = 18;
            a.Damage = 25;
            a.HitStopFrames = 6;
            a.KnockbackVelocity = new Vector2(5f, 1f);
            a.LocksMovement = true;
            a.SuppressGravity = false;
            a.CancelWindowFrames = 6;
            a.PostAttackGraceFrames = 12;
            a.HitboxOffset = new Vector2(1.0f, 0.2f);
            a.HitboxSize = new Vector2(1.8f, 1.4f);
            a.SlashOffset = new Vector2(1.0f, 0.2f);
            a.SlashScale = new Vector2(2.0f, 0.9f);
            a.SlashRotation = -60f;
            a.SlashLifetimeFrames = 10;
            a.SlashFadeOut = true;
            a.SlashSortingOrder = 5;
        }

        static void ConfigureTankLight2(MeleeAttackData a)
        {
            a.StartupFrames = 10;
            a.ActiveFrames = 5;
            a.RecoveryFrames = 16;
            a.Damage = 30;
            a.HitStopFrames = 7;
            a.KnockbackVelocity = new Vector2(6f, 1.5f);
            a.LocksMovement = true;
            a.SuppressGravity = false;
            a.CancelWindowFrames = 6;
            a.PostAttackGraceFrames = 12;
            a.HitboxOffset = new Vector2(1.2f, 0f);
            a.HitboxSize = new Vector2(2.0f, 1.2f);
            a.SlashOffset = new Vector2(1.2f, 0f);
            a.SlashScale = new Vector2(2.1f, 0.85f);
            a.SlashRotation = 0f;
            a.SlashLifetimeFrames = 10;
            a.SlashFadeOut = true;
            a.SlashSortingOrder = 5;
        }

        static void ConfigureTankLight3(MeleeAttackData a)
        {
            a.StartupFrames = 14;
            a.ActiveFrames = 5;
            a.RecoveryFrames = 22;
            a.Damage = 45;
            a.HitStopFrames = 10;
            a.KnockbackVelocity = new Vector2(10f, 4f);
            a.LocksMovement = true;
            a.SuppressGravity = false;
            a.CancelWindowFrames = 5;
            a.PostAttackGraceFrames = 16;
            a.HitboxOffset = new Vector2(1.3f, 0.1f);
            a.HitboxSize = new Vector2(2.3f, 1.5f);
            a.SlashOffset = new Vector2(1.3f, 0.1f);
            a.SlashScale = new Vector2(2.3f, 1.0f);
            a.SlashRotation = 30f;
            a.SlashLifetimeFrames = 12;
            a.SlashFadeOut = true;
            a.SlashSortingOrder = 5;
        }

        static void ConfigureTankStyle(StyleProfile s, MovementProfile move, RollSkillData roll,
                                       MeleeAttackData l1, MeleeAttackData l2, MeleeAttackData l3)
        {
            s.StyleName = "Tank";
            s.PlayerTint = new Color(0.95f, 0.5f, 0.4f, 1f);
            s.MovementProfile = move;
            s.Slot1Skill = roll;
            s.Slot2Skill = null;
            s.Slot3Skill = null;
            s.LightCombo = new List<AttackData> { l1, l2, l3 };
        }

        // -------------------- Helpers --------------------

        static T EnsureAsset<T>(string path, System.Action<T> configureIfNew) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            configureIfNew(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static GameObject ResolvePlayer()
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayer);
            if (playerLayer >= 0)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (root.layer == playerLayer && root.GetComponent<MovementController>() != null)
                        return root;
            }
            var any = Object.FindFirstObjectByType<MovementController>();
            return any != null ? any.gameObject : null;
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
