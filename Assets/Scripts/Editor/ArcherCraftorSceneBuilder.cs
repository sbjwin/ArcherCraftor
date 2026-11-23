using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using ArcherCraftor.Combat;
using ArcherCraftor.Player;
using ArcherCraftor.Enemy;
using ArcherCraftor.Core;
using ArcherCraftor.Crafting;
using ArcherCraftor.UI;

namespace ArcherCraftor.Editor
{
    /// <summary>
    /// 최소 구성(플레이어 + 고정된 적 1체) 2D 테스트 샌드박스 씬 빌더
    /// </summary>
    public static class ArcherCraftorSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeSandbox.unity";
        private const string SpritesDir = "Assets/Sprites";
        private const string PrefabsDir = "Assets/Prefabs";

        [MenuItem("ArcherCraftor/Setup Prototype Sandbox Scene")]
        public static void SetupPrototypeScene()
        {
            BuildSceneInternal(showDialog: true);
        }

        public static void SetupPrototypeSceneBatch()
        {
            BuildSceneInternal(showDialog: false);
        }

        private static void BuildSceneInternal(bool showDialog)
        {
            EnsureDirectories();
            EnsureTagExists("Player");
            EnsureTagExists("Enemy");
            EnsureTagExists("Obstacle");
            EnsureURPPipelineAsset();

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. 기본 2D 스프라이트 생성
            Sprite groundGridSprite = CreateGridFloorSprite("Spr_GroundTile", 64);
            Sprite playerBodySprite = CreateArcherBodySprite("Spr_PlayerBody", 32);
            Sprite bowSprite = CreateBowSprite("Spr_Bow", 32);
            Sprite quiverSprite = CreateDiamondSprite("Spr_QuiverGlow", 16, Color.white);
            Sprite arrowSprite = CreateArrowSprite("Spr_Arrow", 32);
            Sprite enemySprite = CreateEnemyGolemSprite("Spr_EnemyGolem", 32);
            Sprite bulletSprite = CreateCircleSprite("Spr_Bullet", 24, Color.white, outline: new Color(1f, 0.65f, 0.65f));
            Sprite circleSprite = CreateCircleSprite("Spr_Circle", 32, Color.white, outline: Color.clear);
            Sprite ringSprite = CreateRingSprite("Spr_Ring", 64, thickness: 6);

            // 2. 바닥 격자 & 매니저
            CreateGridFloor(groundGridSprite);
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            gmObj.AddComponent<FieldCraftingManager>();

            // 3. 프리팹 생성
            GameObject sparkPrefab = CreateSparkVFXPrefab();
            GameObject bulletPrefab = CreateEnemyBulletPrefab(bulletSprite, sparkPrefab);
            GameObject arrowPrefab = CreateArrowPrefab(arrowSprite, sparkPrefab, circleSprite);

            // 4. 플레이어 (노란색 FOV 범위 미표시)
            GameObject player = CreatePlayerObject(playerBodySprite, bowSprite, quiverSprite, arrowPrefab);

            // 5. 2D 직교 카메라
            CreateTopDown2DCamera(player);

            // 6. 고정된 적 1체 (5초마다 1발 조준 사격)
            CreateSingleStationaryEnemy(enemySprite, bulletPrefab);

            // 7. 최소 조준선 UI
            CreateMinimalUI(player, circleSprite, ringSprite);

            // 8. 씬 저장 및 빌드 등록
            EditorSceneManager.SaveScene(newScene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("ArcherCraftor 2D Minimal Scene",
                    "단순화된 2D 테스트 씬(PrototypeSandbox.unity) 생성이 완료되었습니다!\n\n" +
                    "- 플레이어와 고정된 적 1체만 배치됩니다.\n" +
                    "- 적은 이동하지 않으며 5초마다 1발씩 사격합니다.\n" +
                    "- 항상 보이던 노란색 시야 범위는 제거되었습니다.", "확인");
            }
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(SpritesDir)) AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(PrefabsDir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static void CreateGridFloor(Sprite groundGridSprite)
        {
            GameObject ground = new GameObject("Ground_Grid");
            ground.transform.position = Vector3.zero;
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = groundGridSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(80f, 80f);
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 0;
        }

        private static GameObject CreateSparkVFXPrefab()
        {
            GameObject spark = new GameObject("FX_ParrySpark");
            var ps = spark.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.35f;
            main.startLifetime = 0.28f;
            main.startSpeed = 7f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.88f, 0.25f);
            main.loop = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

            if (spark.TryGetComponent<ParticleSystemRenderer>(out var psr))
            {
                psr.sortingLayerName = "Effects";
                psr.sortingOrder = 10;
            }

            spark.AddComponent<ImpactEffect>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(spark, $"{PrefabsDir}/FX_ParrySpark.prefab");
            Object.DestroyImmediate(spark);
            return prefab;
        }

        private static GameObject CreateEnemyBulletPrefab(Sprite bulletSprite, GameObject sparkPrefab)
        {
            GameObject bulletObj = new GameObject("EnemyBullet");
            int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
            if (bulletLayer >= 0) bulletObj.layer = bulletLayer;

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.sprite = bulletSprite;
            sr.color = new Color(1f, 0.24f, 0.24f);
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 5;
            bulletObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.36f;

            var bullet = bulletObj.AddComponent<EnemyBullet>();
            SerializedObject so = new SerializedObject(bullet);
            so.FindProperty("parrySparkEffectPrefab").objectReferenceValue = sparkPrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, $"{PrefabsDir}/EnemyBullet.prefab");
            Object.DestroyImmediate(bulletObj);
            return prefab;
        }

        private static GameObject CreateArrowPrefab(Sprite arrowSprite, GameObject sparkPrefab, Sprite circleSprite)
        {
            GameObject arrowObj = new GameObject("ArrowProjectile");
            int arrowLayer = LayerMask.NameToLayer("PlayerArrow");
            if (arrowLayer >= 0) arrowObj.layer = arrowLayer;

            var sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.color = new Color(0.75f, 0.96f, 1f);
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 8;

            var col = arrowObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f;

            var arrow = arrowObj.AddComponent<ArrowProjectile>();
            SerializedObject so = new SerializedObject(arrow);
            so.FindProperty("parrySparkPrefab").objectReferenceValue = sparkPrefab;
            so.FindProperty("zoneSprite").objectReferenceValue = circleSprite;
            int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
            if (bulletLayer >= 0)
            {
                so.FindProperty("enemyBulletLayer").intValue = 1 << bulletLayer;
            }
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(arrowObj, $"{PrefabsDir}/ArrowProjectile.prefab");
            Object.DestroyImmediate(arrowObj);
            return prefab;
        }

        private static GameObject CreatePlayerObject(Sprite bodySprite, Sprite bowSprite, Sprite quiverSprite, GameObject arrowPrefab)
        {
            GameObject player = new GameObject("Player_Archer");
            player.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) player.layer = playerLayer;
            player.transform.position = Vector3.zero;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;

            player.AddComponent<Health>();
            var controller = player.AddComponent<PlayerController>();
            var bow = player.AddComponent<PlayerBow>();

            GameObject bodyObj = new GameObject("BodySprite");
            bodyObj.transform.parent = player.transform;
            bodyObj.transform.localPosition = Vector3.zero;
            var bodySr = bodyObj.AddComponent<SpriteRenderer>();
            bodySr.sprite = bodySprite;
            bodySr.sortingLayerName = "Actors";
            bodySr.sortingOrder = 10;

            GameObject quiverObj = new GameObject("QuiverGlow");
            quiverObj.transform.parent = player.transform;
            quiverObj.transform.localPosition = new Vector3(-0.22f, 0.18f, 0f);
            quiverObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            var quiverSr = quiverObj.AddComponent<SpriteRenderer>();
            quiverSr.sprite = quiverSprite;
            quiverSr.color = ArrowVisualProfile.GetArrowColor(ArrowType.Impact);
            quiverSr.sortingLayerName = "Actors";
            quiverSr.sortingOrder = 9;

            GameObject bowPivot = new GameObject("BowPivot");
            bowPivot.transform.parent = player.transform;
            bowPivot.transform.localPosition = Vector3.zero;

            GameObject bowObj = new GameObject("BowSprite");
            bowObj.transform.parent = bowPivot.transform;
            bowObj.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            var bowSr = bowObj.AddComponent<SpriteRenderer>();
            bowSr.sprite = bowSprite;
            bowSr.sortingLayerName = "Actors";
            bowSr.sortingOrder = 11;

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.parent = bowPivot.transform;
            firePoint.transform.localPosition = new Vector3(0.85f, 0f, 0f);

            GameObject scarfObj = new GameObject("Scarf_Verlet");
            scarfObj.transform.parent = player.transform;
            scarfObj.transform.localPosition = Vector3.zero;
            var scarfLine = scarfObj.AddComponent<LineRenderer>();
            scarfLine.startWidth = 0.2f;
            scarfLine.endWidth = 0.06f;
            scarfLine.numCapVertices = 4;
            scarfLine.material = new Material(Shader.Find("Sprites/Default"));
            scarfLine.startColor = new Color(0.18f, 0.85f, 0.55f, 0.95f);
            scarfLine.endColor = new Color(0.65f, 1f, 0.78f, 0.45f);
            scarfLine.sortingLayerName = "Actors";
            scarfLine.sortingOrder = 8;
            scarfObj.AddComponent<ScarfVerlet2D>();

            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("bodyRenderer").objectReferenceValue = bodySr;
            ctrlSo.FindProperty("bowPivot").objectReferenceValue = bowPivot.transform;
            ctrlSo.FindProperty("bowRenderer").objectReferenceValue = bowSr;
            ctrlSo.FindProperty("quiverGlowRenderer").objectReferenceValue = quiverSr;
            ctrlSo.ApplyModifiedProperties();

            SerializedObject bowSo = new SerializedObject(bow);
            bowSo.FindProperty("arrowPrefab").objectReferenceValue = arrowPrefab;
            bowSo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            bowSo.ApplyModifiedProperties();

            return player;
        }

        private static void CreateTopDown2DCamera(GameObject player)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8.5f;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            camObj.AddComponent<UniversalAdditionalCameraData>();
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<TopDownCameraFollow>();
            camObj.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        }

        private static void CreateSingleStationaryEnemy(Sprite enemySprite, GameObject bulletPrefab)
        {
            Vector3 spawnPos = new Vector3(6f, 0f, 0f);

            GameObject enemyTemplate = new GameObject("Enemy_Golem");
            enemyTemplate.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) enemyTemplate.layer = enemyLayer;

            var sr = enemyTemplate.AddComponent<SpriteRenderer>();
            sr.sprite = enemySprite;
            sr.sortingLayerName = "Actors";
            sr.sortingOrder = 9;

            var rb = enemyTemplate.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            var col = enemyTemplate.AddComponent<CircleCollider2D>();
            col.radius = 0.55f;

            enemyTemplate.AddComponent<Health>();
            var emitter = enemyTemplate.AddComponent<EnemyBulletEmitter>();
            var ai = enemyTemplate.AddComponent<EnemyAI>();

            SerializedObject emitterSo = new SerializedObject(emitter);
            emitterSo.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
            emitterSo.ApplyModifiedProperties();

            SerializedObject aiSo = new SerializedObject(ai);
            aiSo.FindProperty("isStationary").boolValue = true;
            aiSo.FindProperty("detectionRadius").floatValue = 30f;
            aiSo.FindProperty("attackRadius").floatValue = 30f;
            aiSo.FindProperty("attackInterval").floatValue = 5.0f;
            aiSo.FindProperty("defaultPattern").enumValueIndex = (int)EnemyAI.AttackPattern.AimShot;
            aiSo.ApplyModifiedProperties();

            GameObject enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyTemplate, $"{PrefabsDir}/Enemy_Golem.prefab");
            Object.DestroyImmediate(enemyTemplate);

            GameObject initialEnemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
            initialEnemy.transform.position = spawnPos;

            GameObject spawnerObj = new GameObject("Enemy_Spawner");
            spawnerObj.transform.position = spawnPos;
            var respawner = spawnerObj.AddComponent<EnemyRespawner2D>();
            SerializedObject respSo = new SerializedObject(respawner);
            respSo.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            respSo.FindProperty("currentEnemy").objectReferenceValue = initialEnemy;
            respSo.FindProperty("respawnDelay").floatValue = 3.0f;
            respSo.ApplyModifiedProperties();
        }

        private static void CreateMinimalUI(GameObject player, Sprite circleSprite, Sprite ringSprite)
        {
            GameObject canvasObj = new GameObject("InGame_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 마우스 크로스헤어 & 차징 링만 유지 (항상 보이는 범위 링/가이드 제거)
            GameObject crosshairObj = new GameObject("AimCrosshair", typeof(RectTransform));
            crosshairObj.transform.SetParent(canvasObj.transform, false);
            var crosshairRect = crosshairObj.GetComponent<RectTransform>();
            crosshairRect.sizeDelta = new Vector2(44f, 44f);

            var centerDot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            centerDot.transform.SetParent(crosshairObj.transform, false);
            centerDot.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 8f);
            var dotImg = centerDot.GetComponent<Image>();
            dotImg.sprite = circleSprite;
            dotImg.color = Color.white;
            dotImg.raycastTarget = false;

            var chargeRingObj = new GameObject("ChargeRing", typeof(RectTransform), typeof(Image));
            chargeRingObj.transform.SetParent(crosshairObj.transform, false);
            chargeRingObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
            var chargeImg = chargeRingObj.GetComponent<Image>();
            chargeImg.sprite = ringSprite;
            chargeImg.type = Image.Type.Filled;
            chargeImg.fillMethod = Image.FillMethod.Radial360;
            chargeImg.fillAmount = 0f;
            chargeImg.raycastTarget = false;

            GameObject aimConeObj = new GameObject("AimConeGuide");
            var aimConeLine = aimConeObj.AddComponent<LineRenderer>();
            aimConeLine.startWidth = 0.05f;
            aimConeLine.endWidth = 0.05f;
            aimConeLine.material = new Material(Shader.Find("Sprites/Default"));
            aimConeLine.sortingLayerName = "GroundDecal";
            aimConeLine.sortingOrder = 3;
            aimConeLine.enabled = false;

            var aimUI = canvasObj.AddComponent<AimReticleUI>();
            SerializedObject aimSo = new SerializedObject(aimUI);
            aimSo.FindProperty("playerBow").objectReferenceValue = player.GetComponent<PlayerBow>();
            aimSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
            aimSo.FindProperty("crosshairRect").objectReferenceValue = crosshairRect;
            aimSo.FindProperty("chargeFillImage").objectReferenceValue = chargeImg;
            aimSo.FindProperty("aimConeLine").objectReferenceValue = aimConeLine;
            aimSo.ApplyModifiedProperties();
        }

        #region Procedural 2D Sprite Generators

        private static Sprite SaveAndLoadSprite(string name, Texture2D tex, float pixelsPerUnit = 32f)
        {
            string path = $"{SpritesDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreateGridFloorSprite(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color cellA = new Color(0.13f, 0.15f, 0.18f);
            Color cellB = new Color(0.15f, 0.17f, 0.21f);
            Color gridLine = new Color(0.24f, 0.28f, 0.34f);

            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = x == 0 || y == 0 || x == half || y == half;
                    bool checker = ((x / half) + (y / half)) % 2 == 0;
                    tex.SetPixel(x, y, isLine ? gridLine : (checker ? cellA : cellB));
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex, pixelsPerUnit: 32f);
        }

        private static Sprite CreateArcherBodySprite(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color cloak = new Color(0.2f, 0.72f, 0.42f);
            Color hood = new Color(0.14f, 0.52f, 0.3f);
            Color face = new Color(0.95f, 0.85f, 0.72f);

            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / (size * 0.35f);
                    float dy = (y - c) / (size * 0.42f);
                    float d = dx * dx + dy * dy;

                    if (d > 1.0f) tex.SetPixel(x, y, clear);
                    else if (y > size * 0.65f) tex.SetPixel(x, y, hood);
                    else if (y > size * 0.48f && Mathf.Abs(x - c) < size * 0.18f) tex.SetPixel(x, y, face);
                    else tex.SetPixel(x, y, cloak);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        private static Sprite CreateEnemyGolemSprite(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color body = new Color(0.75f, 0.25f, 0.25f);
            Color core = new Color(1f, 0.45f, 0.2f);
            Color dark = new Color(0.35f, 0.12f, 0.14f);

            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - c) / (size * 0.42f);
                    float dy = Mathf.Abs(y - c) / (size * 0.42f);
                    if (dx > 1f || dy > 1f || dx + dy > 1.55f)
                        tex.SetPixel(x, y, clear);
                    else if (dx < 0.22f && dy < 0.22f)
                        tex.SetPixel(x, y, core);
                    else if (dx > 0.85f || dy > 0.85f)
                        tex.SetPixel(x, y, dark);
                    else
                        tex.SetPixel(x, y, body);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        private static Sprite CreateBowSprite(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color wood = new Color(0.82f, 0.6f, 0.32f);
            Color stringCol = new Color(0.85f, 0.96f, 1f, 0.8f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ny = (y - size * 0.5f) / (size * 0.42f);
                    float arcX = size * 0.65f - (ny * ny) * (size * 0.35f);
                    if (Mathf.Abs(ny) <= 1f && Mathf.Abs(x - arcX) <= 2.2f)
                        tex.SetPixel(x, y, wood);
                    else if (Mathf.Abs(ny) <= 0.95f && Mathf.Abs(x - size * 0.3f) <= 0.8f)
                        tex.SetPixel(x, y, stringCol);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        private static Sprite CreateArrowSprite(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            int mid = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dy = Mathf.Abs(y - mid);
                    bool isShaft = x >= 4 && x < size - 6 && dy <= 1;
                    bool isTip = x >= size - 9 && dy <= (size - 1 - x) / 2;
                    bool isFletch = x >= 2 && x <= 7 && dy <= 3 && dy >= 1;
                    tex.SetPixel(x, y, (isShaft || isTip || isFletch) ? Color.white : clear);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        private static Sprite CreateCircleSprite(string name, int size, Color fill, Color outline)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            float c = (size - 1) * 0.5f;
            float r = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d > r) tex.SetPixel(x, y, clear);
                    else if (outline.a > 0f && d > r - 2.5f) tex.SetPixel(x, y, outline);
                    else tex.SetPixel(x, y, fill);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        private static Sprite CreateRingSprite(string name, int size, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            float c = (size - 1) * 0.5f;
            float outer = size * 0.47f;
            float inner = outer - thickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, (d <= outer && d >= inner) ? Color.white : clear);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex, pixelsPerUnit: 64f);
        }

        private static Sprite CreateDiamondSprite(string name, int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            float c = (size - 1) * 0.5f;
            float r = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float manhattan = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                    tex.SetPixel(x, y, manhattan <= r ? fill : clear);
                }
            }
            tex.Apply();
            return SaveAndLoadSprite(name, tex);
        }

        #endregion

        #region Project Settings Helpers

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
            {
                if (s.path == scenePath)
                {
                    s.enabled = true;
                    EditorBuildSettings.scenes = existing;
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            updated[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < existing.Length; i++)
            {
                updated[i + 1] = existing[i];
            }
            EditorBuildSettings.scenes = updated;
        }

        private static void EnsureTagExists(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                {
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }

        private static void EnsureURPPipelineAsset()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existingUrp)
            {
                if (QualitySettings.renderPipeline == null)
                {
                    QualitySettings.renderPipeline = existingUrp;
                }
                return;
            }

            const string urpAssetPath = "Assets/UniversalRenderPipelineAsset.asset";
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
                urpAsset.LoadBuiltinRendererData();
                EditorUtility.SetDirty(urpAsset);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
        }

        #endregion
    }
}
