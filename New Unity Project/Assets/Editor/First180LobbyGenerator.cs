using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace First180.Editor
{
    [InitializeOnLoad]
    public static class First180LobbyGenerator
    {
        private const string ScenePath = "Assets/Scenes/FIRST180_Lobby.unity";
        private const string MaterialFolder = "Assets/Materials/FIRST180";
        private const string VersionMarkerPath = "Assets/Scenes/FIRST180_Lobby.version.txt";
        private const string GeneratorVersion = "2.2-mind-storm-inspired";

        private static readonly Color White = new Color(0.82f, 0.84f, 0.84f);
        private static readonly Color SoftWhite = new Color(0.52f, 0.55f, 0.56f);
        private static readonly Color Dark = new Color(0.012f, 0.018f, 0.022f);
        private static readonly Color DarkPanel = new Color(0.018f, 0.03f, 0.036f);
        private static readonly Color Cyan = new Color(0.06f, 0.62f, 0.82f);
        private static readonly Color Orange = new Color(1.0f, 0.34f, 0.025f);

        static First180LobbyGenerator()
        {
            EditorApplication.delayCall += AutoGenerate;
        }

        private static void AutoGenerate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoGenerate;
                return;
            }

            string markerAbsolutePath = Path.Combine(Application.dataPath, "Scenes", "FIRST180_Lobby.version.txt");
            bool currentVersion = File.Exists(markerAbsolutePath)
                && File.ReadAllText(markerAbsolutePath) == GeneratorVersion;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null || !currentVersion)
            {
                GenerateLobby(true);
            }
        }

        [MenuItem("FIRST//180/Regenerate Lobby Scene")]
        private static void RegenerateLobby()
        {
            if (!EditorUtility.DisplayDialog(
                    "Regenerate FIRST//180 Lobby",
                    "This replaces the generated lobby scene. Continue?",
                    "Regenerate",
                    "Cancel"))
            {
                return;
            }

            GenerateLobby(true);
        }

        public static void GenerateFromCommandLine()
        {
            GenerateLobby(true);
        }

        private static void GenerateLobby(bool replaceExisting)
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(MaterialFolder);

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && string.IsNullOrEmpty(activeScene.path) && activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene, "Assets/Scenes/PreLobby_Untitled_Backup.unity");
            }

            Scene lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(lobbyScene);

            Material ceramic = Material("Gallery Ceramic", White, 0.04f, 0.58f);
            Material concrete = Material("Warm Concrete", new Color(0.34f, 0.35f, 0.34f), 0.02f, 0.32f);
            Material wood = Material("Ceiling Walnut", new Color(0.31f, 0.285f, 0.255f), 0.02f, 0.34f);
            Material metal = Material("Brushed Metal", new Color(0.20f, 0.22f, 0.23f), 0.82f, 0.62f);
            Material darkMat = Material("Floor Charcoal", Dark, 0.12f, 0.42f);
            Material panelMat = Material("Display Glass", DarkPanel, 0.55f, 0.86f);
            Material trunkMat = Material("Tree Trunk", new Color(0.12f, 0.055f, 0.025f), 0.01f, 0.24f);
            Material foliageMat = Material("Amber Foliage", new Color(0.82f, 0.20f, 0.018f), 0.02f, 0.34f);
            Material cyanMat = EmissiveMaterial("Lobby Cyan", Cyan, 2.2f);

            GameObject root = new GameObject("FIRST//180 - TRAINING LOBBY");
            BuildArchitecture(root.transform, ceramic, concrete, wood, metal, darkMat, cyanMat);
            BuildCentralSculpture(root.transform, ceramic, concrete, metal, trunkMat, foliageMat, cyanMat);
            BuildScenarioGallery(root.transform, ceramic, metal, panelMat);
            BuildLighting(root.transform);
            Camera camera = BuildCamera(root.transform);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.74f, 0.77f, 0.79f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.54f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.31f, 0.32f);
            RenderSettings.ambientIntensity = 1.12f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.72f, 0.74f, 0.75f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 82f;

            CapturePreview(camera);

            EditorSceneManager.SaveScene(lobbyScene, ScenePath);
            File.WriteAllText(Path.Combine(Application.dataPath, "Scenes", "FIRST180_Lobby.version.txt"), GeneratorVersion);
            AssetDatabase.ImportAsset(VersionMarkerPath, ImportAssetOptions.ForceUpdate);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.AlignViewToObject(camera.transform);
                SceneView.lastActiveSceneView.Repaint();
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("FIRST//180 lobby generated at " + ScenePath);
        }

        private static void BuildArchitecture(
            Transform parent,
            Material ceramic,
            Material concrete,
            Material wood,
            Material metal,
            Material dark,
            Material cyan)
        {
            GameObject architecture = new GameObject("01 - Circular Gallery Architecture");
            architecture.transform.SetParent(parent);
            Vector3 center = new Vector3(0f, 0f, 4f);

            CreateCylinder("Gallery Floor", architecture.transform, center + new Vector3(0f, -0.22f, 0f), new Vector3(15.4f, 0.18f, 15.4f), ceramic);
            CreateCylinder("Inset Floor", architecture.transform, center + new Vector3(0f, -0.015f, 0f), new Vector3(12.9f, 0.035f, 12.9f), ceramic);
            CreateCube("Entry Threshold", architecture.transform, new Vector3(0f, 0.03f, -11.0f), new Vector3(7.6f, 0.09f, 7.0f), ceramic);
            CreateCube("Entry Runner", architecture.transform, new Vector3(0f, 0.085f, -7.5f), new Vector3(4.2f, 0.045f, 8.8f), dark);
            CreateCube("Entry Light Left", architecture.transform, new Vector3(-2.08f, 0.13f, -7.4f), new Vector3(0.045f, 0.02f, 8.3f), cyan);
            CreateCube("Entry Light Right", architecture.transform, new Vector3(2.08f, 0.13f, -7.4f), new Vector3(0.045f, 0.02f, 8.3f), cyan);

            const int wallSegments = 48;
            for (int i = 0; i < wallSegments; i++)
            {
                float angle = i * (360f / wallSegments);
                if (angle > 150f && angle < 210f) continue;

                float radians = angle * Mathf.Deg2Rad;
                Vector3 wallPosition = center + new Vector3(Mathf.Sin(radians) * 14.55f, 0f, Mathf.Cos(radians) * 14.55f);
                Quaternion wallRotation = Quaternion.Euler(0f, angle, 0f);

                for (int band = 0; band < 6; band++)
                {
                    GameObject slat = CreateCube(
                        "Curved Wall Slat " + i.ToString("00") + "-" + band,
                        architecture.transform,
                        wallPosition + Vector3.up * (0.65f + band * 0.86f),
                        new Vector3(1.88f, band == 0 ? 0.52f : 0.24f, 0.25f),
                        band % 2 == 0 ? ceramic : concrete);
                    slat.transform.rotation = wallRotation;
                }

                if (i % 4 == 0)
                {
                    GameObject column = CreateCube(
                        "Structural Column " + i.ToString("00"),
                        architecture.transform,
                        wallPosition * 0.992f + Vector3.up * 3.35f,
                        new Vector3(0.42f, 6.6f, 0.62f),
                        concrete);
                    column.transform.rotation = wallRotation;
                    column.transform.localRotation *= Quaternion.Euler(0f, 0f, i % 8 == 0 ? -5f : 5f);
                }
            }

            for (int i = 0; i < 24; i++)
            {
                float angle = i * 15f;
                float radians = angle * Mathf.Deg2Rad;
                float length = i % 3 == 0 ? 7.1f : 8.2f;
                float midRadius = 4.25f + length * 0.5f;
                Vector3 position = center + new Vector3(Mathf.Sin(radians) * midRadius, 7.45f, Mathf.Cos(radians) * midRadius);
                GameObject petal = CreateCube(
                    "Timber Ceiling Petal " + (i + 1).ToString("00"),
                    architecture.transform,
                    position,
                    new Vector3(i % 2 == 0 ? 1.45f : 1.12f, 0.28f, length),
                    wood);
                petal.transform.rotation = Quaternion.Euler(0f, angle, i % 2 == 0 ? 0.8f : -0.8f);

                float rimX = Mathf.Sin(radians) * 4.45f;
                float rimZ = Mathf.Cos(radians) * 4.45f;
                GameObject rim = CreateCube(
                    "Oculus Rim " + (i + 1).ToString("00"),
                    architecture.transform,
                    center + new Vector3(rimX, 7.28f, rimZ),
                    new Vector3(1.18f, 0.22f, 0.34f),
                    metal);
                rim.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            for (int i = 0; i < 36; i++)
            {
                float angle = i * 10f;
                if (angle > 145f && angle < 215f) continue;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 railPosition = center + new Vector3(Mathf.Sin(radians) * 12.65f, 4.75f, Mathf.Cos(radians) * 12.65f);
                GameObject rail = CreateCube("Upper Gallery Rail " + i.ToString("00"), architecture.transform, railPosition, new Vector3(2.05f, 0.12f, 0.14f), metal);
                rail.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                GameObject lowerRail = CreateCube("Upper Gallery Fascia " + i.ToString("00"), architecture.transform, railPosition + Vector3.down * 0.72f, new Vector3(2.05f, 0.22f, 0.38f), ceramic);
                lowerRail.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            for (int step = 0; step < 10; step++)
            {
                Vector3 stepPosition = new Vector3(-8.8f - step * 0.26f, 0.18f + step * 0.38f, 6.0f + step * 0.48f);
                GameObject stair = CreateCube("Floating Stair " + (step + 1).ToString("00"), architecture.transform, stepPosition, new Vector3(3.5f, 0.20f, 0.82f), ceramic);
                stair.transform.rotation = Quaternion.Euler(0f, -28f, 0f);
            }

            CreateText("Gallery Title", architecture.transform, "FIRST//180", new Vector3(0f, 6.82f, 17.6f), 0.62f, new Color(0.08f, 0.11f, 0.13f), FontStyle.Bold);
            CreateText("Gallery Subtitle", architecture.transform, "IMMERSIVE EMERGENCY RESPONSE GALLERY", new Vector3(0f, 6.38f, 17.55f), 0.16f, Cyan, FontStyle.Bold);
            CreateText("Entry Instruction", architecture.transform, "SELECT A TRAINING SCENARIO", new Vector3(0f, 0.16f, -5.6f), 0.19f, new Color(0.72f, 0.78f, 0.8f), FontStyle.Bold, new Vector3(90f, 0f, 0f));
        }

        private static void BuildCentralSculpture(
            Transform parent,
            Material ceramic,
            Material concrete,
            Material metal,
            Material trunk,
            Material foliage,
            Material cyan)
        {
            GameObject sculpture = new GameObject("02 - Living Response Tree");
            sculpture.transform.SetParent(parent);
            Vector3 treeCenter = new Vector3(0f, 0f, 3.75f);

            CreateCylinder("Tree Well", sculpture.transform, treeCenter + new Vector3(0f, 0.16f, 0f), new Vector3(2.3f, 0.18f, 2.3f), metal);
            CreateCylinder("Soil Bed", sculpture.transform, treeCenter + new Vector3(0f, 0.36f, 0f), new Vector3(1.92f, 0.08f, 1.92f), concrete);

            for (int i = 0; i < 11; i++)
            {
                float angle = 196f + i * 14.8f;
                float radians = angle * Mathf.Deg2Rad;
                float radius = 3.0f + (i % 2) * 0.25f;
                Vector3 benchPosition = treeCenter + new Vector3(Mathf.Sin(radians) * radius, 0.48f, Mathf.Cos(radians) * radius);
                GameObject bench = CreateCube("Radial Bench " + (i + 1).ToString("00"), sculpture.transform, benchPosition, new Vector3(1.55f, 0.18f, 0.72f), ceramic);
                bench.transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 supportPosition = treeCenter + new Vector3(Mathf.Sin(radians) * (radius - 0.18f), 0.26f, Mathf.Cos(radians) * (radius - 0.18f));
                GameObject support = CreateCube("Bench Support " + (i + 1).ToString("00"), sculpture.transform, supportPosition, new Vector3(0.58f, 0.38f, 0.45f), concrete);
                support.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            Vector3 trunkBase = treeCenter + Vector3.up * 0.38f;
            Vector3 trunkFork = treeCenter + new Vector3(0f, 3.55f, 0f);
            CreateBeam("Main Trunk", sculpture.transform, trunkBase, trunkFork, 0.28f, trunk);

            Vector3[] crownPoints =
            {
                treeCenter + new Vector3(-2.65f, 5.15f, 0.15f),
                treeCenter + new Vector3(-1.55f, 5.85f, -0.2f),
                treeCenter + new Vector3(-0.35f, 6.1f, 0.45f),
                treeCenter + new Vector3(0.85f, 5.95f, -0.35f),
                treeCenter + new Vector3(2.15f, 5.45f, 0.25f),
                treeCenter + new Vector3(-1.9f, 4.75f, -1.0f),
                treeCenter + new Vector3(1.85f, 4.8f, -0.85f),
                treeCenter + new Vector3(0.1f, 5.2f, 1.35f)
            };

            for (int i = 0; i < crownPoints.Length; i++)
            {
                Vector3 joint = trunkFork + new Vector3((i % 3 - 1) * 0.16f, (i % 2) * 0.18f, (i % 4 - 1.5f) * 0.08f);
                CreateBeam("Primary Branch " + (i + 1).ToString("00"), sculpture.transform, joint, crownPoints[i], 0.10f, trunk);
                CreateBeam("Secondary Branch " + (i + 1).ToString("00"), sculpture.transform, crownPoints[i], crownPoints[i] + new Vector3((i % 2 == 0 ? -0.7f : 0.7f), 0.35f, (i % 3 - 1) * 0.45f), 0.055f, trunk);
            }

            for (int i = 0; i < 112; i++)
            {
                float angle = i * 2.399963f;
                float layer = i % 11;
                float radius = 0.58f + layer * 0.27f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius * 0.68f;
                float y = 4.48f + (i % 13) * 0.13f + Mathf.Sin(angle * 1.7f) * 0.2f;
                Vector3 leafScale = new Vector3(0.42f + (i % 4) * 0.11f, 0.12f + (i % 3) * 0.04f, 0.25f + (i % 5) * 0.048f);
                GameObject leaf = CreateSphere("Amber Leaf Cluster " + (i + 1).ToString("00"), sculpture.transform, treeCenter + new Vector3(x, y, z), leafScale, foliage);
                leaf.transform.rotation = Quaternion.Euler((i * 13f) % 35f, (i * 47f) % 360f, (i * 7f) % 25f);
            }

            CreateText("Tree Caption", sculpture.transform, "RESPONSE GARDEN", treeCenter + new Vector3(0f, 0.86f, -3.05f), 0.18f, Cyan, FontStyle.Bold);
        }

        private static void BuildScenarioGallery(Transform parent, Material ceramic, Material metal, Material panel)
        {
            GameObject gallery = new GameObject("03 - Scenario Exhibitions");
            gallery.transform.SetParent(parent);

            string[] titles =
            {
                "ANAPHYLAXIS",
                "OBSTRUCTED\nAIRWAYS",
                "TRAUMATIC\nHEMORRHAGING",
                "LOST LIMB",
                "FROSTBITE"
            };
            string[] symbols = { "A", "O", "H", "L", "F" };
            Color[] colors =
            {
                new Color(0.08f, 0.66f, 0.92f),
                new Color(1f, 0.48f, 0.05f),
                new Color(0.95f, 0.08f, 0.08f),
                new Color(0.75f, 0.12f, 0.52f),
                new Color(0.16f, 0.58f, 0.92f)
            };
            float[] angles = { -58f, -29f, 0f, 29f, 58f };

            for (int i = 0; i < titles.Length; i++)
            {
                float radians = angles[i] * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Sin(radians) * 11.8f, 0f, 4f + Mathf.Cos(radians) * 11.8f);
                Material accent = EmissiveMaterial("Scenario " + (i + 1).ToString("00"), colors[i], 2.25f);
                CreateScenarioExhibit(gallery.transform, i + 1, titles[i], symbols[i], position, colors[i], ceramic, metal, panel, accent);
            }
        }

        private static void CreateScenarioExhibit(
            Transform parent,
            int number,
            string title,
            string symbol,
            Vector3 position,
            Color accentColor,
            Material ceramic,
            Material metal,
            Material panel,
            Material accent)
        {
            GameObject exhibit = new GameObject(number.ToString("00") + " - " + title.Replace("\n", " "));
            exhibit.transform.SetParent(parent);
            exhibit.transform.position = position;
            Vector3 outward = (position - new Vector3(0f, 0f, 4f)).normalized;
            exhibit.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

            CreateCubeLocal("Architectural Blade", exhibit.transform, new Vector3(0f, 2.75f, 0.32f), new Vector3(3.65f, 5.55f, 0.48f), ceramic);
            CreateCubeLocal("Dark Display", exhibit.transform, new Vector3(0f, 2.78f, -0.01f), new Vector3(3.08f, 4.75f, 0.16f), panel);
            CreateCubeLocal("Display Inset", exhibit.transform, new Vector3(0f, 2.75f, -0.115f), new Vector3(2.76f, 4.35f, 0.055f), metal);
            CreateCubeLocal("Glass Face", exhibit.transform, new Vector3(0f, 2.75f, -0.18f), new Vector3(2.56f, 4.10f, 0.045f), panel);
            CreateCubeLocal("Accent Header", exhibit.transform, new Vector3(0f, 4.88f, -0.27f), new Vector3(2.55f, 0.06f, 0.06f), accent);
            CreateCubeLocal("Accent Foot", exhibit.transform, new Vector3(0f, 0.68f, -0.27f), new Vector3(2.55f, 0.04f, 0.06f), accent);
            CreateCubeLocal("Exhibit Plinth", exhibit.transform, new Vector3(0f, 0.14f, -0.5f), new Vector3(3.8f, 0.26f, 1.45f), ceramic);
            CreateCubeLocal("Floor Marker", exhibit.transform, new Vector3(0f, 0.30f, -1.35f), new Vector3(2.25f, 0.025f, 1.15f), accent);

            CreateTextLocal("Module Number", exhibit.transform, "MODULE " + number.ToString("00"), new Vector3(0f, 4.52f, -0.245f), 0.15f, accentColor, FontStyle.Bold);
            CreateTextLocal("Scenario Symbol", exhibit.transform, symbol, new Vector3(0f, 3.62f, -0.25f), 0.62f, accentColor, FontStyle.Bold);
            CreateTextLocal("Scenario Title", exhibit.transform, title, new Vector3(0f, 2.58f, -0.25f), 0.25f, Color.white, FontStyle.Bold);
            CreateTextLocal("Training Label", exhibit.transform, "TRAINING SCENARIO", new Vector3(0f, 1.55f, -0.25f), 0.13f, accentColor, FontStyle.Bold);
            CreateTextLocal("Offline Label", exhibit.transform, "DISPLAY ONLY  |  OFFLINE", new Vector3(0f, 1.08f, -0.25f), 0.105f, new Color(0.62f, 0.67f, 0.69f), FontStyle.Normal);

            Vector3 lightPosition = exhibit.transform.TransformPoint(new Vector3(0f, 5.25f, -2.1f));
            Vector3 lightTarget = exhibit.transform.TransformPoint(new Vector3(0f, 2.2f, -0.25f));
            AddSpotLight(exhibit.transform, "Exhibit Light", lightPosition, lightTarget, accentColor, 5.1f, 8.5f, 42f);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject lighting = new GameObject("04 - Architectural Lighting");
            lighting.transform.SetParent(parent);

            GameObject sunObject = new GameObject("Oculus Sunlight");
            sunObject.transform.SetParent(lighting.transform);
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.71f);
            sun.intensity = 1.18f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.76f;

            AddPointLight(lighting.transform, "Oculus Bounce", new Vector3(0f, 6.8f, 4f), new Color(1f, 0.73f, 0.42f), 7.5f, 13f, true);
            AddPointLight(lighting.transform, "Entry Fill", new Vector3(0f, 4.1f, -8f), new Color(0.58f, 0.78f, 0.9f), 4.5f, 15f, false);
            AddPointLight(lighting.transform, "Left Gallery Fill", new Vector3(-9f, 4.2f, 6f), new Color(0.73f, 0.82f, 0.88f), 3.8f, 12f, false);
            AddPointLight(lighting.transform, "Right Gallery Fill", new Vector3(9f, 4.2f, 6f), new Color(0.73f, 0.82f, 0.88f), 3.8f, 12f, false);
            AddPointLight(lighting.transform, "Rear Gallery Fill", new Vector3(0f, 5.0f, 14f), new Color(0.76f, 0.86f, 0.94f), 4.2f, 11f, false);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f + 22.5f;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Sin(radians) * 8.3f, 6.7f, 4f + Mathf.Cos(radians) * 8.3f);
                AddSpotLight(lighting.transform, "Ceiling Wash " + (i + 1).ToString("00"), position, position + Vector3.down * 6f, new Color(0.87f, 0.91f, 0.93f), 3.6f, 10f, 54f);
            }

            GameObject probeObject = new GameObject("Gallery Reflection Probe");
            probeObject.transform.SetParent(lighting.transform);
            probeObject.transform.position = new Vector3(0f, 3.2f, 4f);
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.size = new Vector3(28f, 9f, 28f);
            probe.intensity = 0.75f;
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Lobby Preview Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = new Vector3(0f, 2.25f, -13.2f);
            cameraObject.transform.LookAt(new Vector3(0f, 3.0f, 5.55f));

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.69f, 0.70f);
            camera.allowHDR = true;
            camera.renderingPath = RenderingPath.DeferredShading;
            camera.tag = "MainCamera";
            return camera;
        }

        private static void BuildArchitectureLegacy(
            Transform parent,
            Material white,
            Material softWhite,
            Material dark,
            Material panel,
            Material cyan)
        {
            GameObject architecture = new GameObject("01 - Architecture");
            architecture.transform.SetParent(parent);

            CreateCube("Reflective Floor", architecture.transform, new Vector3(0f, -0.3f, 3f), new Vector3(30f, 0.55f, 40f), dark);
            CreateCube("Back Wall", architecture.transform, new Vector3(0f, 3.4f, 14.2f), new Vector3(30f, 7.2f, 0.5f), softWhite);
            CreateCube("Left Wall", architecture.transform, new Vector3(-15f, 3.1f, 3f), new Vector3(0.45f, 6.4f, 40f), white);
            CreateCube("Right Wall", architecture.transform, new Vector3(15f, 3.1f, 3f), new Vector3(0.45f, 6.4f, 40f), white);

            CreateCube("Entry Platform", architecture.transform, new Vector3(0f, 0.05f, -13.4f), new Vector3(10f, 0.16f, 5.2f), white);
            CreateCube("Central Walkway", architecture.transform, new Vector3(0f, 0.02f, 1.5f), new Vector3(7.5f, 0.08f, 24f), panel);
            CreateCube("Walkway Glow Left", architecture.transform, new Vector3(-3.7f, 0.09f, 1.5f), new Vector3(0.08f, 0.035f, 24f), cyan);
            CreateCube("Walkway Glow Right", architecture.transform, new Vector3(3.7f, 0.09f, 1.5f), new Vector3(0.08f, 0.035f, 24f), cyan);

            for (int i = 0; i < 8; i++)
            {
                float z = -13f + i * 4.1f;
                GameObject rib = new GameObject("Ceiling Rib " + (i + 1).ToString("00"));
                rib.transform.SetParent(architecture.transform);

                CreateCube("Left Column", rib.transform, new Vector3(-13.6f, 3.25f, z), new Vector3(0.28f, 6.5f, 0.4f), white);
                CreateCube("Right Column", rib.transform, new Vector3(13.6f, 3.25f, z), new Vector3(0.28f, 6.5f, 0.4f), white);
                CreateBeam("Left Sweep", rib.transform, new Vector3(-13.6f, 5.9f, z), new Vector3(-9.5f, 7.15f, z), 0.14f, white);
                CreateBeam("Right Sweep", rib.transform, new Vector3(13.6f, 5.9f, z), new Vector3(9.5f, 7.15f, z), 0.14f, white);
                CreateCube("Ceiling Spine", rib.transform, new Vector3(0f, 7.15f, z), new Vector3(19f, 0.22f, 0.34f), softWhite);
                CreateCube("Ceiling Light", rib.transform, new Vector3(0f, 7.02f, z - 0.02f), new Vector3(5.5f, 0.045f, 0.22f), cyan);
            }

            for (int i = 0; i < 6; i++)
            {
                float z = -9f + i * 4.2f;
                CreateCube("Left Wall Panel " + (i + 1), architecture.transform, new Vector3(-14.68f, 3.1f, z), new Vector3(0.1f, 4.2f, 2.8f), panel);
                CreateCube("Right Wall Panel " + (i + 1), architecture.transform, new Vector3(14.68f, 3.1f, z), new Vector3(0.1f, 4.2f, 2.8f), panel);
                CreateCube("Left Panel Light " + (i + 1), architecture.transform, new Vector3(-14.61f, 4.8f, z), new Vector3(0.04f, 0.08f, 1.8f), cyan);
                CreateCube("Right Panel Light " + (i + 1), architecture.transform, new Vector3(14.61f, 4.8f, z), new Vector3(0.04f, 0.08f, 1.8f), cyan);
            }

            CreateText("Main Title", architecture.transform, "FIRST//180", new Vector3(0f, 6.35f, 13.82f), 1.0f, Color.white, FontStyle.Bold);
            CreateText("Lobby Instruction", architecture.transform, "SELECT A TRAINING SCENARIO", new Vector3(0f, 5.25f, 13.8f), 0.36f, Cyan, FontStyle.Bold);
            CreateText("Prototype Label", architecture.transform, "LOBBY PROTOTYPE  |  SCENARIO LOGIC COMING NEXT", new Vector3(0f, 0.22f, -10.6f), 0.19f, SoftWhite, FontStyle.Normal, new Vector3(90f, 0f, 0f));
        }

        private static void BuildCentralSculptureLegacy(
            Transform parent,
            Material white,
            Material dark,
            Material cyan,
            Material orange)
        {
            GameObject sculpture = new GameObject("02 - Response Tree Sculpture");
            sculpture.transform.SetParent(parent);

            CreateCylinder("Base", sculpture.transform, new Vector3(0f, 0.16f, -1.3f), new Vector3(2.4f, 0.14f, 2.4f), dark);
            CreateCylinder("Base Ring", sculpture.transform, new Vector3(0f, 0.34f, -1.3f), new Vector3(1.85f, 0.05f, 1.85f), cyan);
            CreateCylinder("White Plinth", sculpture.transform, new Vector3(0f, 0.48f, -1.3f), new Vector3(1.4f, 0.14f, 1.4f), white);

            Vector3 trunkBase = new Vector3(0f, 0.58f, -1.3f);
            Vector3 trunkTop = new Vector3(0f, 3.5f, -1.3f);
            CreateBeam("Trunk", sculpture.transform, trunkBase, trunkTop, 0.28f, orange);

            Vector3[] branchEnds =
            {
                new Vector3(-2.3f, 4.45f, -1.2f),
                new Vector3(-1.4f, 5.05f, -1.0f),
                new Vector3(0f, 5.25f, -1.25f),
                new Vector3(1.55f, 5.0f, -1.5f),
                new Vector3(2.4f, 4.35f, -1.45f),
                new Vector3(-1.9f, 4.0f, -0.15f),
                new Vector3(1.9f, 4.0f, -2.35f)
            };

            for (int i = 0; i < branchEnds.Length; i++)
            {
                Vector3 joint = trunkTop + new Vector3((i - 3) * 0.06f, 0.12f, 0f);
                CreateBeam("Branch " + (i + 1).ToString("00"), sculpture.transform, joint, branchEnds[i], 0.11f, orange);
            }

            for (int i = 0; i < 28; i++)
            {
                float angle = i * 2.39996f;
                float radius = 0.8f + (i % 5) * 0.34f;
                float x = Mathf.Cos(angle) * radius;
                float z = -1.3f + Mathf.Sin(angle) * radius * 0.48f;
                float y = 4.15f + (i % 7) * 0.18f;
                float scale = 0.2f + (i % 3) * 0.06f;
                CreateSphere("Signal Leaf " + (i + 1).ToString("00"), sculpture.transform, new Vector3(x, y, z), Vector3.one * scale, orange);
            }

            CreateText("Sculpture Caption", sculpture.transform, "RESPONSE NETWORK", new Vector3(0f, 0.82f, -3.0f), 0.2f, Cyan, FontStyle.Bold);

            GameObject glow = new GameObject("Sculpture Glow");
            glow.transform.SetParent(sculpture.transform);
            glow.transform.position = new Vector3(0f, 3.6f, -1.3f);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Orange;
            light.intensity = 7f;
            light.range = 8f;
            light.shadows = LightShadows.Soft;
        }

        private static void BuildScenarioGalleryLegacy(Transform parent, Material white, Material panel)
        {
            GameObject gallery = new GameObject("03 - Scenario Gallery");
            gallery.transform.SetParent(parent);

            string[] titles =
            {
                "ANAPHYLAXIS",
                "OBSTRUCTED\nAIRWAYS",
                "TRAUMATIC\nHEMORRHAGING",
                "LOST LIMB",
                "FROSTBITE"
            };

            string[] symbols = { "A", "O", "H", "L", "F" };
            Color[] colors =
            {
                new Color(0.1f, 0.8f, 1f),
                new Color(1f, 0.62f, 0.08f),
                new Color(1f, 0.18f, 0.2f),
                new Color(0.9f, 0.2f, 0.65f),
                new Color(0.35f, 0.78f, 1f)
            };

            float[] xPositions = { -10.4f, -5.2f, 0f, 5.2f, 10.4f };
            for (int i = 0; i < titles.Length; i++)
            {
                Material accent = EmissiveMaterial("Scenario " + (i + 1).ToString("00"), colors[i], 3f);
                CreateScenarioPortalLegacy(gallery.transform, i + 1, titles[i], symbols[i], xPositions[i], 11.65f, white, panel, accent, colors[i]);
            }
        }

        private static void CreateScenarioPortalLegacy(
            Transform parent,
            int number,
            string title,
            string symbol,
            float x,
            float z,
            Material white,
            Material panel,
            Material accent,
            Color accentColor)
        {
            GameObject portal = new GameObject(number.ToString("00") + " - " + title.Replace("\n", " "));
            portal.transform.SetParent(parent);

            CreateCube("Display Back", portal.transform, new Vector3(x, 2.85f, z), new Vector3(4.25f, 5.15f, 0.34f), panel);
            CreateCube("Left Frame", portal.transform, new Vector3(x - 2.13f, 2.85f, z - 0.18f), new Vector3(0.22f, 5.6f, 0.45f), white);
            CreateCube("Right Frame", portal.transform, new Vector3(x + 2.13f, 2.85f, z - 0.18f), new Vector3(0.22f, 5.6f, 0.45f), white);
            CreateCube("Top Frame", portal.transform, new Vector3(x, 5.62f, z - 0.18f), new Vector3(4.48f, 0.22f, 0.45f), white);
            CreateCube("Bottom Frame", portal.transform, new Vector3(x, 0.12f, z - 0.18f), new Vector3(4.48f, 0.22f, 0.45f), white);

            CreateCube("Left Accent", portal.transform, new Vector3(x - 1.96f, 2.9f, z - 0.44f), new Vector3(0.055f, 4.75f, 0.08f), accent);
            CreateCube("Right Accent", portal.transform, new Vector3(x + 1.96f, 2.9f, z - 0.44f), new Vector3(0.055f, 4.75f, 0.08f), accent);
            CreateCube("Header Accent", portal.transform, new Vector3(x, 5.42f, z - 0.44f), new Vector3(3.9f, 0.055f, 0.08f), accent);
            CreateCube("Floor Accent", portal.transform, new Vector3(x, 0.24f, z - 1.45f), new Vector3(3.2f, 0.04f, 2.2f), accent);

            CreateText("Module Number", portal.transform, number.ToString("00"), new Vector3(x - 1.45f, 4.85f, z - 0.42f), 0.22f, accentColor, FontStyle.Bold);
            CreateText("Scenario Symbol", portal.transform, symbol, new Vector3(x, 3.75f, z - 0.42f), 0.72f, accentColor, FontStyle.Bold);
            CreateText("Scenario Title", portal.transform, title, new Vector3(x, 2.45f, z - 0.42f), 0.31f, Color.white, FontStyle.Bold);
            CreateText("Training Status", portal.transform, "TRAINING MODULE", new Vector3(x, 1.42f, z - 0.42f), 0.17f, accentColor, FontStyle.Bold);
            CreateText("Prototype Status", portal.transform, "DISPLAY ONLY  |  OFFLINE", new Vector3(x, 0.92f, z - 0.42f), 0.13f, SoftWhite, FontStyle.Normal);

            GameObject spotlightObject = new GameObject("Portal Light");
            spotlightObject.transform.SetParent(portal.transform);
            spotlightObject.transform.position = new Vector3(x, 5.1f, z - 2f);
            spotlightObject.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            Light spotlight = spotlightObject.AddComponent<Light>();
            spotlight.type = LightType.Spot;
            spotlight.color = accentColor;
            spotlight.intensity = 4.5f;
            spotlight.range = 9f;
            spotlight.spotAngle = 48f;
            spotlight.shadows = LightShadows.Soft;
        }

        private static void BuildLightingLegacy(Transform parent)
        {
            GameObject lighting = new GameObject("04 - Lighting");
            lighting.transform.SetParent(parent);

            GameObject sunObject = new GameObject("Soft Directional Light");
            sunObject.transform.SetParent(lighting.transform);
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.9f, 0.96f, 1f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;

            for (int i = 0; i < 6; i++)
            {
                GameObject lightObject = new GameObject("Lobby Fill " + (i + 1).ToString("00"));
                lightObject.transform.SetParent(lighting.transform);
                lightObject.transform.position = new Vector3((i % 2 == 0 ? -1f : 1f) * 8f, 5.4f, -9f + (i / 2) * 8f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = i % 2 == 0 ? new Color(0.75f, 0.93f, 1f) : new Color(1f, 0.95f, 0.85f);
                light.intensity = 3.8f;
                light.range = 11f;
                light.shadows = LightShadows.Soft;
            }
        }

        private static Camera BuildCameraLegacy(Transform parent)
        {
            GameObject cameraObject = new GameObject("Lobby Preview Camera");
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = new Vector3(0f, 2.15f, -15.3f);
            cameraObject.transform.LookAt(new Vector3(0f, 2.75f, 7.5f));

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 67f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.78f, 0.82f);
            camera.allowHDR = true;
            camera.tag = "MainCamera";
            return camera;
        }

        private static void CapturePreview(Camera camera)
        {
            EnsureFolder("Assets/Preview");

            const int width = 1600;
            const int height = 900;
            RenderTexture previewTarget = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = RenderTexture.active;
            Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = previewTarget;
                RenderTexture.active = previewTarget;
                camera.Render();
                preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                preview.Apply();

                string previewPath = Path.Combine(Application.dataPath, "Preview", "FIRST180_Lobby_Preview.png");
                File.WriteAllBytes(previewPath, preview.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousTarget;
                RenderTexture.ReleaseTemporary(previewTarget);
                UnityEngine.Object.DestroyImmediate(preview);
            }

            AssetDatabase.ImportAsset("Assets/Preview/FIRST180_Lobby_Preview.png", ImportAssetOptions.ForceUpdate);
        }

        private static GameObject CreateCubeLocal(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject CreateTextLocal(
            string name,
            Transform parent,
            string value,
            Vector3 localPosition,
            float size,
            Color color,
            FontStyle style)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 96;
            mesh.characterSize = size / 10f;
            mesh.fontStyle = style;
            mesh.color = color;
            mesh.richText = false;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private static void AddPointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            bool castShadows)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void AddSpotLight(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 target,
            Color color,
            float intensity,
            float range,
            float spotAngle)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            lightObject.transform.LookAt(target);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = LightShadows.Soft;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject CreateBeam(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 direction = end - start;
            GameObject beam = CreateCylinder(name, parent, (start + end) * 0.5f, new Vector3(radius, direction.magnitude * 0.5f, radius), material);
            beam.transform.up = direction.normalized;
            return beam;
        }

        private static GameObject CreateText(
            string name,
            Transform parent,
            string value,
            Vector3 position,
            float size,
            Color color,
            FontStyle style,
            Vector3? rotation = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(rotation ?? Vector3.zero);

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 96;
            mesh.characterSize = size / 10f;
            mesh.fontStyle = style;
            mesh.color = color;
            mesh.richText = false;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EmissiveMaterial(string name, Color color, float intensity)
        {
            Material material = Material(name, color, 0.2f, 0.72f);
            Color emission = color * intensity;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
