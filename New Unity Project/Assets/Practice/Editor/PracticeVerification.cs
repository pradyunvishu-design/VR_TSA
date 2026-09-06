using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace First180.Practice.Editor
{
    public static class PracticeVerification
    {
        static int assertions;
        static void Check(bool condition,string message){if(!condition)throw new Exception("FAILED: "+message);assertions++;}
        [MenuItem("FIRST180 Practice/Verify logic and scene contracts")]
        public static void Run()
        {
            assertions=0;
            Check(!PracticeRules.CanCertify(new[]{100,100,100,100,99}),"99 must not unlock certification");
            Check(!PracticeRules.CanCertify(new[]{100}),"Incomplete mastery array rejected");
            Check(PracticeRules.CanCertify(new[]{100,100,100,100,100}),"Five perfect cases unlock");
            for(int i=0;i<5;i++)
            {
                var e=new Encounter(i,false);Check(!e.Perform("monitor",0),"No care before assessment");
                Check(e.ChoosePatient(0,1),"Correct triage");Check(e.Diagnose(i,2),"Correct diagnosis");
                foreach(string action in e.definition.actions)Check(e.Perform(action,3),"Ordered action "+action);
                var r=e.Finish(25);Check(r.score==100 && r.completed,"Perfect run earns100");Check(!e.Perform("safety",26),"Finished encounter immutable");
                var bad=new Encounter(i,false);bad.ChoosePatient(1,1);bad.ChoosePatient(0,2);bad.Diagnose(i,3);
                foreach(string action in bad.definition.actions)bad.Perform(action,4);
                Check(bad.Finish(25).score<100,"Retry after mistake cannot erase error");
                var timeout=new Encounter(i,true);timeout.ChoosePatient(0,1);timeout.Diagnose(i,2);Check(!timeout.Finish(180,true).completed,"Timeout fails");
            }
            for(int seed=0;seed<50;seed++)Check(PracticeRules.ShuffledOrder(new System.Random(seed)).OrderBy(x=>x).SequenceEqual(new[]{0,1,2,3,4}),"Shuffle is permutation");
            string old=EditorSceneManager.GetActiveScene().path;
            foreach(var d in ScenarioCatalog.All)
            {
                var scene=EditorSceneManager.OpenScene("Assets/Practice/Scenes/"+d.scene+".unity");
                var room=UnityEngine.Object.FindFirstObjectByType<PracticeRoom>();Check(room && room.scenarioId==d.id,"Scene ID matches");
                Check(room.config && room.patients.Length==3,"Config and three patients");
                Check(room.GetComponent<TreatmentManager>() && room.GetComponent<TrainingManager>() && room.GetComponent<SpatialPanel>(),"Reusable systems installed");
                Check(UnityEngine.Object.FindFirstObjectByType<VRInteractionManager>()!=null,"XR rig present");
                foreach(var action in d.actions)Check(UnityEngine.Object.FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None).Any(t=>t.actionId==action),"Tool for "+action);
                foreach(var root in scene.GetRootGameObjects())foreach(var transform in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"No missing scripts");
            }
            if(!string.IsNullOrEmpty(old))EditorSceneManager.OpenScene(old);
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Design/PracticeDevelopment/logic-verification.txt"));
            File.WriteAllText(output,"UTC: "+DateTime.UtcNow.ToString("o")+"\nPASS: "+assertions+" logic and serialized-scene assertions\nHardware VR not tested.\n");
            Debug.Log("PRACTICE_ASSERTIONS_PASSED="+assertions);
        }
    }
}
