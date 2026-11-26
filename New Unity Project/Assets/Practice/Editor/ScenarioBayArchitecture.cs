using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    internal static class ScenarioBayArchitecture
    {
        internal const string RootName = "PULSE simulation enclosure";
        const int AlpineTrail = 4;
        const string Art = "Assets/Practice/Art/";
        const string Field = "Field/Materials/";
        const string Interior = "Interiors/Materials/";

        // Keep the exercise anonymous in certification. The bay describes its setting only.
        static readonly string[] BayNames = { "FOOD SERVICE", "DINING HALL", "SERVICE ROAD", "MACHINE SHOP" };
        static readonly string[] AccentMaterials = {
            Interior + "Juniper green", Interior + "Canteen blue", Field + "Signal ochre",
            Field + "Red oxide enamel"
        };

        internal static void Apply(PracticeRoom room, Transform art)
        {
            if (room.scenarioId < 0 || room.scenarioId > AlpineTrail)
                throw new ArgumentOutOfRangeException(nameof(room), "A simulation bay requires a scenario.");
            var existing = art.Find(RootName);
            if (existing) Object.DestroyImmediate(existing.gameObject);
            if (room.scenarioId != AlpineTrail)
                new Bay(room.scenarioId, art).Construct();

            ExpandedVenueBuilder.Apply(room, art);

            // TextMesh atlases may be rebuilt on device. Include these signs in the existing room binder
            // even when only the enclosure is updated, independently of the larger environment art pass.
            var binder = room.GetComponent<WorldSignAtlas>();
            if (!binder) binder = room.gameObject.AddComponent<WorldSignAtlas>();
            binder.signs = room.gameObject.scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<TextMesh>(true)).ToArray();
            binder.Refresh();
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        }

        internal static void Verify(PracticeRoom room, Transform art)
        {
            var root = art.Find(RootName);
            if (room.scenarioId == AlpineTrail)
            {
                Physics.SyncTransforms();
                if (root || Physics.Raycast(new Vector3(0, 1.65f, 0), Vector3.up, 30f, ~0, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("The alpine exercise area must be open to the sky.");
                return;
            }
            if (!root) throw new InvalidOperationException("Missing simulation bay in " + room.name);
            var faces = new[] { "Foundation", "Ceiling", "West", "East", "Entry", "Rear" };
            var directions = new[] { Vector3.down, Vector3.up, Vector3.left, Vector3.right, Vector3.back, Vector3.forward };
            Physics.SyncTransforms();
            var origin = new Vector3(0, 1.65f, 0);
            for (int i = 0; i < faces.Length; i++)
            {
                var face = root.Find(faces[i]);
                if (!face || !face.TryGetComponent(out BoxCollider boundary) || boundary.isTrigger ||
                    !face.TryGetComponent(out MeshRenderer surface) || !surface.enabled || !surface.sharedMaterial ||
                    !boundary.Raycast(new Ray(origin, directions[i]), out _, 32f))
                    throw new InvalidOperationException("Missing solid " + faces[i] + " face in " + room.name);
            }
            var wordmark = root.Find("Equipment identification/PULSE");
            if (!wordmark || !wordmark.GetComponent<Renderer>().sharedMaterial.mainTexture ||
                !root.GetComponentsInChildren<TextMesh>().Any(sign => sign.text.Contains(BayNames[room.scenarioId])))
                throw new InvalidOperationException("Missing bay identification in " + room.name);
        }

        sealed class Bay
        {
            readonly Transform root;
            readonly int id;
            readonly bool stage;
            readonly float halfWidth, rear, entry, ceiling;
            readonly Material enamel = Finish("Shared/Linen enamel");
            readonly Material graphite = Finish(Field + "Blue-black enamel");
            readonly Material steel = Finish("Shared/Brushed rails");
            readonly Material luminous = Finish(Field + "Clerestory daylight");
            readonly Material glass = Finish(Interior + "Canteen blue");
            readonly Material letters = Finish("Shared/World type");
            readonly Material accent;
            readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            internal Bay(int scenarioId, Transform art)
            {
                id = scenarioId;
                stage = id == 2;
                halfWidth = stage ? 10.55f : 10.19f;
                rear = stage ? 11.85f : 9.17f;
                entry = stage ? -9.86f : -9.04f;
                ceiling = stage ? 7.55f : id == 3 ? 5.22f : 4.98f;
                accent = Finish(AccentMaterials[id]);
                root = Group(art, RootName, Vector3.zero);
            }

            internal void Construct()
            {
                const float skin = .16f;
                const float baseLevel = -.42f;
                float height = ceiling - baseLevel;
                float midY = (ceiling + baseLevel) / 2f;
                float midZ = (entry + rear) / 2f;
                float depth = rear - entry;
                float width = halfWidth * 2;
                // The subfloor sits below the existing walkable substrate. Opaque boundary panels
                // receive light; their shadows stay off to preserve the rooms' daylight rigs.
                Box(root, "Foundation", 0,baseLevel,midZ, width+skin,skin,depth+skin, graphite,true);
                Box(root, "Ceiling", 0,ceiling,midZ, width+skin,skin,depth+skin, enamel,true);
                Box(root, "West", -halfWidth,midY,midZ, skin,height,depth+skin,enamel,true);
                Box(root, "East", halfWidth,midY,midZ, skin,height,depth+skin,enamel,true);
                Box(root, "Entry", 0,midY,entry, width+skin,height,skin,enamel,true);
                Box(root, "Rear", 0,midY,rear, width+skin,height,skin,enamel,true);

                ObservationWindow();
                EquipmentSign();
                if (stage) HighBay();
                if (id == 3)
                {
                    // The workshop has an older, shadow-casting roof. Approximate its broad
                    // overhead diffusers with one shadowless fill across all patient marks.
                    var fixture = Group(root, "Workshop diffuser fill", new Vector3(0, 4.2f, -3));
                    fixture.localRotation = Quaternion.Euler(52f, 16f, 0);
                    var fill = fixture.gameObject.AddComponent<Light>();
                    fill.type = LightType.Directional;
                    fill.color = new Color(1f, .96f, .9f);
                    fill.intensity = .76f;
                    fill.shadows = LightShadows.None;
                }
            }

            void ObservationWindow()
            {
                var front = Group(root,"Observation station",new Vector3(0,0,entry+.10f));
                front.localRotation = Quaternion.Euler(0,180,0);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(front,"Protective wall rail",side*(halfWidth*.5f+2.15f),1.04f,-.045f,
                        halfWidth-4.3f,.13f,.085f,accent);
                    Box(front,"Plinth",side*(halfWidth*.5f+2.15f),.24f,-.03f,
                        halfWidth-4.3f,.48f,.045f,graphite);
                }
                // A shallow glazed observation recess with a dim console behind the frame.
                Box(front,"Observation recess",0,1.85f,-.025f,8.56f,2.16f,.04f,graphite);
                Box(front,"Tinted observation glazing",0,1.88f,-.055f,8.27f,1.88f,.025f,glass);
                foreach (float x in new[] {-4.22f,-1.39f,1.39f,4.22f})
                    Box(front,"Window jamb",x,1.87f,-.13f,.067f,2.02f,.13f,steel);
                Box(front,"Window head",0,2.9f,-.15f,8.55f,.075f,.22f,steel);
                Box(front,"Window transom",0,2.48f,-.13f,8.50f,.045f,.12f,steel);
                Box(front,"Observation sill",0,.84f,-.16f,8.62f,.10f,.29f,steel);
                Box(front,"Observer nameplate",-2.45f,3.26f,-.05f,3.54f,.47f,.06f,graphite);
                Text(front,"Control room plaque","SIMULATION CONTROL",-2.45f,3.26f,-.084f,3.13f,.19f);
                Box(front,"Observer lamp",-.91f,3.26f,-.089f,.07f,.07f,.015f,luminous);
            }

            void EquipmentSign()
            {
                var sign = Group(root,"Equipment identification",new Vector3(-5.35f,2.96f,-3.27f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float length = ceiling - 3.24f;
                    Box(sign,"Suspension",side*1.21f,.28f+length*.5f,0,.028f,length,.028f,steel);
                }
                Box(sign,"Folded aluminum housing",0,0,0,3.22f,.64f,.10f,graphite);
                Box(sign,"Color key",-1.55f,0,-.057f,.068f,.53f,.016f,accent);
                var wordmark = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wordmark.name = "PULSE";
                wordmark.transform.SetParent(sign,false);
                wordmark.transform.localPosition = new Vector3(-.96f,.015f,-.059f);
                wordmark.transform.localScale = new Vector3(.99f,.275f,1);
                Object.DestroyImmediate(wordmark.GetComponent<Collider>());
                var renderer = wordmark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Resources.Load<Material>("PulseSign");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Text(sign,"Simulation identifier","SIMULATION BAY  0"+(id+1),.51f,.145f,-.057f,1.80f,.145f);
                Text(sign,"Venue key",BayNames[id],.51f,-.105f,-.057f,1.80f,.16f);
            }

            void HighBay()
            {
                var frame = Group(root,"Road rescue hall",Vector3.zero);
                var roof = Finish(Field + "Roof deck");
                foreach (int side in new[] {-1,1})
                {
                    float x = side*(halfWidth-.16f);
                    Box(frame,"Wall liner",x,1.03f,.98f,.12f,2.06f,rear-entry,roof);
                    Box(frame,"Datum band",x-side*.08f,2.07f,.98f,.03f,.09f,rear-entry,accent);
                }
                foreach (float z in new[] {-7f,-1f,5f,10f})
                {
                    Box(frame,"Overhead portal beam",0,ceiling-.24f,z,2*halfWidth,.28f,.18f,graphite);
                    foreach(int side in new[] {-1,1})
                    {
                        Box(frame,"Structural pilaster",side*(halfWidth-.22f),ceiling*.5f,z,.22f,ceiling,.24f,steel);
                        Box(frame,"Recessed light housing",side*4.8f,ceiling-.39f,z,3.5f,.13f,.43f,graphite);
                        Box(frame,"Diffuse lens",side*4.8f,ceiling-.47f,z,3.25f,.045f,.32f,luminous);
                    }
                }
                Box(frame,"Equipment recess",0,3.30f,rear-.13f,6.84f,6.6f,.07f,graphite);
                Box(frame,"Sectional loading door",0,2.91f,rear-.19f,6.48f,5.82f,.09f,roof);
                for(int i=1;i<=8;i++)
                    Box(frame,"Section joint",0,i*.64f,rear-.25f,6.46f,.032f,.015f,steel);
                foreach(float x in new[] {-2f,0f,2f})
                    Box(frame,"Loading door vision panel",x,3.50f,rear-.255f,1.24f,.35f,.02f,glass);
                Text(frame,"Freight bay plate","FREIGHT ACCESS",0,6.22f,rear-.19f,3.36f,.31f);
            }

            void Text(Transform parent,string name,string value,float x,float y,float z,float width,float height)
            {
                var label = Group(parent,name,new Vector3(x,y,z)).gameObject.AddComponent<TextMesh>();
                label.font = font;
                label.fontSize = 96;
                label.fontStyle = FontStyle.Bold;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.text = value;
                label.color = Color.white;
                var renderer = label.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = letters;
                var extents = renderer.localBounds.size;
                label.transform.localScale = Vector3.one*Mathf.Min(width/Mathf.Max(.01f,extents.x),height/Mathf.Max(.01f,extents.y));
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            static Transform Group(Transform parent,string name,Vector3 position)
            {
                var group = new GameObject(name).transform;
                group.SetParent(parent,false);
                group.localPosition = position;
                return group;
            }

            static void Box(Transform parent,string name,float x,float y,float z,float width,float height,float depth,Material material,bool solid=false)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = name;
                part.transform.SetParent(parent,false);
                part.transform.localPosition = new Vector3(x,y,z);
                part.transform.localScale = new Vector3(width,height,depth);
                if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());
                var renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(part, StaticEditorFlags.BatchingStatic);
            }

            static Material Finish(string asset)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Art+asset+".mat");
                if (!material) throw new InvalidOperationException("Missing simulation bay finish " + asset);
                return material;
            }
        }
    }
}
