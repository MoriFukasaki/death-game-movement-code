using System.IO;
using MetroidvaniaMVP.Enemy;
using MetroidvaniaMVP.Level;
using MetroidvaniaMVP.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MetroidvaniaMVP.Editor
{
    public static class MetroidvaniaMvpSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Metroidvania_MVP.unity";
        private const string SquareSpritePath = "Assets/Art/Placeholder/Square.png";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/EnemyPatrol.prefab";
        private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";
        private const string NoFrictionMaterialPath = "Assets/Physics/NoFriction.physicsMaterial2D";

        [MenuItem("Tools/Metroidvania MVP/Build Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before rebuilding the Metroidvania MVP scene.");
                return;
            }

            EnsureFolders();

            int groundLayer = EnsureLayer("Ground");
            int playerLayer = EnsureLayer("Player");
            int enemyLayer = EnsureLayer("Enemy");
            int checkpointLayer = EnsureLayer("Checkpoint");

            Sprite squareSprite = EnsureSquareSprite();
            PhysicsMaterial2D noFrictionMaterial = EnsureNoFrictionMaterial();
            LayerMask groundMask = LayerMask.GetMask("Ground");
            LayerMask enemyMask = LayerMask.GetMask("Enemy");

            GameObject playerPrefab = CreatePlayerPrefab(squareSprite, playerLayer, groundMask, enemyMask, noFrictionMaterial);
            GameObject enemyPrefab = CreateEnemyPrefab(squareSprite, enemyLayer);
            GameObject checkpointPrefab = CreateCheckpointPrefab(squareSprite, checkpointLayer);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Metroidvania_MVP";

            GameObject levelRoot = new GameObject("Level");
            GameObject gameplayRoot = new GameObject("Gameplay");
            GameObject groundRoot = new GameObject("Ground And Platforms");
            GameObject gateRoot = new GameObject("Ability Gates");
            GameObject checkpointRoot = new GameObject("Checkpoints");
            GameObject enemyRoot = new GameObject("Enemies");
            GameObject patrolPointRoot = new GameObject("Patrol Points");
            GameObject hazardRoot = new GameObject("Hazards");
            GameObject systemsRoot = new GameObject("Game Systems");

            groundRoot.transform.SetParent(levelRoot.transform);
            gateRoot.transform.SetParent(levelRoot.transform);
            checkpointRoot.transform.SetParent(gameplayRoot.transform);
            enemyRoot.transform.SetParent(gameplayRoot.transform);
            patrolPointRoot.transform.SetParent(gameplayRoot.transform);
            hazardRoot.transform.SetParent(gameplayRoot.transform);

            BuildLevelGeometry(squareSprite, groundLayer, groundRoot.transform, gateRoot.transform, noFrictionMaterial);
            CreateFallRespawnBarrier(hazardRoot.transform);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(-8f, -0.55f, 0f);
            player.transform.SetParent(gameplayRoot.transform);

            GameObject checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(checkpointPrefab);
            checkpoint.name = "Checkpoint_01";
            checkpoint.transform.position = new Vector3(2.7f, -0.65f, 0f);
            checkpoint.transform.SetParent(checkpointRoot.transform);

            GameObject leftPatrolPoint = CreateMarker("Enemy_01_LeftPoint", new Vector3(12.7f, -0.7f, 0f), patrolPointRoot.transform);
            GameObject rightPatrolPoint = CreateMarker("Enemy_01_RightPoint", new Vector3(16.8f, -0.7f, 0f), patrolPointRoot.transform);

            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
            enemy.name = "Enemy_Patrol_01";
            enemy.transform.position = new Vector3(14.2f, -0.65f, 0f);
            enemy.transform.SetParent(enemyRoot.transform);
            enemy.GetComponent<EnemyPatrol>().Configure(enemy.GetComponent<Rigidbody2D>(), leftPatrolPoint.transform, rightPatrolPoint.transform);

            BuildCameraAndLighting(player.transform, systemsRoot.transform);

            Selection.activeObject = player;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Metroidvania MVP/Stop Play Mode")]
        public static void StopPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scripts/Core",
                "Assets/Scripts/Player",
                "Assets/Scripts/Enemy",
                "Assets/Scripts/Level",
                "Assets/Scripts/Editor",
                "Assets/Prefabs",
                "Assets/Scenes",
                "Assets/Art",
                "Assets/Art/Placeholder",
                "Assets/Physics"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static Sprite EnsureSquareSprite()
        {
            if (!File.Exists(SquareSpritePath))
            {
                Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(SquareSpritePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        private static PhysicsMaterial2D EnsureNoFrictionMaterial()
        {
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("NoFriction");
                AssetDatabase.CreateAsset(material, NoFrictionMaterialPath);
            }

            material.friction = 0f;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static GameObject CreatePlayerPrefab(Sprite squareSprite, int playerLayer, LayerMask groundMask, LayerMask enemyMask, PhysicsMaterial2D noFrictionMaterial)
        {
            GameObject player = new GameObject("Player");
            player.layer = playerLayer;
            player.transform.localScale = new Vector3(0.8f, 1.4f, 1f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(0.25f, 0.55f, 1f, 1f);
            renderer.sortingOrder = 10;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.4f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = noFrictionMaterial;

            AbilityController abilities = player.AddComponent<AbilityController>();
            PlayerController controller = player.AddComponent<PlayerController>();
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            PlayerCombat combat = player.AddComponent<PlayerCombat>();

            Transform groundCheck = CreateChild(player.transform, "GroundCheck", new Vector3(0f, -0.54f, 0f));
            Transform wallCheck = CreateChild(player.transform, "WallCheck", new Vector3(0.58f, 0f, 0f));
            Transform attackPoint = CreateChild(player.transform, "AttackPoint", new Vector3(0.95f, 0.05f, 0f));

            controller.Configure(body, abilities, groundCheck, wallCheck, groundMask, collider);
            health.Configure(body, controller);
            combat.Configure(abilities, attackPoint, enemyMask, controller);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(Sprite squareSprite, int enemyLayer)
        {
            GameObject enemy = new GameObject("EnemyPatrol");
            enemy.layer = enemyLayer;
            enemy.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(1f, 0.32f, 0.25f, 1f);
            renderer.sortingOrder = 9;

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            EnemyPatrol patrol = enemy.AddComponent<EnemyPatrol>();
            health.Configure(body);
            patrol.Configure(body, null, null);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject CreateCheckpointPrefab(Sprite squareSprite, int checkpointLayer)
        {
            GameObject checkpoint = new GameObject("Checkpoint");
            checkpoint.layer = checkpointLayer;
            checkpoint.transform.localScale = new Vector3(0.45f, 1.3f, 1f);

            SpriteRenderer renderer = checkpoint.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(0.2f, 0.8f, 0.5f, 1f);
            renderer.sortingOrder = 8;

            BoxCollider2D trigger = checkpoint.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;

            Checkpoint checkpointComponent = checkpoint.AddComponent<Checkpoint>();
            checkpointComponent.Configure(renderer);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(checkpoint, CheckpointPrefabPath);
            Object.DestroyImmediate(checkpoint);
            return prefab;
        }

        private static void BuildLevelGeometry(Sprite squareSprite, int groundLayer, Transform groundRoot, Transform gateRoot, PhysicsMaterial2D noFrictionMaterial)
        {
            Color groundColor = new Color(0.36f, 0.38f, 0.42f, 1f);
            Color wallColor = new Color(0.48f, 0.48f, 0.55f, 1f);
            Color dashColor = new Color(0.15f, 0.85f, 1f, 0.55f);
            Color wallJumpColor = new Color(1f, 0.82f, 0.22f, 0.65f);
            Color exitColor = new Color(0.4f, 0.65f, 0.35f, 1f);

            CreateSolidBox("Start Platform", new Vector3(-4f, -2f, 0f), new Vector2(10f, 1f), groundColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Checkpoint Platform", new Vector3(3.2f, -2f, 0f), new Vector2(4.4f, 1f), groundColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Dash Landing Platform", new Vector3(10.2f, -2f, 0f), new Vector2(4f, 1f), groundColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Enemy Patrol Platform", new Vector3(15f, -2f, 0f), new Vector2(6.6f, 1f), groundColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Shaft Floor", new Vector3(20f, -2f, 0f), new Vector2(4f, 1f), groundColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Exit Ledge", new Vector3(24.5f, 5.8f, 0f), new Vector2(7f, 1f), exitColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);

            CreateSolidBox("Start Back Wall", new Vector3(-9.5f, 0.5f, 0f), new Vector2(1f, 5f), wallColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Wall Jump Shaft Left Wall", new Vector3(18.2f, 1.9f, 0f), new Vector2(1f, 8.8f), wallColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Wall Jump Shaft Right Wall", new Vector3(21.8f, 1.9f, 0f), new Vector2(1f, 8.8f), wallColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);
            CreateSolidBox("Exit Backstop", new Vector3(28.3f, 7.4f, 0f), new Vector2(1f, 4f), wallColor, squareSprite, groundLayer, groundRoot, noFrictionMaterial);

            CreateVisualBox("Dash Required Gap Marker", new Vector3(6.9f, -0.7f, 0f), new Vector2(0.18f, 2.4f), dashColor, squareSprite, gateRoot);
            CreateVisualBox("Wall Jump Required Shaft Marker", new Vector3(20f, 1.9f, 0f), new Vector2(0.25f, 6.4f), wallJumpColor, squareSprite, gateRoot);
        }

        private static GameObject CreateSolidBox(string name, Vector3 position, Vector2 size, Color color, Sprite sprite, int layer, Transform parent, PhysicsMaterial2D noFrictionMaterial)
        {
            GameObject box = CreateVisualBox(name, position, size, color, sprite, parent);
            box.layer = layer;
            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = noFrictionMaterial;
            return box;
        }

        private static GameObject CreateVisualBox(string name, Vector3 position, Vector2 size, Color color, Sprite sprite, Transform parent)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 0;

            return box;
        }

        private static void CreateFallRespawnBarrier(Transform parent)
        {
            GameObject barrier = new GameObject("Fall Respawn Barrier");
            barrier.transform.SetParent(parent);
            barrier.transform.position = new Vector3(9.5f, -52f, 0f);

            BoxCollider2D trigger = barrier.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(60f, 1f);

            barrier.AddComponent<FallRespawnZone>();
        }

        private static void BuildCameraAndLighting(Transform player, Transform systemsRoot)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(systemsRoot);
            cameraObject.transform.position = new Vector3(-4f, 0f, -10f);
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            follow.Configure(player);

            GameObject lightObject = new GameObject("Global Light 2D");
            lightObject.transform.SetParent(systemsRoot);
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static GameObject CreateMarker(string name, Vector3 position, Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            return marker;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.localPosition = localPosition;
            return child.transform;
        }

        private static int EnsureLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == layerName)
                {
                    return i;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            Debug.LogWarning($"No free Unity layer slot found for '{layerName}'. Falling back to Default layer.");
            return 0;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < existingScenes.Length; i++)
            {
                if (existingScenes[i].path == scenePath)
                {
                    existingScenes[i].enabled = true;
                    EditorBuildSettings.scenes = existingScenes;
                    return;
                }
            }

            EditorBuildSettingsScene[] updatedScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
            for (int i = 0; i < existingScenes.Length; i++)
            {
                updatedScenes[i] = existingScenes[i];
            }

            updatedScenes[updatedScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updatedScenes;
        }
    }
}
