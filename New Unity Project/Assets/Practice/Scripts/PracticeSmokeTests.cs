#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace First180.Practice
{
    // Editor-only integration harness. Never writes the learner's scores.
    public class PracticeSmokeTests : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();bool failed;
        void Check(bool value,string message){if(!value){failed=true;Debug.LogError("SMOKE_FAILED="+message);}checks.Add((value?"PASS ":"FAIL ")+message);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var session=PracticeSession.Get();session.testMode=true;session.best=new int[5];
            Check(!session.StartCertification(),"Certification locked without mastery");
            session.StartTraining(0);yield return LoadFinished();
            var wrongRoom=FindFirstObjectByType<PracticeRoom>();wrongRoom.BeginEmergency();wrongRoom.ChoosePatient(0);wrongRoom.Diagnose(0);
            var wrongCare=wrongRoom.GetComponent<TreatmentManager>();
            var wrongTool=FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None).First(t=>t.actionId=="epinephrine");
            var wrongTarget=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None).First(t=>t.patientId==0 && t.Accepts("monitor"));
            wrongCare.Grab(wrongTool,Camera.main.transform);wrongCare.Use(wrongTarget,true,3);wrongCare.Release();
            Check(wrongRoom.encounter.Mistakes==1 && wrongRoom.encounter.Step==0,"Wrong anatomical placement penalized without advancing");
            Check(wrongRoom.patients[0].GetComponent<PatientManager>().stability<100,"Incorrect treatment worsens state");
            wrongRoom.Complete(true);Check(wrongRoom.result.score<100 && wrongRoom.result.criticalMistakes==1,"Critical placement mistake prevents mastery");
            for(int i=0;i<5;i++)
            {
                session.StartTraining(i);yield return LoadFinished();
                yield return CompleteCase(i);
                Check(session.best[i]==100,"Perfect equipment-based training saves transient mastery: "+i);
            }
            Check(session.CanCertify,"Five100% training results unlock certification");
            Check(session.StartCertification(),"Certification starts after mastery");
            int[] seen=new int[5];
            for(int round=0;round<5;round++)
            {
                yield return LoadFinished();int id=session.pendingId;seen[id]++;
                yield return CompleteCase(id);
                session.Next();
            }
            yield return LoadFinished();yield return new WaitForSeconds(2);
            Check(seen.All(x=>x==1),"Certification visits all5 exactly once");
            Check(session.dashboard && session.results.Count==5,"Final analytics receives five cases");
            Check(AnalyticsManager.Passed(session.results),"Perfect certification earns practice CERTIFIED");
            Debug.Log("CAPTURE_ANALYTICS_NOW / automated integration results; not a human attempt");
            yield return new WaitForSeconds(20);
            var portals=FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).Where(p=>!p.returnToHub && !p.certification).ToArray();
            Check(portals.Length==5,"All five original lobby windows connected at runtime");
            if(portals.Length>0){int targetId=portals[0].scenarioId;portals[0].Use();yield return LoadFinished();Check(FindFirstObjectByType<PracticeRoom>().scenarioId==targetId,"Lobby portal routes to case");}
            // Never leave fabricated mastery in memory after testing.
            for(int i=0;i<5;i++)session.best[i]=PlayerPrefs.GetInt(PracticeSession.PrefPrefix+i,0);
            session.BackToHub();yield return LoadFinished();yield return new WaitForSeconds(3);
            Check(SceneManager.GetActiveScene().name==ScenarioCatalog.Hub,"Returning to lobby remains stable without movement");
            session.testMode=false;
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Design/PracticeDevelopment/runtime-verification.txt")),"UTC: "+DateTime.UtcNow.ToString("o")+"\n"+string.Join("\n",checks)+"\nHardware VR and manual controller gestures not tested.\n");
            Debug.Log("PRACTICE_RUNTIME_SMOKE="+(failed?"FAIL":"PASS"));Destroy(gameObject);UnityEditor.EditorApplication.isPlaying=false;
        }
        IEnumerator LoadFinished(){yield return null;while(PracticeSession.Get().loading)yield return null;yield return new WaitForSeconds(.3f);}
        IEnumerator CompleteCase(int id)
        {
            var room=FindFirstObjectByType<PracticeRoom>();Check(room!=null && room.scenarioId==id,"Scene loaded "+id);if(!room)yield break;
            room.BeginEmergency();room.Inspect(0);room.ChoosePatient(0);room.Diagnose(id);
            var care=room.GetComponent<TreatmentManager>();var hand=Camera.main.transform;
            foreach(string action in room.Definition.actions)
            {
                var tool=FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None).FirstOrDefault(t=>t.actionId==action);
                var target=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None).FirstOrDefault(t=>t.patientId==0 && t.Accepts(action));
                Check(tool!=null && (target!=null || action=="call"),"Equipment and contact target for "+action);
                if(!tool)continue;
                care.Grab(tool,hand);care.Use(target,true,tool.holdSeconds+.1f);care.Use(target,false,0);care.Release();
                yield return new WaitForSeconds(.4f);
            }
            Check(room.result!=null && room.result.score==100,"All real treatment handlers finish "+id);
            Check(room.patients[0].GetComponent<PatientManager>().stabilized,"Patient stabilized "+id);
        }
    }
}
#endif
