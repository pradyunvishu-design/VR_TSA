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
        public bool documentCorrection;
        readonly List<string> checks=new List<string>();bool failed;
        void Check(bool value,string message){if(!value){failed=true;Debug.LogError("SMOKE_FAILED="+message);}checks.Add((value?"PASS ":"FAIL ")+message);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var session=PracticeSession.Get();session.testMode=true;session.best=new int[5];
            while(session.loading) yield return null;
            if(FindFirstObjectByType<PracticeRoom>().scenarioId<0)
            {
                Physics.SyncTransforms();
                var signs=FindFirstObjectByType<LobbyConnections>().GetComponent<WorldSignAtlas>().signs;
                Check(signs.Length>0 && signs.All(s=>s.font &&
                    s.GetComponent<MeshRenderer>().sharedMaterial.shader.name=="FIRST180/World lettering"),
                    "Gallery signs respect depth behind menus and use a valid font atlas");
                foreach(var piece in new[] {"Gallery Floor","Inset Floor","Tree Well","Soil Bed"})
                {
                    var go=GameObject.Find(piece);
                    var surface=go ? go.GetComponent<MeshCollider>() : null;
                    Check(surface && surface.bounds.size.y<.5f, "Gallery walking surface matches shallow visible geometry: "+piece);
                }
                CheckLobbyEnclosure();
                foreach(var portal in FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).Where(p=>!p.certification && !p.returnToHub))
                {
                    var exhibitFace=portal.transform.Find("Glass Face");
                    var eye=portal.transform.TransformPoint(new Vector3(0,1.65f,-3));
                    var aim=exhibitFace.position-eye;
                    Check(Physics.Raycast(eye,aim.normalized,out var hit,8,~0,QueryTriggerInteraction.Collide)
                        && hit.collider.GetComponentInParent<PracticePortal>()==portal,"World ray selects visible gallery exhibit "+portal.label);
                }
            }
            Check(!session.StartCertification(),"Certification locked without mastery");
            session.StartTraining(0);yield return LoadFinished();
            var wrongRoom=FindFirstObjectByType<PracticeRoom>();
            PracticeSpatialQa.CheckCase(wrongRoom,Check);
            wrongRoom.BeginEmergency();PracticeSpatialQa.Approach(wrongRoom.patients[0].transform);
            wrongRoom.Inspect(0);wrongRoom.ChoosePatient(0);wrongRoom.Diagnose(0);
            wrongRoom.Inspect(2);
            Check(wrongRoom.inspected==0 && wrongRoom.observation==wrongRoom.Definition.symptoms,"Bystander inspection cannot replace selected patient findings during care");
            var wrongCare=wrongRoom.GetComponent<TreatmentManager>();
            var wrongTool=FindObjectsByType<TreatmentTool>(FindObjectsSortMode.None).First(t=>t.actionId=="epinephrine");
            var wrongTarget=FindObjectsByType<TreatmentTarget>(FindObjectsSortMode.None).First(t=>t.patientId==0 && t.Accepts("monitor"));
            wrongCare.Grab(wrongTool,Camera.main.transform);wrongCare.Use(wrongTarget,true,3);wrongCare.Release();
            yield return null;
            var display=wrongRoom.transform.Find("ROOK instructor display");
            var portrait=display ? display.GetComponentInChildren<UnityEngine.UI.RawImage>() : null;
            Check(display && display.GetComponent<Canvas>().enabled
                && portrait && portrait.texture && portrait.texture == Resources.Load<Texture2D>("RookAnimation")
                && portrait.uvRect.width < .125f && portrait.uvRect.height < .25f
                && display.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Incorrect placement")),
                "Animated ROOK card shows the placement correction");
            if(documentCorrection)
            {
                yield return new WaitForSeconds(.4f);
                Debug.Log("CAPTURE_TRAINING_CORRECTION_NOW");
                yield return new WaitForSeconds(7);
            }
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
                var examRoom=FindFirstObjectByType<PracticeRoom>();
                Check(Enumerable.Range(0,3).All(slot => examRoom.patientLabels[examRoom.PatientInSlot(slot)] == ((char)('A'+slot)).ToString()),
                    "Triage button slots follow visible patient letters in case " + id);
                examRoom.BeginEmergency();
                Check(!examRoom.GetComponent<RookInstructor>() && !examRoom.GetComponent<PracticeAudio>().robot.gameObject.activeInHierarchy,
                    "Instructor hidden throughout certification case "+id);
                Check(examRoom.feedback == "Incident active.", "Certification onset has neutral feedback");
                yield return CompleteCase(id);
                session.Next();
            }
            yield return LoadFinished();yield return new WaitForSeconds(2);
            Check(seen.All(x=>x==1),"Certification visits all5 exactly once");
            Check(session.dashboard && session.results.Count==5,"Final analytics receives five cases");
            Check(AnalyticsManager.Passed(session.results),"Perfect certification earns practice CERTIFIED");
            PracticeSpatialQa.CheckDashboard(FindFirstObjectByType<PracticeRoom>(),Check);
            if(documentCorrection) yield return DocumentSpatialBoard(FindFirstObjectByType<PracticeRoom>());
            CheckReport(session);
            Debug.Log("CAPTURE_ANALYTICS_NOW / automated integration results; not a human attempt");
            yield return new WaitForSeconds(20);
            var portals=FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).Where(p=>!p.returnToHub && !p.certification).ToArray();
            Check(portals.Length==5,"All five original lobby windows connected at runtime");
            if(portals.Length>0)
            {
                portals[0].Use();
                Check(session.dashboard && !session.loading && session.results.Count==5,"World entrance cannot dismiss the certification dashboard");
            }
            session.BackToHub();yield return LoadFinished();
            // Enter via each original exhibit and use the room's actual return portal.
            for(int id=0;id<5;id++)
            {
                float deadline=Time.realtimeSinceStartup+12;
                PracticePortal entry=null;
                while(!entry && Time.realtimeSinceStartup<deadline)
                {
                    entry=FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).FirstOrDefault(p=>!p.certification && !p.returnToHub && p.scenarioId==id);
                    yield return null;
                }
                Check(entry!=null,"Original exhibit loaded "+id);
                if(!entry)break;
                var lobbyRig = FindFirstObjectByType<VRInteractionManager>();
                var exhibit = entry.transform.Find("Glass Face").GetComponent<Collider>();
                lobbyRig.ApplyHand(1, lobbyRig.rightHand, exhibit, PracticePortal.SelectionRange + 1,
                    true, true, false, false, false);
                Check(!session.loading, "Headset selection rejects an out-of-reach portal " + id);
                yield return WalkToExhibit(entry);
                yield return LoadFinished();
                var current=FindFirstObjectByType<PracticeRoom>();
                Check(current.scenarioId==id,"Original exhibit opens matching case "+id);
                var exit=FindObjectsByType<PracticePortal>(FindObjectsSortMode.None).FirstOrDefault(p=>p.returnToHub);
                Check(exit!=null,"Return entrance present "+id);
                if(!exit)break;
                current.TogglePause();exit.Use();
                Check(!session.loading && current.Paused,"Paused room ignores world entrance "+id);
                current.TogglePause();
                var roomRig = FindFirstObjectByType<VRInteractionManager>();
                roomRig.ApplyHand(0, roomRig.leftHand, exit.GetComponent<Collider>(), 4,
                    true, true, false, false, false);
                yield return LoadFinished();
                Check(SceneManager.GetActiveScene().name==ScenarioCatalog.Hub,"Return entrance opens hub "+id);
            }
            Check(session.StartCertification(),"Mastery permits a second certification attempt");
            for (int round=0;round<5;round++)
            {
                yield return LoadFinished();
                var exam=FindFirstObjectByType<PracticeRoom>();
                exam.BeginEmergency();
                if(round==0) { PracticeSpatialQa.Approach(exam.patients[1].transform);exam.Inspect(1);exam.ChoosePatient(1);exam.Diagnose((exam.scenarioId+1)%5); }
                exam.elapsed=exam.Definition.timeLimit;
                yield return null;yield return null;
                Check(exam.result!=null && exam.result.timedOut && !exam.result.completed,"Certification timeout records an incomplete case "+round);
                if(exam.result==null) yield break;
                session.Next();
            }
            yield return LoadFinished();
            Check(session.dashboard && session.results.Count==5 && !AnalyticsManager.Passed(session.results)
                && session.results[0].mistakes==2 && session.results[0].priority==0,"Failed certification reports missed priority, diagnosis and timeouts");
            if(documentCorrection) { Debug.Log("CAPTURE_FAILED_CERTIFICATION_NOW");yield return new WaitForSeconds(10); }
            // Never leave fabricated mastery in memory after testing.
            for(int i=0;i<5;i++)session.best[i]=PlayerPrefs.GetInt(PracticeSession.PrefPrefix+i,0);
            session.BackToHub();yield return LoadFinished();yield return new WaitForSeconds(3);
            Check(SceneManager.GetActiveScene().name==ScenarioCatalog.Hub,"Returning to lobby remains stable without movement");
            session.testMode=false;
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Design/PracticeDevelopment/runtime-verification.txt")),"UTC: "+DateTime.UtcNow.ToString("o")+"\n"+string.Join("\n",checks)+"\nHardware VR and manual controller gestures not tested.\n");
            Debug.Log("PRACTICE_RUNTIME_SMOKE="+(failed?"FAIL":"PASS"));Destroy(gameObject);UnityEditor.EditorApplication.isPlaying=false;
        }
        void CheckLobbyEnclosure()
        {
            var centre = new Vector3(0, 0, 4);
            bool walls = true, floor = true, roof = true;
            for (int i = 0; i < 64; i++)
            {
                var radial = Quaternion.Euler(0, i * 360f / 64, 0) * Vector3.forward;
                var perimeter = centre + radial * 13.5f;
                walls &= Physics.Raycast(perimeter + Vector3.up * 6, radial, out var wallHit, 2)
                    && wallHit.collider.name.StartsWith("Perimeter wall");
                floor &= Physics.Raycast(perimeter + Vector3.up * .25f, Vector3.down, out var floorHit, .3f)
                    && floorHit.collider.name == "Gallery Floor";
                roof &= Physics.Raycast(perimeter + Vector3.up * 7.3f, Vector3.up, out var roofHit, 1)
                    && roofHit.collider.name == "Sealed roof";
            }
            Check(walls && floor && roof, "Gallery perimeter has continuous walls, visible floor and a sealed roof");
            Check(!GameObject.Find("Floating Stair 01"), "The gallery uses a single level without stairs");
        }

        IEnumerator WalkToExhibit(PracticePortal entry)
        {
            var player = FindFirstObjectByType<PracticePlayer>();
            var controller = player.GetComponent<CharacterController>();
            var session = PracticeSession.Get();
            var centre = new Vector3(0, 0, 4);
            int id = entry.scenarioId;
            string label = entry.label;
            var approach = entry.transform.TransformPoint(new Vector3(0, 0, -.7f));
            float azimuth = Vector3.SignedAngle(Vector3.forward, approach - centre, Vector3.up);
            var waypoints = new List<Vector3>();
            float startAngle = azimuth <= 0 ? -180 : 180;
            for (int i = 0; i <= 10; i++)
            {
                float angle = Mathf.Lerp(startAngle, azimuth, i / 10f);
                waypoints.Add(centre + Quaternion.Euler(0, angle, 0) * Vector3.forward * 6.5f);
            }
            waypoints.Add(approach);
            foreach (var waypoint in waypoints)
            {
                for (int step = 0; step < 60 && !session.loading; step++)
                {
                    var remaining = Vector3.ProjectOnPlane(waypoint - player.transform.position, Vector3.up);
                    if (remaining.magnitude < .08f) break;
                    controller.Move(Vector3.ClampMagnitude(remaining, .23f) + Vector3.down * .1f);
                    yield return new WaitForFixedUpdate();
                }
                if (session.loading) break;
            }
            Check(session.loading && session.pendingId == id, "Walking from the hub spawn activates the intended entrance: " + label);
        }

        IEnumerator DocumentSpatialBoard(PracticeRoom room)
        {
            var panel=room.GetComponent<SpatialPanel>();
            if(VRInteractionManager.Active) yield break;
            var head=Camera.main;
            var original=head.transform.rotation;
            var fov=head.fieldOfView;
            try
            {
                room.enabled=false;
                panel.enabled=false;
                panel.RefreshPanel(true);
                head.transform.LookAt(panel.transform.Find("Spatial decision panel"));
                head.fieldOfView=52;
                yield return null;
                Debug.Log("CAPTURE_SPATIAL_BOARD_PREVIEW");
                yield return new WaitForSeconds(8);
            }
            finally
            {
                head.transform.rotation=original;head.fieldOfView=fov;
                room.enabled=true;panel.enabled=true;panel.RefreshPanel(false);
            }
        }
        [Serializable] class SavedReport { public string mode; public CaseResult[] cases; public int[] trainingBest; }
        void CheckReport(PracticeSession session)
        {
            string directory=Path.Combine(Application.temporaryCachePath,"FIRST180-QA-"+Guid.NewGuid().ToString("N"));
            try
            {
                string file=session.WriteReport(directory);
                var report=JsonUtility.FromJson<SavedReport>(File.ReadAllText(file));
                Check(report.mode=="Certification" && report.cases.Length==5 && report.trainingBest.SequenceEqual(session.best)
                    && report.cases.Select(c=>c.scenario).SequenceEqual(session.results.Select(c=>c.scenario))
                    && report.cases.All(c=>c.events.Count>0 && c.patientStabilized),"Analytics export round-trips all five cases and action histories");
            }
            finally
            {
                string reportFile=Path.Combine(directory,"latest-certification.json");
                if(File.Exists(reportFile)) File.Delete(reportFile);
                if(Directory.Exists(directory)) Directory.Delete(directory);
            }
        }
        IEnumerator LoadFinished(){yield return null;while(PracticeSession.Get().loading)yield return null;yield return new WaitForSeconds(.3f);}
        IEnumerator CompleteCase(int id)
        {
            var room=FindFirstObjectByType<PracticeRoom>();Check(room!=null && room.scenarioId==id,"Scene loaded "+id);if(!room)yield break;
            room.BeginEmergency();PracticeSpatialQa.Approach(room.patients[0].transform);room.Inspect(0);room.ChoosePatient(0);room.Diagnose(id);
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
