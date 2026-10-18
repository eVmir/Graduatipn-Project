using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class FactoryIllustratedBlockoutGenerator
{
    private const string ScenePath = "Assets/Scenes/FactoryIllustratedBlockout.unity";
    private const string ArtPath = "Assets/Blockout/IllustratedArt";
    private const string NavMeshDataPath = "Assets/Blockout/NavMeshes/FactoryIllustratedBlockoutNavMesh.asset";
    private static string OutputPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "..", "Builds", "BlockoutPreviews"));
    private static Camera sceneCamera;
    private static Material floorMaterial;
    private static Material characterMaterial;
    private static Material spriteOverlayMaterial;
    [MenuItem("Tools/Crew/Generate Illustrated Factory Blockout")]
    public static void Generate()
    {
        EnsureFolder("Assets/Blockout");
        EnsureFolder(ArtPath);
        EnsureFolder("Assets/Scenes");
        CreateArtwork();
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FactoryIllustratedBlockout";
        var root = new GameObject("FACTORY • ILLUSTRATED 2.5D BLOCKOUT");

        floorMaterial = MakeMaterial("Blockout_IllustratedFloor", new Color(0.21f, 0.23f, 0.22f));
        characterMaterial = MakeMaterial("Blockout_Player_Teal", new Color(0.20f, 0.57f, 0.53f));
        spriteOverlayMaterial = CreateSpriteOverlayMaterial();
        CreateFloor(root.transform);
        CreateStationPlatform(root.transform);
        CreateCameraAndLights(root.transform);
        CreateIllustratedRoom(root.transform);
        CreatePlayer(root.transform);
        CreateNavMesh(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!Application.isBatchMode)
        {
            CaptureViews(sceneCamera);
            FocusSceneView(root.transform.position);
        }
        Debug.Log("Illustrated blockout saved to " + ScenePath);
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Illustrated Factory Blockout", "Finished. The scene combines an illustrated 2D factory backdrop with an isometric 3D player, five interactive prop blockouts, and a clear raised station bay. The original FactoryBlockout scene was left in place.", "OK");
    }

    private static void CreateFloor(Transform parent)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "3D • Walkable floor / NavMesh source";
        floor.transform.SetParent(parent, false);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(2.0f, 1f, 1.8f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
        floor.AddComponent<BlockoutWalkable>();

    }

    private static void CreateStationPlatform(Transform parent)
    {
        Material platformMaterial = MakeMaterial("Blockout_StationPlatform", new Color(0.14f, 0.17f, 0.17f));
        Material bayMaterial = MakeMaterial("Blockout_StationBay", new Color(0.22f, 0.24f, 0.22f));
        GameObject machineBay = CreateBlock(parent, "3D • raised machine bay", new Vector3(0f, 0.08f, 5.7f),
            new Vector3(18.6f, 0.16f, 4.8f), platformMaterial);
        machineBay.AddComponent<BlockoutWalkable>();

        // Station bays run parallel to the factory's long back wall. The locker and
        // maintenance door sit at the two ends, with workstations between them.
        CreateBlock(parent, "Bay • locker", new Vector3(-7.8f, 0.166f, 7.25f), new Vector3(2.5f, 0.012f, 2.4f), bayMaterial);
        CreateBlock(parent, "Bay • shift desk", new Vector3(-4f, 0.166f, 7.0f), new Vector3(3.25f, 0.012f, 2.5f), bayMaterial);
        CreateBlock(parent, "Bay • incident press", new Vector3(0f, 0.166f, 6.65f), new Vector3(3.65f, 0.012f, 3.0f), bayMaterial);
        CreateBlock(parent, "Bay • resonance unit", new Vector3(4f, 0.166f, 7.0f), new Vector3(2.75f, 0.012f, 2.5f), bayMaterial);
        CreateBlock(parent, "Bay • maintenance door", new Vector3(8f, 0.166f, 7.55f), new Vector3(2.45f, 0.012f, 1.1f), bayMaterial);

        // Keep the front half of the room open and connect it to the raised bay.
        GameObject accessRamp = CreateBlock(parent, "3D • clear access ramp", new Vector3(0f, 0.08f, 2.4f),
            new Vector3(4f, 0.16f, 1.8f), platformMaterial);
        accessRamp.AddComponent<BlockoutWalkable>();

        // Low perimeter markers define the room without closing the camera view.
        Material boundaryMaterial = MakeMaterial("Blockout_Boundary", new Color(0.25f, 0.28f, 0.26f));
        CreateBlock(parent, "Room boundary • left", new Vector3(-9.75f, 0.38f, 0f), new Vector3(0.22f, 0.76f, 17.9f), boundaryMaterial);
        CreateBlock(parent, "Room boundary • right", new Vector3(9.75f, 0.38f, 0f), new Vector3(0.22f, 0.76f, 17.9f), boundaryMaterial);
    }

    private static GameObject CreateBlock(Transform parent, string objectName, Vector3 position, Vector3 size, Material material, float yaw = 0f)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        block.transform.localScale = size;
        block.GetComponent<MeshRenderer>().sharedMaterial = material;
        return block;
    }

    private static void CreateIllustratedRoom(Transform parent)
    {
        CreateWallPlane(parent);
        Material frame = MakeMaterial("Blockout_Prop_Frame", new Color(0.10f, 0.14f, 0.15f));
        Material metal = MakeMaterial("Blockout_Prop_Metal", new Color(0.34f, 0.39f, 0.37f));
        Material inset = MakeMaterial("Blockout_Prop_Inset", new Color(0.21f, 0.27f, 0.27f));
        Material amber = MakeMaterial("Blockout_Prop_Amber", new Color(0.82f, 0.43f, 0.15f));
        Material paper = MakeMaterial("Blockout_Prop_Paper", new Color(0.77f, 0.71f, 0.56f));
        Material violet = MakeMaterial("Blockout_Prop_Anomaly", new Color(0.53f, 0.31f, 0.70f));

        // Interactive props are small real-time 3D blockouts; the room art stays 2D.
        CreateInteractableBlockout(parent, "3D • Worker locker", "Locker",
            new Vector3(-7.8f, 0.16f, 7.25f), new Vector3(-7.8f, 0.16f, 5.7f),
            new Vector3(1.7f, 2.9f, 0.8f), "Worker locker", "The locker is shut, but the handle has fresh marks.", frame, metal, inset, amber, paper, violet);
        CreateInteractableBlockout(parent, "3D • Shift desk", "Desk",
            new Vector3(-4f, 0.16f, 7f), new Vector3(-4f, 0.16f, 5.3f),
            new Vector3(2.9f, 1.6f, 1.8f), "Shift ledger", "A time entry is missing from the final shift.", frame, metal, inset, amber, paper, violet);
        CreateInteractableBlockout(parent, "3D • Incident press", "Press",
            new Vector3(0f, 0.16f, 6.65f), new Vector3(0f, 0.16f, 4.95f),
            new Vector3(3.1f, 3.4f, 2.5f), "Press machine", "The safety guard is open. The shift log says it was closed.", frame, metal, inset, amber, paper, violet);
        CreateInteractableBlockout(parent, "3D • Resonance unit", "Anomaly",
            new Vector3(4f, 0.16f, 7f), new Vector3(4f, 0.16f, 5.3f),
            new Vector3(1.9f, 2.8f, 1.8f), "Resonance unit", "A quiet pulse repeats at an interval that is not on the factory clock.", frame, metal, inset, amber, paper, violet);
        CreateInteractableBlockout(parent, "3D • Maintenance door", "Door",
            new Vector3(8f, 0.16f, 7.55f), new Vector3(8f, 0.16f, 6.1f),
            new Vector3(2.2f, 3.6f, 0.7f), "Maintenance door", "The door will not release while the line is powered.", frame, metal, inset, amber, paper, violet);
    }

    private static void CreateInteractableBlockout(Transform parent, string objectName, string kind,
        Vector3 position, Vector3 approachPosition, Vector3 hitSize, string displayName, string observation,
        Material frame, Material metal, Material inset, Material amber, Material paper, Material violet)
    {
        var root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var hitbox = root.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.size = hitSize;
        hitbox.center = new Vector3(0f, hitSize.y * 0.5f, 0f);
        var interactable = root.AddComponent<BlockoutInteractable>();
        interactable.displayName = displayName;
        interactable.observation = observation;
        var approach = new GameObject(displayName + " • approach point");
        approach.transform.SetParent(parent, false);
        approach.transform.position = approachPosition;
        interactable.approachPoint = approach.transform;

        if (kind == "Locker")
        {
            CreateModelPart(root.transform, "Steel shell", PrimitiveType.Cube, new Vector3(0f, 1.43f, 0f), new Vector3(1.65f, 2.86f, 0.65f), frame);
            CreateModelPart(root.transform, "Locker door", PrimitiveType.Cube, new Vector3(0f, 1.44f, 0.35f), new Vector3(1.35f, 2.56f, 0.07f), metal);
            CreateModelPart(root.transform, "Vent inset", PrimitiveType.Cube, new Vector3(0f, 2.32f, 0.40f), new Vector3(0.72f, 0.05f, 0.03f), inset);
            CreateModelPart(root.transform, "Handle", PrimitiveType.Cube, new Vector3(0.49f, 1.39f, 0.42f), new Vector3(0.07f, 0.34f, 0.07f), amber);
        }
        else if (kind == "Desk")
        {
            CreateModelPart(root.transform, "Desk top", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0f), new Vector3(2.85f, 0.18f, 1.55f), metal);
            CreateModelPart(root.transform, "Desk shadow lip", PrimitiveType.Cube, new Vector3(0f, 0.90f, 0f), new Vector3(2.75f, 0.09f, 1.48f), frame);
            for (float x = -1.18f; x <= 1.19f; x += 2.36f)
                for (float z = -0.56f; z <= 0.57f; z += 1.13f)
                    CreateModelPart(root.transform, "Desk leg", PrimitiveType.Cube, new Vector3(x, 0.46f, z), new Vector3(0.13f, 0.88f, 0.13f), frame);
            CreateModelPart(root.transform, "Shift paper", PrimitiveType.Cube, new Vector3(-0.12f, 1.16f, 0.22f), new Vector3(0.92f, 0.035f, 0.72f), paper);
            CreateModelPart(root.transform, "Desk lamp", PrimitiveType.Cylinder, new Vector3(0.92f, 1.25f, -0.18f), new Vector3(0.12f, 0.22f, 0.12f), amber);
        }
        else if (kind == "Press")
        {
            CreateModelPart(root.transform, "Press base", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(2.9f, 2.0f, 2.3f), frame);
            CreateModelPart(root.transform, "Front plate", PrimitiveType.Cube, new Vector3(0f, 1.0f, 1.17f), new Vector3(2.52f, 1.54f, 0.08f), metal);
            CreateModelPart(root.transform, "Press head", PrimitiveType.Cube, new Vector3(0f, 2.35f, 0f), new Vector3(2.15f, 0.48f, 2.0f), metal);
            CreateModelPart(root.transform, "Ram", PrimitiveType.Cube, new Vector3(0f, 1.92f, 0.30f), new Vector3(0.72f, 0.48f, 0.78f), inset);
            CreateModelPart(root.transform, "Safety stripe", PrimitiveType.Cube, new Vector3(0f, 0.48f, 1.23f), new Vector3(2.25f, 0.13f, 0.06f), amber);
            CreateModelPart(root.transform, "Open guard", PrimitiveType.Cube, new Vector3(1.05f, 1.47f, 0.63f), new Vector3(0.10f, 0.88f, 0.70f), amber);
        }
        else if (kind == "Anomaly")
        {
            CreateModelPart(root.transform, "Resonance cabinet", PrimitiveType.Cube, new Vector3(0f, 1.08f, 0f), new Vector3(1.72f, 2.16f, 1.42f), frame);
            CreateModelPart(root.transform, "Instrument panel", PrimitiveType.Cube, new Vector3(0f, 1.13f, 0.74f), new Vector3(1.42f, 1.56f, 0.07f), metal);
            CreateModelPart(root.transform, "Glass inset", PrimitiveType.Cube, new Vector3(0f, 1.55f, 0.79f), new Vector3(0.98f, 0.70f, 0.04f), inset);
            CreateModelPart(root.transform, "Pulse core", PrimitiveType.Sphere, new Vector3(0f, 2.62f, 0f), new Vector3(0.96f, 0.96f, 0.96f), violet);
            CreateModelPart(root.transform, "Control light", PrimitiveType.Sphere, new Vector3(0f, 1.13f, 0.84f), new Vector3(0.18f, 0.18f, 0.08f), amber);
        }
        else if (kind == "Door")
        {
            CreateModelPart(root.transform, "Door frame", PrimitiveType.Cube, new Vector3(0f, 1.80f, 0f), new Vector3(2.15f, 3.60f, 0.44f), frame);
            CreateModelPart(root.transform, "Door leaf", PrimitiveType.Cube, new Vector3(0f, 1.80f, 0.25f), new Vector3(1.78f, 3.20f, 0.08f), metal);
            CreateModelPart(root.transform, "Recess", PrimitiveType.Cube, new Vector3(0f, 2.00f, 0.30f), new Vector3(1.42f, 2.32f, 0.035f), inset);
            CreateModelPart(root.transform, "Handle", PrimitiveType.Cylinder, new Vector3(0.58f, 1.62f, 0.34f), new Vector3(0.09f, 0.12f, 0.09f), amber);
            CreateModelPart(root.transform, "Warning lamp", PrimitiveType.Cube, new Vector3(-0.65f, 2.80f, 0.34f), new Vector3(0.19f, 0.22f, 0.08f), violet);
        }
    }

    private static void CreateModelPart(Transform parent, string partName, PrimitiveType primitive,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        var part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }
    private static void CreateWallPlane(Transform parent)
    {
        // Mount the illustrated wall vertically along the floor's long back edge.
        var wall = CreateSpriteObject("2D ART • full rear factory wall", "RearWall", parent,
            new Vector3(0f, 0f, 8.95f), 19.4f, 5.15f, false);
        wall.transform.rotation = Quaternion.identity;
        wall.GetComponent<SpriteRenderer>().sortingOrder = -100;
    }

    private static Material CreateSpriteOverlayMaterial()
    {
        string path = "Assets/Blockout/Materials/Blockout_SpriteOverlay.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Crew/IllustratedSpriteOverlay");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        return material;
    }
    private static GameObject CreateSpriteObject(string objectName, string spriteName, Transform parent,
        Vector3 groundPosition, float width, float height, bool foreground)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.position = groundPosition;
        FaceCamera(go.transform);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/" + spriteName + ".png");
        renderer.sharedMaterial = spriteOverlayMaterial;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = foreground ? 30 : 0;
        renderer.drawMode = SpriteDrawMode.Simple;
        if (renderer.sprite != null)
        {
            float spriteWidth = renderer.sprite.bounds.size.x;
            float spriteHeight = renderer.sprite.bounds.size.y;
            go.transform.localScale = new Vector3(width / spriteWidth, height / spriteHeight, 1f);
        }
        return go;
    }

    private static void CreateInteractableArt(string objectName, string spriteName, Transform parent,
        Vector3 groundPosition, float width, float height, float hitDepth, string displayName,
        string observation, Vector3 approachPosition)
    {
        var go = CreateSpriteObject(objectName, spriteName, parent, groundPosition, width, height, false);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(width, height, hitDepth);
        box.center = new Vector3(0f, height * 0.5f, 0f);
        var interactable = go.AddComponent<BlockoutInteractable>();
        interactable.displayName = displayName;
        interactable.observation = observation;
        var approach = new GameObject(displayName + " • approach point");
        approach.transform.SetParent(parent, false);
        approach.transform.position = approachPosition;
        interactable.approachPoint = approach.transform;
    }

    private static void FaceCamera(Transform item)
    {
        if (sceneCamera == null) return;
        item.rotation = Quaternion.LookRotation(sceneCamera.transform.position - item.position, Vector3.up);
    }

    private static void CreatePlayer(Transform parent)
    {
        var player = new GameObject("3D • Player character placeholder");
        player.transform.SetParent(parent, false);
        player.transform.localPosition = new Vector3(-4.3f, 0.02f, -4.3f);
        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 3.2f;
        agent.angularSpeed = 540f;
        agent.acceleration = 14f;
        agent.radius = 0.32f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 1.15f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Temporary 3D character silhouette";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.91f, 0f);
        body.transform.localScale = new Vector3(0.66f, 0.91f, 0.66f);
        body.GetComponent<MeshRenderer>().sharedMaterial = characterMaterial;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.layer = 2;

        var controller = player.AddComponent<PointAndClickController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("sceneCamera").objectReferenceValue = sceneCamera;
        serialized.FindProperty("agent").objectReferenceValue = agent;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateNavMesh(Transform parent)
    {
        var surfaceObject = new GameObject("Navigation Surface • floor collider only");
        surfaceObject.transform.SetParent(parent, false);
        var surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
        EnsureFolder("Assets/Blockout/NavMeshes");
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshDataPath) != null)
            AssetDatabase.DeleteAsset(NavMeshDataPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshDataPath);
        EditorUtility.SetDirty(surface);
    }

    private static void CreateCameraAndLights(Transform parent)
    {
        var lightObject = new GameObject("Soft factory key light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.rotation = Quaternion.Euler(50f, -32f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.84f, 0.67f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.orthographic = true;
        sceneCamera.orthographicSize = 11f;
        sceneCamera.nearClipPlane = 0.1f;
        sceneCamera.farClipPlane = 100f;
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.09f, 0.11f, 0.11f);
        cameraObject.transform.position = new Vector3(-22f, 28.2f, -22f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 4.5f, 0f) - cameraObject.transform.position, Vector3.up);
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Blockout/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }
    private static void CreateArtwork()
    {
        CreateWallArtwork();
        CreateFloorArtwork();
        CreateDeskArtwork();
        CreateLockerArtwork();
        CreatePressArtwork();
        CreateConveyorArtwork();
        CreateAnomalyArtwork();
        CreateDoorArtwork();
        CreateRailArtwork();
    }

    private static void CreateWallArtwork()
    {
        var c = new ArtCanvas(1600, 640, new Color(0.16f, 0.20f, 0.20f));
        for (int x = 0; x < 1600; x += 180)
        {
            c.Rect(x, 55, 12, 570, new Color(0.23f, 0.27f, 0.26f));
            c.Rect(x + 22, 75, 142, 500, new Color(0.18f, 0.22f, 0.22f));
        }
        c.Rect(0, 55, 1600, 22, new Color(0.31f, 0.34f, 0.31f));
        c.Rect(0, 570, 1600, 40, new Color(0.12f, 0.15f, 0.15f));
        c.Rect(0, 610, 1600, 18, new Color(0.73f, 0.40f, 0.15f));
        for (int x = 40; x < 1600; x += 310)
        {
            c.Rect(x, 102, 190, 112, new Color(0.10f, 0.14f, 0.15f));
            c.Rect(x + 14, 116, 162, 82, new Color(0.28f, 0.39f, 0.38f));
            c.Rect(x + 84, 116, 8, 82, new Color(0.12f, 0.18f, 0.18f));
        }
        // Repeated pipe silhouettes and an unlabelled shift sign keep the artwork easy to repaint.
        c.Rect(0, 18, 1600, 24, new Color(0.08f, 0.10f, 0.11f));
        c.Rect(110, 32, 420, 18, new Color(0.39f, 0.42f, 0.38f));
        c.Rect(970, 28, 500, 20, new Color(0.39f, 0.42f, 0.38f));
        c.Rect(150, 260, 360, 94, new Color(0.08f, 0.11f, 0.12f));
        c.Rect(175, 279, 230, 10, new Color(0.78f, 0.43f, 0.17f));
        c.Rect(175, 308, 295, 8, new Color(0.40f, 0.44f, 0.40f));
        c.Rect(175, 328, 188, 7, new Color(0.31f, 0.35f, 0.33f));
        SaveSprite("RearWall", c, new Vector2(0.5f, 0f));
    }

    private static void CreateFloorArtwork()
    {
        var c = new ArtCanvas(1600, 1200, Color.clear);
        SaveSprite("FloorMarks", c, new Vector2(0.5f, 0.5f));
    }
    private static void CreateDeskArtwork()
    {
        var c = new ArtCanvas(360, 310, Color.clear);
        c.Rect(24, 98, 310, 32, new Color(0.28f, 0.31f, 0.29f));
        c.Rect(36, 129, 286, 20, new Color(0.48f, 0.43f, 0.34f));
        c.Rect(54, 149, 22, 136, new Color(0.18f, 0.21f, 0.21f));
        c.Rect(278, 149, 22, 136, new Color(0.18f, 0.21f, 0.21f));
        c.Rect(94, 83, 100, 12, new Color(0.78f, 0.68f, 0.47f));
        c.Rect(105, 48, 78, 36, new Color(0.81f, 0.77f, 0.65f));
        c.Rect(115, 57, 42, 3, new Color(0.44f, 0.43f, 0.38f));
        c.Rect(115, 67, 54, 3, new Color(0.44f, 0.43f, 0.38f));
        c.Rect(115, 76, 36, 3, new Color(0.44f, 0.43f, 0.38f));
        c.Rect(247, 20, 8, 65, new Color(0.24f, 0.27f, 0.25f));
        c.Rect(222, 16, 58, 14, new Color(0.75f, 0.46f, 0.20f));
        SaveSprite("WorkerDesk", c, new Vector2(0.5f, 0f));
    }

    private static void CreateLockerArtwork()
    {
        var c = new ArtCanvas(200, 360, Color.clear);
        c.Rect(17, 10, 166, 340, new Color(0.15f, 0.19f, 0.19f));
        c.Rect(27, 22, 146, 316, new Color(0.31f, 0.35f, 0.33f));
        c.Rect(37, 32, 126, 296, new Color(0.25f, 0.29f, 0.28f));
        c.Rect(48, 58, 94, 7, new Color(0.45f, 0.48f, 0.43f));
        c.Rect(48, 78, 94, 6, new Color(0.18f, 0.22f, 0.22f));
        c.Rect(146, 166, 9, 38, new Color(0.82f, 0.51f, 0.19f));
        c.Rect(48, 300, 60, 6, new Color(0.42f, 0.45f, 0.40f));
        SaveSprite("Locker", c, new Vector2(0.5f, 0f));
    }

    private static void CreatePressArtwork()
    {
        var c = new ArtCanvas(440, 480, Color.clear);
        c.Rect(42, 90, 356, 336, new Color(0.13f, 0.17f, 0.18f));
        c.Rect(61, 114, 318, 280, new Color(0.35f, 0.39f, 0.37f));
        c.Rect(88, 143, 264, 202, new Color(0.20f, 0.24f, 0.24f));
        c.Rect(125, 19, 190, 106, new Color(0.15f, 0.18f, 0.19f));
        c.Rect(142, 42, 156, 63, new Color(0.40f, 0.43f, 0.39f));
        c.Rect(170, 96, 100, 110, new Color(0.25f, 0.29f, 0.27f));
        c.Rect(100, 342, 240, 23, new Color(0.78f, 0.49f, 0.18f));
        c.Rect(100, 376, 240, 10, new Color(0.11f, 0.14f, 0.15f));
        c.Rect(135, 208, 170, 14, new Color(0.80f, 0.50f, 0.18f));
        c.Rect(112, 232, 12, 49, new Color(0.64f, 0.24f, 0.18f));
        c.Rect(316, 234, 13, 40, new Color(0.25f, 0.58f, 0.53f));
        c.Circle(220, 261, 38, new Color(0.10f, 0.13f, 0.14f));
        c.Circle(220, 261, 20, new Color(0.47f, 0.28f, 0.48f));
        c.Rect(55, 428, 330, 18, new Color(0.10f, 0.13f, 0.14f));
        SaveSprite("PressMachine", c, new Vector2(0.5f, 0f));
    }

    private static void CreateConveyorArtwork()
    {
        var c = new ArtCanvas(680, 180, Color.clear);
        c.Rect(24, 48, 632, 96, new Color(0.12f, 0.16f, 0.17f));
        c.Rect(40, 38, 600, 48, new Color(0.36f, 0.40f, 0.38f));
        c.Rect(54, 47, 570, 25, new Color(0.18f, 0.22f, 0.21f));
        for (int x = 65; x < 610; x += 80)
        {
            c.Rect(x, 49, 5, 20, new Color(0.77f, 0.49f, 0.18f));
            c.Circle(x + 5, 134, 12, new Color(0.20f, 0.24f, 0.24f));
        }
        c.Rect(80, 145, 520, 12, new Color(0.11f, 0.14f, 0.15f));
        SaveSprite("Conveyor", c, new Vector2(0.5f, 0f));
    }

    private static void CreateAnomalyArtwork()
    {
        var c = new ArtCanvas(280, 360, Color.clear);
        c.Rect(27, 105, 226, 228, new Color(0.12f, 0.16f, 0.17f));
        c.Rect(44, 121, 192, 178, new Color(0.33f, 0.37f, 0.36f));
        c.Rect(68, 145, 144, 119, new Color(0.15f, 0.19f, 0.20f));
        c.Rect(91, 290, 98, 19, new Color(0.23f, 0.27f, 0.27f));
        c.Circle(140, 90, 62, new Color(0.09f, 0.11f, 0.15f));
        c.Circle(140, 90, 47, new Color(0.42f, 0.31f, 0.57f));
        c.Circle(140, 90, 29, new Color(0.12f, 0.15f, 0.18f));
        c.Circle(140, 90, 14, new Color(0.64f, 0.48f, 0.78f));
        c.Rect(82, 196, 22, 12, new Color(0.77f, 0.49f, 0.18f));
        c.Rect(128, 196, 22, 12, new Color(0.26f, 0.62f, 0.57f));
        c.Rect(176, 196, 22, 12, new Color(0.77f, 0.49f, 0.18f));
        SaveSprite("AnomalyUnit", c, new Vector2(0.5f, 0f));
    }

    private static void CreateDoorArtwork()
    {
        var c = new ArtCanvas(260, 430, Color.clear);
        c.Rect(18, 8, 224, 415, new Color(0.12f, 0.15f, 0.16f));
        c.Rect(34, 24, 192, 382, new Color(0.34f, 0.38f, 0.36f));
        c.Rect(51, 43, 158, 345, new Color(0.19f, 0.23f, 0.23f));
        c.Rect(67, 60, 125, 208, new Color(0.24f, 0.28f, 0.27f));
        c.Rect(184, 215, 10, 46, new Color(0.80f, 0.50f, 0.18f));
        c.Rect(60, 291, 142, 14, new Color(0.76f, 0.49f, 0.18f));
        c.Rect(78, 322, 103, 8, new Color(0.11f, 0.14f, 0.15f));
        c.Rect(88, 345, 82, 7, new Color(0.11f, 0.14f, 0.15f));
        c.Rect(200, 55, 24, 35, new Color(0.72f, 0.26f, 0.17f));
        SaveSprite("MaintenanceDoor", c, new Vector2(0.5f, 0f));
    }

    private static void CreateRailArtwork()
    {
        var c = new ArtCanvas(1400, 95, Color.clear);
        c.Rect(0, 21, 1400, 14, new Color(0.37f, 0.40f, 0.37f));
        c.Rect(0, 62, 1400, 20, new Color(0.17f, 0.20f, 0.20f));
        for (int x = 0; x < 1400; x += 92)
        {
            c.Rect(x, 12, 12, 76, new Color(0.33f, 0.37f, 0.35f));
            if ((x / 92) % 2 == 0) c.Rect(x + 20, 64, 42, 16, new Color(0.75f, 0.48f, 0.18f));
        }
        SaveSprite("ForegroundRail", c, new Vector2(0.5f, 0f));
    }

    private static void SaveSprite(string name, ArtCanvas canvas, Vector2 pivot)
    {
        string fullPath = Path.Combine(Application.dataPath, "Blockout/IllustratedArt/" + name + ".png");
        File.WriteAllBytes(fullPath, canvas.Encode());
        string assetPath = ArtPath + "/" + name + ".png";
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = pivot;
            importer.SetTextureSettings(spriteSettings);
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void CaptureViews(Camera camera)
    {
        Directory.CreateDirectory(OutputPath);
        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        float oldSize = camera.orthographicSize;
        camera.transform.position = new Vector3(-22f, 28.2f, -22f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 4.5f, 0f) - camera.transform.position, Vector3.up);
        camera.orthographicSize = 11f;
        RenderPng(camera, Path.Combine(OutputPath, "factory-illustrated-blockout-isometric.png"), 1920, 1080);
        camera.transform.position = new Vector3(0f, 32f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.orthographicSize = 9.3f;
        var rearWall = GameObject.Find("2D ART • full rear factory wall");
        var rearWallRenderer = rearWall != null ? rearWall.GetComponent<SpriteRenderer>() : null;
        if (rearWallRenderer != null) rearWallRenderer.enabled = false;
        RenderPng(camera, Path.Combine(OutputPath, "factory-illustrated-blockout-topdown.png"), 1920, 1080);
        if (rearWallRenderer != null) rearWallRenderer.enabled = true;
        camera.transform.position = oldPosition;
        camera.transform.rotation = oldRotation;
        camera.orthographicSize = oldSize;
    }

    private static void RenderPng(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture oldTarget = camera.targetTexture;
        camera.targetTexture = target;
        target.Create();
        camera.Render();
        RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        image.Apply(false, false);
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = oldTarget;
        RenderTexture.active = previous;
        target.Release();
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(image);
    }

    private static void FocusSceneView(Vector3 pivot)
    {
        if (SceneView.lastActiveSceneView == null) return;
        SceneView.lastActiveSceneView.pivot = pivot;
        SceneView.lastActiveSceneView.rotation = Quaternion.Euler(35f, 45f, 0f);
        SceneView.lastActiveSceneView.size = 20f;
        SceneView.lastActiveSceneView.orthographic = true;
        SceneView.lastActiveSceneView.Repaint();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private sealed class ArtCanvas
    {
        private readonly int width;
        private readonly int height;
        private readonly Color[] pixels;

        public ArtCanvas(int width, int height, Color background)
        {
            this.width = width;
            this.height = height;
            pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = background;
        }

        public void Rect(int x, int y, int w, int h, Color color)
        {
            int x0 = Mathf.Clamp(x, 0, width);
            int x1 = Mathf.Clamp(x + w, 0, width);
            int y0 = Mathf.Clamp(y, 0, height);
            int y1 = Mathf.Clamp(y + h, 0, height);
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++) Blend(px, height - 1 - py, color);
        }

        public void Circle(int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= r2)
                    {
                        int px = cx + x;
                        int py = height - 1 - (cy + y);
                        if (px >= 0 && px < width && py >= 0 && py < height) Blend(px, py, color);
                    }
        }

        private void Blend(int x, int y, Color source)
        {
            int index = y * width + x;
            Color destination = pixels[index];
            float a = source.a;
            pixels[index] = new Color(
                source.r * a + destination.r * (1f - a),
                source.g * a + destination.g * (1f - a),
                source.b * a + destination.b * (1f - a),
                a + destination.a * (1f - a));
        }

        public byte[] Encode()
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            byte[] data = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return data;
        }
    }
}
