using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace First180.Practice.Editor
{
    // This is authored scenery: the player build loads ordinary scene meshes and colliders.
    public static class ExpandedVenueBuilder
    {
        const string Art = "Assets/Practice/Art/";
        const string Extension = "South concourse";
        const float South = -26.3f;

        [MenuItem("PULSE/Environment/Bake expanded scenario venues")]
        public static void BakeAll()
        {
            if (EditorApplication.isPlaying || !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var scenario in ScenarioCatalog.All)
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Practice/Scenes/" + scenario.scene + ".unity");
                    var room = Object.FindFirstObjectByType<PracticeRoom>();
                    var art = scene.GetRootGameObjects().Single(x => x.name == "Location art").transform;
                    Apply(room, art);
                    AddResponseStation(room,art);
                    var binder = room.GetComponent<WorldSignAtlas>();
                    binder.signs = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<TextMesh>(true)).ToArray();
                    binder.Refresh();
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + scene.path);
                    Debug.Log("EXPANDED_VENUE_BAKED=" + scene.name);
                }
            }
            finally
            {
                if (previous.Length > 0 && previous.All(x => !string.IsNullOrEmpty(x.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        internal static void Apply(PracticeRoom room, Transform art)
        {
            var prior = art.Find(Extension);
            if (prior) Object.DestroyImmediate(prior.gameObject);
            var root = new GameObject(Extension).transform;
            root.SetParent(art, false);
            var set = new Venue(root);
            if (room.scenarioId == 4) set.Alpine(art);
            else
            {
                var bay = art.Find(ScenarioBayArchitecture.RootName);
                var shell = room.gameObject.scene.GetRootGameObjects().Single(x => x.name.StartsWith("Environment / ", StringComparison.Ordinal)).transform;
                set.ExtendBay(bay, shell.Find("Floor").GetComponent<MeshRenderer>().sharedMaterial, room.scenarioId);
                switch (room.scenarioId)
                {
                    case 0: set.Coffee(); break;
                    case 1: set.Dining(); break;
                    case 2: set.Dispatch(); break;
                    case 3: set.Workshop(); break;
                }
            }
            // Enter from the concourse so the learner sees people and destinations in depth.
            // The original return doorway remains in the old hall, clear of this arrival point.
            var player=room.gameObject.scene.GetRootGameObjects()
                .SelectMany(x=>x.GetComponentsInChildren<PracticePlayer>(true)).Single();
            player.transform.SetPositionAndRotation(new Vector3(4.9f,.035f,-23.95f),Quaternion.Euler(0,-14f,0));
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        }

        internal static void AddResponseStation(PracticeRoom room,Transform art)
        {
            var wing=art.Find(Extension);
            var oldStation=wing.Find("South response station");
            if(oldStation) Object.DestroyImmediate(oldStation.gameObject);
            var sourceCart=art.Find("Response cart");
            var roots=room.gameObject.scene.GetRootGameObjects();
            var sourceTools=roots.Single(x=>x.name=="Interactive response equipment").transform;
            var sourceLabel=roots.SelectMany(x=>x.GetComponentsInChildren<TextMesh>(true))
                .Single(x=>x.name=="Equipment station label").transform;
            var station=new GameObject("South response station").transform;
            station.SetParent(wing,false);
            station.position=new Vector3(-2.95f,sourceCart.position.y,-18.00f);
            var delta=station.position-sourceCart.position;
            foreach(var original in new[]{sourceCart,sourceTools,sourceLabel})
            {
                var copy=Object.Instantiate(original.gameObject,station,true).transform;
                copy.name=original==sourceTools ? "South response equipment" : original==sourceCart ? "Response cart" : "South response kit label";
                copy.position=original.position+delta;
            }
        }

        sealed class Venue
        {
            readonly Transform root;
            readonly Dictionary<string, Material> materials = new();
            const string Interior = "Interiors/Materials/";
            const string Field = "Field/Materials/";
            const string Wood = Interior + "Honey ash", Steel = Field + "Galvanized steel", Light = Field + "Clerestory daylight";
            const string Cream = Interior + "Pale limestone", Dark = Field + "Blue-black enamel";
            const string Green = Interior + "Juniper green", Blue = Interior + "Canteen blue";
            const string Ochre = Field + "Signal ochre", Red = Field + "Red oxide enamel";
            public Venue(Transform root) { this.root = root; }

            public void ExtendBay(Transform bay, Material originalFloor, int id)
            {
                float rear = bay.Find("Rear").localPosition.z;
                float centre = (rear + South) / 2f;
                foreach (string name in new[] { "Foundation", "Ceiling", "West", "East" })
                {
                    var face = bay.Find(name);
                    var at = face.localPosition; at.z = centre; face.localPosition = at;
                    var dimensions = face.localScale; dimensions.z = rear - South + .16f; face.localScale = dimensions;
                }
                var entry = bay.Find("Entry"); var edge = entry.localPosition;
                edge.z = South; entry.localPosition = edge;
                var observation = bay.Find("Observation station"); var window = observation.localPosition;
                window.z = South + .10f; observation.localPosition = window;
                float half = bay.Find("East").localPosition.x;
                // The original floor ends at -10. Both walking surfaces meet at the same elevation.
                var floor = Box("Concourse floor",0,-.15f,(South-10f)/2,half*2,.3f,-10f-South,Dark,true);
                floor.GetComponent<MeshRenderer>().sharedMaterial = id==0 ? Finish(Wood) : id==1 ? Finish(Interior+"Moss tile") : originalFloor;
                // A change of floor finish marks the seam and visually separates the old care bay from the new public area.
                Box("Threshold inlay",0,.003f,-10.02f,half*2,.006f,.095f,id<2 ? Dark : Ochre);
                if(id==0 || id==1)
                {
                    foreach(float x in new[]{-8.65f,8.65f})
                        Box("Seating strip",x,.0035f,-17.93f,2.28f,.007f,15.65f,id==0 ? Interior+"Warm slate floor" : Cream);
                    for(int i=0;i<6;i++)
                        Box("Floor joint",0,.0026f,-11f-2.52f*i,14.98f,.0052f,.016f,id==0 ? Interior+"Warm black" : Field+"Industrial wall plinth");
                }
                foreach (var side in new[] {-1,1})
                {
                    float x = side*(half-.10f);
                    Box("Lower wall panel",x,.58f,-17.91f,.07f,1.16f,16.64f,id==0 ? Green : id==1 ? Blue : Field+"Industrial wall plinth");
                    Box("Protective wall rail",x-side*.055f,1.2f,-17.91f,.10f,.09f,16.64f,id<2 ? Wood : Steel);
                }
                float roof = bay.Find("Ceiling").localPosition.y;
                for(int i=0;i<3;i++)
                {
                    float z = -12.15f - i*5.35f;
                    Box("Beam",0,roof-.16f,z,half*2,.22f,.16f,id<2 ? Wood : Steel);
                    foreach(int sign in new[] {-1,1})
                    {
                        Box("Fixture frame",sign*4.8f,roof-.30f,z,2.7f,.10f,.42f,Dark);
                        Box("Light diffuser",sign*4.8f,roof-.36f,z,2.54f,.025f,.32f,Light);
                    }
                }
                var fill=new GameObject("Concourse bounce").AddComponent<Light>();
                fill.transform.SetParent(root,false); fill.transform.localPosition=new Vector3(1f,roof-1.2f,-17.5f);
                fill.type=UnityEngine.LightType.Point; fill.range=17f; fill.intensity=id<2?.57f:1.04f;
                fill.color=id==0 ? new Color(1,.91f,.79f) : new Color(.91f,.95f,1);
                fill.shadows=LightShadows.None;
                fill.renderMode=LightRenderMode.ForceVertex;
                string[] names={"READING ROOM","SOUTH COMMONS","PARCEL EXCHANGE","ASSEMBLY FLOOR"};
                string[] finishes={Green,Blue,Dark,Red};
                foreach(float x in new[]{-2.90f,-.26f})
                    Box("Sign suspension",x,(roof+3.53f)*.5f,-10.65f,.024f,roof-3.53f,.024f,Steel);
                Plaque(names[id],-1.58f,3.32f,-10.65f,3.54f,0,finishes[id],.45f);
                Box("Hall name reverse",-1.58f,3.32f,-10.61f,3.54f,.45f,.027f,finishes[id]);
            }

            public void Coffee()
            {
                // Two quiet banquettes line the reading room. A pastry display anchors the far end.
                foreach (float z in new[] {-13.4f,-19.65f})
                {
                    Furniture("benchCushion",9.36f,0,z,.64f,.87f,2.30f);
                    Furniture("tableRound",8.77f,0,z,.88f,.72f,.93f);
                    Food("mug",8.71f,.72f,z+.2f,.14f,.13f,.14f);
                    Food("plate",8.78f,.72f,z-.17f,.23f,.025f,.23f);
                    Food("muffin",8.78f,.745f,z-.17f,.12f,.12f,.12f);
                    for (int i=0;i<5;i++) Box("Oak acoustic slat",9.99f,2.62f,z-1+i*.43f,.09f,2.7f,.16f,Wood);
                }
                Box("Pastry counter",5.95f,.48f,-25.29f,5.05f,.96f,1.13f,Green,true);
                Box("Stone counter top",5.95f,1.01f,-25.26f,5.18f,.10f,1.23f,Cream);
                for(int i=0;i<3;i++)
                {
                    float x=4.1f+i*.69f;
                    Food("plate-rectangle",x,1.06f,-25.13f,.53f,.03f,.39f);
                    Food("loaf-baguette",x,1.09f,-25.12f,.41f,.13f,.14f,15f);
                }
                Furniture("kitchenCoffeeMachine",7.41f,1.06f,-25.27f,.70f,.59f,.50f);
                Plaque("BAKERY",6.2f,3.0f,-26.14f,4.3f,180,Green);
                ReadingTable(-7.86f,-21.13f,1f);
                ReadingTable(3.18f,-16.68f,-1f);
                foreach(float z in new[]{-12.65f,-20.65f})
                {
                    Box("Bookcase back",-9.75f,1.03f,z,.15f,2.06f,2.48f,Green);
                    foreach(float offset in new[]{-1.17f,1.17f})
                        Box("Bookcase cheek",-9.53f,1.03f,z+offset,.47f,2.06f,.075f,Wood,true);
                    for(int shelf=0;shelf<3;shelf++)
                    {
                        float y=.26f+shelf*.66f;
                        Box("Library shelf",-9.52f,y,z,.48f,.065f,2.44f,Wood);
                        for(int k=0;k<8;k++)
                        {
                            float h=.25f+.065f*((k+2*shelf)%4);
                            string jacket=k%3==0 ? Red : k%3==1 ? Cream : Blue;
                            Box("Book",-9.51f,y+h*.5f+.034f,z-.91f+k*.185f,.248f,h,.115f+(k%2)*.04f,jacket);
                        }
                    }
                    Furniture("plantSmall1",-9.48f,1.62f,z+.86f,.34f,.36f,.34f);
                }
                Box("Reading island",0,.34f,-18,.92f,.68f,2.22f,Green,true);
                Box("Reading island rim",0,.69f,-18,1.04f,.09f,2.32f,Wood);
                Furniture("plantSmall1",0,.74f,-18,.53f,.73f,.53f);
                for(int i=0;i<2;i++) Box("Periodical",0,.752f,-17.27f-.09f*i,.44f,.017f,.31f,i==0 ? Dark : Cream);
                Plaque("READING ROOM",-9.98f,2.48f,-17.1f,4.35f,270,Green);
            }

            void ReadingTable(float x,float z,float side)
            {
                Furniture("tableRound",x,0,z,1.13f,.74f,1.13f);
                Model("Interiors/Furniture/chairRounded",Interior+"Furniture palette",x-.84f*side,0,z+.30f,.57f,.87f,.57f,true,-90f*side);
                Model("Interiors/Furniture/chairModernCushion",Interior+"Furniture palette",x+.56f*side,0,z-.85f,.59f,.88f,.62f,true,35f*side);
                Food("mug",x-.26f,.74f,z+.10f,.14f,.13f,.14f);
                Box("Open reading copy",x+.16f,.75f,z,.34f,.025f,.25f,Cream);
                Box("Book gutter",x+.16f,.765f,z,.014f,.003f,.24f,Wood);
            }

            public void Dining()
            {
                foreach(float z in new[] {-12.6f,-18.3f,-22.1f})
                {
                    Furniture("bench",9.22f,0,z,.68f,.69f,2.36f);
                    Furniture("table",9.23f,0,z+1.13f,.91f,.79f,1.12f);
                    Food("glass",9.18f,.79f,z+1.18f,.09f,.14f,.09f);
                    Food("apple",9.02f,.79f,z+.87f,.10f,.10f,.10f);
                }
                Box("Water station",0,.47f,-18,.91f,.94f,2.15f,Blue,true);
                Box("Water station worktop",0,.98f,-18,1.02f,.09f,2.25f,Steel);
                foreach(float z in new[] {-17.40f,-17.82f}) Food("glass",.24f,1.03f,z,.085f,.125f,.085f);
                Box("Dispenser",0,1.31f,-18.56f,.47f,.57f,.49f,Steel);
                Box("Dispensing tap",0,1.21f,-18.26f,.13f,.09f,.17f,Dark);
                Box("Servery wall tile",-6f,1.97f,-26.03f,6.15f,3.8f,.11f,Blue);
                Box("Grill service window",-6f,1.90f,-25.90f,4.7f,1.63f,.10f,Dark);
                Box("Pass counter",-6f,.50f,-25.15f,5.16f,1f,1.12f,Cream,true);
                Box("Tray slide",-6f,1.02f,-25.05f,5.22f,.09f,1.25f,Steel);
                foreach(float x in new[]{-7.75f,-7f,-6.25f,-5.5f})
                {
                    Food("bowl",x,1.07f,-25f,.37f,.16f,.37f);
                    Food("salad",x,1.16f,-25f,.29f,.11f,.29f);
                }
                Plaque("GARDEN GRILL",-6f,3.18f,-25.82f,4.65f,180,Blue);
                Plaque("SOUTH COMMONS",9.98f,2.65f,-16.7f,5.2f,90,Blue);
                Furniture("table",3.38f,0,-16.90f,1.20f,.79f,2.10f);
                foreach(float x in new[]{2.48f,4.29f})
                    Furniture("bench",x,0,-16.9f,.51f,.52f,1.87f);
                foreach(float z in new[]{-16.34f,-17.40f})
                {
                    Food("bowl",3.29f,.79f,z,.30f,.12f,.30f);
                    Food("salad",3.29f,.865f,z,.23f,.08f,.23f);
                    Food("glass",3.66f,.79f,z+.13f,.085f,.145f,.085f);
                }
                foreach(float z in new[]{-12.35f,-20.75f})
                {
                    Furniture("benchCushion",-9.40f,0,z,.66f,.82f,2.25f);
                    Furniture("tableRound",-8.53f,0,z,1.00f,.74f,1f);
                    Food("plate-rectangle",-8.48f,.74f,z,.34f,.032f,.24f);
                    Food("sandwich",-8.48f,.772f,z,.19f,.09f,.14f);
                }
                Furniture("pottedPlant",-9.05f,0,-14.05f,.70f,1.12f,.70f);
            }

            public void Dispatch()
            {
                // Marked loading bays face the sorting lane. Pallets and parcel cages sit near the perimeter.
                foreach(float x in new[]{-6.8f,6.8f})
                {
                    Box("Roll-up door surround",x,2.5f,-26.1f,4.3f,5.0f,.14f,Dark);
                    Box("Shutter",x,2.22f,-25.99f,3.88f,4.37f,.11f,Field+"Rolling door panels");
                    for (int k=1;k<8;k++) Box("Shutter joint",x,k*.52f,-25.916f,3.84f,.028f,.04f,Steel);
                    Plaque(x<0?"DOCK  03":"DOCK  04",x,4.94f,-25.97f,3.28f,180,Dark);
                    foreach(float dx in new[]{-1.95f,1.95f}) Box("Dock guard",x+dx,.50f,-25.29f,.18f,1f,.19f,Ochre,true);
                    Box("Dock floor marking",x,.008f,-23.97f,4.8f,.012f,.09f,Ochre);
                }
                foreach(float z in new[]{-13.3f,-16.85f,-20.4f})
                {
                    Box("Freight pallet",9.28f,.09f,z,1.26f,.18f,1.60f,Wood,true);
                    for(int k=0;k<2;k++) Parcel(9.25f,.18f+.55f*k,z,.91f,.55f,1.13f);
                }
                Box("Sorting pallet",0,.075f,-18,1.3f,.15f,2.22f,Wood,true);
                Parcel(0,.15f,-18.5f,.91f,.63f,.88f);
                Parcel(0,.15f,-17.48f,.88f,.43f,.80f);
                Box("Lane divider",-8.32f,.009f,-18,.065f,.013f,14.2f,Ochre);
                Plaque("PARCEL DISPATCH",-10.39f,2.6f,-15.9f,5.1f,270,Dark);
                foreach(float z in new[]{-12.70f,-20.73f})
                {
                    foreach(float dz in new[]{-.98f,.98f})
                    foreach(float x in new[]{-9.75f,-8.71f})
                        Box("Storage rack upright",x,1.3f,z+dz,.075f,2.6f,.075f,Steel,true);
                    for(int k=0;k<3;k++)
                    {
                        float y=.17f+k*.98f;
                        Box("Pallet rack tier",-9.23f,y,z,1.14f,.10f,2.08f,Ochre);
                        if(k<2)
                            foreach(float dz in new[]{-.48f,.48f}) Parcel(-9.22f,y+.05f,z+dz,.82f,.55f,.8f);
                    }
                }
                Box("Outbound scale",3.53f,.12f,-16.8f,1.64f,.24f,1.80f,Steel,true);
                Parcel(3.53f,.24f,-16.8f,1.24f,.70f,1.21f);
                Model("Field/car-kit/cone",Field+"car-kit palette",4.74f,0,-16.05f,.43f,.59f,.43f);
            }

            public void Workshop()
            {
                foreach(float z in new[]{-13.6f,-19.8f})
                {
                    Box("Bench base",9.21f,.45f,z,1.12f,.90f,2.96f,Field+"Industrial wall plinth",true);
                    Box("Workbench top",9.21f,.96f,z,1.24f,.12f,3.04f,Field+"Oiled beech");
                    Model("Field/bench_vice_01",Field+"bench_vice_01 coat",9.12f,1.02f,z-.44f,.42f,.30f,.51f);
                    Model("Field/adjustable_wrench",Field+"adjustable_wrench coat",9.12f,1.02f,z+.51f,.29f,.055f,.10f);
                }
                Box("Assembly cart",0,.44f,-18,1.08f,.88f,2.13f,Red,true);
                Box("Assembly surface",0,.93f,-18,1.21f,.10f,2.27f,Wood);
                Model("Field/bench_vice_01",Field+"bench_vice_01 coat",0,.98f,-18.48f,.44f,.32f,.53f);
                Box("Inspection drawing",0,.986f,-17.32f,.54f,.008f,.38f,Cream);
                foreach(float x in new[]{-7.4f,-5.5f})
                {
                    Model("Field/metal_tool_chest",Field+"metal_tool_chest coat",x,0,-25.18f,1.30f,1.20f,.72f,true);
                    Box("Perforated toolboard",x,2f,-26.12f,1.60f,1.25f,.08f,Field+"Industrial wall plinth");
                    for(int i=0;i<3;i++) Box("Hanging tool",x-.45f+i*.42f,2.0f,-26.03f,.07f,.49f,.07f,Steel);
                }
                Plaque("TOOL CRIB",-6.5f,3.13f,-26.05f,3.8f,180,Red);
                Plaque("ASSEMBLY",9.98f,2.65f,-16.7f,4.1f,90,Red);
                Model("Field/metal_tool_chest",Field+"metal_tool_chest coat",3.38f,0,-16.85f,1.43f,1.04f,.73f,true);
                Model("Field/metal_stool_02",Field+"metal_stool_02 coat",4.33f,0,-17.5f,.50f,.64f,.50f,true);
                foreach(float z in new[]{-12.67f,-20.71f})
                {
                    Box("Machine stand",-9.16f,.45f,z,1.03f,.90f,2.14f,Field+"Industrial wall plinth",true);
                    Box("Machine table",-9.16f,.96f,z,1.16f,.12f,2.23f,Steel);
                    Model("Field/drill_press_01",Field+"drill_press_01 coat",-9.16f,1.02f,z,.85f,1.28f,1.32f);
                    Box("Tool shadow board",-9.98f,1.94f,z,.095f,1.56f,2.70f,Red);
                }
            }

            public void Alpine(Transform art)
            {
                var trail = art.Find("North Ridge winter refuge/Trail boundaries and accessible shelter");
                foreach (Transform child in trail)
                {
                    if (child.name != "Trail boundary") continue;
                    if (child.localPosition.z < -9f && Mathf.Abs(child.localPosition.x) < .5f)
                        child.localPosition = new Vector3(0f,1.5f,South);
                    else if (Mathf.Abs(child.localPosition.x)>9f)
                    {
                        child.localPosition=new Vector3(child.localPosition.x,1.5f,(9.9f+South)*.5f);
                        child.GetComponent<BoxCollider>().size=new Vector3(.2f,3f,9.9f-South+.2f);
                    }
                }
                var floor=Box("Packed snow walking surface",0,-.15f,(South-10)/2,20,.30f,-10-South,Cream,true);
                floor.GetComponent<MeshRenderer>().enabled=false; // The painted TrailClearing mesh supplies the ground surface.
                foreach(int side in new[]{-1,1})
                {
                    for (int i=0;i<5;i++)
                    {
                        float z=-10.9f-i*3.77f, x=side*9.7f;
                        Box("Waypost",x,.59f,z,.125f,1.18f,.125f,Wood);
                        Box("Waypost blaze",x,1.03f,z,.132f,.126f,.132f,Ochre);
                        if(i<4) Box("Boundary rope",x,.78f,z-1.885f,.030f,.030f,3.77f,Dark);
                    }
                }
                for(int i=-3;i<=3;i++)
                {
                    float x=i*3.15f;
                    Box("Lookout rail post",x,.64f,-26.12f,.16f,1.28f,.16f,Wood);
                    if(i<3)
                    {
                        Box("Lookout rail",x+1.575f,1.10f,-26.12f,3.15f,.16f,.18f,Wood);
                        Box("Lower rail",x+1.575f,.54f,-26.12f,3.15f,.10f,.12f,Wood);
                    }
                }
                // The low trail junction stays visible around its sign from either walking loop.
                Box("Junction post",0,.77f,-18,.16f,1.54f,.16f,Wood,true);
                Plaque("REFUGE",0,1.55f,-18.1f,1.42f,0,Green,.32f);
                Plaque("OVERLOOK",0,1.18f,-17.90f,1.55f,180,Green,.32f);
                foreach(float x in new[]{-6.6f,6.6f})
                    Furniture("bench",x,0,-25.27f,2.48f,.63f,.57f);
                Plaque("NORTH RIDGE LOOKOUT",0,1.62f,-26.15f,3.60f,180,Green,.48f);
            }

            Material Finish(string name)
            {
                if (!materials.TryGetValue(name,out var mat))
                {
                    mat=AssetDatabase.LoadAssetAtPath<Material>(Art+name+".mat");
                    if (!mat) throw new InvalidOperationException("Missing scenery material "+name);
                    materials[name]=mat;
                }
                return mat;
            }

            Transform Box(string name,float x,float y,float z,float w,float h,float d,string finish,bool solid=false)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name=name;
                obj.transform.SetParent(root,false);
                obj.transform.localPosition=new Vector3(x,y,z);
                obj.transform.localScale=new Vector3(w,h,d);
                if (!solid) Object.DestroyImmediate(obj.GetComponent<Collider>());
                obj.GetComponent<MeshRenderer>().sharedMaterial=Finish(finish);
                GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic);
                return obj.transform;
            }

            void Plaque(string value,float x,float y,float z,float width,float yaw,string finish,float height=.61f)
            {
                var board=Box(value+" panel",x,y,z,width,height,.065f,finish);
                board.localRotation=Quaternion.Euler(0,yaw,0);
                var label=new GameObject(value).AddComponent<TextMesh>();
                label.transform.SetParent(root,false);
                label.transform.SetPositionAndRotation(board.position-board.forward*.037f,board.rotation);
                label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
                label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize=72;label.fontStyle=FontStyle.Bold;label.text=value;label.color=Color.white;
                var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=Finish("Shared/World type");
                var size=renderer.localBounds.size;
                label.transform.localScale=Vector3.one*Mathf.Min(width*.86f/Mathf.Max(.001f,size.x),height*.65f/Mathf.Max(.001f,size.y));
                renderer.shadowCastingMode=ShadowCastingMode.Off;
            }

            void Furniture(string name,float x,float y,float z,float w,float h,float d) =>
                Model("Interiors/Furniture/"+name,Interior+"Furniture palette",x,y,z,w,h,d,y<.01f);
            void Food(string name,float x,float y,float z,float w,float h,float d,float yaw=0) =>
                Model("Interiors/Food/"+name,Interior+"Food palette",x,y,z,w,h,d,false,yaw);
            void Parcel(float x,float y,float z,float w,float h,float d) =>
                Model("Field/car-kit/box",Field+"car-kit palette",x,y,z,w,h,d,true);

            void Model(string path,string finish,float x,float y,float z,float w,float h,float d,bool solid=false,float yaw=0)
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+path+".obj");
                if (!asset) throw new InvalidOperationException("Missing venue model "+path);
                var pivot=new GameObject(asset.name).transform;
                pivot.SetParent(root,false);
                pivot.localPosition=new Vector3(x,y,z);
                var model=Object.Instantiate(asset,pivot,false).transform;
                model.localPosition=Vector3.zero;
                var renderers=model.GetComponentsInChildren<MeshRenderer>();
                Bounds bounds=renderers[0].bounds;
                foreach(var renderer in renderers)
                {
                    renderer.sharedMaterials=Enumerable.Repeat(Finish(finish),renderer.sharedMaterials.Length).ToArray();
                    bounds.Encapsulate(renderer.bounds);
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
                }
                // Keep the geometry at its natural proportions within the requested furnishing footprint.
                float scale=Mathf.Min(w/Mathf.Max(.001f,bounds.size.x),h/Mathf.Max(.001f,bounds.size.y),d/Mathf.Max(.001f,bounds.size.z));
                // Long benches have their long axis across X; rotate them to follow the side walls.
                if ((w<d && bounds.size.x>bounds.size.z*1.8f) || (d<w && bounds.size.z>bounds.size.x*1.8f))
                {
                    model.localRotation=Quaternion.Euler(0,90,0)*model.localRotation;
                    bounds=renderers[0].bounds;
                    foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    scale=Mathf.Min(w/Mathf.Max(.001f,bounds.size.x),h/Mathf.Max(.001f,bounds.size.y),d/Mathf.Max(.001f,bounds.size.z));
                }
                model.localScale*=scale;
                bounds=renderers[0].bounds;
                foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.position+=pivot.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                if(solid)
                {
                    var collider=pivot.gameObject.AddComponent<BoxCollider>();
                    collider.size=bounds.size;collider.center=Vector3.up*bounds.extents.y;
                }
                pivot.localRotation=Quaternion.Euler(0,yaw,0);
            }
        }
    }
}
