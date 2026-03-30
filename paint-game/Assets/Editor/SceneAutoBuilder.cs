// SceneAutoBuilder.cs — Unity Editor tool that builds the complete PaintGame scene.
// Menu: PaintGame/Build Scene
// Tested against Unity 6000.0.x + Cinemachine 3.1.x + URP 17.x
//
// USAGE: Open any scene, click PaintGame → Build Scene.
//        The script clears the scene, creates all required GameObjects,
//        creates missing assets (PaintRT, Weapon SOs), and saves to
//        Assets/_Project/Scenes/Game.unity ready for Play.

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Cinemachine 3 types live in Unity.Cinemachine
using Unity.Cinemachine;

namespace PaintGame.Editor
{
    public static class SceneAutoBuilder
    {
        // ── Paths ─────────────────────────────────────────────────────────────
        private const string SCENE_PATH      = "Assets/_Project/Scenes/Game.unity";
        private const string RT_PATH         = "Assets/_Project/Textures/PaintRT.renderTexture";
        private const string SHOTGUN_PATH    = "Assets/_Project/ScriptableObjects/Shotgun.asset";
        private const string AK_PATH         = "Assets/_Project/ScriptableObjects/AK.asset";
        private const string PLAYER_PREFAB   = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string BOT_PREFAB      = "Assets/_Project/Prefabs/Player/Bot.prefab";
        private const string BULLET_PREFAB   = "Assets/_Project/Prefabs/FX/Bullet.prefab";
        private const string SPLAT_PREFAB    = "Assets/_Project/Prefabs/FX/Splat.prefab";
        private const string FLASH_PREFAB    = "Assets/_Project/Prefabs/FX/ImpactFlash.prefab";
        private const string BURST_PREFAB    = "Assets/_Project/Prefabs/FX/DeathBurst.prefab";
        private const string STAMP_MAT_PATH  = "Assets/_Project/Materials/StampMaterial.mat";
        private const string FLOOR_MAT_PATH  = "Assets/_Project/Materials/FloorPaintMaterial.mat";

        // ── Menu items ────────────────────────────────────────────────────────
        [MenuItem("PaintGame/Build Scene", priority = 1)]
        public static void BuildScene()
        {
            try
            {
                // Confirm with user if scene has unsaved changes
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                Debug.Log("[SceneAutoBuilder] Starting scene build...");

                // Fix Input System activeInputHandler setting if invalid (-1 = not set)
                FixInputHandlerSetting();

                EnsureFolders();
                EnsurePlayerPrefab();
                EnsureBotPrefab();
                var bulletPrefab = EnsureBulletPrefab();
                var splatPrefab  = EnsureSplatPrefab();
                var flashPrefab  = EnsureImpactFlashPrefab();
                var burstPrefab  = EnsureDeathBurstPrefab();

                // ── 1. Create / open a clean new scene ────────────────────────
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // ── 2. Assets that must exist before scene objects reference them
                var paintRT     = EnsureRenderTexture();
                var stampMat    = EnsureStampMaterial();
                var floorMat    = EnsureFloorMaterial(paintRT);
                var shotgunSO   = EnsureShotgunAsset();
                var akSO        = EnsureAKAsset();

                // ── 3. Floor Quad (1920×1920) ─────────────────────────────────
                var floorQuad   = CreateFloorQuad(floorMat);

                // ── 4. GameManager (root) + child systems ─────────────────────
                var gmGO        = CreateGameManager();

                // ── 5. TerritorySystem (child of GameManager) ─────────────────
                var terrGO      = CreateTerritorySystem(gmGO, paintRT, stampMat, floorQuad);

                // ── 6. PoolRegistry ───────────────────────────────────────────
                var poolGO      = CreatePoolRegistry(bulletPrefab, splatPrefab, flashPrefab, burstPrefab);

                // ── 7. PlayerSpawnManager ─────────────────────────────────────
                var spawnGO     = CreatePlayerSpawnManager(shotgunSO, akSO);

                // ── 8. Six Checkpoints ────────────────────────────────────────
                CreateCheckpoints();

                // ── 9. Camera ─────────────────────────────────────────────────
                CreateCameraRig();

                // ── 10. HUD Canvas ────────────────────────────────────────────
                CreateHUDCanvas();

                // ── 11. Directional light (needed by URP even in 2D) ──────────
                CreateDirectionalLight();

                // ── 12. Save scene ────────────────────────────────────────────
                EnsureDir(Path.GetDirectoryName(SCENE_PATH));
                EditorSceneManager.SaveScene(scene, SCENE_PATH);

                AssetDatabase.Refresh();
                Debug.Log($"[SceneAutoBuilder] Done. Scene saved to {SCENE_PATH}");
                EditorUtility.DisplayDialog("PaintGame", $"Scene built and saved to\n{SCENE_PATH}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneAutoBuilder] FAILED: {ex}");
                EditorUtility.DisplayDialog("PaintGame — Build Failed",
                    $"An error occurred:\n{ex.Message}\n\nCheck the Console for details.", "OK");
            }
        }

        [MenuItem("PaintGame/Create Player Prefab", priority = 2)]
        public static void CreatePlayerPrefab()
        {
            try
            {
                EnsureFolders();
                EnsurePlayerPrefab();
                EnsureBotPrefab();
                AssetDatabase.Refresh();
                Debug.Log("[SceneAutoBuilder] Player + Bot prefabs created.");
                EditorUtility.DisplayDialog("PaintGame", "Player and Bot prefabs created in\nAssets/_Project/Prefabs/Player/", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneAutoBuilder] CreatePlayerPrefab FAILED: {ex}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ASSET CREATION
        // ══════════════════════════════════════════════════════════════════════

        private static RenderTexture EnsureRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
            if (existing != null)
            {
                Debug.Log("[SceneAutoBuilder] PaintRT already exists, reusing.");
                return existing;
            }

            var rt = new RenderTexture(1920, 1920, 0, RenderTextureFormat.ARGB32)
            {
                name        = "PaintRT",
                filterMode  = FilterMode.Bilinear,
                wrapMode    = TextureWrapMode.Clamp,
                useMipMap   = false,
                autoGenerateMips = false,
            };
            rt.Create();
            AssetDatabase.CreateAsset(rt, RT_PATH);
            Debug.Log($"[SceneAutoBuilder] Created RenderTexture at {RT_PATH}");
            return rt;
        }

        private static Material EnsureStampMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(STAMP_MAT_PATH);
            if (existing != null) return existing;

            // Use the built-in Unlit/Color shader — opaque, no blending
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            var mat = new Material(shader) { name = "StampMaterial" };
            AssetDatabase.CreateAsset(mat, STAMP_MAT_PATH);
            return mat;
        }

        private static Material EnsureFloorMaterial(RenderTexture paintRT)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(FLOOR_MAT_PATH);
            if (existing != null)
            {
                // Keep the RT reference fresh
                existing.SetTexture("_MainTex",  paintRT);
                existing.SetTexture("_PaintRT",  paintRT);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Unlit shader that samples the render texture as the main texture
            var shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader) { name = "FloorPaintMaterial" };
            if (mat.HasProperty("_MainTex"))  mat.SetTexture("_MainTex",  paintRT);
            if (mat.HasProperty("_PaintRT"))  mat.SetTexture("_PaintRT",  paintRT);
            if (mat.HasProperty("_BaseMap"))  mat.SetTexture("_BaseMap",  paintRT);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(mat, FLOOR_MAT_PATH);
            return mat;
        }

        private static WeaponConfigSO EnsureShotgunAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WeaponConfigSO>(SHOTGUN_PATH);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<WeaponConfigSO>();
            so.name = "Shotgun";
            // Shotgun defaults (mirror WeaponConfigSO.SetShotgunDefaults)
            SetField(so, "weaponName",          "Shotgun");
            SetField(so, "moveSpeedMultiplier", 1.15f);
            SetField(so, "sprayHalfAngle",      36f * Mathf.Deg2Rad);
            SetField(so, "sprayRange",          160f);
            SetField(so, "bulletsPerShot",      5);
            SetField(so, "bulletSpreadDeg",     18f);
            SetField(so, "fireRate",            2.5f);
            SetField(so, "inkDrainPerSec",      80f);
            SetField(so, "bulletDamage",        1f);
            SetField(so, "bulletSpeed",         GameConstants.BULLET_SPEED);
            AssetDatabase.CreateAsset(so, SHOTGUN_PATH);
            return so;
        }

        private static WeaponConfigSO EnsureAKAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WeaponConfigSO>(AK_PATH);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<WeaponConfigSO>();
            so.name = "AK";
            SetField(so, "weaponName",          "AK");
            SetField(so, "moveSpeedMultiplier", 0.90f);
            SetField(so, "sprayHalfAngle",      15f * Mathf.Deg2Rad);
            SetField(so, "sprayRange",          280f);
            SetField(so, "bulletsPerShot",      1);
            SetField(so, "bulletSpreadDeg",     3f);
            SetField(so, "fireRate",            6f);
            SetField(so, "inkDrainPerSec",      40f);
            SetField(so, "bulletDamage",        1f);
            SetField(so, "bulletSpeed",         GameConstants.BULLET_SPEED);
            AssetDatabase.CreateAsset(so, AK_PATH);
            return so;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GAMEOBJECT CREATION
        // ══════════════════════════════════════════════════════════════════════

        private static GameObject CreateFloorQuad(Material floorMat)
        {
            // Unity's built-in Quad is 1×1 unit; scale to 1920×1920 world units
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FloorQuad";

            // Remove the 3D collider — this is a 2D game
            UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshCollider>());

            // Centre on the world (world is 0–1920 on X and Y)
            go.transform.position   = new Vector3(960f, 960f, 0f);
            go.transform.localScale = new Vector3(1920f, 1920f, 1f);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial  = floorMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows    = false;
            }

            return go;
        }

        private static GameObject CreateGameManager()
        {
            var go = new GameObject("GameManager");

            AddComponentSafe<GameManager>(go);
            // MatchManager and ScoreTracker are searched via GetComponentInChildren,
            // so add them directly on this GO as well.
            AddComponentSafe<MatchManager>(go);
            AddComponentSafe<ScoreTracker>(go);

            return go;
        }

        private static GameObject CreateTerritorySystem(GameObject parent,
            RenderTexture paintRT, Material stampMat, GameObject floorQuad)
        {
            var go = new GameObject("TerritorySystem");
            go.transform.SetParent(parent.transform);

            AddComponentSafe<TerritoryMap>(go);

            var renderer = AddComponentSafe<TerritoryRenderer>(go);
            if (renderer != null)
            {
                // Wire serialized fields via SerializedObject so Unity properly records them
                var so = new SerializedObject(renderer);
                so.FindProperty("_paintRT")     .objectReferenceValue = paintRT;
                so.FindProperty("_stampMaterial").objectReferenceValue = stampMat;
                so.FindProperty("_floorQuad")   .objectReferenceValue = floorQuad;
                so.ApplyModifiedProperties();
            }

            AddComponentSafe<SpawnZoneSeeder>(go);

            return go;
        }

        private static GameObject CreatePoolRegistry(BulletProjectile bulletPrefab,
            SplatEffect splatPrefab, ImpactFlash flashPrefab, DeathBurst burstPrefab)
        {
            var go = new GameObject("PoolRegistry");
            var registry = AddComponentSafe<PoolRegistry>(go);
            if (registry != null)
            {
                var so = new SerializedObject(registry);
                var bulletProp = so.FindProperty("_bulletPrefab");
                var splatProp  = so.FindProperty("_splatPrefab");
                var flashProp  = so.FindProperty("_flashPrefab");
                var burstProp  = so.FindProperty("_burstPrefab");
                if (bulletProp != null) bulletProp.objectReferenceValue = bulletPrefab;
                if (splatProp  != null) splatProp.objectReferenceValue  = splatPrefab;
                if (flashProp  != null) flashProp.objectReferenceValue  = flashPrefab;
                if (burstProp  != null) burstProp.objectReferenceValue  = burstPrefab;
                so.ApplyModifiedProperties();
            }
            return go;
        }

        private static GameObject CreatePlayerSpawnManager(WeaponConfigSO shotgun, WeaponConfigSO ak)
        {
            var go = new GameObject("PlayerSpawnManager");
            var psm = AddComponentSafe<PlayerSpawnManager>(go);
            if (psm != null)
            {
                var so = new SerializedObject(psm);
                // Wire weapon configs; prefab references must be set manually if prefabs exist
                var shotgunProp = so.FindProperty("_shotgunConfig");
                var akProp      = so.FindProperty("_akConfig");
                if (shotgunProp != null) shotgunProp.objectReferenceValue = shotgun;
                if (akProp != null)      akProp.objectReferenceValue = ak;

                // Wire prefabs if they already exist
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB);
                var botPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(BOT_PREFAB);
                if (playerPrefab != null)
                {
                    var playerProp = so.FindProperty("_playerPrefab");
                    if (playerProp != null)
                        playerProp.objectReferenceValue = playerPrefab.GetComponent<PlayerController>();
                }
                if (botPrefab != null)
                {
                    var botProp = so.FindProperty("_botPrefab");
                    if (botProp != null)
                        botProp.objectReferenceValue = botPrefab.GetComponent<PlayerController>();
                }

                so.ApplyModifiedProperties();
            }
            return go;
        }

        private static void CreateCheckpoints()
        {
            var checkpointsRoot = new GameObject("Checkpoints");

            for (int i = 0; i < GameConstants.SPAWN_TILES.Length; i++)
            {
                var spawnTile  = GameConstants.SPAWN_TILES[i];
                var worldPos   = GameConstants.TileToWorld(spawnTile.x, spawnTile.y);
                byte ownerIdx  = (byte)(i + 1);

                var go = new GameObject($"Checkpoint_{ownerIdx}");
                go.transform.SetParent(checkpointsRoot.transform);
                go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

                var cc = AddComponentSafe<CheckpointController>(go);
                if (cc != null)
                    cc.OwnerIndex = ownerIdx;

                var cv = AddComponentSafe<CheckpointVisuals>(go);

                // Small circle sprite placeholder (visible in editor)
                if (go.GetComponent<SpriteRenderer>() == null)
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.color = GameConstants.PLAYER_COLORS[ownerIdx];
                    sr.sprite = GetDefaultSprite();
                    sr.sortingOrder = 3;
                    go.transform.localScale = new Vector3(18f, 18f, 1f);
                }
            }
        }

        private static void CreateCameraRig()
        {
            // Main camera GO
            var mainCamGO = new GameObject("Main Camera");
            mainCamGO.tag = "MainCamera";
            var mainCam = mainCamGO.AddComponent<Camera>();
            mainCam.orthographic     = true;
            mainCam.orthographicSize = 78f;
            mainCam.clearFlags       = CameraClearFlags.SolidColor;
            mainCam.backgroundColor  = new Color(0.2f, 0.2f, 0.2f, 1f);
            mainCam.nearClipPlane    = -100f;
            mainCam.farClipPlane     = 100f;
            // Start at map centre
            mainCamGO.transform.position = new Vector3(960f, 960f, -10f);
            mainCamGO.AddComponent<AudioListener>();

            // Cinemachine Brain on the main camera
            AddComponentSafe<CinemachineBrain>(mainCamGO);

            // Cinemachine Camera (Cinemachine 3 replaces CinemachineVirtualCamera)
            var cmCamGO = new GameObject("CinemachineCamera");
            var cmCam = cmCamGO.AddComponent<CinemachineCamera>();
            cmCam.Lens = new LensSettings
            {
                OrthographicSize = 78f,
                NearClipPlane    = -100f,
                FarClipPlane     = 100f,
            };

            // In Cinemachine 3, position control is added via AddComponent
            // CinemachinePositionComposer is a CinemachineComponentBase
            cmCamGO.AddComponent<CinemachinePositionComposer>();

            // Confiner — requires a Collider2D; create a bounding box GO
            var confinerGO = new GameObject("CameraConfinerBounds");
            var poly       = confinerGO.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            float tileSize = GameConstants.TILE_SIZE;
            float pad = 4f * tileSize;
            float x0 = Mathf.Max(0, (52f - 28f) * tileSize - pad);
            float x1 = Mathf.Min(GameConstants.WORLD_W, (188f + 28f) * tileSize + pad);
            float y0 = Mathf.Max(0, (61f - 28f) * tileSize - pad);
            float y1 = Mathf.Min(GameConstants.WORLD_H, (179f + 28f) * tileSize + pad);
            poly.SetPath(0, new Vector2[]
            {
                new Vector2(x0, y0), new Vector2(x1, y0),
                new Vector2(x1, y1), new Vector2(x0, y1),
            });

            var confiner = cmCamGO.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = poly;

            // Impulse source on a dedicated CameraController GO
            var ctrlGO = new GameObject("CameraController");
            var controller = AddComponentSafe<CameraController>(ctrlGO);
            ctrlGO.AddComponent<CinemachineImpulseSource>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var vcamProp = so.FindProperty("_vcam");
                var normalOrthoProp = so.FindProperty("_normalOrthoSize");
                var deadOrthoProp   = so.FindProperty("_deadOrthoSize");
                if (vcamProp != null) vcamProp.objectReferenceValue = cmCam;
                if (normalOrthoProp != null) normalOrthoProp.floatValue = 78f;
                if (deadOrthoProp != null) deadOrthoProp.floatValue = 160f;
                so.ApplyModifiedProperties();
            }
        }

        private static void CreateHUDCanvas()
        {
            var canvasGO = new GameObject("HUD_Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var hud = AddComponentSafe<HUDController>(canvasGO);

            // Create child UI placeholder GameObjects so HUDController fields are wirable
            // (they are [SerializeField] and must be assigned; we create the GOs and
            // assign via SerializedObject so they show up correctly in the Inspector)
            var timerGO      = CreateUIChild(canvasGO, "TimerUI");
            var inkBarGO     = CreateUIChild(canvasGO, "InkBarUI");
            var hpDisplayGO  = CreateUIChild(canvasGO, "HPDisplayUI");
            var scoreboardGO = CreateUIChild(canvasGO, "ScoreboardUI");
            var countdownGO  = CreateUIChild(canvasGO, "CountdownUI");
            var killFeedGO   = CreateUIChild(canvasGO, "KillFeedUI");
            var mobileCtrlGO = CreateUIChild(canvasGO, "MobileControls");

            // Add stub UI components (so HUDController can find them)
            var timerComp     = AddComponentSafe<TimerUI>(timerGO);
            var inkBarComp    = AddComponentSafe<InkBarUI>(inkBarGO);
            var hpComp        = AddComponentSafe<HPDisplayUI>(hpDisplayGO);
            var scoreComp     = AddComponentSafe<ScoreboardUI>(scoreboardGO);
            var countdownComp = AddComponentSafe<CountdownUI>(countdownGO);
            var killFeedComp  = AddComponentSafe<KillFeedUI>(killFeedGO);

            // Wire references into HUDController
            if (hud != null)
            {
                var so = new SerializedObject(hud);
                so.FindProperty("_timerUI")      .objectReferenceValue = timerComp;
                so.FindProperty("_inkBarUI")     .objectReferenceValue = inkBarComp;
                so.FindProperty("_hpUI")         .objectReferenceValue = hpComp;
                so.FindProperty("_scoreboardUI") .objectReferenceValue = scoreComp;
                so.FindProperty("_countdownUI")  .objectReferenceValue = countdownComp;
                so.FindProperty("_killFeedUI")   .objectReferenceValue = killFeedComp;
                so.FindProperty("_mobileControls").objectReferenceValue = mobileCtrlGO;
                so.ApplyModifiedProperties();
            }

            // Disable mobile controls by default in editor
            mobileCtrlGO.SetActive(false);
        }

        private static void CreateDirectionalLight()
        {
            var go    = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PREFAB CREATION
        // ══════════════════════════════════════════════════════════════════════

        private static void EnsurePlayerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB) != null)
            {
                AssetDatabase.DeleteAsset(PLAYER_PREFAB);
                Debug.Log("[SceneAutoBuilder] Rebuilding Player prefab.");
            }

            EnsureDir("Assets/_Project/Prefabs/Player");

            var go = new GameObject("Player");

            // Body sprite renderer
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(go.transform);
            var sr = bodyGO.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 5;
            sr.color            = Color.red;
            sr.sprite           = GetDefaultSprite();
            bodyGO.transform.localScale = new Vector3(12f, 12f, 1f);

            // Gun pivot child
            var pivotGO = new GameObject("GunPivot");
            pivotGO.transform.SetParent(go.transform);
            var gunSR   = new GameObject("GunSprite");
            gunSR.transform.SetParent(pivotGO.transform);
            gunSR.transform.localPosition = new Vector3(9f, 0f, 0f);
            var gunRenderer = gunSR.AddComponent<SpriteRenderer>();
            gunRenderer.color = Color.grey;
            gunRenderer.sprite = GetDefaultSprite();
            gunRenderer.sortingOrder = 6;
            gunSR.transform.localScale = new Vector3(8f, 1.8f, 1f);

            // Components
            AddComponentSafe<PlayerController>(go);
            AddComponentSafe<PlayerStats>(go);
            var visuals = AddComponentSafe<PlayerVisuals>(go);
            AddComponentSafe<ShotgunWeapon>(go);
            AddComponentSafe<AimConeRenderer>(go);

            if (visuals != null)
            {
                var vso = new SerializedObject(visuals);
                var bodyProp = vso.FindProperty("_body");
                var pivotProp = vso.FindProperty("_gunPivot");
                var gunProp = vso.FindProperty("_gunRenderer");
                if (bodyProp != null) bodyProp.objectReferenceValue = sr;
                if (pivotProp != null) pivotProp.objectReferenceValue = pivotGO.transform;
                if (gunProp != null) gunProp.objectReferenceValue = gunRenderer;
                vso.ApplyModifiedProperties();
            }

            // Input — add PlayerInput (Unity's new input system component)
            // PlayerInput here refers to our custom PaintGame.PlayerInput
            AddComponentSafe<PlayerInput>(go);

            // Rigidbody2D (kinematic) for layer-based collision queries
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType         = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // CircleCollider2D (radius matches half-size in PlayerController = 3.5 units)
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 3.5f;

            PrefabUtility.SaveAsPrefabAsset(go, PLAYER_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[SceneAutoBuilder] Player prefab saved to {PLAYER_PREFAB}");
        }

        private static void EnsureBotPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BOT_PREFAB) != null)
            {
                AssetDatabase.DeleteAsset(BOT_PREFAB);
                Debug.Log("[SceneAutoBuilder] Rebuilding Bot prefab.");
            }

            EnsureDir("Assets/_Project/Prefabs/Player");

            var go = new GameObject("Bot");

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(go.transform);
            var sr = bodyGO.AddComponent<SpriteRenderer>();
            sr.color = Color.blue;
            sr.sprite = GetDefaultSprite();
            sr.sortingOrder = 5;
            bodyGO.transform.localScale = new Vector3(12f, 12f, 1f);

            var pivotGO = new GameObject("GunPivot");
            pivotGO.transform.SetParent(go.transform);
            var gunGO = new GameObject("GunSprite");
            gunGO.transform.SetParent(pivotGO.transform);
            gunGO.transform.localPosition = new Vector3(9f, 0f, 0f);
            var gunRenderer = gunGO.AddComponent<SpriteRenderer>();
            gunRenderer.color = Color.grey;
            gunRenderer.sprite = GetDefaultSprite();
            gunRenderer.sortingOrder = 6;
            gunGO.transform.localScale = new Vector3(8f, 1.8f, 1f);

            AddComponentSafe<PlayerController>(go);
            AddComponentSafe<PlayerStats>(go);
            var visuals = AddComponentSafe<PlayerVisuals>(go);
            AddComponentSafe<ShotgunWeapon>(go);
            AddComponentSafe<AimConeRenderer>(go);
            AddComponentSafe<BotController>(go);

            if (visuals != null)
            {
                var vso = new SerializedObject(visuals);
                var bodyProp = vso.FindProperty("_body");
                var pivotProp = vso.FindProperty("_gunPivot");
                var gunProp = vso.FindProperty("_gunRenderer");
                if (bodyProp != null) bodyProp.objectReferenceValue = sr;
                if (pivotProp != null) pivotProp.objectReferenceValue = pivotGO.transform;
                if (gunProp != null) gunProp.objectReferenceValue = gunRenderer;
                vso.ApplyModifiedProperties();
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 3.5f;

            PrefabUtility.SaveAsPrefabAsset(go, BOT_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[SceneAutoBuilder] Bot prefab saved to {BOT_PREFAB}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Adds a component only if one does not already exist on the GO.
        /// Returns the existing or newly added component.
        /// </summary>
        private static T AddComponentSafe<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;
            try
            {
                return go.AddComponent<T>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneAutoBuilder] Could not add {typeof(T).Name} to {go.name}: {ex.Message}");
                return null;
            }
        }

        private static GameObject CreateUIChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(200f, 50f);
            return go;
        }

        /// <summary>Reflect-set a public or serialized field on a ScriptableObject.</summary>
        private static void SetField(ScriptableObject so, string fieldName, object value)
        {
            var field = so.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(so, value);
            else
                Debug.LogWarning($"[SceneAutoBuilder] Field '{fieldName}' not found on {so.GetType().Name}");
        }

        private static void FixInputHandlerSetting()
        {
            // activeInputHandler: 0=Legacy, 1=New, 2=Both, -1=unset (causes crash)
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue < 0)
            {
                prop.intValue = 2; // Both
                so.ApplyModifiedProperties();
                Debug.Log("[SceneAutoBuilder] Fixed activeInputHandler → Both (2)");
            }
        }

        private static void EnsureFolders()
        {
            EnsureDir("Assets/_Project/Textures");
            EnsureDir("Assets/_Project/Materials");
            EnsureDir("Assets/_Project/Scenes");
            EnsureDir("Assets/_Project/ScriptableObjects");
            EnsureDir("Assets/_Project/Prefabs");
            EnsureDir("Assets/_Project/Prefabs/Player");
            EnsureDir("Assets/_Project/Prefabs/FX");
        }

        private static BulletProjectile EnsureBulletPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BULLET_PREFAB);
            if (existing != null) return existing.GetComponent<BulletProjectile>();

            EnsureDir("Assets/_Project/Prefabs/FX");
            var go = new GameObject("Bullet");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.sortingOrder = 8;
            go.AddComponent<BulletProjectile>();
            PrefabUtility.SaveAsPrefabAsset(go, BULLET_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BULLET_PREFAB).GetComponent<BulletProjectile>();
        }

        private static SplatEffect EnsureSplatPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SPLAT_PREFAB);
            if (existing != null) return existing.GetComponent<SplatEffect>();

            EnsureDir("Assets/_Project/Prefabs/FX");
            var go = new GameObject("Splat");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.sortingOrder = 7;
            go.AddComponent<SplatEffect>();
            PrefabUtility.SaveAsPrefabAsset(go, SPLAT_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(SPLAT_PREFAB).GetComponent<SplatEffect>();
        }

        private static ImpactFlash EnsureImpactFlashPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FLASH_PREFAB);
            if (existing != null) return existing.GetComponent<ImpactFlash>();

            EnsureDir("Assets/_Project/Prefabs/FX");
            var go = new GameObject("ImpactFlash");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            sr.sortingOrder = 9;
            go.AddComponent<ImpactFlash>();
            PrefabUtility.SaveAsPrefabAsset(go, FLASH_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(FLASH_PREFAB).GetComponent<ImpactFlash>();
        }

        private static DeathBurst EnsureDeathBurstPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BURST_PREFAB);
            if (existing != null) return existing.GetComponent<DeathBurst>();

            EnsureDir("Assets/_Project/Prefabs/FX");
            var go = new GameObject("DeathBurst");
            go.AddComponent<ParticleSystem>();
            go.AddComponent<DeathBurst>();
            PrefabUtility.SaveAsPrefabAsset(go, BURST_PREFAB);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BURST_PREFAB).GetComponent<DeathBurst>();
        }

        private static Sprite GetDefaultSprite()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (sprite == null) sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            return sprite;
        }

        private static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            // Walk up and create each segment
            var parts  = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
