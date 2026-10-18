using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class FactoryBlockoutGenerator
{
    private const string ScenePath = "Assets/Scenes/FactoryBlockout.unity";
    private const string MaterialPath = "Assets/Blockout/Materials";
    private const string OutputPath = @"C:\Users\isu\Documents\Codex\2026-10-11\c\outputs";

    private static Material floorMat;
    private static Material wallMat;
    private static Material steelMat;
    private static Material darkMat;
    private static Material amberMat;
    private static Material tealMat;
    private static Material violetMat;
    private static Material tapeMat;

    [MenuItem("Tools/Crew/Generate Factory Blockout")]
    public static void Generate()
    {

        EnsureFolder("Assets/Blockout");
        EnsureFolder(MaterialPath);
        CreateMaterials();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FactoryBlockout";

        var root = new GameObject("FACTORY BLOCKOUT");
        CreateRoom(root.transform);
        CreateFactoryLine(root.transform);
        CreateWorkerStation(root.transform);
        CreateAnomalyStation(root.transform);
        CreateDoor(root.transform);
        CreateLightingAndCamera(root.transform);
        CreatePlayer(root.transform);

        var surfaceObject = new GameObject("Navigation Surface");
        var surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CaptureBlockoutViews(GameObject.Find("Main Camera").GetComponent<Camera>());
        FocusSceneView(root.transform.position);
        Debug.Log("Factory blockout saved to " + ScenePath);
        EditorUtility.DisplayDialog("Factory Blockout", "Scene generated. Click the floor to move and click marked objects to inspect.", "OK");
    }

    private static void CreateMaterials()
    {
        floorMat = MakeMaterial("Blockout_Floor", new Color(0.16f, 0.19f, 0.20f));
        wallMat = MakeMaterial("Blockout_Walls", new Color(0.26f, 0.29f, 0.29f));
        steelMat = MakeMaterial("Blockout_Steel", new Color(0.39f, 0.42f, 0.40f));
        darkMat = MakeMaterial("Blockout_DarkMetal", new Color(0.10f, 0.12f, 0.13f));
        amberMat = MakeMaterial("Blockout_Amber", new Color(0.77f, 0.42f, 0.13f), new Color(0.9f, 0.28f, 0.06f));
        tealMat = MakeMaterial("Blockout_Teal", new Color(0.16f, 0.53f, 0.52f), new Color(0.05f, 0.35f, 0.40f));
        violetMat = MakeMaterial("Blockout_Anomaly", new Color(0.37f, 0.29f, 0.58f), new Color(0.27f, 0.12f, 0.60f));
        tapeMat = MakeMaterial("Blockout_Caution", new Color(0.83f, 0.65f, 0.22f));
    }

    private static Material MakeMaterial(string name, Color color, Color emission = default)
    {
        string path = MaterialPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (emission.maxColorComponent > 0.001f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<MeshRenderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<MeshRenderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<MeshRenderer>().sharedMaterial = material;
        return item;
    }

    private static void AddInteractable(GameObject target, string displayName, string observation, Vector3 approachPosition, Transform parent)
    {
        var interactable = target.AddComponent<BlockoutInteractable>();
        interactable.displayName = displayName;
        interactable.observation = observation;
        var approach = new GameObject(displayName + " Approach");
        approach.transform.SetParent(parent, false);
        approach.transform.localPosition = approachPosition;
        interactable.approachPoint = approach.transform;
    }

    private static void CreateRoom(Transform parent)
    {
        GameObject floor = Box("Walkable Factory Floor", parent, new Vector3(0, -0.15f, 0), new Vector3(18, 0.3f, 14), floorMat);
        floor.AddComponent<BlockoutWalkable>();

        Box("Back Wall", parent, new Vector3(0, 2.6f, 7.1f), new Vector3(18, 5.2f, 0.3f), wallMat);
        Box("East Wall", parent, new Vector3(9.1f, 2.6f, 0), new Vector3(0.3f, 5.2f, 14), wallMat);
        Box("Back Wall Beam", parent, new Vector3(0, 5.15f, 6.85f), new Vector3(18, 0.25f, 0.5f), steelMat);
        Box("East Wall Beam", parent, new Vector3(8.85f, 5.15f, 0), new Vector3(0.5f, 0.25f, 14), steelMat);

        for (int x = -7; x <= 7; x += 2)
        {
            Box("Floor Grid Inlay", parent, new Vector3(x, 0.012f, 0), new Vector3(0.025f, 0.02f, 13.7f), steelMat);
        }
        for (int z = -5; z <= 5; z += 2)
        {
            Box("Floor Grid Inlay", parent, new Vector3(0, 0.013f, z), new Vector3(17.7f, 0.02f, 0.025f), steelMat);
        }

        for (int i = 0; i < 6; i++)
        {
            float x = -7.5f + i * 3f;
            Cylinder("Overhead Pipe", parent, new Vector3(x, 4.25f, 6.45f), new Vector3(0.12f, 7.5f, 0.12f), darkMat);
            Box("Pipe Support", parent, new Vector3(x, 4.02f, 6.45f), new Vector3(0.28f, 0.22f, 0.28f), steelMat);
        }

        Box("Factory Sign Panel", parent, new Vector3(-4.4f, 4.0f, 6.88f), new Vector3(4.4f, 1.0f, 0.12f), darkMat);
        Box("Factory Sign Accent", parent, new Vector3(-4.4f, 4.38f, 6.78f), new Vector3(2.7f, 0.08f, 0.08f), amberMat);

        // Low boundary blocks frame the playable area while keeping the isometric view open.
        Box("South Safety Rail", parent, new Vector3(-2.3f, 0.52f, -6.55f), new Vector3(13.0f, 1.0f, 0.16f), steelMat);
        Box("South Rail Break", parent, new Vector3(5.2f, 0.52f, -6.55f), new Vector3(2.0f, 1.0f, 0.16f), steelMat);
        Box("West Safety Rail", parent, new Vector3(-8.55f, 0.52f, -1.1f), new Vector3(0.16f, 1.0f, 9.8f), steelMat);
    }

    private static void CreateFactoryLine(Transform parent)
    {
        // A low conveyor gives the room a strong center while leaving a clear path around it.
        Box("Conveyor Base", parent, new Vector3(0.25f, 0.32f, 0.25f), new Vector3(7.4f, 0.64f, 1.7f), darkMat);
        Box("Conveyor Belt", parent, new Vector3(0.25f, 0.67f, 0.25f), new Vector3(7.0f, 0.08f, 1.48f), steelMat);
        for (int x = -3; x <= 3; x++)
        {
            Box("Belt Segment", parent, new Vector3(x + 0.25f, 0.72f, 0.25f), new Vector3(0.035f, 0.025f, 1.42f), amberMat);
        }
        for (float x = -3.0f; x <= 3.5f; x += 1.3f)
        {
            Cylinder("Conveyor Roller", parent, new Vector3(x + 0.25f, 0.53f, 0.25f), new Vector3(0.16f, 0.82f, 0.16f), steelMat);
        }

        // Main machine block; the violet core is an unexplained visual anomaly, not a solved plot clue.
        Box("Press Machine Housing", parent, new Vector3(0.0f, 1.55f, 2.4f), new Vector3(3.5f, 2.6f, 2.5f), steelMat);
        Box("Press Machine Front", parent, new Vector3(0.0f, 1.35f, 1.08f), new Vector3(2.2f, 1.5f, 0.18f), darkMat);
        Box("Press Machine Head", parent, new Vector3(0.0f, 2.95f, 2.35f), new Vector3(2.7f, 0.3f, 2.1f), darkMat);
        Box("Machine Warning Strip", parent, new Vector3(0.0f, 2.22f, 1.00f), new Vector3(2.2f, 0.12f, 0.12f), amberMat);

        // Caution frame marks a point of interest without depicting the worker or fixing the cause of death.
        for (int x = -2; x <= 2; x += 4)
        {
            for (int z = 0; z <= 4; z += 4)
                Box("Caution Post", parent, new Vector3(x, 0.55f, z), new Vector3(0.16f, 1.1f, 0.16f), tapeMat);
        }
        Box("Caution Rail Back", parent, new Vector3(0, 0.95f, 4.0f), new Vector3(4.0f, 0.12f, 0.12f), tapeMat);
        Box("Caution Rail Left", parent, new Vector3(-2.0f, 0.95f, 2.0f), new Vector3(0.12f, 0.12f, 4.0f), tapeMat);
        Box("Caution Rail Right", parent, new Vector3(2.0f, 0.95f, 2.0f), new Vector3(0.12f, 0.12f, 4.0f), tapeMat);
        Box("Caution Rail Front", parent, new Vector3(0, 0.95f, 0.0f), new Vector3(4.0f, 0.12f, 0.12f), tapeMat);

        var panel = Box("Machine Control Panel", parent, new Vector3(3.5f, 1.25f, 2.25f), new Vector3(0.8f, 1.6f, 0.55f), darkMat);
        Box("Panel Screen", parent, new Vector3(3.5f, 1.62f, 1.96f), new Vector3(0.54f, 0.4f, 0.08f), tealMat);
        Sphere("Panel Button Amber", parent, new Vector3(3.34f, 1.15f, 1.92f), new Vector3(0.14f, 0.14f, 0.08f), amberMat);
        Sphere("Panel Button Teal", parent, new Vector3(3.62f, 1.15f, 1.92f), new Vector3(0.14f, 0.14f, 0.08f), tealMat);
        AddInteractable(panel, "Machine controls", "The status lights do not match the shift record.", new Vector3(4.35f, 0, 2.0f), parent);

        // Floor-level directional marks preserve the player's route around the machine.
        Box("Route Mark", parent, new Vector3(0.0f, 0.025f, -2.5f), new Vector3(4.0f, 0.035f, 0.08f), amberMat);
    }

    private static void CreateWorkerStation(Transform parent)
    {
        Box("Worker Desk", parent, new Vector3(-6.0f, 0.48f, 2.2f), new Vector3(2.8f, 0.9f, 1.25f), steelMat);
        Box("Desk Top", parent, new Vector3(-6.0f, 0.98f, 2.2f), new Vector3(3.0f, 0.12f, 1.42f), darkMat);
        Box("Desk Lamp", parent, new Vector3(-5.0f, 1.35f, 2.5f), new Vector3(0.12f, 0.65f, 0.12f), amberMat);
        Box("Desk Lamp Shade", parent, new Vector3(-5.0f, 1.7f, 2.5f), new Vector3(0.65f, 0.18f, 0.46f), amberMat);

        var ledger = Box("Shift Ledger", parent, new Vector3(-6.3f, 1.08f, 2.18f), new Vector3(0.72f, 0.08f, 0.52f), tapeMat);
        AddInteractable(ledger, "Shift ledger", "A time entry is missing from the final shift.", new Vector3(-4.2f, 0, 2.4f), parent);

        Box("Worker Chair Seat", parent, new Vector3(-6.0f, 0.55f, 0.95f), new Vector3(0.85f, 0.15f, 0.8f), darkMat);
        Box("Worker Chair Back", parent, new Vector3(-6.0f, 1.1f, 0.58f), new Vector3(0.85f, 1.0f, 0.16f), steelMat);

        var locker = Box("Worker Locker", parent, new Vector3(-7.55f, 1.1f, 4.2f), new Vector3(1.7f, 2.2f, 0.85f), darkMat);
        Box("Locker Door", parent, new Vector3(-7.55f, 1.1f, 3.72f), new Vector3(1.5f, 2.0f, 0.08f), steelMat);
        Sphere("Locker Handle", parent, new Vector3(-6.98f, 1.12f, 3.64f), new Vector3(0.10f, 0.10f, 0.08f), amberMat);
        AddInteractable(locker, "Worker locker", "The locker is shut, but the handle has fresh marks.", new Vector3(-6.0f, 0, 5.2f), parent);

        Box("Tool Cart", parent, new Vector3(-4.6f, 0.45f, -0.15f), new Vector3(1.6f, 0.8f, 0.9f), steelMat);
        Box("Tool Cart Top", parent, new Vector3(-4.6f, 0.9f, -0.15f), new Vector3(1.75f, 0.1f, 1.0f), darkMat);
        Box("Tool Tray", parent, new Vector3(-4.6f, 1.02f, -0.15f), new Vector3(0.9f, 0.06f, 0.3f), amberMat);
    }

    private static void CreateAnomalyStation(Transform parent)
    {
        // Industrial equipment frames a small impossible-looking object to introduce the fantasy/sci-fi tone.
        var baseUnit = Box("Resonance Unit", parent, new Vector3(6.1f, 0.9f, -1.8f), new Vector3(2.4f, 1.8f, 1.6f), darkMat);
        Box("Resonance Unit Face", parent, new Vector3(6.1f, 1.0f, -2.63f), new Vector3(1.6f, 1.2f, 0.08f), steelMat);
        Cylinder("Resonance Ring Outer", parent, new Vector3(6.1f, 2.2f, -1.8f), new Vector3(0.75f, 0.08f, 0.75f), violetMat);
        Cylinder("Resonance Ring Inner", parent, new Vector3(6.1f, 2.32f, -1.8f), new Vector3(0.48f, 0.06f, 0.48f), darkMat);
        Sphere("Resonance Core", parent, new Vector3(6.1f, 2.7f, -1.8f), new Vector3(0.72f, 0.72f, 0.72f), violetMat);
        for (int i = 0; i < 3; i++)
        {
            float z = -2.2f + i * 0.4f;
            Box("Resonance Status Light", parent, new Vector3(6.1f, 1.05f, z), new Vector3(0.16f, 0.12f, 0.08f), tealMat);
        }
        AddInteractable(baseUnit, "Resonance unit", "A quiet pulse repeats at an interval that is not on the factory clock.", new Vector3(4.7f, 0, -1.8f), parent);

        Box("Overhead Conduit", parent, new Vector3(6.1f, 3.65f, -1.8f), new Vector3(0.28f, 0.28f, 4.0f), tealMat);
        Box("Conduit Support", parent, new Vector3(6.1f, 3.25f, -1.8f), new Vector3(0.18f, 0.75f, 0.18f), steelMat);
    }

    private static void CreateDoor(Transform parent)
    {
        Box("Maintenance Door Frame", parent, new Vector3(8.72f, 1.55f, 4.4f), new Vector3(0.5f, 3.1f, 2.8f), steelMat);
        var door = Box("Maintenance Door", parent, new Vector3(8.39f, 1.45f, 4.4f), new Vector3(0.12f, 2.65f, 2.4f), darkMat);
        Box("Door Status Light", parent, new Vector3(8.25f, 2.9f, 5.55f), new Vector3(0.12f, 0.2f, 0.2f), amberMat);
        Box("Door Floor Threshold", parent, new Vector3(8.1f, 0.06f, 4.4f), new Vector3(1.6f, 0.12f, 2.7f), tapeMat);
        AddInteractable(door, "Maintenance door", "The door will not release while the line is powered.", new Vector3(7.0f, 0, 4.4f), parent);
    }

    private static void CreatePlayer(Transform parent)
    {
        var player = new GameObject("Player Character Blockout");
        player.transform.SetParent(parent, false);
        player.transform.localPosition = new Vector3(-6.0f, 0, -4.0f);

        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.angularSpeed = 540f;
        agent.acceleration = 14f;
        agent.radius = 0.34f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.stoppingDistance = 1.25f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Temporary Character Silhouette";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0, 0.92f, 0);
        body.transform.localScale = new Vector3(0.7f, 0.92f, 0.7f);
        body.GetComponent<MeshRenderer>().sharedMaterial = tealMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.layer = 2;

        var controller = player.AddComponent<PointAndClickController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("sceneCamera").objectReferenceValue = GameObject.Find("Main Camera").GetComponent<Camera>();
        serialized.FindProperty("agent").objectReferenceValue = agent;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLightingAndCamera(Transform parent)
    {
        var lightObject = new GameObject("Factory Key Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.rotation = Quaternion.Euler(50, -32, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.color = new Color(1.0f, 0.83f, 0.64f);

        var fillObject = new GameObject("Anomaly Fill Light");
        fillObject.transform.SetParent(parent, false);
        fillObject.transform.localPosition = new Vector3(6.1f, 3.0f, -1.8f);
        var fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = 5.0f;
        fill.intensity = 3.0f;
        fill.color = new Color(0.45f, 0.28f, 0.95f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10.8f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.13f);
        camera.transform.position = new Vector3(-22f, 25f, -22f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.2f, 0f) - camera.transform.position, Vector3.up);
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

    private static void CaptureBlockoutViews(Camera camera)
    {
        Directory.CreateDirectory(OutputPath);

        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        float oldSize = camera.orthographicSize;

        camera.transform.position = new Vector3(-22f, 25f, -22f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.2f, 0f) - camera.transform.position, Vector3.up);
        camera.orthographicSize = 10.8f;
        RenderPng(camera, Path.Combine(OutputPath, "factory-blockout-isometric.png"), 1920, 1080);

        camera.transform.position = new Vector3(0f, 32f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.orthographicSize = 7.8f;
        RenderPng(camera, Path.Combine(OutputPath, "factory-blockout-topdown.png"), 1920, 1080);

        camera.transform.position = oldPosition;
        camera.transform.rotation = oldRotation;
        camera.orthographicSize = oldSize;
    }

    private static void RenderPng(Camera camera, string path, int width, int height)
    {
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture oldTarget = camera.targetTexture;
        camera.targetTexture = target;
        target.Create();
        camera.Render();
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
}




