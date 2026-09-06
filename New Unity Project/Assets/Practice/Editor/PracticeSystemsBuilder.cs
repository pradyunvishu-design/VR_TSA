using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace First180.Practice.Editor
{
    public static class PracticeSystemsBuilder
    {
        const string Root="Assets/Practice";
        static Material material;
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;return o;
        }
        static void Target(PracticePatient patient,string name,Vector3 p,params string[] actions)
        {
            var o=Shape(name,PrimitiveType.Sphere,patient.transform,p,new Vector3(.18f,.18f,.10f));o.GetComponent<Collider>().isTrigger=true;
            var t=o.AddComponent<TreatmentTarget>();t.actions=actions;t.patientId=patient.patientId;t.anatomicalSite=name;o.GetComponent<Renderer>().enabled=false;
        }
        [MenuItem("FIRST180 Practice/Add modular interactions and PC VR")]
        public static void Upgrade()
        {
            Directory.CreateDirectory(Root+"/Configs");
            material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Clinical teal.mat");
            string[] sceneNames={"TrainingHub","Cafe_Anaphylaxis","Dining_ObstructedAirways","Road_Hemorrhaging","Workshop_LostLimb","Trail_Frostbite"};
            foreach(string sceneName in sceneNames)
            {
                var scene=EditorSceneManager.OpenScene(Root+"/Scenes/"+sceneName+".unity");
                var room=Object.FindFirstObjectByType<PracticeRoom>();
                if(!room.GetComponent<SpatialPanel>())room.gameObject.AddComponent<SpatialPanel>();
                if(!room.GetComponent<TreatmentManager>())room.gameObject.AddComponent<TreatmentManager>();
                if(room.scenarioId>=0)
                {
                    if(!room.GetComponent<TrainingManager>())room.gameObject.AddComponent<TrainingManager>();
                    string configPath=Root+"/Configs/"+sceneName+".asset";
                    var config=AssetDatabase.LoadAssetAtPath<ScenarioConfig>(configPath);
                    if(!config){config=ScriptableObject.CreateInstance<ScenarioConfig>();config.scenarioId=room.scenarioId;AssetDatabase.CreateAsset(config,configPath);}room.config=config;
                    foreach(var patient in room.patients)
                    {
                        if(patient.patientId==0 && patient.injuryVisual && (room.scenarioId==2||room.scenarioId==3))patient.injuryVisual.transform.localPosition=new Vector3(.36f,1.02f,-.13f);
                        if(!patient.GetComponent<PatientManager>())patient.gameObject.AddComponent<PatientManager>();
                        if(patient.GetComponentInChildren<TreatmentTarget>())continue;
                        Target(patient,"Assessment and consent",new Vector3(0,1.65f,-.18f),"safety","monitor","call","water");
                        Target(patient,"Outer mid-thigh",new Vector3(.23f,.65f,-.14f),"epinephrine");
                        Target(patient,"Wound site",new Vector3(.36f,1.02f,-.12f),"pressure");
                        Target(patient,"Upper arm away from joint",new Vector3(.36f,1.34f,-.12f),"tourniquet","loosen");
                        Target(patient,"Between shoulder blades",new Vector3(0,1.3f,.21f),"backblows");
                        Target(patient,"Above navel",new Vector3(0,.98f,-.22f),"thrusts");
                        Target(patient,"Cold hand protection",new Vector3(-.37f,.88f,-.15f),"protect","rub");
                        Target(patient,"Warm clothing",new Vector3(0,1.17f,-.24f),"shelter");
                        Target(patient,"Protected recovery container",new Vector3(.7f,.4f,0),"preserve");
                    }
                    if(!GameObject.Find("Interactive response equipment"))
                    {
                        var toolsRoot=new GameObject("Interactive response equipment");int index=0;
                        string[] ids={"safety","call","epinephrine","backblows","thrusts","pressure","tourniquet","preserve","shelter","protect","monitor","water","rub","loosen"};
                        string[] names={"Protective gloves","Emergency phone","Prescribed injector trainer","Back-blow practice glove","Thrust practice glove","Pressure dressing","Tourniquet trainer","Sealed recovery kit","Dry clothing bundle","Loose dry dressing","Assessment scanner","Water bottle","Rubbing glove","Tourniquet release"};
                        for(int i=0;i<ids.Length;i++)
                        {
                            bool relevant=System.Array.IndexOf(room.Definition.actions,ids[i])>=0 || ids[i]=="water" || (room.scenarioId==4 && ids[i]=="rub") || ((room.scenarioId==2||room.scenarioId==3)&&ids[i]=="loosen");if(!relevant)continue;
                            var root=new GameObject(names[i]);root.transform.SetParent(toolsRoot.transform);root.transform.position=new Vector3(-6.2f+(index%3)*.85f,.98f,-4+(index/3)*.65f);index++;
                            var tool=root.AddComponent<TreatmentTool>();tool.actionId=ids[i];tool.holdSeconds=ids[i]=="pressure"?4:ids[i]=="backblows"||ids[i]=="thrusts"?3:2;
                            Shape("Tool body",ids[i]=="epinephrine"||ids[i]=="water"?PrimitiveType.Cylinder:PrimitiveType.Cube,root.transform,Vector3.zero,ids[i]=="epinephrine"?new Vector3(.08f,.18f,.08f):new Vector3(.26f,.12f,.22f));
                            var l=root.AddComponent<Light>();l.type=LightType.Point;l.range=.65f;l.intensity=.6f;l.color=Color.cyan;l.enabled=false;
                            var label=new GameObject("Equipment label");label.transform.SetParent(toolsRoot.transform);label.transform.position=root.transform.position+new Vector3(0,.18f,-.1f);
                            var text=label.AddComponent<TextMesh>();text.text=names[i].Replace(" ","\n");text.fontSize=48;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.black;
                        }
                    }
                    // Correct the initial mannequin's placeholder limb to match its forearm case.
                    if(room.scenarioId==3)
                    {
                        var p=room.patients[0];
                        foreach(Transform t in p.transform)if(t.name=="Arm" && t.localPosition.x>0)t.localScale=new Vector3(.15f,.18f,.15f);
                        if(p.transform.Find("Restored leg")==null)Shape("Restored leg",PrimitiveType.Capsule,p.transform,new Vector3(.14f,.43f,0),new Vector3(.2f,.43f,.2f));
                    }
                    // Reduce the overly bright first-pass illumination, without touching lobby lighting.
                    foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)light.intensity=.75f;
                    foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(text.name=="Equipment station label")text.characterSize=.035f;
                }
                var player=Object.FindFirstObjectByType<PracticePlayer>();
                if(!player.GetComponent<VRInteractionManager>())
                {
                    var vr=player.gameObject.AddComponent<VRInteractionManager>();vr.head=player.view;
                    var left=new GameObject("Left tracked controller");left.transform.SetParent(player.transform,false);vr.leftHand=left.transform;
                    var right=new GameObject("Right tracked controller");right.transform.SetParent(player.transform,false);vr.rightHand=right.transform;
                    foreach(var hand in new[]{left,right}){var o=Shape("Controller proxy",PrimitiveType.Capsule,hand.transform,Vector3.zero,new Vector3(.07f,.08f,.07f));Object.DestroyImmediate(o.GetComponent<Collider>());}
                }
                EditorSceneManager.SaveScene(scene);
            }
            ConfigureXR();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Root+"/Scenes/Cafe_Anaphylaxis.unity");
            Debug.Log("PRACTICE_SYSTEMS_INSTALLED=6_SCENES");
        }
        public static void ConfigureXR()
        {
            var guids=AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            XRGeneralSettingsPerBuildTarget settings;
            if(guids.Length>0)settings=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guids[0]));
            else{settings=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(settings,Root+"/Configs/XRSettings.asset");}
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,settings,true);
            if(!settings.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if(!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            var general=settings.SettingsForBuildTarget(BuildTargetGroup.Standalone);general.InitManagerOnStart=true;
            if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Standalone))throw new System.Exception("OpenXR loader assignment failed");
            var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            var touch=xr.GetFeature<OculusTouchControllerProfile>();if(touch)touch.enabled=true;
            var index=xr.GetFeature<ValveIndexControllerProfile>();if(index)index.enabled=true;
            EditorUtility.SetDirty(settings);EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);EditorUtility.SetDirty(xr);
        }
    }
}
