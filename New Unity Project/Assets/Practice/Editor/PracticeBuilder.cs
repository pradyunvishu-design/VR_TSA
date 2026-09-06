using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace First180.Practice.Editor
{
    public static class PracticeBuilder
    {
        const string Root="Assets/Practice";
        static Material cream, dark, wood, teal, metal, glass, skin, red, snow, yellow;
        static Transform env;
        static Material Mat(string name,Color c,float smooth=.25f,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=c;m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metallic);return m;
        }
        static GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent?parent:env,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;return o;
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null)=>Shape(n,PrimitiveType.Cube,p,s,m,parent);
        static void Text(string n,string text,Vector3 p,float size,Transform parent=null,Color? color=null)
        {
            var o=new GameObject(n);o.transform.SetParent(parent?parent:env,false);o.transform.localPosition=p;
            var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color??Color.white;
        }
        static void LightAt(Vector3 p,Color c,float intensity,float range)
        {
            var o=new GameObject("Practical light");o.transform.SetParent(env);o.transform.position=p;
            var l=o.AddComponent<Light>();l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;
        }
        static void Table(Vector3 p,bool round=false)
        {
            if(round) Shape("Round oak tabletop",PrimitiveType.Cylinder,p+Vector3.up*.8f,new Vector3(1.6f,.07f,1.6f),wood);
            else Box("Oak tabletop",p+Vector3.up*.8f,new Vector3(2.1f,.12f,1.1f),wood);
            Box("Table pedestal",p+Vector3.up*.4f,new Vector3(.2f,.8f,.2f),dark);
            Box("Table base",p+Vector3.up*.05f,new Vector3(.8f,.08f,.65f),dark);
        }
        static void Chair(Vector3 p)
        {
            Box("Chair seat",p+Vector3.up*.43f,new Vector3(.52f,.09f,.5f),teal);
            Box("Chair back",p+new Vector3(0,.78f,.2f),new Vector3(.52f,.6f,.08f),teal);
            foreach(float x in new[]{-.2f,.2f}) foreach(float z in new[]{-.2f,.2f}) Box("Chair leg",p+new Vector3(x,.2f,z),new Vector3(.045f,.4f,.045f),dark);
        }
        static void Plant(Vector3 p)
        {
            Shape("Planter",PrimitiveType.Cylinder,p+Vector3.up*.28f,new Vector3(.65f,.28f,.65f),cream);
            Box("Stem",p+Vector3.up*.9f,new Vector3(.09f,1.2f,.09f),wood);
            for(int i=0;i<5;i++) Shape("Foliage",PrimitiveType.Sphere,p+new Vector3(Mathf.Sin(i*2)*.3f,1.1f+i*.1f,Mathf.Cos(i*2)*.3f),new Vector3(.7f,.3f,.6f),teal);
        }
        static void Shell(int id)
        {
            Box("Floor",new Vector3(0,-.15f,0),new Vector3(20,.3f,20),id==4?snow:cream);
            Box("Rear wall",new Vector3(0,2.4f,9),new Vector3(20,4.8f,.25f),id==3?metal:cream);
            Box("Left wall",new Vector3(-10,2.4f,0),new Vector3(.25f,4.8f,18),cream);
            Box("Right wall",new Vector3(10,2.4f,0),new Vector3(.25f,4.8f,18),cream);
            for(int i=-3;i<=3;i++)
            {
                Box("Rear glazing",new Vector3(i*2.6f,2.7f,8.82f),new Vector3(2.35f,2.7f,.04f),glass);
                Box("Window mullion",new Vector3(i*2.6f-1.2f,2.7f,8.74f),new Vector3(.07f,3,.1f),dark);
            }
            for(int x=-6;x<=6;x+=6)
            {
                Box("Ceiling beam",new Vector3(x,4.6f,0),new Vector3(.18f,.22f,18),wood);
                Box("Linear pendant",new Vector3(x,4.1f,1),new Vector3(.16f,.09f,5),cream);
                LightAt(new Vector3(x,3.8f,1),id==4?new Color(.68f,.8f,1):new Color(1,.88f,.72f),1.5f,13);
            }
            var sun=new GameObject("Daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            RenderSettings.ambientLight=new Color(.38f,.43f,.48f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.66f,.76f,.81f);RenderSettings.fogDensity=.007f;
        }
        static void Environment(int id)
        {
            Shell(id);
            switch(id)
            {
                case 0:
                    Box("Cafe counter",new Vector3(-7, .6f,4),new Vector3(3,1.2f,6),wood);
                    Box("Stone counter top",new Vector3(-7,1.24f,4),new Vector3(3.15f,.12f,6.15f),cream);
                    Box("Espresso machine",new Vector3(-7,1.65f,6),new Vector3(1.1f,.7f,.65f),metal);
                    Text("Cafe sign","JUNIPER\nCOFFEE & KITCHEN",new Vector3(-6.5f,3.4f,8.5f),.13f,null,Color.black);
                    for(int i=0;i<3;i++){Table(new Vector3(5,0,-2+i*3),true);Chair(new Vector3(5,0,-3+i*3));}
                    Table(new Vector3(-1,0,3),true);Plant(new Vector3(8,0,7));
                    Shape("Allergen snack plate",PrimitiveType.Cylinder,new Vector3(-1,.9f,3),new Vector3(.4f,.025f,.4f),cream);
                    break;
                case 1:
                    Text("Commons sign","THE COMMONS / DINING",new Vector3(0,3.8f,8.5f),.16f,null,Color.black);
                    for(int x=-6;x<=6;x+=6)for(int z=2;z<=6;z+=4){Table(new Vector3(x,0,z));Chair(new Vector3(x,0,z-1));Chair(new Vector3(x,0,z+1));}
                    Box("Serving station",new Vector3(-8,.6f,4),new Vector3(2,1.2f,6),wood);Plant(new Vector3(8,0,7));break;
                case 2:
                    Box("Service road",new Vector3(0,.015f,0),new Vector3(8,.025f,19),dark);
                    for(int z=-8;z<9;z+=3)Box("Road marking",new Vector3(0,.034f,z),new Vector3(.14f,.01f,1.3f),yellow);
                    Box("Delivery vehicle",new Vector3(-2,.85f,5),new Vector3(2.2f,1.3f,4.2f),teal);
                    Box("Vehicle cabin",new Vector3(-2,1.85f,4),new Vector3(2.1f,.9f,1.8f),metal);
                    for(int x=-3;x<=-1;x+=2)for(int z=4;z<=6;z+=2){var w=Shape("Wheel",PrimitiveType.Cylinder,new Vector3(x,.46f,z),new Vector3(.85f,.16f,.85f),dark);w.transform.Rotate(0,0,90);}
                    for(int z=0;z<8;z+=2)Box("Safety bollard",new Vector3(5,.55f,z),new Vector3(.18f,1.1f,.18f),yellow);
                    Text("Road sign","RIVERSIDE / SERVICE ACCESS",new Vector3(0,3.7f,8.5f),.14f);break;
                case 3:
                    Text("Workshop sign","FABRICATION LAB / BAY 04",new Vector3(0,3.7f,8.5f),.14f,null,Color.black);
                    for(int i=0;i<3;i++){Table(new Vector3(-6,0,i*3));Box("Machine housing",new Vector3(-6,1.2f,i*3),new Vector3(1.2f,.65f,.75f),metal);}
                    Box("Isolated equipment",new Vector3(3,.85f,5),new Vector3(3,1.7f,2),metal);
                    Box("Safety guard",new Vector3(3,1.9f,5),new Vector3(3.2f,.15f,2.2f),yellow);
                    for(int i=0;i<5;i++)Box("Safety floor stripe",new Vector3(1+i*.7f,.02f,3.4f),new Vector3(.3f,.025f,.6f),yellow);
                    break;
                case 4:
                    Box("Shelter deck",new Vector3(-5,.1f,4),new Vector3(7,.2f,7),wood);
                    Box("Shelter roof",new Vector3(-5,3.5f,4),new Vector3(7.5f,.24f,7.5f),wood);
                    foreach(float x in new[]{-8f,-2f})foreach(float z in new[]{1f,7f})Box("Shelter post",new Vector3(x,1.8f,z),new Vector3(.24f,3.5f,.24f),wood);
                    for(int i=0;i<7;i++){var p=new Vector3(3+i%3*2,0,3+i/3*2);Box("Pine trunk",p+Vector3.up*1.3f,new Vector3(.24f,2.6f,.24f),wood);Shape("Snowy pine crown",PrimitiveType.Capsule,p+Vector3.up*3,new Vector3(1.5f,1.7f,1.5f),snow);}
                    Text("Trail sign","NORTH RIDGE\nEMERGENCY SHELTER",new Vector3(-5,3.05f,.8f),.13f);break;
            }
        }
        static PracticePatient Patient(int index,int scenario,Vector3 pos)
        {
            var root=new GameObject("Patient "+(char)('A'+index));root.transform.position=pos;
            var p=root.AddComponent<PracticePatient>();p.patientId=index;
            p.body=Shape("Torso",PrimitiveType.Capsule,new Vector3(0,1.05f,0),new Vector3(.48f,.43f,.3f),index==0?teal:wood,root.transform).transform;
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.7f,0),new Vector3(.32f,.37f,.32f),skin,root.transform);
            foreach(float side in new[]{-1f,1f})
            {
                if(!(scenario==3 && index==0 && side>0))Shape("Leg",PrimitiveType.Capsule,new Vector3(side*.14f,.43f,0),new Vector3(.2f,.43f,.2f),dark,root.transform);
                Shape("Arm",PrimitiveType.Capsule,new Vector3(side*.34f,1.05f,0),new Vector3(.15f,.36f,.15f),skin,root.transform);
            }
            Text("Patient marker","PATIENT "+(char)('A'+index),new Vector3(0,2.15f,0),.055f,root.transform,Color.black);
            p.injuryVisual=Shape("Non-graphic symptom indicator",PrimitiveType.Sphere,new Vector3(.19f,scenario==0?1.65f:scenario==1?1.4f:.5f,-.16f),new Vector3(.2f,.19f,.06f),scenario==4?snow:red,root.transform);
            return p;
        }
        static void Rig(Vector3 spawn)
        {
            var rig=new GameObject("Player / desktop and XR-ready origin");rig.transform.position=spawn;
            var cc=rig.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.25f;cc.center=new Vector3(0,.9f,0);
            var head=new GameObject("Head camera");head.transform.SetParent(rig.transform);head.transform.localPosition=new Vector3(0,1.65f,0);
            var camera=head.AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.fieldOfView=70;camera.tag="MainCamera";
            head.AddComponent<AudioListener>();rig.AddComponent<PracticePlayer>().view=camera;
        }
        static void Robot(PracticeAudio audio)
        {
            var root=new GameObject("ROOK / medical training companion");root.transform.position=new Vector3(2,1.7f,0);audio.robot=root.transform;
            Shape("Ceramic body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.55f,.45f,.4f),cream,root.transform);
            Box("Face screen",new Vector3(0,.04f,-.2f),new Vector3(.37f,.16f,.045f),dark,root.transform);
            foreach(float x in new[]{-.09f,.09f})Shape("Optic",PrimitiveType.Sphere,new Vector3(x,.055f,-.23f),new Vector3(.05f,.05f,.02f),teal,root.transform);
            Shape("Hover base",PrimitiveType.Cylinder,new Vector3(0,-.27f,0),new Vector3(.36f,.045f,.36f),teal,root.transform);
        }
        static void ReturnPortal()
        {
            var o=Box("Return portal",new Vector3(8,1.3f,-5),new Vector3(1.8f,2.6f,.16f),teal);
            var p=o.AddComponent<PracticePortal>();p.returnToHub=true;p.label="Return to lobby";
            Text("Return label","RETURN\nTO LOBBY",new Vector3(8,1.65f,-5.12f),.13f);
        }
        [MenuItem("FIRST180 Practice/Build missing practice scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Materials");
            cream=Mat("Warm ceramic",new Color(.83f,.81f,.75f),.5f);dark=Mat("Graphite",new Color(.075f,.1f,.12f));
            wood=Mat("Oak",new Color(.43f,.25f,.12f));teal=Mat("Clinical teal",new Color(.07f,.38f,.4f),.45f);
            metal=Mat("Brushed steel",new Color(.43f,.48f,.5f),.65f,.65f);glass=Mat("Blue glazing",new Color(.37f,.58f,.66f),.8f,.25f);
            skin=Mat("Mannequin skin",new Color(.6f,.43f,.32f));red=Mat("Symptom marker",new Color(.65f,.11f,.1f));snow=Mat("Snow",new Color(.91f,.96f,1));yellow=Mat("Safety amber",new Color(.95f,.6f,.12f));
            var paths=new List<string>();
            string hub=Root+"/Scenes/TrainingHub.unity";paths.Add(hub);
            if(!File.Exists(hub))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);env=new GameObject("Runtime additions only / original lobby loaded additively").transform;
                var room=new GameObject("Hub systems").AddComponent<PracticeRoom>();room.scenarioId=-1;room.gameObject.AddComponent<LobbyConnections>();
                var floor=Box("Invisible navigation floor",new Vector3(0,-.12f,0),new Vector3(32,.2f,32),cream);floor.GetComponent<Renderer>().enabled=false;
                Rig(new Vector3(0,.1f,-7));EditorSceneManager.SaveScene(scene,hub);
            }
            foreach(var d in ScenarioCatalog.All)
            {
                string path=Root+"/Scenes/"+d.scene+".unity";paths.Add(path);if(File.Exists(path))continue;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);env=new GameObject("Environment / "+d.location).transform;
                Environment(d.id);var room=new GameObject("Scenario systems / "+d.title).AddComponent<PracticeRoom>();room.scenarioId=d.id;
                room.patients=new[]{Patient(0,d.id,new Vector3(-1,0,1)),Patient(1,d.id,new Vector3(4,0,1)),Patient(2,d.id,new Vector3(-4,0,-1))};
                room.incidentRoot=new GameObject("Unexpected event / warning beacon");var lamp=room.incidentRoot.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.25f,.08f);lamp.range=9;lamp.intensity=2;lamp.transform.position=new Vector3(0,3.4f,2);
                Table(new Vector3(-5,0,-4));Text("Equipment station label","RESPONSE EQUIPMENT",new Vector3(-5,1.55f,-3.5f),.065f,null,Color.black);
                Robot(room.GetComponent<PracticeAudio>());ReturnPortal();Rig(new Vector3(0,.1f,-6));
                EditorSceneManager.SaveScene(scene,path);
            }
            paths.Add("Assets/Scenes/FIRST180_Lobby.unity");
            var builds=new List<EditorBuildSettingsScene>();foreach(var p in paths)builds.Add(new EditorBuildSettingsScene(p,true));
            foreach(var b in EditorBuildSettings.scenes)if(!paths.Contains(b.path))builds.Add(b);
            EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root+"/Scenes/Cafe_Anaphylaxis.unity");
            Debug.Log("PRACTICE_ENVIRONMENTS_BUILT=5; ORIGINAL_LOBBY_UNMODIFIED");
        }
    }
}
