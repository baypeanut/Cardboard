using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
public static class TargetRushBuilder
{
    private const string ScenePath = "Assets/Scenes/TargetRush.unity";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs";
    private const string TexturesPath = "Assets/Textures";

    [MenuItem("Target Rush/Generate Scene")]
    public static void Generate()
    {
        EnsureFolders();
        AssetDatabase.Refresh();
        ConfigurePlayer();
        ConfigureCardboard();
        CreateMaterials();
        CreateProjectilePrefab();
        CreateScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Target Rush scene and Android configuration generated successfully.");
    }

    [MenuItem("Target Rush/Build Android APK")]
    public static void BuildAndroid()
    {
        Generate();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        Directory.CreateDirectory("Builds");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/TargetRush.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new BuildFailedException("Android build failed: " + report.summary.result);
        }

        Debug.Log("Android APK created at Builds/TargetRush.apk");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Editor");
        Directory.CreateDirectory("Assets/Scripts");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialsPath);
        Directory.CreateDirectory(PrefabsPath);
        Directory.CreateDirectory(TexturesPath);
        Directory.CreateDirectory("Submission");
        Directory.CreateDirectory("Builds");
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "COMP590 Student";
        PlayerSettings.productName = "Target Rush";
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "edu.comp590.targetrush");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.Android.androidIsGame = true;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.vulkanEnablePreTransform = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
        ConfigureAndroidGradleTemplates();
    }

    private static void ConfigureAndroidGradleTemplates()
    {
        string pluginsDir = "Assets/Plugins/Android";
        Directory.CreateDirectory(pluginsDir);

        string mainTemplatePath = pluginsDir + "/mainTemplate.gradle";
        string gradlePropertiesPath = pluginsDir + "/gradleTemplate.properties";
        string unityTemplateRoot =
            Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/Tools/GradleTemplates");

        if (!File.Exists(mainTemplatePath))
        {
            string source = Path.Combine(unityTemplateRoot, "mainTemplate.gradle");
            if (File.Exists(source))
            {
                File.Copy(source, mainTemplatePath);
            }
            else
            {
                File.WriteAllText(mainTemplatePath, DefaultMainGradleTemplate());
            }
        }

        string mainTemplate = File.ReadAllText(mainTemplatePath);
        string[] requiredDeps =
        {
            "implementation 'androidx.appcompat:appcompat:1.6.1'",
            "implementation 'com.google.android.gms:play-services-vision:20.1.3'",
            "implementation 'com.google.android.material:material:1.12.0'",
            "implementation 'com.google.protobuf:protobuf-javalite:3.19.4'"
        };
        for (int i = 0; i < requiredDeps.Length; i++)
        {
            if (mainTemplate.IndexOf(requiredDeps[i], System.StringComparison.Ordinal) < 0)
            {
                if (mainTemplate.Contains("**DEPS**"))
                {
                    mainTemplate = mainTemplate.Replace("**DEPS**", requiredDeps[i] + "\n    **DEPS**");
                }
                else if (mainTemplate.Contains("dependencies {"))
                {
                    mainTemplate = mainTemplate.Replace(
                        "dependencies {",
                        "dependencies {\n    " + requiredDeps[i]);
                }
            }
        }
        File.WriteAllText(mainTemplatePath, mainTemplate);

        if (!File.Exists(gradlePropertiesPath))
        {
            string source = Path.Combine(unityTemplateRoot, "gradleTemplate.properties");
            if (File.Exists(source))
            {
                File.Copy(source, gradlePropertiesPath);
            }
            else
            {
                File.WriteAllText(gradlePropertiesPath, "org.gradle.jvmargs=-Xmx**JVM_HEAP_SIZE**M\norg.gradle.parallel=true\n**ADDITIONAL_PROPERTIES**\n");
            }
        }

        string props = File.ReadAllText(gradlePropertiesPath);
        if (props.IndexOf("unityStreamingAssets=**STREAMING_ASSETS**", System.StringComparison.Ordinal) < 0)
        {
            props = "unityStreamingAssets=**STREAMING_ASSETS**\n" + props;
        }
        if (props.IndexOf("android.useAndroidX=true", System.StringComparison.Ordinal) < 0)
        {
            props += "\nandroid.useAndroidX=true\n";
        }
        if (props.IndexOf("android.enableJetifier=true", System.StringComparison.Ordinal) < 0)
        {
            props += "android.enableJetifier=true\n";
        }
        File.WriteAllText(gradlePropertiesPath, props);

        SerializedObject playerSettings = new SerializedObject(
            Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        SerializedProperty mainGradle = playerSettings.FindProperty("useCustomMainGradleTemplate");
        SerializedProperty gradleProps = playerSettings.FindProperty("useCustomGradlePropertiesTemplate");
        SerializedProperty activeInput = playerSettings.FindProperty("activeInputHandler");
        if (mainGradle != null)
        {
            mainGradle.boolValue = true;
        }
        if (gradleProps != null)
        {
            gradleProps.boolValue = true;
        }
        if (activeInput != null)
        {
            activeInput.intValue = 1;
        }
        playerSettings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.Refresh();
    }

    private static string DefaultMainGradleTemplate()
    {
        return
            "apply plugin: 'com.android.library'\n" +
            "**APPLY_PLUGINS**\n\n" +
            "dependencies {\n" +
            "    implementation fileTree(dir: 'libs', include: ['*.jar'])\n" +
            "**DEPS**}\n\n" +
            "android {\n" +
            "    namespace \"com.unity3d.player\"\n" +
            "    ndkPath \"**NDKPATH**\"\n" +
            "    compileSdk **APIVERSION**\n" +
            "    buildToolsVersion '**BUILDTOOLS**'\n\n" +
            "    compileOptions {\n" +
            "        sourceCompatibility JavaVersion.VERSION_11\n" +
            "        targetCompatibility JavaVersion.VERSION_11\n" +
            "    }\n\n" +
            "    defaultConfig {\n" +
            "        minSdk **MINSDK**\n" +
            "        targetSdk **TARGETSDK**\n" +
            "        ndk {\n" +
            "            abiFilters **ABIFILTERS**\n" +
            "        }\n" +
            "        versionCode **VERSIONCODE**\n" +
            "        versionName '**VERSIONNAME**'\n" +
            "        consumerProguardFiles 'proguard-unity.txt'**USER_PROGUARD**\n" +
            "    }\n\n" +
            "    lint {\n" +
            "        abortOnError false\n" +
            "    }\n\n" +
            "    androidResources {\n" +
            "        noCompress = **BUILTIN_NOCOMPRESS** + unityStreamingAssets.tokenize(', ')\n" +
            "        ignoreAssetsPattern = \"!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~\"\n" +
            "    }**PACKAGING**\n" +
            "}\n" +
            "**IL_CPP_BUILD_SETUP**\n" +
            "**SOURCE_BUILD_SETUP**\n" +
            "**EXTERNAL_SOURCES**\n";
    }

    private static void ConfigureCardboard()
    {
        XRGeneralSettingsPerBuildTarget settings;
        if (!EditorBuildSettings.TryGetConfigObject(
                UnityEngine.XR.Management.XRGeneralSettings.k_SettingsKey, out settings))
        {
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            settings.name = "XRGeneralSettingsPerBuildTarget";
            AssetDatabase.CreateAsset(settings, "Assets/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(
                UnityEngine.XR.Management.XRGeneralSettings.k_SettingsKey, settings, true);
        }

        ClearExistingCardboardLoaders(settings);
        ConfigureCardboardForTarget(settings, BuildTargetGroup.Android);
        AssetDatabase.SaveAssets();
    }

    private static void ClearExistingCardboardLoaders(XRGeneralSettingsPerBuildTarget settings)
    {
        var androidSettings = settings.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings != null && androidSettings.Manager != null)
        {
            androidSettings.Manager.loaders.Clear();
        }

        BuildTargetGroup[] groups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.iOS,
            BuildTargetGroup.tvOS,
            BuildTargetGroup.WebGL
        };
        for (int i = 0; i < groups.Length; i++)
        {
            settings.SetSettingsForBuildTarget(groups[i], null);
        }

        string assetPath = AssetDatabase.GetAssetPath(settings);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] == null || subAssets[i] == settings)
            {
                continue;
            }

            string name = subAssets[i].name ?? string.Empty;
            bool isAndroidAsset = name.IndexOf("Android", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isAndroidAsset || subAssets[i] is Google.XR.Cardboard.XRLoader)
            {
                Object.DestroyImmediate(subAssets[i], true);
            }
        }
        AssetDatabase.ImportAsset(assetPath);
    }

    private static void ConfigureCardboardForTarget(
        XRGeneralSettingsPerBuildTarget settings, BuildTargetGroup targetGroup)
    {
        var generalSettings = settings.SettingsForBuildTarget(targetGroup);
        if (generalSettings == null)
        {
            generalSettings = ScriptableObject.CreateInstance<UnityEngine.XR.Management.XRGeneralSettings>();
            generalSettings.name = "XRGeneralSettings-" + targetGroup;
            settings.SetSettingsForBuildTarget(targetGroup, generalSettings);
            AssetDatabase.AddObjectToAsset(generalSettings, settings);
        }

        if (generalSettings.Manager == null)
        {
            var manager = ScriptableObject.CreateInstance<UnityEngine.XR.Management.XRManagerSettings>();
            manager.name = "XRManagerSettings-" + targetGroup;
            AssetDatabase.AddObjectToAsset(manager, settings);
            generalSettings.Manager = manager;
        }

        var managerSettings = generalSettings.Manager;
        managerSettings.loaders.Clear();
        var loader = ScriptableObject.CreateInstance<Google.XR.Cardboard.XRLoader>();
        loader.name = "Cardboard XR Loader";
        AssetDatabase.AddObjectToAsset(loader, managerSettings);
        managerSettings.loaders.Add(loader);
        managerSettings.automaticLoading = true;
        managerSettings.automaticRunning = true;
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(managerSettings);
    }

    private static void CreateMaterials()
    {
        CreateMaterial("NeonCyan", new Color(0.02f, 0.8f, 1f), true);
        CreateMaterial("NeonMagenta", new Color(1f, 0.03f, 0.45f), true);
        CreateMaterial("NeonYellow", new Color(1f, 0.75f, 0.04f), true);
        CreateMaterial("DeepSpace", new Color(0.008f, 0.015f, 0.05f), false);
        CreateMaterial("Platform", new Color(0.025f, 0.07f, 0.14f), false);
        CreateMaterial("WhiteGlow", Color.white, true);
    }

    private static Material CreateMaterial(string name, Color color, bool emission)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material TargetMaterial()
    {
        string path = MaterialsPath + "/TargetTexture.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Unlit/Texture"));
            AssetDatabase.CreateAsset(material, path);
        }

        Texture2D targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/target.png");
        material.mainTexture = targetTexture;
        material.color = Color.white;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateProjectilePrefab()
    {
        string prefabPath = PrefabsPath + "/EnergyBall.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "EnergyBall";
        ball.transform.localScale = Vector3.one * 0.24f;
        ball.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            MaterialsPath + "/NeonYellow.mat");

        Rigidbody body = ball.AddComponent<Rigidbody>();
        body.mass = 0.08f;
        body.drag = 0.05f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ball.AddComponent<BallProjectile>();

        PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
        Object.DestroyImmediate(ball);
    }

    private static void CreateScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.025f, 0.035f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.005f, 0.01f, 0.03f);
        RenderSettings.fogDensity = 0.008f;

        Camera camera = CreateCamera();
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PrefabsPath + "/EnergyBall.prefab");
        SerializedObject shooter = new SerializedObject(camera.GetComponent<CardboardShooter>());
        shooter.FindProperty("projectilePrefab").objectReferenceValue =
            projectilePrefab.GetComponent<BallProjectile>();
        shooter.ApplyModifiedPropertiesWithoutUndo();
        CreateLighting();
        CreateRangeGeometry();
        TargetController target = CreateTarget();
        CreateHud(camera, target);
        CreateCardboardSystems(camera);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.45f, 0f);
        cameraObject.transform.rotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.002f, 0.005f, 0.018f);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 40f;
        camera.fieldOfView = 70f;
        cameraObject.AddComponent<CardboardShooter>();
        cameraObject.AddComponent<CardboardStartup>();
        cameraObject.AddComponent<TrackedPoseDriver>();
        return camera;
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Range Light");
        lightObject.transform.position = new Vector3(0f, 5f, 5f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.2f, 0.5f, 1f);
        light.intensity = 7f;
        light.range = 18f;

        GameObject warmLightObject = new GameObject("Target Light");
        warmLightObject.transform.position = new Vector3(0f, 2f, 8f);
        Light warmLight = warmLightObject.AddComponent<Light>();
        warmLight.type = LightType.Point;
        warmLight.color = new Color(1f, 0.15f, 0.25f);
        warmLight.intensity = 5f;
        warmLight.range = 12f;
    }

    private static void CreateRangeGeometry()
    {
        Material deepSpace = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/DeepSpace.mat");
        Material platform = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Platform.mat");
        Material cyan = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/NeonCyan.mat");
        Material magenta = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/NeonMagenta.mat");
        Material yellow = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/NeonYellow.mat");

        GameObject floor = CreatePrimitive("Range Floor", PrimitiveType.Plane, new Vector3(0f, 0f, 8f),
            new Vector3(1.8f, 1f, 1.8f), platform);
        floor.GetComponent<Collider>().enabled = true;

        CreatePrimitive("Back Wall", PrimitiveType.Cube, new Vector3(0f, 3f, 16f),
            new Vector3(8f, 3.5f, 0.2f), deepSpace);
        CreatePrimitive("Left Rail", PrimitiveType.Cube, new Vector3(-3.6f, 1.1f, 8f),
            new Vector3(0.12f, 1.1f, 8f), cyan);
        CreatePrimitive("Right Rail", PrimitiveType.Cube, new Vector3(3.6f, 1.1f, 8f),
            new Vector3(0.12f, 1.1f, 8f), magenta);

        for (int i = 0; i < 5; i++)
        {
            float z = 2.5f + i * 3.1f;
            CreatePrimitive("Left Beacon " + i, PrimitiveType.Cylinder,
                new Vector3(-3.25f, 0.25f, z), new Vector3(0.16f, 0.25f, 0.16f), cyan);
            CreatePrimitive("Right Beacon " + i, PrimitiveType.Cylinder,
                new Vector3(3.25f, 0.25f, z), new Vector3(0.16f, 0.25f, 0.16f), magenta);
        }

        CreatePrimitive("Target Pedestal", PrimitiveType.Cube, new Vector3(0f, 0.65f, 11.5f),
            new Vector3(2.8f, 0.2f, 0.8f), yellow);
        CreatePrimitive("Left Frame", PrimitiveType.Cube, new Vector3(-1.55f, 2.6f, 11.5f),
            new Vector3(0.12f, 2.2f, 0.12f), cyan);
        CreatePrimitive("Right Frame", PrimitiveType.Cube, new Vector3(1.55f, 2.6f, 11.5f),
            new Vector3(0.12f, 2.2f, 0.12f), cyan);
        CreatePrimitive("Top Frame", PrimitiveType.Cube, new Vector3(0f, 4.78f, 11.5f),
            new Vector3(3.2f, 0.12f, 0.12f), cyan);
        CreatePrimitive("Bottom Frame", PrimitiveType.Cube, new Vector3(0f, 0.48f, 11.5f),
            new Vector3(3.2f, 0.12f, 0.12f), cyan);
    }

    private static TargetController CreateTarget()
    {
        GameObject targetRoot = new GameObject("Target");
        targetRoot.transform.position = new Vector3(0f, 2.55f, 11.5f);
        targetRoot.layer = 8;
        BoxCollider box = targetRoot.AddComponent<BoxCollider>();
        box.size = new Vector3(2.25f, 2.25f, 0.18f);
        TargetController controller = targetRoot.AddComponent<TargetController>();

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Plane);
        board.name = "Concentric Target Texture";
        board.transform.SetParent(targetRoot.transform, false);
        board.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        board.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        board.GetComponent<Renderer>().sharedMaterial = TargetMaterial();
        Object.DestroyImmediate(board.GetComponent<Collider>());

        Material glow = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/WhiteGlow.mat");
        CreateTargetRing(targetRoot.transform, 1.16f, glow, 0.02f);
        CreateTargetRing(targetRoot.transform, 0.86f,
            AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/NeonCyan.mat"), 0.018f);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("pointValue").intValue = 100;
        serialized.FindProperty("targetRenderers").arraySize = 3;
        serialized.FindProperty("targetRenderers").GetArrayElementAtIndex(0).objectReferenceValue =
            board.GetComponent<Renderer>();
        serialized.FindProperty("targetRenderers").GetArrayElementAtIndex(1).objectReferenceValue =
            targetRoot.transform.GetChild(1).GetComponent<Renderer>();
        serialized.FindProperty("targetRenderers").GetArrayElementAtIndex(2).objectReferenceValue =
            targetRoot.transform.GetChild(2).GetComponent<Renderer>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void CreateTargetRing(Transform parent, float radius, Material material, float width)
    {
        GameObject ringObject = new GameObject("Target Glow Ring");
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.localPosition = new Vector3(0f, 0f, -0.035f);
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.startWidth = width;
        line.endWidth = width;
        line.sharedMaterial = material;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private static void CreateHud(Camera camera, TargetController target)
    {
        GameObject gameObject = new GameObject("Target Rush Game");
        TargetRushGame game = gameObject.AddComponent<TargetRushGame>();

        GameObject canvasObject = new GameObject("HUD Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text title = CreateText(canvasObject.transform, "TARGET RUSH", font, 42, Color.cyan,
            new Vector2(0.04f, 0.9f), new Vector2(0.35f, 0.98f), TextAnchor.MiddleLeft);
        Text score = CreateText(canvasObject.transform, "SCORE  0000", font, 34, Color.white,
            new Vector2(0.04f, 0.82f), new Vector2(0.3f, 0.9f), TextAnchor.MiddleLeft);
        Text timer = CreateText(canvasObject.transform, "TIME  60", font, 34, Color.white,
            new Vector2(0.7f, 0.9f), new Vector2(0.96f, 0.98f), TextAnchor.MiddleRight);
        Text streak = CreateText(canvasObject.transform, "STREAK  —", font, 30, Color.yellow,
            new Vector2(0.7f, 0.82f), new Vector2(0.96f, 0.9f), TextAnchor.MiddleRight);
        Text status = CreateText(canvasObject.transform, "READY — FIND A TARGET", font, 30, Color.white,
            new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.16f), TextAnchor.MiddleCenter);
        Text help = CreateText(canvasObject.transform, "LOOK TO AIM  •  PRESS THE VIEWER BUTTON TO FIRE",
            font, 22, new Color(0.6f, 0.75f, 0.9f),
            new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.08f), TextAnchor.MiddleCenter);
        title.raycastTarget = false;

        SerializedObject serialized = new SerializedObject(game);
        serialized.FindProperty("targets").arraySize = 1;
        serialized.FindProperty("targets").GetArrayElementAtIndex(0).objectReferenceValue = target;
        serialized.FindProperty("scoreLabel").objectReferenceValue = score;
        serialized.FindProperty("timerLabel").objectReferenceValue = timer;
        serialized.FindProperty("streakLabel").objectReferenceValue = streak;
        serialized.FindProperty("statusLabel").objectReferenceValue = status;
        serialized.FindProperty("helpLabel").objectReferenceValue = help;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text CreateText(Transform parent, string content, Font font, int size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(content);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateCardboardSystems(Camera camera)
    {
        GameObject reticleObject = new GameObject("Gaze Reticle");
        reticleObject.transform.SetParent(camera.transform, false);
        LineRenderer ring = reticleObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 32;
        ring.startWidth = 0.035f;
        ring.endWidth = 0.035f;
        ring.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/WhiteGlow.mat");
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.08f, Mathf.Sin(angle) * 0.08f, 0f));
        }

        GazeReticle reticle = reticleObject.AddComponent<GazeReticle>();
        SerializedObject serialized = new SerializedObject(reticle);
        serialized.FindProperty("viewCamera").objectReferenceValue = camera;
        serialized.FindProperty("ring").objectReferenceValue = ring;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.position = position;
        primitive.transform.localScale = scale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        return primitive;
    }
}
