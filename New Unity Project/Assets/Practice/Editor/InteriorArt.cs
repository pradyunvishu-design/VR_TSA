using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    public static class InteriorArt
    {
        const string AssetRoot = "Assets/Practice/Art/Interiors";
        static readonly HashSet<string> Replaced = new HashSet<string> {
            "Cafe counter", "Stone counter top", "Espresso machine", "Cafe sign", "Round oak tabletop",
            "Oak tabletop", "Table pedestal", "Table base", "Chair seat", "Chair back", "Chair leg",
            "Planter", "Stem", "Foliage", "Allergen snack plate", "Commons sign", "Serving station",
            "Rear glazing", "Window mullion", "Ceiling beam", "Linear pendant", "Practical light"
        };

        public static void Apply(PracticeRoom room, Transform art)
        {
            if (room.scenarioId != 0 && room.scenarioId != 1) return;
            var env = room.gameObject.scene.GetRootGameObjects().Single(g => g.name.StartsWith("Environment / "));
            foreach (Transform item in env.transform)
                if (Replaced.Contains(item.name)) item.gameObject.SetActive(false);
            var previous = art.Find("Dining architecture and furnishings");
            if (previous) Object.DestroyImmediate(previous.gameObject);
            var group = new GameObject("Dining architecture and furnishings").transform;
            group.SetParent(art, false);
            if (!AssetDatabase.IsValidFolder(AssetRoot + "/Materials"))
                AssetDatabase.CreateFolder(AssetRoot, "Materials");
            new Interior(group, env.transform, room.scenarioId == 0).Build();
        }

        sealed class Interior
        {
            readonly Transform root, shell;
            Transform modelParent;
            readonly bool cafe;
            readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
            Material plaster, timber, ink, accent, apron, metal, sill, daylight, furniture, food;

            public Interior(Transform root, Transform shell, bool cafe)
            {
                this.root = root;
                modelParent = root;
                this.shell = shell;
                this.cafe = cafe;
            }

            Material Finish(string name, string hex, float gloss = .18f, string atlas = null)
            {
                string path = AssetRoot + "/Materials/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat)
                {
                    mat = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(mat, path);
                }
                ColorUtility.TryParseHtmlString("#" + hex, out var color);
                mat.color = color;
                mat.SetFloat("_Glossiness", gloss);
                if (atlas != null)
                    mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetRoot + "/" + atlas + "/Palette.png");
                EditorUtility.SetDirty(mat);
                return mat;
            }

            public void Build()
            {
                plaster = Finish("Oat plaster", "E8E2CE");
                timber = Finish("Honey ash", "AE7952");
                ink = Finish("Warm black", "393D39");
                accent = cafe ? Finish("Juniper green", "30584E") : Finish("Canteen blue", "365A70");
                apron = cafe ? Finish("Moss tile", "7F9B87", .35f) : Finish("Brick enamel", "AA5E46", .3f);
                metal = Finish("Brushed pewter", "A7ABA0", .52f);
                sill = Finish("Pale limestone", "D9CDB7", .27f);
                daylight = Finish("Daylight pane", "B5D7D9", .38f);
                furniture = Finish("Furniture palette", "FFFFFF", .25f, "Furniture");
                food = Finish("Food palette", "FFFFFF", .22f, "Food");
                shell.Find("Floor").GetComponent<Renderer>().sharedMaterial =
                    cafe ? Finish("Warm sand floor", "C5B49B") : Finish("Warm slate floor", "A3AFAE");
                foreach (string wall in new[] { "Rear wall", "Left wall", "Right wall" })
                    shell.Find(wall).GetComponent<Renderer>().sharedMaterial = plaster;
                // Existing side-wall collider stays in place behind the window frames.
                shell.Find("Right wall").GetComponent<Renderer>().enabled = false;
                Architecture();
                if (cafe) Switchback(); else RelayHall();
            }

            GameObject Block(string name, Vector3 center, Vector3 size, Material mat, bool solid = false)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(root, false);
                go.transform.localPosition = center;
                go.transform.localScale = size;
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;
                if (name == "Ceiling" || name == "Window glass") renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
                return go;
            }

            void Model(string kit, string name, Vector3 basePoint, Vector3 size, float yaw, bool solid)
            {
                string path = AssetRoot + "/" + kit + "/" + name + ".obj";
                if (!meshes.TryGetValue(path, out var mesh))
                {
                    mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Single();
                    meshes.Add(path, mesh);
                }
                var go = new GameObject(name);
                go.transform.SetParent(modelParent, false);
                var bounds = mesh.bounds;
                var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                go.transform.localScale = scale;
                var baseCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                go.transform.localPosition = basePoint - go.transform.localRotation * Vector3.Scale(baseCenter, scale);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = kit == "Furniture" ? furniture : food;
                if (solid)
                {
                    var collider = go.AddComponent<BoxCollider>();
                    collider.center = bounds.center;
                    collider.size = bounds.size;
                }
            }

            void Fit(string model, float x, float y, float z, float width, float height, float depth, float yaw = 0, bool solid = true)
                => Model("Furniture", model, new Vector3(x,y,z), new Vector3(width,height,depth), yaw, solid);
            void Dish(string model, float x, float y, float z, float width, float height, float depth, float yaw = 0)
                => Model("Food", model, new Vector3(x,y,z), new Vector3(width,height,depth), yaw, false);
            void Bar(string name,float x,float y,float z,float w,float h,float d,Material m,bool solid=false)
                => Block(name, new Vector3(x,y,z), new Vector3(w,h,d), m, solid);

            void Letter(string value, float x, float y, float z, float maxWidth, float maxHeight, string hex)
            {
                var go = new GameObject(value);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x,y,z);
                var t = go.AddComponent<TextMesh>();
                t.anchor = TextAnchor.MiddleCenter;
                t.alignment = TextAlignment.Center;
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 96;
                t.characterSize = .1f;
                t.text = value;
                ColorUtility.TryParseHtmlString("#" + hex, out var tint);
                t.color = tint;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = t.font.material;
                var size = renderer.localBounds.size;
                go.transform.localScale = Vector3.one * Mathf.Min(maxWidth / Mathf.Max(size.x,.01f), maxHeight / Mathf.Max(size.y,.01f));
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            void Architecture()
            {
                Bar("Ceiling",0,4.95f,-.5f,20,.12f,19,plaster);
                Bar("Left dado",-9.83f,.54f,0,.09f,1.08f,18,apron);
                Bar("Chair rail",-9.73f,1.10f,0,.14f,.07f,18,timber);
                Bar("Low window wall",9.94f,.44f,0,.18f,.88f,18,plaster);
                Bar("High window wall",9.94f,4.36f,0,.18f,.95f,18,plaster);
                Bar("Window sill",9.75f,.91f,0,.43f,.10f,18,timber);
                Bar("Window head",9.84f,3.88f,0,.26f,.12f,18,ink);
                for (int i = 0; i < 6; i++)
                {
                    float z = -7.5f + i * 3;
                    Bar("Window glass",9.94f,2.39f,z,.035f,2.95f,2.94f,daylight);
                    Bar("Window jamb",9.86f,2.39f,z-1.46f,.18f,3,.09f,ink);
                }
                Bar("Transom",9.83f,3.04f,0,.12f,.06f,18,ink);
                foreach(float x in new[]{-7f,0f,7f})
                    Bar("Ceiling joist",x,4.72f,-.2f,.16f,.30f,18.5f,cafe?timber:ink);
                foreach(float x in new[]{-4.9f,3.9f})
                    foreach(float z in new[]{-.8f,4.5f})
                    {
                        Bar("Pendant drop",x,4.51f,z,.024f,.68f,.024f,ink);
                        Fit("lampSquareCeiling",x,3.95f,z,.72f,.24f,.72f,0,false);
                    }
                // Fill from the entry side; low intensity preserves the patient silhouettes.
                var fill = new GameObject("Soft bounce at entrance").AddComponent<Light>();
                fill.transform.SetParent(root, false);
                fill.transform.localPosition = new Vector3(0,3.3f,-2.7f);
                fill.type = LightType.Point;
                fill.range = 15f;
                fill.intensity = .75f;
                fill.color = cafe ? new Color(1,.91f,.80f) : new Color(.85f,.93f,1);
                fill.shadows = LightShadows.None;
            }

            void Switchback()
            {
                Bar("Espresso wall",-4.3f,2.27f,8.78f,10.9f,4.54f,.15f,accent);
                for(int i=0;i<11;i++)
                    Bar("Oak screen",1.25f+i*.17f,2.27f,8.62f,.06f,4.54f,.14f,timber);
                Letter("SWITCHBACK",-4.4f,3.35f,8.65f,6.6f,.68f,"F3E9CF");
                Letter("COFFEE  /  WINDOW SEATS",-4.4f,2.64f,8.65f,4.6f,.26f,"E8D5AC");
                // A folded pale inlay on the counter echoes the switchback in the name.
                for (int i=0;i<3;i++)
                {
                    var inlay=Block("Switchback counter inlay",new Vector3(-5.82f+i*.47f,.61f,5.025f),
                        new Vector3(.53f,.038f,.015f),sill);
                    inlay.transform.localRotation=Quaternion.Euler(0,0,i%2==0?24:-24);
                }
                Bar("Oak service plinth",-5.35f,.53f,5.65f,7.05f,1.06f,.98f,timber,true);
                Bar("Fluted fascia",-5.35f,.52f,5.13f,6.92f,.84f,.07f,apron);
                for(int i=0;i<29;i++)
                    Bar("Panel rib",-8.65f+i*.236f,.52f,5.07f,.035f,.84f,.045f,accent);
                Bar("Limestone countertop",-5.35f,1.09f,5.63f,7.23f,.12f,1.14f,sill,true);
                Fit("kitchenCoffeeMachine",-6.1f,1.15f,5.66f,.81f,.63f,.52f,180,false);
                Fit("kitchenSink",-7.3f,0,7.8f,1.32f,1.03f,.75f,180);
                Fit("kitchenCabinetDrawer",-5.91f,0,7.8f,1.41f,1.03f,.75f,180);
                Fit("plantSmall1",-2.35f,1.15f,5.66f,.31f,.39f,.28f,15,false);
                Dish("plate-rectangle",-4.1f,1.151f,5.55f,.65f,.035f,.32f);
                foreach(float x in new[]{-4.27f,-4.0f})
                    Dish("muffin",x,1.186f,5.55f,.15f,.14f,.14f,17);
                Dish("mug",-6.90f,1.15f,5.51f,.19f,.15f,.15f,35);
                LunchTable(-1.0f,2.95f);
                WindowSeats(7.7f,4.45f);
                WindowSeats(7.7f,-1.35f);
                Fit("pottedPlant",8.45f,0,7.1f,.92f,1.65f,.82f,23);
                Fit("pottedPlant",-9.0f,0,1.3f,.7f,1.20f,.72f,-18);
            }

            void WindowSeats(float x,float z)
            {
                Fit("tableRound",x,0,z,1.16f,.76f,1.16f);
                Fit("chairRounded",x-.78f,0,z-.48f,.55f,.88f,.55f,-120);
                Fit("chairRounded",x-.78f,0,z+.48f,.55f,.88f,.55f,-64);
                Dish("mug",x-.11f,.76f,z-.27f,.14f,.13f,.12f,20);
                Fit("plantSmall1",x+.22f,.76f,z+.16f,.17f,.23f,.17f,35,false);
            }

            void LunchTable(float x, float z)
            {
                Fit("tableRound",x,0,z,1.33f,.76f,1.32f,17);
                Fit("chairRounded",x-.98f,0,z+.13f,.55f,.88f,.55f,85);
                Fit("chairRounded",x+.95f,0,z+.18f,.55f,.88f,.55f,-66);
                Dish("mug",x+.24f,.76f,z+.13f,.14f,.13f,.12f,30);
                Dish("plate",x-.21f,.76f,z-.1f,.29f,.027f,.29f);
                Dish("sandwich",x-.21f,.787f,z-.10f,.18f,.09f,.14f,-22);
            }

            void RelayHall()
            {
                Bar("Servery backdrop",0,2.24f,8.78f,19.6f,4.48f,.14f,accent);
                Bar("Continuous canopy",0,3.76f,7.98f,19.5f,.16f,1.65f,sill);
                Letter("RELAY HALL",0,3.16f,8.65f,6.2f,.55f,"FCF2DA");
                foreach(float x in new[]{-6.25f,6.25f})
                {
                    Bar("Serving hatch",x,1.92f,8.62f,4.6f,2.1f,.12f,ink);
                    Bar("Tile backsplash",x,1.82f,8.51f,4.3f,1.55f,.09f,apron);
                    Bar("Pickup counter",x,.55f,7.91f,4.75f,1.10f,1.05f,plaster,true);
                    Bar("Stainless pass",x,1.13f,7.84f,4.95f,.11f,1.22f,metal,true);
                    Bar("Tray rail",x,.93f,7.16f,4.75f,.075f,.15f,timber);
                    for(int j=0;j<4;j++)
                        Dish("plate",x+.88f,1.19f+j*.021f,7.73f,.31f,.031f,.31f);
                    Dish("bowl",x-.49f,1.19f,7.73f,.35f,.15f,.35f);
                    Dish("salad",x-.49f,1.29f,7.73f,.27f,.075f,.27f,-22);
                    Letter(x<0 ? "01   /   HOT PLATES" : "02   /   SALAD",x,2.49f,8.42f,3.4f,.28f,"FFF1D3");
                }
                Communal("West long table",-3.45f,3.65f,0);
                Communal("East cross table",5.2f,4.45f,90);
                Fit("pottedPlant",-8.75f,0,3.9f,.79f,1.38f,.8f,35);
                Fit("pottedPlant",8.85f,0,4.65f,.84f,1.53f,.82f,-24);
                Fit("benchCushion",-9.00f,0,-.8f,2.22f,.77f,.62f,90);
            }

            void Communal(string name,float x,float z,float yaw)
            {
                var setting=new GameObject(name).transform;
                setting.SetParent(root,false);
                setting.localPosition=new Vector3(x,0,z);
                setting.localRotation=Quaternion.Euler(0,yaw,0);
                modelParent=setting;
                Fit("table",0,0,0,3.2f,.76f,1.04f,90);
                foreach(float dx in new[]{-.96f,.96f})
                    Fit("bench",dx,0,0,2.66f,.71f,.51f,dx<0?90:-90);
                Dish("plate-rectangle",.16f,.76f,-.85f,.38f,.035f,.29f,86);
                Dish("sandwich",.16f,.795f,-.85f,.20f,.085f,.16f,75);
                Dish("glass",.25f,.76f,-.42f,.085f,.14f,.085f);
                Dish("plate",-.17f,.76f,.82f,.26f,.025f,.26f);
                Dish("apple",-.17f,.785f,.82f,.09f,.10f,.09f,21);
                Dish("shaker-salt",0,.76f,.15f,.055f,.095f,.055f);
                modelParent=root;
            }
        }
    }
}
