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

        private static readonly Color White = new Color(0.92f, 0.96f, 0.98f);
        private static readonly Color SoftWhite = new Color(0.65f, 0.73f, 0.78f);
        private static readonly Color Dark = new Color(0.018f, 0.035f, 0.055f);
        private static readonly Color DarkPanel = new Color(0.035f, 0.075f, 0.105f);
        private static readonly Color Cyan = new Color(0.08f, 0.78f, 1.0f);
        private static readonly Color Orange = new Color(1.0f, 0.42f, 0.06f);

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

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                GenerateLobby(false);
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

        private static void GenerateLobby(bool replaceExisting)
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(MaterialFolder);

            if (replaceExisting && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene, "Assets/Scenes/PreLobby_Untitled_Backup.unity");
            }

            Scene lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(lobbyScene);

            Material whiteMat = Material("Architectural White", White, 0.12f, 0.75f);
            Material softWhiteMat = Material("Soft White", SoftWhite, 0.18f, 0.55f);
            Material darkMat = Material("Floor Black", Dark, 0.72f, 0.9f);
            Material panelMat = Material("Display Panel", DarkPanel, 0.5f, 0.72f);
            Material cyanMat = EmissiveMaterial("Lobby Cyan", Cyan, 3.2f);
            Material orangeMat = EmissiveMaterial("Sculpture Orange", Orange, 2.6f);

            GameObject root = new GameObject("FIRST//180 - TRAINING LOBBY");
            BuildArchitecture(root.transform, whiteMat, softWhiteMat, darkMat, panelMat, cyanMat);
            BuildCentralSculpture(root.transform, whiteMat, darkMat, cyanMat, orangeMat);
            BuildScenarioGallery(root.transform, whiteMat, panelMat);
            BuildLighting(root.transform);
            Camera camera = BuildCamera(root.transform);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.34f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.04f, 0.055f);
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.76f, 0.82f, 0.85f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 32f;
            RenderSettings.fogEndDistance = 68f;

            CapturePreview(camera);

            EditorSceneManager.SaveScene(lobbyScene, ScenePath);
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

        private static void BuildCentralSculpture(
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

        private static void BuildScenarioGallery(Transform parent, Material white, Material panel)
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
                CreateScenarioPortal(gallery.transform, i + 1, titles[i], symbols[i], xPositions[i], 11.65f, white, panel, accent, colors[i]);
            }
        }

        private static void CreateScenarioPortal(
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

        private static void BuildLighting(Transform parent)
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

        private static Camera BuildCamera(Transform parent)
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

            const int width = 1280;
            const int height = 720;
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
