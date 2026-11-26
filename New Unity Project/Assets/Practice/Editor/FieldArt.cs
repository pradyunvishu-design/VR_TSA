using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace First180.Practice.Editor
{
    public static class FieldArt
    {
        const string Root = "Assets/Practice/Art/Field/";
        const string Materials = Root + "Materials";
        static readonly HashSet<string> SceneryRoots = new HashSet<string> {
            "Riverside freight spur | shoulder", "Port authority | distant silos and sheds", "Work zone | barrier taper",
            "Fabrication annex | clerestory structure", "Fitting benches | vice and hand tools", "Guarded drilling cell | machine isolation"
        };

        public static void Apply(PracticeRoom room, Transform art)
        {
            if (room.scenarioId != 2 && room.scenarioId != 3) return;
            var shell = room.gameObject.scene.GetRootGameObjects().Single(o => o.name.StartsWith("Environment /", StringComparison.Ordinal)).transform;
            if (!AssetDatabase.IsValidFolder(Materials)) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Materials");
            var set = new Set();
            bool road = room.scenarioId == 2;
            for (int index = art.childCount - 1; index >= 0; index--)
            {
                var existing = art.GetChild(index);
                if (SceneryRoots.Contains(existing.name)) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            foreach (Transform child in shell)
            {
                switch (child.name)
                {
                    case "Rear wall": case "Left wall": case "Right wall":
                        child.GetComponent<Renderer>().enabled = !road;
                        if (!road) child.GetComponent<Renderer>().sharedMaterial = set.Mat("Warm white enamel", "#E1DDCF");
                        break;
                    case "Floor":
                        child.GetComponent<Renderer>().sharedMaterial = road
                            ? set.Mat("Service apron", "#DDD6C5", "concrete_floor_02.jpg", 10)
                            : set.Mat("Worn shop concrete", "#DADBD6", "concrete_floor_02.jpg", 8);
                        break;
                    case "Service road": case "Road marking": case "Delivery vehicle": case "Vehicle cabin": case "Wheel":
                    case "Safety bollard": case "Road sign": case "Workshop sign": case "Machine housing": case "Isolated equipment":
                    case "Safety guard": case "Safety floor stripe": case "Rear glazing": case "Window mullion":
                    case "Ceiling beam": case "Linear pendant":
                        child.gameObject.SetActive(false);
                        break;
                    case "Practical light":
                        child.gameObject.SetActive(!road);
                        if (!road)
                        {
                            var light = child.GetComponent<Light>();
                            light.color = new Color(1,.94f,.85f);
                            light.intensity = .7f;
                            light.transform.position = new Vector3(child.position.x, 3.65f, child.position.z);
                            light.shadows = LightShadows.None;
                        }
                        break;
                    case "Oak tabletop": case "Table pedestal": case "Table base":
                        bool underResponseCart = Mathf.Abs(child.position.x + 5) < .05f && Mathf.Abs(child.position.z + 4) < .05f;
                        if (underResponseCart || (!road && child.position.z >= -.9f)) child.gameObject.SetActive(false);
                        break;
                }
            }
            if (road) set.Riverside(art);
            else set.Workshop(art);
        }

        sealed class Set
        {
            readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
            Material Ink => Mat("Blue-black enamel", "#263B42");
            Material Steel => Mat("Galvanized steel", "#879997");
            Material Teal => Mat("River teal", "#33656B");
            Material Gold => Mat("Signal ochre", "#DEA545");
            Material Chalk => Mat("Warm white enamel", "#E1DDCF");
            Material Rubber => Mat("Rubber graphite", "#373F40");

            public Material Mat(string name, string tint, string image = null, int tiling = 1)
            {
                if (cache.TryGetValue(name, out var material)) return material;
                string path = Materials + "/" + name + ".mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(material, path);
                }
                ColorUtility.TryParseHtmlString(tint, out var color);
                material.color = color;
                material.SetFloat("_Glossiness", .17f);
                material.mainTexture = image == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + image);
                if (image != null && !material.mainTexture) throw new InvalidOperationException("Missing field art texture " + image);
                material.mainTextureScale = new Vector2(tiling,tiling);
                EditorUtility.SetDirty(material);
                cache.Add(name,material);
                return material;
            }

            Transform Group(Transform parent, string label, float x=0, float y=0, float z=0, float yaw=0)
            {
                var go = new GameObject(label);
                go.transform.SetParent(parent,false);
                go.transform.localPosition = new Vector3(x,y,z);
                go.transform.localRotation = Quaternion.Euler(0,yaw,0);
                return go.transform;
            }

            Transform Box(Transform parent, string name, float x,float y,float z,float width,float height,float depth,Material mat,bool solid=false)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name=name;
                go.transform.SetParent(parent,false);
                go.transform.localPosition = new Vector3(x,y,z);
                go.transform.localScale = new Vector3(width,height,depth);
                go.GetComponent<Renderer>().sharedMaterial=mat;
                if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                return go.transform;
            }

            void Sign(Transform parent,string text,float x,float y,float z,float width,float height,Material backing,Color ink)
            {
                Box(parent,"Enamel sign",x,y,z,width,height,.045f,backing);
                var board = Group(parent,text.Replace("\n"," / "),x,y,z-.026f);
                var type = board.gameObject.AddComponent<TextMesh>();
                type.fontSize=72;
                type.anchor=TextAnchor.MiddleCenter;
                type.alignment=TextAlignment.Center;
                type.characterSize=.1f;
                type.text=text;
                type.color=ink;
                // Measure in sign space so side-facing signs get the same optical margins.
                Bounds bounds=type.GetComponent<Renderer>().localBounds;
                float fit=Mathf.Min(width*.86f/Mathf.Max(.01f,bounds.size.x),height*.68f/Mathf.Max(.01f,bounds.size.y));
                board.localScale=Vector3.one*fit;
            }

            Transform Model(Transform parent,string source,string name,float x,float floor,float z,float height,float yaw=0,bool collider=false,Quaternion? tilt=null)
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+source+".obj");
                if (!asset) throw new InvalidOperationException("Missing field scenery model "+source);
                var root=Group(parent,name,x,floor,z,yaw);
                var model=((GameObject)PrefabUtility.InstantiatePrefab(asset,root)).transform;
                model.localPosition=Vector3.zero;
                model.localRotation=tilt ?? Quaternion.identity;
                var renderers=model.GetComponentsInChildren<MeshRenderer>();
                if(renderers.Length==0) throw new InvalidOperationException("No mesh in field scenery "+source);
                var slash=source.IndexOf('/');
                var coat=slash<0 ? Mat(source+" coat","#FFFFFF",source+"-albedo.jpg") :
                    Mat(source.Substring(0,slash)+" palette","#F5EFE5",source.Substring(0,slash)+"/Palette.png");
                Bounds total=renderers[0].bounds;
                foreach(var renderer in renderers)
                {
                    renderer.sharedMaterials=Enumerable.Repeat(coat,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
                    total.Encapsulate(renderer.bounds);
                }
                model.localScale*=height/Mathf.Max(.001f,total.size.y);
                var aligned=renderers[0].bounds;
                foreach(var renderer in renderers) aligned.Encapsulate(renderer.bounds);
                model.position+=root.position-new Vector3(aligned.center.x,aligned.min.y,aligned.center.z);
                if(collider)
                {
                    Bounds local=new Bounds();
                    bool first=true;
                    foreach(var renderer in renderers)
                    {
                        var b=renderer.localBounds;
                        for(int i=0;i<8;i++)
                        {
                            Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            var point=root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                            if(first){local=new Bounds(point,Vector3.zero);first=false;}else local.Encapsulate(point);
                        }
                    }
                    var box=root.gameObject.AddComponent<BoxCollider>(); box.center=local.center;box.size=local.size;
                }
                return root;
            }

            public void Riverside(Transform art)
            {
                var street=Group(art,"Riverside freight spur | shoulder");
                var asphalt=Mat("Milled asphalt", "#A2AAB0", "asphalt_04.jpg", 15);
                Box(street,"Two-lane raised asphalt",0,.018f,6.9f,68,.035f,8.6f,asphalt);
                Box(street,"Beyond the freight yard",0,-.22f,18,130,.3f,80,Mat("Dusty verge","#A9AD91"));
                Box(street,"White shoulder line",0,.043f,2.8f,66,.012f,.105f,Chalk);
                Box(street,"Opposite shoulder line",0,.043f,10.91f,66,.012f,.105f,Chalk);
                for(int i=-5;i<=5;i++) Box(street,"Broken center line",i*5,.043f,6.85f,2.6f,.014f,.11f,Gold);
                for(int i=-3;i<4;i++)
                {
                    Box(street,"Expansion joint",i*3.9f,.001f,-3.3f,.018f,.004f,11.45f,Steel);
                }
                foreach(float x in new[]{-7.4f,5.8f})
                {
                    Box(street,"Drain casting",x,.013f,2.2f,1.12f,.018f,.35f,Steel);
                    for(int i=0;i<9;i++) Box(street,"Drain slot",x-.46f+.115f*i,.025f,2.2f,.051f,.008f,.24f,Ink);
                }

                var yard=Group(art,"Port authority | distant silos and sheds");
                Model(yard,"city-kit-industrial/building-c","Cold store",-17,0,19,6.3f,90);
                Model(yard,"city-kit-industrial/building-p","Transit depot",-4,0,20,4.4f,180);
                Model(yard,"city-kit-industrial/building-f","Boiler shop",17,0,24,7.5f,180);
                Model(yard,"city-kit-industrial/water-tower","Riverside water tower",5.4f,0,20,10.8f,10);
                Model(yard,"city-kit-industrial/chimney-medium","Old kiln stack",-24,0,23,11);
                Model(yard,"city-kit-industrial/detail-tank","Tank farm",11,.01f,16.5f,2.9f,90);
                Model(yard,"city-kit-industrial/shipping-container-a","Loaded freight container",-10,0,11.9f,2.4f,90);
                Model(yard,"city-kit-industrial/shipping-container-a","Freight container at wharf",16,0,11.9f,2.4f,88);
                Model(street,"car-kit/van","Distant fleet vehicle",10,.04f,8.3f,1.85f,90);
                Model(street,"car-kit/delivery","Stopped delivery truck",-2.9f,.04f,4.35f,2.55f,-83,true);
                var shoulder=Group(art,"Work zone | barrier taper");
                foreach(Vector2 p in new[]{new Vector2(.7f,3.15f),new Vector2(2.4f,3.43f),new Vector2(4.05f,3.75f)})
                    Model(shoulder,"car-kit/cone","Reflective traffic cone",p.x,.04f,p.y,.54f);
                Model(shoulder,"car-kit/cone","Setback traffic cone",-6.7f,.04f,3.1f,.54f);
                Model(shoulder,"car-kit/box","Freight parcel",-7.5f,.015f,1.3f,.51f,-11,true);
                Model(shoulder,"car-kit/box","Freight parcel",-7.9f,.015f,1.85f,.38f,6,true);
                foreach(float x in new[]{5.9f,8.7f}) Barrier(shoulder,x,4.35f,-8);
                var sign=Group(yard,"Port service route sign",5.5f,0,9.7f,-8);
                foreach(float x in new[]{-.94f,.94f}) Box(sign,"Signpost",x,1.3f,0,.085f,2.6f,.085f,Steel);
                Sign(sign,"RIVERSIDE",0,2.75f,-.08f,3.15f,.69f,Teal,Color.white);
                Sign(sign,"SERVICE SPUR   /   07",0,2.2f,-.08f,3.15f,.32f,Chalk,new Color(.11f,.21f,.22f));

                // A continuous crash rail visually explains the far edge of the walkable shoulder.
                foreach(int i in Enumerable.Range(-7,15)) Box(yard,"Galvanized rail post",i*2.7f,.39f,10.96f,.14f,.78f,.13f,Steel);
                Box(yard,"Crash rail",0,.65f,10.9f,40,.24f,.11f,Steel);
                for(int i=-2;i<=2;i++)
                {
                    float x=i*10-2;
                    Box(yard,"Streetlight mast",x,3.5f,12,.12f,7,.12f,Ink);
                    Box(yard,"Lamp outreach",x,7,11.4f,.095f,.12f,1.25f,Ink);
                    Box(yard,"Luminaire",x,6.94f,10.71f,.33f,.11f,.7f,Steel);
                }
            }

            void Barrier(Transform parent,float x,float z,float angle)
            {
                var frame=Group(parent,"Detour barricade",x,0,z,angle);
                foreach(float end in new[]{-1.01f,1.01f})
                {
                    Box(frame,"Foot",end,.07f,0,.24f,.12f,.68f,Rubber,true);
                    Box(frame,"Upright",end,.54f,0,.12f,.85f,.12f,Steel);
                }
                Box(frame,"Retroreflective board",0,.78f,-.035f,2.3f,.36f,.095f,Chalk,true);
                for(int i=-2;i<=2;i++)
                    Box(frame,"Amber slash",i*.42f,.78f,-.085f,.17f,.32f,.012f,Gold).localRotation=Quaternion.Euler(0,0,-25);
            }

            void Bench(Transform parent,float x,float z,float width)
            {
                var oak=Mat("Oiled beech","#A07A52");
                Box(parent,"Laminated hardwood top",x,.91f,z,width,.12f,1.26f,oak,true);
                Box(parent,"Bench front apron",x,.78f,z-.52f,width-.1f,.13f,.07f,Ink);
                Box(parent,"Lower shelf",x,.28f,z,width-.18f,.065f,.9f,Steel);
                foreach(float dx in new[]{-width/2+.14f,width/2-.14f})
                foreach(float dz in new[]{-.46f,.46f})
                    Box(parent,"Welded box leg",x+dx,.45f,z+dz,.085f,.9f,.085f,Ink);
            }

            public void Workshop(Transform art)
            {
                var shell=Group(art,"Fabrication annex | clerestory structure");
                var daylight=Mat("Clerestory daylight","#D1E1DC");
                daylight.EnableKeyword("_EMISSION"); daylight.SetColor("_EmissionColor",new Color(.26f,.31f,.3f));
                var oxide=Mat("Red oxide enamel","#A34F39");
                var shutter=Mat("Rolling door panels","#80908D");
                var plinth=Mat("Industrial wall plinth","#5D7370");
                Box(shell,"Painted blockwork",0,.77f,8.835f,19.9f,1.54f,.06f,plinth);
                Box(shell,"Painted datum",0,1.56f,8.78f,19.9f,.09f,.06f,Gold);
                foreach(int side in new[]{-1,1})
                {
                    Box(shell,"Upper wall closure",side*9.96f,4.98f,0,.23f,.44f,18.1f,Chalk);
                    Box(shell,"Side wall plinth",side*9.835f,.77f,0,.06f,1.54f,17.9f,plinth);
                    Box(shell,"Side datum",side*9.80f,1.56f,0,.06f,.09f,17.9f,Gold);
                }
                foreach(float x in new[]{-8f,-4f,0f,4f,8f})
                {
                    Box(shell,"Clerestory light",x,3.81f,8.78f,3.77f,1.15f,.075f,daylight);
                    Box(shell,"Clerestory middle rail",x,3.78f,8.71f,3.79f,.045f,.07f,Steel);
                    Box(shell,"Vertical glazing bar",x,3.81f,8.71f,.052f,1.15f,.07f,Steel);
                }
                Box(shell,"Rear upper wall closure",0,4.98f,9,20,.44f,.23f,Chalk);
                var deck=Mat("Roof deck","#B7B9AD");
                deck.SetFloat("_Glossiness",0);
                deck.SetFloat("_SpecularHighlights",0);
                deck.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                Box(shell,"Roof deck",0,5.12f,0,20.1f,.14f,18.5f,deck);
                foreach(float z in new[]{-5f,.3f,5.7f})
                {
                    Box(shell,"Portal beam",0,4.81f,z,19.9f,.28f,.16f,Ink);
                    foreach(float x in new[]{-9.69f,9.69f})
                        Box(shell,"Structural column",x,2.43f,z,.18f,4.86f,.2f,Ink);
                    foreach(float x in new[]{-4f,4f})
                    {
                        Box(shell,"Suspended channel",x,4.36f,z,3.4f,.1f,.25f,Steel);
                        Box(shell,"Daylight diffuser",x,4.295f,z,3.19f,.035f,.18f,daylight);
                    }
                }
                var door=Group(shell,"Bay 04 | sectional freight door",-.65f,0,8.55f);
                Box(door,"Shutter casing",0,1.65f,0,4.55f,3.3f,.14f,Ink);
                for(int i=0;i<12;i++) Box(door,"Pressed shutter slat",0,.14f+i*.249f,-.10f,4.22f,.234f,.09f,shutter);
                Box(door,"Bottom weather seal",0,.055f,-.15f,4.31f,.08f,.09f,Rubber);
                foreach(float x in new[]{-1.39f,-.46f,.46f,1.39f})
                    Box(door,"Observation light",x,2.38f,-.16f,.65f,.18f,.035f,Ink);
                Sign(shell,"BAY 04",3.12f,2.47f,8.65f,2.16f,.84f,oxide,Color.white);
                Sign(shell,"FABRICATION",3.12f,1.86f,8.65f,2.16f,.31f,Chalk,new Color(.16f,.24f,.25f));

                var fitters=Group(art,"Fitting benches | vice and hand tools");
                Bench(fitters,-4.15f,4.75f,3.75f);
                Model(fitters,"bench_vice_01","Swivel bench vice",-3.15f,.97f,4.39f,.25f,140);
                Model(fitters,"adjustable_wrench","Service wrench",-4.17f,.97f,4.42f,.031f,27,false,Quaternion.Euler(89,0,0));
                Model(fitters,"metal_stool_02","Mechanic stool",-5.65f,0,3.52f,.56f,-24,true);
                Model(fitters,"metal_tool_chest","Rolling tool chest",-6.91f,0,5.02f,.95f,0,true);
                Model(fitters,"metal_tool_chest","Bench-side cabinet",-2.72f,.31f,4.71f,.50f,0);
                Box(fitters,"Shadow board",-5.25f,1.64f,5.38f,2.05f,1.1f,.095f,plinth);
                for(int r=0;r<3;r++) for(int c=0;c<7;c++)
                    Box(fitters,"Peg rail mounting hole",-6.04f+c*.26f,1.4f+r*.23f,5.32f,.036f,.035f,.018f,Ink);
                Sign(fitters,"TOOL ROOM",-5.25f,2.08f,5.3f,1.35f,.185f,Chalk,new Color(.14f,.21f,.22f));

                var machine=Group(art,"Guarded drilling cell | machine isolation",3.34f,0,4.46f);
                Bench(machine,0,0,2.56f);
                Model(machine,"drill_press_01","Bench drill press",0,.97f,0,1.14f,5);
                Box(machine,"Operator mat",0,.012f,-1.14f,2.24f,.021f,.82f,Rubber);
                foreach(float end in new[]{-1.24f,1.24f})
                {
                    Box(machine,"Rear guard post",end,1.43f,.59f,.065f,1.22f,.07f,Gold);
                    Box(machine,"Front guard post",end,1.43f,-.36f,.065f,1.22f,.07f,Gold);
                    for(int i=0;i<6;i++) Box(machine,"Guard mesh horizontal",end,1.05f+i*.178f,.115f,.035f,.024f,.92f,Steel);
                    Box(machine,"Guard upper rail",end,2.04f,.115f,.07f,.07f,1.01f,Gold);
                }
                for(int i=0;i<9;i++) Box(machine,"Rear finger guard",-1.13f+i*.28f,1.54f,.60f,.025f,.96f,.03f,Steel);
                Box(machine,"Rear hazard rail",0,2.04f,.60f,2.54f,.075f,.075f,Gold);
                Box(machine,"Electrical isolator",.88f,1.16f,-.52f,.29f,.33f,.18f,Gold);
                Box(machine,"Lockout switch",.88f,1.16f,-.63f,.13f,.14f,.07f,oxide);
                Sign(machine,"D-04",-.76f,.78f,-.59f,.48f,.15f,Gold,new Color(.14f,.17f,.15f));
                foreach(float dx in new[]{-1.65f,1.65f}) Box(machine,"Cell boundary",dx,.014f,-.14f,.07f,.018f,3.45f,Gold);
                Box(machine,"Cell boundary",0,.014f,-1.84f,3.37f,.018f,.07f,Gold);
                Box(machine,"Cell boundary",0,.014f,1.55f,3.37f,.018f,.07f,Gold);
            }
        }
    }
}
