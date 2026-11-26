using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice.Editor
{
    public static class PracticeEnvironmentArt
    {
        const string Shared = "Assets/Practice/Art/Shared/";
        [MenuItem("FIRST180 Practice/Environment/Apply art to open scenario")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            var room = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PracticeRoom>(true)).SingleOrDefault();
            if (!room || room.scenarioId < 0 || room.scenarioId > 4 || EditorApplication.isPlaying)
                throw new InvalidOperationException("Open a practice scenario in Edit Mode first.");
            var art = scene.GetRootGameObjects().FirstOrDefault(o => o.name == "Location art");
            if (!art) art = new GameObject("Location art");
            switch (room.scenarioId)
            {
                case 0: case 1: InteriorArt.Apply(room, art.transform); break;
                case 2: case 3: FieldArt.Apply(room, art.transform); break;
                case 4: AlpineArt.Apply(room, art.transform); break;
            }
            ApplyShared(room, art, scene);
        }

        internal static void ApplyShared(PracticeRoom room, GameObject art, Scene scene)
        {
            if (room.incidentRoot) room.incidentRoot.SetActive(false);
            var shell = scene.GetRootGameObjects().Single(o => o.name.StartsWith("Environment / ", StringComparison.Ordinal));
            foreach (Transform item in shell.transform)
                if ((item.name == "Oak tabletop" || item.name == "Table pedestal" || item.name == "Table base")
                    && Mathf.Abs(item.position.x + 5) < .05f && Mathf.Abs(item.position.z + 4) < .05f)
                    item.gameObject.SetActive(false);
            var equipment = art.transform.Find("Response cart");
            if (equipment) UnityEngine.Object.DestroyImmediate(equipment.gameObject);
            MakeCart(art.transform);
            ResponseToolArt.Apply(room);
            var labels = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<TextMesh>(true)).ToArray();
            var lettering = Finish("World type", Color.white, "FIRST180/World lettering");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var sign in labels)
            {
                if (!sign.font) sign.font = font;
                if (sign.name == "Patient marker")
                {
                    sign.characterSize = .028f;
                    sign.color = new Color(.09f,.19f,.19f);
                }
                if (sign.name == "Equipment station label")
                {
                    sign.text = "RESPONSE KIT";
                    sign.transform.position = new Vector3(-5.35f,.65f,-4.37f);
                    sign.characterSize = .024f;
                    sign.color = Color.white;
                }
                if (sign.name == "Return label")
                {
                    sign.text = "RETURN\nTO HUB";
                    sign.characterSize = .064f;
                }
                if (sign.name == "Equipment label")
                    FitTrayLabel(sign, art.transform.Find("Response cart"));
                sign.GetComponent<MeshRenderer>().sharedMaterial = lettering;
            }
            var binder = room.GetComponent<WorldSignAtlas>();
            if (!binder) binder = room.gameObject.AddComponent<WorldSignAtlas>();
            binder.signs = labels;
            binder.Refresh();
            foreach (var renderer in art.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<TextMesh>()) continue;
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags | StaticEditorFlags.BatchingStatic);
                if (renderer.bounds.size.sqrMagnitude < .20f)
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (room.scenarioId == 3)
            {
                foreach (var lamp in shell.GetComponentsInChildren<Light>())
                    if (lamp.type == LightType.Point) lamp.renderMode = LightRenderMode.ForceVertex;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
        }

        static void FitTrayLabel(TextMesh sign, Transform cart)
        {
            var gear = sign.transform.parent.GetComponentsInChildren<TreatmentTool>(true);
            var tool = gear.OrderBy(t => (new Vector2(t.transform.position.x-sign.transform.position.x,t.transform.position.z-sign.transform.position.z)).sqrMagnitude).First();
            sign.text = tool.name.Replace(" practice", "").Replace(" trainer", "").Replace(" ", "\n");
            var pose = tool.transform.position + new Vector3(0,-.063f,-.234f);
            var rotation = Quaternion.Euler(78,0,0);
            sign.transform.SetPositionAndRotation(pose,rotation);
            sign.color = Color.white;
            sign.characterSize = .018f;
            var size = sign.GetComponent<MeshRenderer>().localBounds.size;
            sign.transform.localScale = Vector3.one * Mathf.Min(.46f/Mathf.Max(.01f,size.x), .115f/Mathf.Max(.01f,size.y));
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = tool.name + " tray label";
            plate.transform.SetParent(cart,false);
            plate.transform.SetPositionAndRotation(pose + rotation*Vector3.forward*.007f, rotation);
            plate.transform.localScale = new Vector3(.50f,.15f,.008f);
            plate.GetComponent<Renderer>().sharedMaterial = Finish("Deep enamel",new Color(.1f,.26f,.25f));
            UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
        }

        static Material Finish(string name, Color color, string shader = "Standard")
        {
            string path = Shared + name + ".mat";
            var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value) return value;
            var source = Shader.Find(shader);
            if (!source) throw new InvalidOperationException("Missing shader " + shader);
            value = new Material(source) { color = color };
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        static void MakeCart(Transform art)
        {
            var cart = new GameObject("Response cart").transform;
            cart.SetParent(art, false);
            cart.position = new Vector3(-5.35f,0,-3.35f);
            var body = Finish("Deep enamel", new Color(.1f,.26f,.25f));
            var tray = Finish("Linen enamel", new Color(.83f,.85f,.79f));
            var wheel = Finish("Caster rubber", new Color(.13f,.15f,.15f));
            var metal = Finish("Brushed rails", new Color(.53f,.57f,.55f));
            void Part(string name, Vector3 position, Vector3 scale, Material material, bool solid = false, bool tire = false)
            {
                var item = GameObject.CreatePrimitive(tire ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                item.name = name;
                item.transform.SetParent(cart, false);
                item.transform.localPosition = position;
                item.transform.localScale = scale;
                if (tire) item.transform.localRotation = Quaternion.Euler(0,0,90);
                item.GetComponent<Renderer>().sharedMaterial = material;
                if (!solid) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            }
            Part("Instrument worktop",new Vector3(0,.85f,0),new Vector3(2.62f,.09f,2.05f),tray,true);
            Part("Enamel drawer housing",new Vector3(0,.55f,0),new Vector3(2.51f,.49f,1.99f),body,true);
            Part("Lower shelf",new Vector3(0,.26f,0),new Vector3(2.50f,.065f,1.96f),tray);
            foreach (float x in new[] {-1.09f,1.09f})
                foreach (float z in new[] {-.82f,.82f})
                {
                    Part("Steel upright",new Vector3(x,.40f,z),new Vector3(.065f,.65f,.065f),metal);
                    Part("Swivel caster",new Vector3(x,.10f,z),new Vector3(.20f,.05f,.20f),wheel,false,true);
                }
        }
    }
}
