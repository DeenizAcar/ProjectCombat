using System.Collections.Generic;
using System.IO;
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
    /// One-shot setup helpers for Milestone 1 (shinobi movement prototype).
    /// All actions are idempotent — safe to re-run.
    ///
    /// Menu: Tools → ProjectCombat → Milestone 1.
    /// </summary>
    public static class Milestone1Setup
    {
        const string DataFolder = "Assets/Data/Shinobi";
        const string ShinobiStyleAssetPath = DataFolder + "/ShinobiStyle.asset";
        const string ShinobiMovementProfilePath = DataFolder + "/ShinobiMovementProfile.asset";
        const string GroundLayer = "Ground";
        const string PlayerLayer = "Player";

        // -------------------- Full setup --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 1/Run Full Setup (Steps 2-3)", false, 1)]
        public static void RunFullSetup()
        {
            SetupLayers();
            var styleProfile = CreateShinobiAssets();
            UpdateCollisionMaskToGround(styleProfile.MovementProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = styleProfile;
            EditorGUIUtility.PingObject(styleProfile);

            EditorUtility.DisplayDialog(
                "Milestone 1 setup complete",
                "Assets created/updated in " + DataFolder + ":\n" +
                "  - ShinobiMovementProfile.asset\n" +
                "  - ShinobiJump.asset\n" +
                "  - ShinobiDash.asset\n" +
                "  - ShinobiStyle.asset\n\n" +
                "Layers ensured: " + GroundLayer + ", " + PlayerLayer + ".\n" +
                "ShinobiMovementProfile.CollisionMask set to '" + GroundLayer + "' only.\n\n" +
                "Next: select your player GameObject and run 'Step 4 - Configure Selected as Player'.",
                "OK");
        }

        // -------------------- Step 2: SO assets --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 1/Step 2 - Create Shinobi Assets", false, 11)]
        public static void CreateShinobiAssetsMenu()
        {
            var sp = CreateShinobiAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = sp;
            EditorGUIUtility.PingObject(sp);
        }

        static StyleProfile CreateShinobiAssets()
        {
            EnsureFolder(DataFolder);

            var movementProfile = CreateOrLoad<MovementProfile>(ShinobiMovementProfilePath);
            var jumpSkill       = CreateOrLoad<JumpSkillData>($"{DataFolder}/ShinobiJump.asset");
            var dashSkill       = CreateOrLoad<DashSkillData>($"{DataFolder}/ShinobiDash.asset");
            var styleProfile    = CreateOrLoad<StyleProfile>(ShinobiStyleAssetPath);

            styleProfile.StyleName       = "Shinobi";
            styleProfile.MovementProfile = movementProfile;
            styleProfile.Slot1Skill      = jumpSkill;
            styleProfile.Slot2Skill      = dashSkill;
            styleProfile.Slot3Skill      = null;

            EditorUtility.SetDirty(movementProfile);
            EditorUtility.SetDirty(jumpSkill);
            EditorUtility.SetDirty(dashSkill);
            EditorUtility.SetDirty(styleProfile);

            return styleProfile;
        }

        // -------------------- Step 3: layers --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 1/Step 3 - Setup Layers", false, 12)]
        public static void SetupLayersMenu()
        {
            SetupLayers();

            var profile = AssetDatabase.LoadAssetAtPath<MovementProfile>(ShinobiMovementProfilePath);
            if (profile != null) UpdateCollisionMaskToGround(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SetupLayers()
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogError("TagManager.asset not found at ProjectSettings/. Layer setup skipped.");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");

            EnsureLayer(layersProp, GroundLayer);
            EnsureLayer(layersProp, PlayerLayer);

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void UpdateCollisionMaskToGround(MovementProfile profile)
        {
            if (profile == null) return;
            int groundLayer = LayerMask.NameToLayer(GroundLayer);
            if (groundLayer < 0)
            {
                Debug.LogWarning($"Layer '{GroundLayer}' not found after setup; CollisionMask left unchanged.");
                return;
            }
            profile.CollisionMask = 1 << groundLayer;
            EditorUtility.SetDirty(profile);
        }

        static void EnsureLayer(SerializedProperty layersProp, string layerName)
        {
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var layer = layersProp.GetArrayElementAtIndex(i);
                if (layer.stringValue == layerName) return;
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    return;
                }
            }
            Debug.LogError($"No empty user-layer slot available to add '{layerName}'. Free one manually.");
        }

        // -------------------- Step 4: scene wiring --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 1/Step 4 - Configure Selected as Player", false, 13)]
        public static void ConfigureSelectedAsPlayer()
        {
            // 1. Resolve target GameObject (selection first, fallback to first Player-layer object in scene).
            var go = Selection.activeGameObject;
            int playerLayer = LayerMask.NameToLayer(PlayerLayer);
            int groundLayer = LayerMask.NameToLayer(GroundLayer);

            if (go == null && playerLayer >= 0)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (root.layer == playerLayer) { go = root; break; }
            }

            if (go == null)
            {
                EditorUtility.DisplayDialog("Player not found",
                    "Select your player GameObject in the Hierarchy first, then re-run this step.",
                    "OK");
                return;
            }

            if (groundLayer >= 0 && go.layer == groundLayer)
            {
                EditorUtility.DisplayDialog("Wrong selection",
                    $"'{go.name}' is on the Ground layer. Select your player (Capsule) instead.",
                    "OK");
                return;
            }

            // 2. Verify shinobi style asset is present.
            var styleProfile = AssetDatabase.LoadAssetAtPath<StyleProfile>(ShinobiStyleAssetPath);
            if (styleProfile == null)
            {
                EditorUtility.DisplayDialog("Shinobi style missing",
                    "ShinobiStyle.asset not found. Run 'Run Full Setup' first.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(go, "Configure Player");

            // 3. Ensure Player layer set.
            if (playerLayer >= 0 && go.layer != playerLayer)
                go.layer = playerLayer;

            // 4. Components: collider auto-sized to sprite, rigidbody, controller, input handler.
            var capsule = EnsureComponent<CapsuleCollider2D>(go);
            AutoSizeCapsuleToSprite(capsule, go);
            EnsureComponent<Rigidbody2D>(go);
            var controller = EnsureComponent<MovementController>(go);
            var input      = EnsureComponent<PlayerInputHandler>(go);

            // 5. Wire MovementController fields.
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("activeStyle").objectReferenceValue = styleProfile;
            controllerSo.FindProperty("inputHandler").objectReferenceValue = input;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // 6. Wire input action references (may fail if user hasn't saved the asset; reported in dialog).
            string inputWarn = TryWireInputReferences(input);

            // 7. Add BoxCollider2D to Ground-layer scene objects that lack any 2D collider.
            int groundFixed = 0;
            if (groundLayer >= 0)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.layer == groundLayer && root.GetComponent<Collider2D>() == null)
                    {
                        var box = root.AddComponent<BoxCollider2D>();
                        AutoSizeBoxToSprite(box, root);
                        groundFixed++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(go.scene);

            string msg = $"'{go.name}' is now wired as the Milestone 1 player.\n\n";
            msg += "Components: CapsuleCollider2D, Rigidbody2D, MovementController, PlayerInputHandler.\n";
            msg += "StyleProfile: ShinobiStyle.\n";
            msg += string.IsNullOrEmpty(inputWarn)
                ? "Input action references: wired.\n"
                : $"Input action references: {inputWarn}\n";
            if (groundFixed > 0)
                msg += $"Added BoxCollider2D to {groundFixed} Ground-layer GameObject(s).\n";
            msg += "\nPress Play.\n";
            msg += "Walk: WASD / left stick\n";
            msg += "Jump: Space / X (PS)\n";
            msg += "Dash: Left Shift / O (PS)";

            EditorUtility.DisplayDialog("Player configured", msg, "OK");
        }

        static string TryWireInputReferences(PlayerInputHandler handler)
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length == 0)
                return "no InputActionAsset in project — wire manually.";

            string foundPath = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset == null) continue;
                if (asset.FindAction("Move") != null
                    && asset.FindAction("Slot1") != null
                    && asset.FindAction("Slot2") != null
                    && asset.FindAction("Slot3") != null)
                {
                    foundPath = path;
                    break;
                }
            }

            if (foundPath == null)
                return "no asset with Move/Slot1/Slot2/Slot3 found — wire manually.";

            var refs = new Dictionary<string, InputActionReference>();
            foreach (var s in AssetDatabase.LoadAllAssetsAtPath(foundPath))
            {
                if (s is InputActionReference iref && iref.action != null)
                    refs[iref.action.name] = iref;
            }

            if (!refs.ContainsKey("Move") || !refs.ContainsKey("Slot1")
                || !refs.ContainsKey("Slot2") || !refs.ContainsKey("Slot3"))
                return $"sub-references missing in {foundPath}. Open it and click 'Save Asset' to generate them, then re-run Step 4.";

            var so = new SerializedObject(handler);
            so.FindProperty("moveAction").objectReferenceValue  = refs["Move"];
            so.FindProperty("slot1Action").objectReferenceValue = refs["Slot1"];
            so.FindProperty("slot2Action").objectReferenceValue = refs["Slot2"];
            so.FindProperty("slot3Action").objectReferenceValue = refs["Slot3"];
            so.ApplyModifiedPropertiesWithoutUndo();
            return "";
        }

        // -------------------- Helpers --------------------

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        static void AutoSizeCapsuleToSprite(CapsuleCollider2D capsule, GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            var size = sr.sprite.bounds.size;
            capsule.size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.offset = Vector2.zero;
        }

        static void AutoSizeBoxToSprite(BoxCollider2D box, GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            var size = sr.sprite.bounds.size;
            box.size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
            box.offset = Vector2.zero;
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
