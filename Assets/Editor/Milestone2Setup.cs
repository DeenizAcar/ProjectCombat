using System.Collections.Generic;
using System.IO;
using ProjectCombat.Combat.Attacks;
using ProjectCombat.Combat.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectCombat.Combat.EditorTools
{
    /// <summary>
    /// Setup helpers for Milestone 2 (first attack lands on training dummy).
    /// Idempotent — safe to re-run.
    /// Menu: Tools → ProjectCombat → Milestone 2.
    /// </summary>
    public static class Milestone2Setup
    {
        const string DataFolder = "Assets/Data/Shinobi";
        const string AttacksFolder = DataFolder + "/Attacks";
        const string ShinobiLight1Path = AttacksFolder + "/ShinobiLight1.asset";
        const string ShinobiStylePath = DataFolder + "/ShinobiStyle.asset";

        const string EnemyHurtboxLayer = "EnemyHurtbox";
        const string GroundLayer = "Ground";
        const string PlayerLayer = "Player";

        const string PlayerMapName = "Player";
        const string AttackLightActionName = "AttackLight";

        // -------------------- Full setup --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Run Full Setup", false, 1)]
        public static void RunFullSetup()
        {
            EnsureEnemyHurtboxLayer();
            bool inputActionOk = EnsureAttackLightAction();
            var attackData = EnsureShinobiLight1Asset();
            WireStyleProfileLightAttack(attackData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool playerOk = ConfigurePlayerForAttacks(out var playerGo, out string playerWarning);
            bool dummyOk = EnsureTrainingDummy(playerGo, out var dummyReceiver, out string dummyWarning);
            bool hudOk = EnsureDamageHud(dummyReceiver, out string hudWarning);

            if (playerGo != null)
                EditorSceneManager.MarkSceneDirty(playerGo.scene);

            string msg = "Milestone 2 setup complete.\n\n";
            msg += inputActionOk
                ? "✓ AttackLight input action present.\n"
                : "⚠ AttackLight could not be added automatically — add it manually (PS Square / Keyboard J), re-run.\n";
            msg += "✓ EnemyHurtbox layer.\n";
            msg += "✓ ShinobiLight1 attack data wired to ShinobiStyle.LightAttack.\n";
            msg += playerOk ? "✓ Player has AttackController.\n" : $"⚠ Player: {playerWarning}\n";
            msg += dummyOk ? "✓ Training dummy in scene.\n" : $"⚠ Dummy: {dummyWarning}\n";
            msg += hudOk ? "✓ Damage HUD wired.\n" : $"⚠ HUD: {hudWarning}\n";
            msg += "\nPress Play. Walk up to the dummy, press J or Square. Should flash red, count damage, world pauses briefly.";

            EditorUtility.DisplayDialog("Milestone 2", msg, "OK");
        }

        // -------------------- Step A: EnemyHurtbox layer --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step A - Add EnemyHurtbox Layer", false, 11)]
        public static void EnsureEnemyHurtboxLayerMenu()
        {
            EnsureEnemyHurtboxLayer();
            AssetDatabase.SaveAssets();
        }

        static void EnsureEnemyHurtboxLayer()
        {
            var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagAssets == null || tagAssets.Length == 0)
            {
                Debug.LogError("TagManager.asset not found.");
                return;
            }
            var so = new SerializedObject(tagAssets[0]);
            var layers = so.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == EnemyHurtboxLayer) return;
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = EnemyHurtboxLayer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }
            Debug.LogError("No free user-layer slot for EnemyHurtbox.");
        }

        // -------------------- Step B: AttackLight input action --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step B - Add AttackLight Input Action", false, 12)]
        public static void EnsureAttackLightActionMenu()
        {
            if (EnsureAttackLightAction())
                EditorUtility.DisplayDialog("AttackLight", "Action added (or already present).", "OK");
            else
                EditorUtility.DisplayDialog("AttackLight",
                    "Could not auto-add the AttackLight action. Open your InputActions asset, add:\n" +
                    "  Name: AttackLight\n  Type: Button\n  Bindings: <Gamepad>/buttonWest (PS Square), <Keyboard>/j\n" +
                    "Then Save Asset and re-run.", "OK");
        }

        static bool EnsureAttackLightAction()
        {
            string targetPath = null;
            InputActionAsset asset = null;

            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (candidate == null) continue;
                var map = candidate.FindActionMap(PlayerMapName);
                if (map == null) continue;
                if (map.FindAction("Move") != null && map.FindAction("Slot1") != null
                    && map.FindAction("Slot2") != null && map.FindAction("Slot3") != null)
                {
                    targetPath = path;
                    asset = candidate;
                    break;
                }
            }

            if (asset == null)
            {
                Debug.LogError("Milestone 2: Could not find an InputActionAsset with Move/Slot1/Slot2/Slot3.");
                return false;
            }

            var playerMap = asset.FindActionMap(PlayerMapName);
            if (playerMap.FindAction(AttackLightActionName) != null) return true;

            // Asset must be disabled before structural changes.
            asset.Disable();
            var newAction = playerMap.AddAction(AttackLightActionName, InputActionType.Button);
            newAction.AddBinding("<Gamepad>/buttonWest");
            newAction.AddBinding("<Keyboard>/j");
            asset.Enable();

            // Persist by writing the asset's JSON back to disk and forcing reimport.
            // The C# API only updates the runtime model — disk persistence requires the JSON round-trip.
            try
            {
                string json = asset.ToJson();
                File.WriteAllText(targetPath, json);
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Milestone 2: Failed to persist AttackLight action: {e.Message}");
                return false;
            }

            // Verify.
            var reloaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(targetPath);
            return reloaded?.FindActionMap(PlayerMapName)?.FindAction(AttackLightActionName) != null;
        }

        // -------------------- Step C: ShinobiLight1 attack data --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step C - Create ShinobiLight1 Attack", false, 13)]
        public static void EnsureShinobiLight1AssetMenu()
        {
            var d = EnsureShinobiLight1Asset();
            WireStyleProfileLightAttack(d);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = d;
            EditorGUIUtility.PingObject(d);
        }

        static MeleeAttackData EnsureShinobiLight1Asset()
        {
            EnsureFolder(AttacksFolder);
            var attack = AssetDatabase.LoadAssetAtPath<MeleeAttackData>(ShinobiLight1Path);
            if (attack == null)
            {
                attack = ScriptableObject.CreateInstance<MeleeAttackData>();
                AssetDatabase.CreateAsset(attack, ShinobiLight1Path);
            }

            // Always ensure HurtboxLayer points at EnemyHurtbox even if the asset already existed.
            int hurtLayer = LayerMask.NameToLayer(EnemyHurtboxLayer);
            if (hurtLayer >= 0)
            {
                attack.HurtboxLayer = 1 << hurtLayer;
                EditorUtility.SetDirty(attack);
            }
            return attack;
        }

        static void WireStyleProfileLightAttack(MeleeAttackData attack)
        {
            var style = AssetDatabase.LoadAssetAtPath<StyleProfile>(ShinobiStylePath);
            if (style == null)
            {
                Debug.LogError("Milestone 2: ShinobiStyle.asset not found — run Milestone 1 setup first.");
                return;
            }
            // Ensure Light1 is at index 0 of the combo. M3 setup will append Light2 and Light3 on top.
            if (style.LightCombo == null) style.LightCombo = new System.Collections.Generic.List<AttackData>();
            if (style.LightCombo.Count == 0)
                style.LightCombo.Add(attack);
            else
                style.LightCombo[0] = attack;
            EditorUtility.SetDirty(style);
        }

        // -------------------- Step D: configure player for attacks --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step D - Configure Player Attack", false, 14)]
        public static void ConfigurePlayerForAttacksMenu()
        {
            ConfigurePlayerForAttacks(out var go, out string warning);
            if (go != null) EditorSceneManager.MarkSceneDirty(go.scene);
            EditorUtility.DisplayDialog("Player attack",
                go != null ? $"'{go.name}' wired for attacks." : $"Could not configure player: {warning}",
                "OK");
        }

        static bool ConfigurePlayerForAttacks(out GameObject playerGo, out string warning)
        {
            warning = "";
            playerGo = ResolvePlayer();
            if (playerGo == null)
            {
                warning = "select your player GameObject first.";
                return false;
            }
            var movement = playerGo.GetComponent<MovementController>();
            var input = playerGo.GetComponent<PlayerInputHandler>();
            if (movement == null || input == null)
            {
                warning = "player needs MovementController + PlayerInputHandler (run Milestone 1 Step 4 first).";
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(playerGo, "Configure Player Attack");

            var attack = playerGo.GetComponent<AttackController>();
            if (attack == null) attack = playerGo.AddComponent<AttackController>();

            // Wire AttackController fields.
            var attackSo = new SerializedObject(attack);
            attackSo.FindProperty("inputHandler").objectReferenceValue = input;
            attackSo.FindProperty("movementController").objectReferenceValue = movement;
            attackSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire MovementController.attackController back-reference.
            var moveSo = new SerializedObject(movement);
            moveSo.FindProperty("attackController").objectReferenceValue = attack;
            moveSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire AttackLight reference on PlayerInputHandler if available.
            WireAttackLightReferenceIfAvailable(input);

            return true;
        }

        static void WireAttackLightReferenceIfAvailable(PlayerInputHandler input)
        {
            string foundPath = null;
            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (candidate?.FindActionMap(PlayerMapName)?.FindAction(AttackLightActionName) != null)
                {
                    foundPath = path;
                    break;
                }
            }
            if (foundPath == null) return;

            InputActionReference attackRef = null;
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(foundPath))
            {
                if (sub is InputActionReference iar && iar.action != null
                    && iar.action.name == AttackLightActionName)
                {
                    attackRef = iar;
                    break;
                }
            }
            if (attackRef == null) return;

            var so = new SerializedObject(input);
            var prop = so.FindProperty("attackLightAction");
            if (prop != null)
            {
                prop.objectReferenceValue = attackRef;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // -------------------- Step E: training dummy --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step E - Create Training Dummy", false, 15)]
        public static void EnsureTrainingDummyMenu()
        {
            var player = ResolvePlayer();
            EnsureTrainingDummy(player, out var receiver, out string warning);
            if (receiver != null) EditorSceneManager.MarkSceneDirty(receiver.gameObject.scene);
            EditorUtility.DisplayDialog("Training dummy",
                receiver != null ? "Dummy ready." : $"Could not create dummy: {warning}",
                "OK");
        }

        static bool EnsureTrainingDummy(GameObject playerGo, out DamageReceiver receiver, out string warning)
        {
            warning = "";
            receiver = null;

            int groundLayer = LayerMask.NameToLayer(GroundLayer);
            int hurtboxLayer = LayerMask.NameToLayer(EnemyHurtboxLayer);
            if (hurtboxLayer < 0)
            {
                warning = "EnemyHurtbox layer missing.";
                return false;
            }

            // Look for existing dummy by name.
            GameObject dummy = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "TrainingDummy") { dummy = root; break; }
            }

            if (dummy == null)
            {
                dummy = new GameObject("TrainingDummy");
                Vector3 pos = playerGo != null ? playerGo.transform.position + new Vector3(3f, 0f, 0f) : Vector3.zero;
                dummy.transform.position = pos;
                Undo.RegisterCreatedObjectUndo(dummy, "Create Training Dummy");
            }

            if (groundLayer >= 0) dummy.layer = groundLayer;

            // Visual: SpriteRenderer with a borrowed sprite (any sprite from scene, or built-in fallback).
            var sr = dummy.GetComponent<SpriteRenderer>();
            if (sr == null) sr = dummy.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = FindAnyExistingSprite();
            sr.color = new Color(0.85f, 0.7f, 0.55f);  // sandbag tan

            // Solid body for the player to collide with.
            var body = dummy.GetComponent<BoxCollider2D>();
            if (body == null) body = dummy.AddComponent<BoxCollider2D>();
            body.isTrigger = false;
            if (sr.sprite != null) body.size = sr.sprite.bounds.size;

            // Rigidbody2D so Unity gravity + ground collision handle knockback arcs naturally.
            var rb = dummy.GetComponent<Rigidbody2D>();
            if (rb == null) rb = dummy.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.mass = 5f;
            rb.linearDamping = 1.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // DamageReceiver on root.
            var dr = dummy.GetComponent<DamageReceiver>();
            if (dr == null) dr = dummy.AddComponent<DamageReceiver>();
            var drSo = new SerializedObject(dr);
            drSo.FindProperty("visualRenderer").objectReferenceValue = sr;
            drSo.ApplyModifiedPropertiesWithoutUndo();
            receiver = dr;

            // Hurtbox child.
            Transform hurtboxChild = dummy.transform.Find("Hurtbox");
            GameObject hurtbox;
            if (hurtboxChild == null)
            {
                hurtbox = new GameObject("Hurtbox");
                hurtbox.transform.SetParent(dummy.transform, false);
                Undo.RegisterCreatedObjectUndo(hurtbox, "Create Hurtbox");
            }
            else hurtbox = hurtboxChild.gameObject;

            hurtbox.layer = hurtboxLayer;
            var hbCollider = hurtbox.GetComponent<BoxCollider2D>();
            if (hbCollider == null) hbCollider = hurtbox.AddComponent<BoxCollider2D>();
            hbCollider.isTrigger = true;
            if (sr.sprite != null)
                hbCollider.size = sr.sprite.bounds.size * 0.95f;  // slightly inset

            var hb = hurtbox.GetComponent<Hurtbox>();
            if (hb == null) hb = hurtbox.AddComponent<Hurtbox>();
            var hbSo = new SerializedObject(hb);
            hbSo.FindProperty("receiver").objectReferenceValue = dr;
            hbSo.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        // -------------------- Step F: damage HUD --------------------

        [MenuItem("Tools/ProjectCombat/Milestone 2/Step F - Create Damage HUD", false, 16)]
        public static void EnsureDamageHudMenu()
        {
            DamageReceiver receiver = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "TrainingDummy") { receiver = root.GetComponent<DamageReceiver>(); break; }

            EnsureDamageHud(receiver, out string warn);
            EditorUtility.DisplayDialog("Damage HUD",
                string.IsNullOrEmpty(warn) ? "HUD ready." : $"HUD warning: {warn}",
                "OK");
        }

        static bool EnsureDamageHud(DamageReceiver receiver, out string warning)
        {
            warning = "";
            if (receiver == null)
            {
                warning = "no training dummy found in scene.";
                return false;
            }

            // Find existing HUD by component.
            DamageHUD existing = Object.FindFirstObjectByType<DamageHUD>();
            if (existing != null)
            {
                var so = new SerializedObject(existing);
                so.FindProperty("source").objectReferenceValue = receiver;
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }

            // Create Canvas.
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
            }

            // Text + DamageHUD child.
            var textGo = new GameObject("DamageText", typeof(RectTransform), typeof(Text), typeof(DamageHUD));
            textGo.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(textGo, "Create Damage Text");

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(400f, 60f);

            var text = textGo.GetComponent<Text>();
            text.fontSize = 28;
            text.color = Color.white;
            text.text = "Damage: 0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var hud = textGo.GetComponent<DamageHUD>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("source").objectReferenceValue = receiver;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        // -------------------- Helpers --------------------

        static GameObject ResolvePlayer()
        {
            var go = Selection.activeGameObject;
            if (go != null && go.GetComponent<MovementController>() != null) return go;

            int playerLayer = LayerMask.NameToLayer(PlayerLayer);
            if (playerLayer >= 0)
            {
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (root.layer == playerLayer && root.GetComponent<MovementController>() != null) return root;
            }
            // Fall back to anything with a MovementController.
            var any = Object.FindFirstObjectByType<MovementController>();
            return any != null ? any.gameObject : null;
        }

        static Sprite FindAnyExistingSprite()
        {
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sr != null && sr.sprite != null) return sr.sprite;
            return null;
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
